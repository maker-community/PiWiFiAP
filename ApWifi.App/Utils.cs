using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using ZXing;
using ZXing.QrCode;
using SkiaSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;

namespace ApWifi.App
{
    public static class Utils
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
        /// 获取当前WiFi连接的IP地址
        /// </summary>
        /// <returns>WiFi连接的IP地址，如果未找到则返回空字符串</returns>
        public static string GetWiFiConnectedIpAddress()
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
                    
                    // 查找无线网络接口
                    bool isWireless = netInterface.Description.ToLower().Contains("wireless") ||
                                      netInterface.Name.ToLower().Contains("wlan") ||
                                      netInterface.Name.ToLower().Contains("wi-fi");
                    
                    if (isWireless)
                    {
                        IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                        foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                var ip = addr.Address.ToString();
                                // 排除热点IP段，只返回正常WiFi连接的IP
                                if (!ip.StartsWith("192.168.4.") && 
                                    !ip.StartsWith("10.42.0.") &&
                                    !ip.StartsWith("169.254.")) // 排除APIPA地址
                                {
                                    return ip;
                                }
                            }
                        }
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
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
          /// <summary>
        /// 使用 ZXing.Net + ImageSharp 生成二维码，返回 ImageSharp.Image 类型
        /// </summary>
        public static Image<Bgra32> GenerateQrCodeImage(string text, int size = 400)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = size,
                    Width = size,
                    Margin = 1
                }
            };
            var pixelData = writer.Write(text);
            // 创建ImageSharp BGRA32图像
            var image = new Image<Bgra32>(pixelData.Width, pixelData.Height);
            for (int y = 0; y < pixelData.Height; y++)
            {
                for (int x = 0; x < pixelData.Width; x++)
                {
                    // ZXing的像素数据是BGRA，二维码只有黑白，取B通道判断
                    byte b = pixelData.Pixels[(y * pixelData.Width + x) * 4];
                    var color = b == 0 ? new Bgra32(0, 0, 0, 255) : new Bgra32(255, 255, 255, 255);
                    image[x, y] = color;
                }
            }
            return image;
        }

        /// <summary>
        /// 创建仅包含IP地址文本的图像，用于显示设备连接状态
        /// </summary>
        /// <param name="ipAddress">IP地址文本</param>
        /// <param name="width">目标图像宽度</param>
        /// <param name="height">目标图像高度</param>
        /// <returns>包含IP地址文本的图像</returns>
        public static Image<Bgra32> CreateIpDisplayImage(string ipAddress, int width, int height)
        {
            // 创建目标图像
            var image = new Image<Bgra32>(width, height);
            
            // 填充白色背景
            image.Mutate(ctx => ctx.Fill(Color.White));
            
            try
            {
                // 使用SkiaSharp绘制文本
                using var bitmap = new SKBitmap(width, height);
                using var canvas = new SKCanvas(bitmap);
                
                // 填充白色背景
                canvas.Clear(SKColors.White);
                
                // 设置字体大小 - 根据屏幕大小调整
                int fontSize = Math.Min(24, Math.Min(width / 12, height / 8));
                using var font = new SKFont(SKTypeface.Default, fontSize);
                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    IsAntialias = true
                };
                
                // 绘制"设备已连接"文本
                string connectedText = "Network connected";
                int connectedY = height / 3;
                canvas.DrawText(connectedText, width / 2, connectedY, SKTextAlign.Center, font, paint);
                
                // 绘制IP地址文本（更大字体）
                using var ipFont = new SKFont(SKTypeface.Default, fontSize + 4);
                using var ipPaint = new SKPaint
                {
                    Color = SKColors.Blue,
                    IsAntialias = true,
                };
                
                string ipText = $"IP: {ipAddress}";
                int ipY = height / 2 + fontSize;
                canvas.DrawText(ipText, width / 2, ipY, SKTextAlign.Center, ipFont, ipPaint);
                
                // 将结果转回ImageSharp
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var color = bitmap.GetPixel(x, y);
                        image[x, y] = new Bgra32(color.Red, color.Green, color.Blue, color.Alpha);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：无法绘制IP地址文本: {ex.Message}");
                // 如果绘制失败，至少返回白色背景
            }
            
            return image;
        }
        
        /// <summary>
        /// 创建包含二维码和IP地址文本的图像，适配指定尺寸的屏幕
        /// </summary>
        /// <param name="url">二维码URL</param>
        /// <param name="ipAddress">IP地址文本</param>
        /// <param name="width">目标图像宽度</param>
        /// <param name="height">目标图像高度</param>
        /// <returns>包含二维码和文本的图像</returns>
        public static Image<Bgra32> CreateQrCodeWithTextImage(string url, string ipAddress, int width, int height)
        {
            // 创建目标图像
            var image = new Image<Bgra32>(width, height);
            
            // 计算二维码大小 - 留出空间给文本
            int textAreaHeight = Math.Min(30, height / 10); // 文本区域高度
            int qrSize = Math.Min(width - 20, height - textAreaHeight - 20); // 二维码大小，留边距
            
            // 生成二维码
            var qrCodeImage = GenerateQrCodeImage(url, qrSize);
            
            // 计算二维码位置（居中上部）
            int qrX = (width - qrSize) / 2;
            int qrY = 10; // 顶部边距
            
            // 填充白色背景
            image.Mutate(ctx => ctx.Fill(Color.White));
            
            // 将二维码复制到目标图像上
            for (int y = 0; y < qrCodeImage.Height && (qrY + y) < height; y++)
            {
                for (int x = 0; x < qrCodeImage.Width && (qrX + x) < width; x++)
                {
                    var pixel = qrCodeImage[x, y];
                    if (qrX + x >= 0 && qrY + y >= 0)
                    {
                        image[qrX + x, qrY + y] = pixel;
                    }
                }
            }
              // 使用SkiaSharp绘制文本（作为后备方案）
            try
            {
                using var bitmap = new SKBitmap(width, height);
                using var canvas = new SKCanvas(bitmap);
                
                // 将ImageSharp图像转换为SKBitmap
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var pixel = image[x, y];
                        bitmap.SetPixel(x, y, new SKColor(pixel.R, pixel.G, pixel.B, pixel.A));
                    }
                }
                
                // 绘制IP地址文本
                using var font = new SKFont(SKTypeface.Default, Math.Min(16, textAreaHeight - 4));
                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    IsAntialias = true
                };
                
                int textY = qrY + qrSize + textAreaHeight / 2 + (int)(font.Size / 2);
                canvas.DrawText(ipAddress, width / 2, textY, SKTextAlign.Center, font, paint);
                
                // 将结果转回ImageSharp
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var color = bitmap.GetPixel(x, y);
                        image[x, y] = new Bgra32(color.Red, color.Green, color.Blue, color.Alpha);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：无法绘制IP地址文本: {ex.Message}");
            }
            
            qrCodeImage.Dispose();
            return image;
        }
          /// <summary>
        /// 获取热点网关IP地址，优先查找热点接口
        /// </summary>
        /// <param name="defaultIp">默认IP地址</param>
        /// <returns>热点网关IP地址</returns>
        public static string GetHotspotGatewayIp(string defaultIp)
        {
            try
            {
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                
                // 首先尝试找到热点相关的接口
                foreach (NetworkInterface netInterface in networkInterfaces)
                {
                    if (netInterface.OperationalStatus != OperationalStatus.Up)
                        continue;
                    if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;
                    
                    // 查找可能是热点的接口（通常是wlan0、ap0等）
                    string interfaceName = netInterface.Name.ToLower();
                    bool isHotspotInterface = interfaceName.Contains("wlan") || 
                                            interfaceName.Contains("ap") ||
                                            interfaceName.Contains("hotspot") ||
                                            interfaceName.Contains("wireless");
                    
                    if (isHotspotInterface)
                    {
                        IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                        foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                var ip = addr.Address.ToString();
                                // 检查是否与配置的AP网段匹配（优先检查192.168.4.x网段）
                                if (ip.StartsWith("192.168.4.") || 
                                    ip.StartsWith("192.168.1.") || 
                                    ip.StartsWith("10.42.0.") ||   // nmcli默认网段
                                    ip.StartsWith("10.0.0.") || 
                                    ip.StartsWith("172."))
                                {
                                    Console.WriteLine($"找到热点网关IP: {ip} (接口: {netInterface.Name})");
                                    return ip;
                                }
                            }
                        }
                    }
                }
                
                // 如果没有找到明确的热点接口，回退到原方法
                return GetApIpAddress(defaultIp);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取热点网关IP时出错: {ex.Message}");
                return defaultIp;
            }
        }
    }
}
