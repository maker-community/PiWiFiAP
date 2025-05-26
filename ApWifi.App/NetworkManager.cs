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
        /// 启动WiFi热点 (仅使用 nmcli)
        /// </summary>
        public async Task<bool> StartHotspotAsync(string ssid, string password)
        {
            // 直接调用 nmcli 方式
            return await StartHotspotWithNmcliAsync(ssid, password);
        }

        /// <summary>
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
        }
        
        /// <summary>
        /// 断开设备连接
        /// </summary>
        public async Task<bool> DisconnectDeviceAsync()
        {
            Console.WriteLine($"正在断开设备连接: {_interface}");
            
            var disconnectCmd = $"device disconnect {_interface}";
            var result = await RunNmcliCommandAsync(disconnectCmd);
            
            return result.Success;
        }        
        
        /// <summary>
        /// 关闭热点连接 (仅使用 nmcli)
        /// </summary>
        public async Task<bool> StopHotspotAsync()
        {
            // 直接调用 nmcli 方式，使用配置的热点名
            return await StopHotspotWithNmcliAsync(_config.ApConfig.Ssid);
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
        
        /// <summary>
        /// 使用nmcli启动WiFi热点
        /// </summary>
        public async Task<bool> StartHotspotWithNmcliAsync(string ssid, string password)
        {
            Console.WriteLine($"正在使用nmcli启动WiFi热点: {ssid}");
            Console.WriteLine($"使用配置的IP地址: {_config.ApConfig.Ip}");
            Console.WriteLine($"使用配置的DHCP范围: {_config.ApConfig.DhcpStart} - {_config.ApConfig.DhcpEnd}");

            try
            {
                // 停止任何可能正在运行的热点
                await StopHotspotAsync();

                // 确保设备被NetworkManager管理
                await SetDeviceManagedAsync(true);

                // 删除可能存在的相同名称的连接
                var deleteCmd = $"connection delete {ssid}";
                await RunNmcliCommandAsync(deleteCmd);

                // 创建新的热点连接
                var createHotspotCmd = $"device wifi hotspot ifname {_interface} con-name {ssid} ssid \"{ssid}\" password \"{password}\"";
                var result = await RunNmcliCommandAsync(createHotspotCmd);

                if (!result.Success)
                {
                    Console.WriteLine($"创建WiFi热点失败: {result.Error}");
                    return false;
                }

                // 设置IP地址和掩码（使用配置文件中的IP）
                var ipCmd = $"connection modify {ssid} ipv4.addresses {_config.ApConfig.Ip}/24";
                var ipResult = await RunNmcliCommandAsync(ipCmd);
                if (!ipResult.Success)
                {
                    Console.WriteLine($"设置IP地址失败: {ipResult.Error}");
                }

                // 设置为手动IP模式
                var methodCmd = $"connection modify {ssid} ipv4.method manual";
                var methodResult = await RunNmcliCommandAsync(methodCmd);
                if (!methodResult.Success)
                {
                    Console.WriteLine($"设置IP模式失败: {methodResult.Error}");
                }

                // 启用DHCP服务器（使用配置文件中的DHCP范围）
                var dhcpCmd = $"connection modify {ssid} ipv4.dhcp-range \"{_config.ApConfig.DhcpStart},{_config.ApConfig.DhcpEnd}\"";
                var dhcpResult = await RunNmcliCommandAsync(dhcpCmd);
                if (!dhcpResult.Success)
                {
                    Console.WriteLine($"设置DHCP范围失败: {dhcpResult.Error}");
                }

                // 重新应用配置
                var upCmd = $"connection up {ssid}";
                var upResult = await RunNmcliCommandAsync(upCmd);

                if (!upResult.Success)
                {
                    Console.WriteLine($"启动WiFi热点失败: {upResult.Error}");
                    return false;
                }

                Console.WriteLine($"WiFi热点启动成功: {ssid}");
                Console.WriteLine($"热点IP: {_config.ApConfig.Ip}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动WiFi热点时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用nmcli关闭WiFi热点
        /// </summary>
        public async Task<bool> StopHotspotWithNmcliAsync(string ssid)
        {
            Console.WriteLine($"正在关闭nmcli WiFi热点: {ssid}");

            try
            {
                // 关闭连接
                var downCmd = $"connection down {ssid}";
                await RunNmcliCommandAsync(downCmd);

                // 删除连接
                var deleteCmd = $"connection delete {ssid}";
                var result = await RunNmcliCommandAsync(deleteCmd);

                if (!result.Success)
                {
                    Console.WriteLine($"关闭WiFi热点失败: {result.Error}");
                    return false;
                }

                Console.WriteLine("WiFi热点已关闭");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"关闭WiFi热点时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查WiFi热点是否正在运行
        /// </summary>
        public async Task<bool> IsHotspotRunningAsync(string ssid)
        {
            var checkCmd = $"connection show {ssid}";
            var result = await RunNmcliCommandAsync(checkCmd);

            if (!result.Success)
            {
                return false;
            }

            // 检查连接是否处于活动状态
            var activeCmd = $"connection show --active";
            var activeResult = await RunNmcliCommandAsync(activeCmd);

            if (!activeResult.Success)
            {
                return false;
            }

            return activeResult.Output.Contains(ssid);
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
