using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using ZXing;
using ZXing.QrCode;
using SkiaSharp;

namespace ApWifi.App
{    public static class Utils
    {
        public static string GetApIpAddress(string defaultIp)
        {
            try
            {
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface netInterface in networkInterfaces)
                {
                    if (netInterface.OperationalStatus != OperationalStatus.Up)
                        continue;
                    if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;
                    bool isWireless = netInterface.Description.ToLower().Contains("wireless") ||
                                      netInterface.Name.ToLower().Contains("wlan") ||
                                      netInterface.Name.ToLower().Contains("wi-fi");
                    IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                    foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            if (isWireless)
                                return addr.Address.ToString();
                            return addr.Address.ToString();
                        }
                    }
                }
                return defaultIp;
            }
            catch
            {
                return defaultIp;
            }
        }        
        
        /// <summary>
        /// 同步执行shell命令（保留用于兼容性）
        /// </summary>
        public static string RunCommand(string command)
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
                using var process = Process.Start(psi);
                if (process == null)
                    return string.Empty;
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (!string.IsNullOrEmpty(error))
                    Console.WriteLine($"命令 '{command}' 执行错误: {error}");
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行命令 '{command}' 时出错: {ex.Message}");
                return string.Empty;
            }
        }

        public static void ShowQrCode(string url)
        {
            // 1. 生成二维码像素数据
            var qrWriter = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = 400,
                    Width = 400,
                    Margin = 1
                }
            };
            var pixelData = qrWriter.Write(url);

            // 2. 创建二维码位图 - 修复像素处理
            using var bitmap = new SKBitmap(pixelData.Width, pixelData.Height);
            var pixels = pixelData.Pixels;

            // ZXing的pixelData.Pixels实际上是BGRA格式，每个像素4个字节
            for (int y = 0; y < pixelData.Height; y++)
            {
                for (int x = 0; x < pixelData.Width; x++)
                {
                    int offset = (y * pixelData.Width + x) * 4; // 4 bytes per pixel (BGRA)
                    // 根据像素值设置为黑色或白色
                    // ZXing生成的QR码通常是黑白的，0表示黑色，255表示白色
                    byte pixelValue = pixels[offset]; // 取B通道值判断（BGR中任何一个通道都可以）
                    bitmap.SetPixel(x, y, pixelValue == 0 ? SKColors.Black : SKColors.White);
                }
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            // 3. 生成文件路径，兼容Windows和Linux
            string fileName = "wifi-setup-qr.png";
            string tmpDir = Path.GetTempPath();
            string filePath = Path.Combine(tmpDir, fileName);

            // 4. 如果文件不存在则创建（覆盖写入）
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                data.SaveTo(stream);
            }

            // 5. 打开二维码图片（跨平台）
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo("explorer", $"\"{filePath}\"") { UseShellExecute = true });
                }
                //else if (OperatingSystem.IsLinux())
                //{
                //    Process.Start("xdg-open", filePath);
                //}
                //else if (OperatingSystem.IsMacOS())
                //{
                //    Process.Start("open", filePath);
                //}
                //else
                //{
                //    Console.WriteLine($"二维码已生成，请手动打开 {filePath}");
                //}
            }
            catch
            {
                Console.WriteLine($"二维码已生成，请手动打开 {filePath}");
            }
        }
        
        /// <summary>
        /// 检查端口是否被占用（同步版本，保留用于兼容性）
        /// </summary>
        public static bool IsPortInUse(int port)
        {
            try
            {
                // 使用命令行检查端口是否被占用
                var result = RunCommand($"sudo lsof -i :{port}");
                return !string.IsNullOrWhiteSpace(result);
            }
            catch
            {
                return false;
            }
        }

        public static bool StartDnsmasq(string configPath, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    // 确保先杀死所有dnsmasq进程
                    RunCommand("sudo killall -9 dnsmasq 2>/dev/null || true");
                    Thread.Sleep(1000); // 等待进程终止
                    
                    // 尝试启动dnsmasq
                    var result = RunCommand($"sudo dnsmasq -C {configPath}");
                    
                    // 检查是否成功启动
                    if (string.IsNullOrEmpty(result) || !result.Contains("failed"))
                    {
                        // 再次检查服务是否真的运行
                        var checkResult = RunCommand("pidof dnsmasq");
                        if (!string.IsNullOrEmpty(checkResult))
                        {
                            Console.WriteLine("dnsmasq服务成功启动");
                            return true;
                        }
                    }
                    
                    Console.WriteLine($"尝试启动dnsmasq失败 (尝试 {i+1}/{maxRetries})，等待重试...");
                    Thread.Sleep(2000); // 等待端口释放
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"启动dnsmasq时出错: {ex.Message}");
                }
            }
            
            Console.WriteLine("无法启动dnsmasq服务，尝试使用备选方法...");
            try
            {
                // 备选方法：使用systemd启动
                RunCommand("sudo systemctl restart dnsmasq");
                Thread.Sleep(2000);
                var checkResult = RunCommand("pidof dnsmasq");
                return !string.IsNullOrEmpty(checkResult);
            }
            catch
            {
                return false;
            }
        }
    }
}
