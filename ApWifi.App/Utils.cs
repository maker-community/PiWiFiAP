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
    }
}
