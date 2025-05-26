using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using Fluid;

namespace ApWifi.App
{
    public class NetworkManager
    {
        private readonly DeviceConfig _config;
        private readonly string _interface;

        public NetworkManager(DeviceConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _interface = _config.ApConfig.Interface;
        }        /// <summary>
        /// 异步执行nmcli命令
        /// </summary>
        private async Task<CommandResult> RunNmcliCommandAsync(string arguments, int timeoutSeconds = 30)
        {
            if (!OperatingSystem.IsLinux())
            {
                Console.WriteLine("非Linux系统，跳过nmcli命令执行");
                return new CommandResult { Success = false, Output = "非Linux系统" };
            }

            try
            {
                // 构建完整的命令，包含sudo
                var fullCommand = $"sudo nmcli {arguments}";
                
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{fullCommand.Replace("\"", "\\\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };
                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                var completed = await Task.WhenAny(
                    Task.WhenAll(outputTask, errorTask),
                    Task.Delay(TimeSpan.FromSeconds(timeoutSeconds))
                );

                if (completed.Id == Task.WhenAll(outputTask, errorTask).Id)
                {
                    await process.WaitForExitAsync();
                    var output = await outputTask;
                    var error = await errorTask;

                    var result = new CommandResult
                    {
                        Success = process.ExitCode == 0,
                        Output = output,
                        Error = error,
                        ExitCode = process.ExitCode
                    };

                    if (!result.Success && !string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine($"nmcli命令执行失败: {error}");
                    }

                    return result;
                }
                else
                {
                    // 超时处理
                    try
                    {
                        process.Kill();
                    }
                    catch { }

                    return new CommandResult
                    {
                        Success = false,
                        Output = "",
                        Error = $"命令执行超时({timeoutSeconds}秒)",
                        ExitCode = -1
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行nmcli命令时出错: {ex.Message}");
                return new CommandResult
                {
                    Success = false,
                    Output = "",
                    Error = ex.Message,
                    ExitCode = -1
                };
            }
        }
        
        /// <summary>
        /// 启动WiFi热点 (使用 hostapd + dnsmasq)
        /// </summary>
        public async Task<bool> StartHotspotAsync(string ssid, string password)
        {
            Console.WriteLine($"正在启动WiFi热点: {ssid}");
            
            try
            {
                // 先断开当前连接
                await DisconnectDeviceAsync();
                
                // 设置设备为非管理状态
                await SetDeviceManagedAsync(false);
                
                // 生成hostapd配置文件
                var hostapd_success = await GenerateHostapdConfigAsync(ssid, password);
                if (!hostapd_success)
                {
                    Console.WriteLine("生成hostapd配置失败");
                    return false;
                }
                
                // 生成dnsmasq配置文件
                var dnsmasq_success = await GenerateDnsmasqConfigAsync();
                if (!dnsmasq_success)
                {
                    Console.WriteLine("生成dnsmasq配置失败");
                    return false;
                }
                
                // 启动hostapd
                var hostapd_result = await AsyncUtils.RunCommandAsync("sudo hostapd -B /tmp/hostapd.conf");
                if (!hostapd_result.Success)
                {
                    Console.WriteLine($"启动hostapd失败: {hostapd_result.Error}");
                    return false;
                }
                
                // 配置IP地址
                var ip_result = await AsyncUtils.RunCommandAsync($"sudo ip addr add {_config.ApConfig.Ip}/24 dev {_interface}");
                if (!ip_result.Success)
                {
                    Console.WriteLine($"配置IP地址失败: {ip_result.Error}");
                }
                
                // 启动dnsmasq
                var dnsmasq_result = Utils.StartDnsmasq("/tmp/dnsmasq.conf");
                if (!dnsmasq_result)
                {
                    Console.WriteLine("启动dnsmasq失败");
                    return false;
                }
                
                Console.WriteLine($"WiFi热点启动成功: {ssid}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动WiFi热点时出错: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 生成hostapd配置文件
        /// </summary>
        private async Task<bool> GenerateHostapdConfigAsync(string ssid, string password)
        {
            try
            {
                var template = await File.ReadAllTextAsync("Templates/hostapd_conf.liquid");
                var parser = new FluidParser();
                
                if (!parser.TryParse(template, out var fluidTemplate, out var error))
                {
                    Console.WriteLine($"hostapd模板解析错误: {error}");
                    return false;
                }
                
                var context = new TemplateContext();
                context.SetValue("ap", _config.ApConfig);
                
                var config = await fluidTemplate.RenderAsync(context);
                await File.WriteAllTextAsync("/tmp/hostapd.conf", config);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成hostapd配置时出错: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 生成dnsmasq配置文件
        /// </summary>
        private async Task<bool> GenerateDnsmasqConfigAsync()
        {
            try
            {
                var template = await File.ReadAllTextAsync("Templates/dnsmasq_conf.liquid");
                var parser = new FluidParser();
                
                if (!parser.TryParse(template, out var fluidTemplate, out var error))
                {
                    Console.WriteLine($"dnsmasq模板解析错误: {error}");
                    return false;
                }
                
                var context = new TemplateContext();
                context.SetValue("ap", _config.ApConfig);
                context.SetValue("dhcpStart", _config.ApConfig.DhcpStart);
                context.SetValue("dhcpEnd", _config.ApConfig.DhcpEnd);
                
                var config = await fluidTemplate.RenderAsync(context);
                await File.WriteAllTextAsync("/tmp/dnsmasq.conf", config);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成dnsmasq配置时出错: {ex.Message}");
                return false;
            }
        }        /// <summary>
        /// 连接到WiFi网络
        /// </summary>
        public async Task<bool> ConnectToWifiAsync(string ssid, string password)
        {
            Console.WriteLine($"正在连接到WiFi: {ssid}");
            
            // 使用正确的nmcli参数，添加引号处理特殊字符
            var connectCmd = $"device wifi connect \"{ssid}\" password \"{password}\" ifname {_interface}";
            var result = await RunNmcliCommandAsync(connectCmd);
            
            if (result.Success)
            {
                Console.WriteLine($"WiFi连接成功: {ssid}");
                Console.WriteLine(result.Output);
            }
            else
            {
                Console.WriteLine($"WiFi连接失败: {result.Error}");
            }
            
            return result.Success;
        }/// <summary>
        /// 断开设备连接
        /// </summary>
        public async Task<bool> DisconnectDeviceAsync()
        {
            Console.WriteLine($"正在断开设备连接: {_interface}");
            
            var disconnectCmd = $"device disconnect {_interface}";
            var result = await RunNmcliCommandAsync(disconnectCmd);
            
            return result.Success;
        }        /// <summary>
        /// 关闭热点连接
        /// </summary>
        public async Task<bool> StopHotspotAsync()
        {
            Console.WriteLine("正在关闭热点连接");
            
            try
            {
                // 停止dnsmasq
                var dnsmasq_result = await AsyncUtils.RunCommandAsync("sudo killall -9 dnsmasq 2>/dev/null || true");
                
                // 停止hostapd
                var hostapd_result = await AsyncUtils.RunCommandAsync("sudo killall -9 hostapd 2>/dev/null || true");
                
                // 清除IP地址
                var ip_result = await AsyncUtils.RunCommandAsync($"sudo ip addr flush dev {_interface}");
                
                // 恢复设备管理状态
                await SetDeviceManagedAsync(true);
                
                Console.WriteLine("热点已关闭");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"关闭热点时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置设备管理状态
        /// </summary>
        public async Task<bool> SetDeviceManagedAsync(bool managed)
        {
            var managedState = managed ? "yes" : "no";
            Console.WriteLine($"正在设置设备管理状态: {_interface} -> {managedState}");
            
            var setCmd = $"device set {_interface} managed {managedState}";
            var result = await RunNmcliCommandAsync(setCmd);
            
            return result.Success;
        }

        /// <summary>
        /// 连接设备
        /// </summary>
        public async Task<bool> ConnectDeviceAsync()
        {
            Console.WriteLine($"正在连接设备: {_interface}");
            
            var connectCmd = $"device connect {_interface}";
            var result = await RunNmcliCommandAsync(connectCmd);
            
            return result.Success;
        }

        /// <summary>
        /// 获取WiFi扫描结果
        /// </summary>
        public async Task<string> ScanWifiAsync()
        {
            Console.WriteLine("正在扫描WiFi网络");
            
            var scanCmd = $"device wifi list ifname {_interface}";
            var result = await RunNmcliCommandAsync(scanCmd);
            
            return result.Success ? result.Output : "";
        }

        /// <summary>
        /// 检查连接状态
        /// </summary>
        public async Task<string> GetConnectionStatusAsync()
        {
            var statusCmd = $"device status";
            var result = await RunNmcliCommandAsync(statusCmd);
            
            return result.Success ? result.Output : "";
        }
    }

    public class CommandResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public int ExitCode { get; set; }
    }
}
