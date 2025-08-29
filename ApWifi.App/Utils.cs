using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using ZXing;
using ZXing.QrCode;
using SkiaSharp;

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
            try
            {
                var qrCodeData = GenerateQrCodeData(url, 400);
                
                // 生成文件路径，兼容Windows和Linux
                string fileName = "wifi-setup-qr.png";
                string tmpDir = Path.GetTempPath();
                string filePath = Path.Combine(tmpDir, fileName);

                // 保存PNG文件
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    qrCodeData.SaveTo(stream);
                }

                // 打开二维码图片（跨平台）
                try
                {
                    if (OperatingSystem.IsWindows())
                    {
                        Process.Start(new ProcessStartInfo("explorer", $"\"{filePath}\"") { UseShellExecute = true });
                    }
                    else
                    {
                        Console.WriteLine($"二维码已生成，请手动打开 {filePath}");
                    }
                }
                catch
                {
                    Console.WriteLine($"二维码已生成，请手动打开 {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成二维码时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成二维码数据（PNG格式）
        /// </summary>
        private static SKData GenerateQrCodeData(string text, int size)
        {
            var qrWriter = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = size,
                    Width = size,
                    Margin = 1
                }
            };
            var pixelData = qrWriter.Write(text);

            // 创建SkiaSharp位图
            using var bitmap = new SKBitmap(pixelData.Width, pixelData.Height);
            var pixels = pixelData.Pixels;

            // ZXing的pixelData.Pixels是BGRA格式，每个像素4个字节
            for (int y = 0; y < pixelData.Height; y++)
            {
                for (int x = 0; x < pixelData.Width; x++)
                {
                    int offset = (y * pixelData.Width + x) * 4; // 4 bytes per pixel (BGRA)
                    // 根据像素值设置为黑色或白色
                    byte pixelValue = pixels[offset]; // 取B通道值判断
                    bitmap.SetPixel(x, y, pixelValue == 0 ? SKColors.Black : SKColors.White);
                }
            }

            using var image = SKImage.FromBitmap(bitmap);
            return image.Encode(SKEncodedImageFormat.Png, 100);
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
        /// 使用 ZXing.Net + SkiaSharp 生成二维码，返回 RGB565 字节数组
        /// </summary>
        public static byte[] GenerateQrCodeImage(string text, int width, int height)
        {
            return CreateQrCodeWithTextImage(text, "", width, height);
        }

        /// <summary>
        /// 创建仅包含IP地址文本的图像，用于显示设备连接状态，返回RGB565格式字节数组
        /// </summary>
        /// <param name="ipAddress">IP地址文本</param>
        /// <param name="width">目标图像宽度</param>
        /// <param name="height">目标图像高度</param>
        /// <returns>RGB565格式的字节数组</returns>
        public static byte[] CreateIpDisplayImage(string ipAddress, int width, int height)
        {
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            using var canvas = surface.Canvas;
            
            // 清除背景为白色
            canvas.Clear(SKColors.White);
            
            try
            {
                // 设置字体大小 - 根据屏幕大小调整
                int fontSize = Math.Min(24, Math.Min(width / 12, height / 8));
                
                using var connectedFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal), fontSize);
                using var connectedPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    IsAntialias = true
                };
                
                using var ipFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), fontSize + 4);
                using var ipPaint = new SKPaint
                {
                    Color = SKColors.Blue,
                    IsAntialias = true
                };
                
                // 绘制"设备已连接"文本
                string connectedText = "Network connected";
                var connectedTextBounds = connectedFont.MeasureText(connectedText);
                float connectedX = (width - connectedTextBounds) / 2;
                float connectedY = height / 3;
                canvas.DrawText(connectedText, connectedX, connectedY, SKTextAlign.Left, connectedFont, connectedPaint);
                
                // 绘制IP地址文本
                string ipText = $"IP: {ipAddress}";
                var ipTextBounds = ipFont.MeasureText(ipText);
                float ipX = (width - ipTextBounds) / 2;
                float ipY = height / 2 + fontSize;
                canvas.DrawText(ipText, ipX, ipY, SKTextAlign.Left, ipFont, ipPaint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：无法绘制IP地址文本: {ex.Message}");
            }
            
            // 获取图像并转换为RGB565
            using var image = surface.Snapshot();
            using var pixmap = image.PeekPixels();
            return ConvertToRgb565(pixmap, width, height);
        }

        /// <summary>
        /// 将SkiaSharp像素转换为RGB565格式
        /// </summary>
        private static byte[] ConvertToRgb565(SKPixmap pixmap, int width, int height)
        {
            byte[] buffer = new byte[width * height * 2]; // 16位/像素
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var color = pixmap.GetPixelColor(x, y);
                    // 转换为RGB565格式
                    byte r = (byte)(color.Red >> 3);    // 5位红色
                    byte g = (byte)(color.Green >> 2);  // 6位绿色
                    byte b = (byte)(color.Blue >> 3);   // 5位蓝色
                    
                    ushort rgb565 = (ushort)((r << 11) | (g << 5) | b);
                    
                    int index = (y * width + x) * 2;
                    buffer[index] = (byte)(rgb565 >> 8);     // 高字节
                    buffer[index + 1] = (byte)(rgb565 & 0xFF); // 低字节
                }
            }
            return buffer;
        }
        
        /// <summary>
        /// 创建包含二维码和IP地址文本的图像，适配指定尺寸的屏幕，返回RGB565格式字节数组
        /// </summary>
        /// <param name="url">二维码URL</param>
        /// <param name="ipAddress">IP地址文本</param>
        /// <param name="width">目标图像宽度</param>
        /// <param name="height">目标图像高度</param>
        /// <returns>RGB565格式的字节数组</returns>
        public static byte[] CreateQrCodeWithTextImage(string url, string ipAddress, int width, int height)
        {
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            using var canvas = surface.Canvas;
            
            // 清除背景为白色
            canvas.Clear(SKColors.White);
            
            try
            {
                // 计算二维码大小 - 留出空间给文本
                int textAreaHeight = string.IsNullOrEmpty(ipAddress) ? 0 : Math.Min(30, height / 10);
                int qrSize = Math.Min(width - 20, height - textAreaHeight - 20);
                
                // 生成二维码
                var qrWriter = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Height = qrSize,
                        Width = qrSize,
                        Margin = 1
                    }
                };
                var pixelData = qrWriter.Write(url);
                
                // 计算二维码位置（居中上部）
                int qrX = (width - qrSize) / 2;
                int qrY = 10; // 顶部边距
                
                // 绘制二维码
                var pixels = pixelData.Pixels;
                for (int y = 0; y < pixelData.Height; y++)
                {
                    for (int x = 0; x < pixelData.Width; x++)
                    {
                        int offset = (y * pixelData.Width + x) * 4; // BGRA格式
                        byte pixelValue = pixels[offset]; // 取B通道值
                        var color = pixelValue == 0 ? SKColors.Black : SKColors.White;
                        
                        int drawX = qrX + x;
                        int drawY = qrY + y;
                        if (drawX >= 0 && drawX < width && drawY >= 0 && drawY < height)
                        {
                            using var paint = new SKPaint { Color = color };
                            canvas.DrawPoint(drawX, drawY, paint);
                        }
                    }
                }
                
                // 绘制IP地址文本（如果提供）
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    int fontSize = Math.Min(16, textAreaHeight - 4);
                    using var font = new SKFont(SKTypeface.Default, fontSize);
                    using var paint = new SKPaint
                    {
                        Color = SKColors.Black,
                        IsAntialias = true
                    };
                    
                    var textBounds = font.MeasureText(ipAddress);
                    float textX = (width - textBounds) / 2;
                    float textY = qrY + qrSize + textAreaHeight / 2 + fontSize / 2;
                    
                    if (textY < height - 5) // 确保文本在屏幕内
                    {
                        canvas.DrawText(ipAddress, textX, textY, SKTextAlign.Left, font, paint);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：无法绘制二维码: {ex.Message}");
            }
            
            // 获取图像并转换为RGB565
            using var image = surface.Snapshot();
            using var pixmap = image.PeekPixels();
            return ConvertToRgb565(pixmap, width, height);
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
