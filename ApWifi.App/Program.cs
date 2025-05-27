using ApWifi.App;
using ApWifi.App.Services;
using Fluid;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Device.Gpio;
using System.Device.Spi;
using System.Runtime.InteropServices;
using Verdure.Iot.Device;

// 默认AP热点IP
const string DefaultApIp = "192.168.4.1";
// 默认AP热点端口
const int WebServerPort = 5000;

DeviceConfig config = LoadConfig();
NetworkManager networkManager = new(config);
LocalizationService localizationService = new();

ST7789Display? _st7789Display24;
ST7789Display? _st7789Display47;

GpioController? _gpioController;

DeviceConfig LoadConfig()
{
    try
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        var configuration = builder.Build();
        var apConfig = configuration.GetSection("ApConfig").Get<ApConfig>() ?? new ApConfig();
        var country = configuration["Country"] ?? "CN";
        return new DeviceConfig { ApConfig = apConfig, Country = country };
    }
    catch
    {
        return new DeviceConfig();
    }
}

// 1. 检查网络连接
if (!await AsyncUtils.IsNetworkAvailableAsync())
{
    // 2. 获取AP热点IP（优先使用配置文件中的IP，若为空则使用默认值或自动检测）
    string apIp = !string.IsNullOrWhiteSpace(config.ApConfig.Ip) ? 
                  config.ApConfig.Ip : 
                  Utils.GetApIpAddress(DefaultApIp);
    Console.WriteLine($"预设热点IP地址: {apIp}");
    
    // 3. 启动AP热点（使用配置参数）
    string actualApIp = await StartAccessPointAsync(apIp);

    // 4. 构建配网地址URL（使用实际的热点网关IP）
    var configUrl = $"http://{actualApIp}:{WebServerPort}";
    Console.WriteLine($"实际配网地址: {configUrl}");

    // 5. 启动本地Web服务器（监听所有接口）
    var serverUrl = $"http://0.0.0.0:{WebServerPort}";
    var webHostTask = StartWebServer(serverUrl);

    // 6. 生成二维码并显示在屏幕（使用实际配网地址）
    await ShowQrCodeOnDisplayAsync(configUrl, actualApIp);

    // 7. 等待Web配置完成
    await webHostTask;
}
else
{
    Console.WriteLine("已连接到网络，无需配置。");
}

async Task<string> StartAccessPointAsync(string ip)
{
    if (!OperatingSystem.IsLinux())
    {
        Console.WriteLine("非Linux系统，跳过AP热点启动。");
        return ip; // 返回预设IP
    }

    var ap = config.ApConfig;
    Console.WriteLine($"正在启动AP热点: {ap.Ssid}");
    Console.WriteLine($"配置的热点IP地址: {ap.Ip}");
    Console.WriteLine($"配置的DHCP范围: {ap.DhcpStart} - {ap.DhcpEnd}");

    // 使用NetworkManager启动热点（会使用配置文件中的IP地址设置）
    var success = await networkManager.StartHotspotAsync(ap.Ssid, ap.Password);    
    if (success)
    {
        // 热点启动成功后，等待一小段时间让网络接口完全初始化
        await Task.Delay(2000);
        
        // 重新获取实际的热点网关IP（应该与配置的IP一致）
        string actualIp = Utils.GetHotspotGatewayIp(ip);
        Console.WriteLine($"AP热点已启动，实际网关IP地址: {actualIp}");
        
        // 验证实际IP是否与配置IP一致
        if (actualIp != ap.Ip && !string.IsNullOrWhiteSpace(ap.Ip))
        {
            Console.WriteLine($"警告：实际IP({actualIp})与配置IP({ap.Ip})不一致！");
        }
        
        return actualIp;
    }
    else
    {
        Console.WriteLine("AP热点启动失败");
        return ip; // 启动失败时返回预设IP作为备用
    }
}

async Task StartWebServer(string url)
{
    Console.WriteLine($"正在启动Web服务器，监听地址: {url}");
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls(url);
    var app = builder.Build();    
    app.MapGet("/", async (HttpRequest req) =>
    {
        // 获取语言参数
        var langParam = req.Query["lang"].FirstOrDefault();
        if (!string.IsNullOrEmpty(langParam))
        {
            localizationService.SetLanguage(langParam);
        }        var template = await File.ReadAllTextAsync("Templates/wifi_form.liquid");
        var parser = new FluidParser();
        
        // 配置Fluid模型绑定，允许访问LanguageItem的属性
        TemplateOptions.Default.MemberAccessStrategy.Register<LanguageItem>();
        
        if (!parser.TryParse(template, out var fluidTemplate, out var error))
        {
            return Results.Content($"模板解析错误: {error}", "text/plain");
        }
        
        var context = new TemplateContext();
        context.SetValue("ssid", "");
        context.SetValue("pwd", "");
        context.SetValue("strings", localizationService.GetAllStrings());
        context.SetValue("currentLanguage", localizationService.GetCurrentLanguage());
          // 准备语言列表
        var languages = localizationService.GetAvailableLanguages()
            .Select(lang => new LanguageItem { Code = lang, Name = localizationService.GetLanguageDisplayName(lang) })
            .ToList();
        context.SetValue("languages", languages);
        
        var html = await fluidTemplate.RenderAsync(context);
        return Results.Content(html, "text/html");
    });    app.MapPost("/config", async (HttpRequest req) =>
    {
        var form = await req.ReadFormAsync();
        var ssid = form["ssid"].ToString();
        var pwd = form["pwd"].ToString();
        var language = form["language"].ToString();
        
        // 设置语言
        if (!string.IsNullOrEmpty(language))
        {
            localizationService.SetLanguage(language);
        }
        
        if (string.IsNullOrEmpty(ssid))
        {
            var errorMessage = localizationService.GetString("WifiNameRequired");
            var errorTitle = localizationService.GetString("Error");
            var backLink = localizationService.GetString("BackLink");
            return Results.Content($"<html><body><h1>{errorTitle}</h1><p>{errorMessage}</p><a href='/'>{backLink}</a></body></html>", "text/html");
        }
        
        await SaveWifiConfigAsync(ssid, pwd);        var template = await File.ReadAllTextAsync("Templates/wifi_success.liquid");
        var parser = new FluidParser();
        
        // 配置Fluid模型绑定，确保一致性
        TemplateOptions.Default.MemberAccessStrategy.Register<LanguageItem>();
        
        if (!parser.TryParse(template, out var fluidTemplate, out var error))
        {
            return Results.Content($"模板解析错误: {error}", "text/plain");
        }
        
        var context = new TemplateContext();
        context.SetValue("ssid", ssid);
        context.SetValue("strings", localizationService.GetAllStrings());
        
        var html = await fluidTemplate.RenderAsync(context);
        _ = Task.Run(async () =>
        {
            await Task.Delay(50000);
            await RebootAsync();
        });
        return Results.Content(html, "text/html");
    });

    await app.RunAsync();
}

async Task SaveWifiConfigAsync(string ssid, string pwd)
{
    if (!OperatingSystem.IsLinux())
    {
        Console.WriteLine("非Linux系统，跳过WiFi配置保存。");
        return;
    }

    Console.WriteLine("正在保存WiFi配置...");

    // 关闭热点，恢复接口管理
    await networkManager.StopHotspotAsync();
    await networkManager.SetDeviceManagedAsync(true);

    // 启动WiFi连接
    await networkManager.ConnectDeviceAsync();

    // 使用NetworkManager连接WiFi
    var success = await networkManager.ConnectToWifiAsync(ssid, pwd);

    if (success)
    {
        Console.WriteLine("WiFi配置已保存并连接成功");
    }
    else
    {
        Console.WriteLine("WiFi配置保存失败");
    }
}

async Task RebootAsync()
{
    if (!OperatingSystem.IsLinux())
    {
        Console.WriteLine("非Linux系统，跳过重启。");
        return;
    }
    Console.WriteLine("执行系统重启...");
    await AsyncUtils.RebootAsync();
}

async Task ShowQrCodeOnDisplayAsync(string url, string gatewayIp)
{    if (!OperatingSystem.IsLinux())
    {
        Console.WriteLine("非Linux系统，跳过二维码显示，仅生成二维码图片。");
        var qrcodeImage = Utils.CreateQrCodeWithTextImage(url, $"IP: {gatewayIp}", 400, 400);
        // 保存生成的二维码图片到本地文件
        var qrCodeFilePath = Path.Combine(Directory.GetCurrentDirectory(), "qrcode.png");
        qrcodeImage.Save(qrCodeFilePath);
        Console.WriteLine($"二维码图片已保存到: {qrCodeFilePath}");
        Console.WriteLine($"配网地址: {url}");
        Console.WriteLine($"网关IP: {gatewayIp}");

        //Utils.ShowQrCode(url);
        return;
    }
    try
    {
        Console.WriteLine("正在显示二维码到连接的屏幕...");
        bool hasDisplay = false;
        try
        {
            var i2cDevices = Utils.RunCommand("ls /dev/i2c* 2>/dev/null || echo ''");
            hasDisplay = !string.IsNullOrEmpty(i2cDevices) && !i2cDevices.Contains("No such file");
            if (!hasDisplay)
            {
                var spiDevices = Utils.RunCommand("ls /dev/spidev* 2>/dev/null || echo ''");
                hasDisplay = !string.IsNullOrEmpty(spiDevices) && !spiDevices.Contains("No such file");
            }
        }
        catch
        {
            hasDisplay = false;
        }
        if (!hasDisplay)
        {
            Console.WriteLine("未检测到显示设备，仅生成二维码图片文件");
            Utils.ShowQrCode(url);
            return;
        }

        _gpioController = new GpioController();

        var settings1 = new SpiConnectionSettings(0, 0)
        {
            ClockFrequency = 24_000_000, // 尝试降低SPI时钟频率以减少闪烁
            Mode = SpiMode.Mode0,
        };

        var settings2 = new SpiConnectionSettings(0, 1)
        {
            ClockFrequency = 24_000_000,
            Mode = SpiMode.Mode0,
        };

        Console.WriteLine("正在初始化2.4寸显示器...");
        _st7789Display24 = new ST7789Display(settings1, _gpioController, true, dcPin: 25, resetPin: 27, displayType: DisplayType.Display24Inch);
        Console.WriteLine("2.4寸显示器初始化完成");

        Console.WriteLine("正在初始化1.47寸显示器...");
        _st7789Display47 = new ST7789Display(settings2, _gpioController, false, dcPin: 25, resetPin: 27, displayType: DisplayType.Display147Inch);
        Console.WriteLine("1.47寸显示器初始化完成");        // 清屏以准备播放动画 不清屏是不能写入数据的
        _st7789Display24.FillScreen(0x0000);  // 黑色
        _st7789Display47.FillScreen(0x0000);  // 黑色

        // 使用传入的网关IP作为显示IP
        Console.WriteLine($"在屏幕上显示配网地址: {url}");
        Console.WriteLine($"网关IP: {gatewayIp}");
        
        // 为2.4寸屏幕(240x320)生成带文本的二维码图像（横屏模式：320x240）
        var qrImage24 = Utils.CreateQrCodeWithTextImage(url, $"IP: {gatewayIp}", 320, 240);
        
        // 为1.47寸屏幕(172x320)生成带文本的二维码图像（横屏模式：320x172）
        var qrImage47 = Utils.CreateQrCodeWithTextImage(url, $"IP: {gatewayIp}", 320, 172);

        using Image<Bgr24> converted2inch4Image = qrImage24.CloneAs<Bgr24>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            converted2inch4Image.Mutate(x => x.Rotate(90));
            var data1 = _st7789Display24?.GetImageBytes(converted2inch4Image);

            if (data1 != null)
            {
                _st7789Display24?.SendData(data1);
            }

            await Task.Delay(5); // 短暂延时确保传输完成
        }

        using Image<Bgr24> converted1inch47Image = qrImage47.CloneAs<Bgr24>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            converted1inch47Image.Mutate(x => x.Rotate(90));
            var data2 = _st7789Display47?.GetImageBytes(converted1inch47Image);
            if (data2 != null)
            {
                _st7789Display47?.SendData(data2);
            }
        }
          // 释放资源
        qrImage24.Dispose();
        qrImage47.Dispose();
        Console.WriteLine($"请访问 {url} 配置WiFi");
        Console.WriteLine($"或直接访问网关IP: {gatewayIp}:{WebServerPort}");
        Console.WriteLine("或扫描屏幕上的二维码连接配置网页");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"显示二维码时出错: {ex.Message}");
        Utils.ShowQrCode(url);
    }
}

// 语言项类，用于Fluid模板渲染
public class LanguageItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
