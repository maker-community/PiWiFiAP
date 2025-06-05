using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ApWifi.App
{
    public static class AsyncUtils
    {
        /// <summary>
        /// 异步执行shell命令
        /// </summary>
        public static async Task<CommandResult> RunCommandAsync(string command, int timeoutSeconds = 30)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"",
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

                    Console.WriteLine($"命令 '{command}' 执行完成，输出: {output}, 错误: {error}");
                    var result = new CommandResult
                    {
                        Success = process.ExitCode == 0,
                        Output = output,
                        Error = error,
                        ExitCode = process.ExitCode
                    };

                    if (!result.Success && !string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine($"命令 '{command}' 执行错误: {error}");
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行命令 '{command}' 时出错: {ex.Message}");
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
        /// 异步重启系统
        /// </summary>
        public static async Task RebootAsync()
        {
            if (!OperatingSystem.IsLinux())
            {
                Console.WriteLine("非Linux系统，跳过重启");
                return;
            }

            Console.WriteLine("执行系统重启...");
            await RunCommandAsync("sudo reboot");
        }

        /// <summary>
        /// 异步检查网络连接
        /// </summary>
        public static async Task<bool> IsNetworkAvailableAsync()
        {
            try
            {
                var result = await RunCommandAsync("sudo ping -c 1 -W 1 8.8.8.8");
                if (!result.Success)
                {
                    Console.WriteLine("网络连接检查失败: " + result.Error);
                    return false;
                }
                Console.WriteLine("网络连接检查成功");
                return result.Success;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"检查网络连接时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 异步检查端口是否被占用
        /// </summary>
        public static async Task<bool> IsPortInUseAsync(int port)
        {
            try
            {
                var result = await RunCommandAsync($"sudo lsof -i :{port}");
                return result.Success && !string.IsNullOrWhiteSpace(result.Output);
            }
            catch
            {
                return false;
            }
        }
    }
}