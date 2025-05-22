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
        public static bool IsNetworkAvailable()
        {
            try
            {
                var ping = new Ping();
                var reply = ping.Send("8.8.8.8", 1000);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

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
            using var bitmap = new SKBitmap(pixelData.Width, pixelData.Height, SKColorType.Gray8, SKAlphaType.Opaque);
            var bytes = pixelData.Pixels;
            for (int y = 0; y < pixelData.Height; y++)
            {
                for (int x = 0; x < pixelData.Width; x++)
                {
                    byte v = bytes[y * pixelData.Width + x];
                    bitmap.SetPixel(x, y, v == 0 ? SKColors.Black : SKColors.White);
                }
            }
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite("/tmp/wifi-setup-qr.png");
            data.SaveTo(stream);
            Console.WriteLine("二维码已生成，请在屏幕查看或手动打开 /tmp/wifi-setup-qr.png");
        }
    }
}
