using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using Fluid;
using Serilog;

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
        }        
        
        /// <summary>
        /// 异步执行nmcli命令
        /// </summary>        
        private async Task<CommandResult> RunNmcliCommandAsync(string arguments, int timeoutSeconds = 30)
        {
            if (!OperatingSystem.IsLinux())
            {
                Log.Warning("非Linux系统，跳过nmcli命令执行");
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

                // 修复：将任务存储在变量中，避免重复创建Task对象
                var allTask = Task.WhenAll(outputTask, errorTask);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
                
                var completed = await Task.WhenAny(allTask, timeoutTask);

                if (completed == allTask)
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
                    };                    if (!result.Success && !string.IsNullOrEmpty(error))
                    {
                        Log.Error("nmcli命令执行失败: {Error}", error);
                    }

                    return result;
                }
                else
                {
                    // 超时处理
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
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
            }            catch (Exception ex)
            {
                Log.Error(ex, "执行nmcli命令时出错");
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
            Log.Information("正在连接到WiFi: {Ssid}", ssid);
            
            // 使用正确的nmcli参数，添加引号处理特殊字符
            var connectCmd = $"device wifi connect \"{ssid}\" password \"{password}\" ifname {_interface}";
            var result = await RunNmcliCommandAsync(connectCmd);
            
            if (result.Success)
            {
                Log.Information("WiFi连接成功: {Ssid}", ssid);
                Log.Debug("连接输出: {Output}", result.Output);
            }
            else
            {
                Log.Error("WiFi连接失败: {Error}", result.Error);
            }
            
            return result.Success;
        }
        
        /// <summary>
        /// 断开设备连接
        /// </summary>        
        public async Task<bool> DisconnectDeviceAsync()
        {
            Log.Information("正在断开设备连接: {Interface}", _interface);
            
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
            Log.Information("正在设置设备管理状态: {Interface} -> {ManagedState}", _interface, managedState);
            
            var setCmd = $"device set {_interface} managed {managedState}";
            var result = await RunNmcliCommandAsync(setCmd);
            
            return result.Success;
        }

        /// <summary>
        /// 连接设备
        /// </summary>        
        public async Task<bool> ConnectDeviceAsync()
        {
            Log.Information("正在连接设备: {Interface}", _interface);
            
            var connectCmd = $"device connect {_interface}";
            var result = await RunNmcliCommandAsync(connectCmd);
            
            return result.Success;
        }

        /// <summary>
        /// 获取WiFi扫描结果
        /// </summary>        
        public async Task<string> ScanWifiAsync()
        {
            Log.Information("正在扫描WiFi网络");
            
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
            Log.Information("正在使用nmcli启动WiFi热点: {Ssid}", ssid);
            Log.Information("使用配置的IP地址: {Ip}", _config.ApConfig.Ip);
            Log.Information("使用配置的DHCP范围: {DhcpStart} - {DhcpEnd}", _config.ApConfig.DhcpStart, _config.ApConfig.DhcpEnd);

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
                    Log.Error("创建WiFi热点失败: {Error}", result.Error);
                    return false;
                }

                // 设置IP地址和掩码（使用配置文件中的IP）
                var ipCmd = $"connection modify {ssid} ipv4.addresses {_config.ApConfig.Ip}/24";
                var ipResult = await RunNmcliCommandAsync(ipCmd);                
                if (!ipResult.Success)
                {
                    Log.Warning("设置IP地址失败: {Error}", ipResult.Error);
                }

                // 设置为手动IP模式
                var methodCmd = $"connection modify {ssid} ipv4.method manual";
                var methodResult = await RunNmcliCommandAsync(methodCmd);                
                if (!methodResult.Success)
                {
                    Log.Warning("设置IP模式失败: {Error}", methodResult.Error);
                }

                // 启用DHCP服务器（使用配置文件中的DHCP范围）
                var dhcpCmd = $"connection modify {ssid} ipv4.dhcp-range \"{_config.ApConfig.DhcpStart},{_config.ApConfig.DhcpEnd}\"";
                var dhcpResult = await RunNmcliCommandAsync(dhcpCmd);                
                if (!dhcpResult.Success)
                {
                    Log.Warning("设置DHCP范围失败: {Error}", dhcpResult.Error);
                }

                // 重新应用配置
                var upCmd = $"connection up {ssid}";
                var upResult = await RunNmcliCommandAsync(upCmd);                
                if (!upResult.Success)
                {
                    Log.Error("启动WiFi热点失败: {Error}", upResult.Error);
                    return false;
                }

                Log.Information("WiFi热点启动成功: {Ssid}", ssid);
                Log.Information("热点IP: {Ip}", _config.ApConfig.Ip);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "启动WiFi热点时出错");
                return false;
            }
        }

        /// <summary>
        /// 使用nmcli关闭WiFi热点
        /// </summary>        
        public async Task<bool> StopHotspotWithNmcliAsync(string ssid)
        {
            Log.Information("正在关闭nmcli WiFi热点: {Ssid}", ssid);

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
                    Log.Error("关闭WiFi热点失败: {Error}", result.Error);
                    return false;
                }

                Log.Information("WiFi热点已关闭: {Ssid}", ssid);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "关闭WiFi热点时出错");
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
