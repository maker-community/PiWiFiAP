using ApWifi.App;
using Fluid;

// 默认AP热点IP
const string DefaultApIp = "192.168.4.1";
// 默认AP热点端口
const int WebServerPort = 5000;

DeviceConfig config = LoadConfig();

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
if (!Utils.IsNetworkAvailable())
{
    // 2. 获取主机IP作为AP热点IP（优先配置文件）
    string apIp = !string.IsNullOrWhiteSpace(config.ApConfig.Ip) ? config.ApConfig.Ip : Utils.GetApIpAddress(DefaultApIp);
    Console.WriteLine($"使用IP地址 {apIp} 作为AP热点IP");

    // 3. 启动AP热点（用配置参数）
    StartAccessPoint(apIp);

    // 4. 启动本地Web服务器（用配置参数）
    var url = $"http://0.0.0.0:{WebServerPort}";
    var webHostTask = StartWebServer(url);

    // 5. 生成二维码并显示在屏幕
    ShowQrCodeOnDisplay(url);

    // 6. 等待Web配置完成
    await webHostTask;
}
else
{
    Console.WriteLine("已连接到网络，无需配置。");
}

void StartAccessPoint(string ip)
{
    if (!OperatingSystem.IsLinux())
    {
        Console.WriteLine("非Linux系统，跳过AP热点启动。");
        return;
    }
    var ap = config.ApConfig;
    // 关闭 wlan0 相关连接，防止冲突
    Utils.RunCommand($"sudo nmcli device disconnect {ap.Interface}");
    Utils.RunCommand($"sudo nmcli device set {ap.Interface} managed no");
    // 启动热点
    var hotspotCmd = $"sudo nmcli device wifi hotspot ifname {ap.Interface} ssid '{ap.Ssid}' password '{ap.Password}'";
    Console.WriteLine($"执行命令: {hotspotCmd}，请稍等...");
    var result = Utils.RunCommand(hotspotCmd);
    Console.WriteLine(result);
    // 设置IP（nmcli hotspot 默认会分配IP，但如需自定义可用如下命令）
    if (!string.IsNullOrWhiteSpace(ip) && ip != "192.168.8.1")
    {
        Utils.RunCommand($"sudo nmcli connection modify Hotspot ipv4.addresses {ip}/24 ipv4.method shared");
        Utils.RunCommand($"sudo nmcli connection up Hotspot");
    }
    Console.WriteLine($"AP热点已启动，IP地址: {ip}");
}

async Task StartWebServer(string url)
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls(url);
    var app = builder.Build();

    app.MapGet("/", async () =>
    {
        var template = await File.ReadAllTextAsync("Templates/wifi_form.liquid");
        var parser = new FluidParser();
        if (!parser.TryParse(template, out var fluidTemplate, out var error))
        {
            return Results.Content($"模板解析错误: {error}", "text/plain");
        }
        var context = new TemplateContext();
        context.SetValue("ssid", "");
        context.SetValue("pwd", "");
        var html = await fluidTemplate.RenderAsync(context);
        return Results.Content(html, "text/html");
    });

    app.MapPost("/config", async (HttpRequest req) =>
    {
        var form = await req.ReadFormAsync();
        var ssid = form["ssid"].ToString();
        var pwd = form["pwd"].ToString();
        if (string.IsNullOrEmpty(ssid))
        {
            return Results.Content("<html><body><h1>错误</h1><p>WiFi名称不能为空</p><a href='/'>返回</a></body></html>", "text/html");
        }
        SaveWifiConfig(ssid, pwd);
        ApplyWifiConfig();
        var template = await File.ReadAllTextAsync("Templates/wifi_success.liquid");
        var parser = new FluidParser();
        if (!parser.TryParse(template, out var fluidTemplate, out var error))
        {
            return Results.Content($"模板解析错误: {error}", "text/plain");
        }
        var context = new TemplateContext();
        context.SetValue("ssid", ssid);
        var html = await fluidTemplate.RenderAsync(context);
        _ = Task.Run(async () =>
        {
            await Task.Delay(5000);
            Reboot();
        });
        return Results.Content(html, "text/html");
    });

    await app.RunAsync();
}

void SaveWifiConfig(string ssid, string pwd)
{
    if (!OperatingSystem.IsLinux())
    {
        Console.WriteLine("非Linux系统，跳过WiFi配置保存。");
        return;
    }
    // 直接用nmcli保存WiFi配置
    var ap = config.ApConfig;
    var connectCmd = $"sudo nmcli device wifi connect '{ssid}' password '{pwd}' ifname {ap.Interface}";
    var result = Utils.RunCommand(connectCmd);
    Console.WriteLine(result);
    Console.WriteLine("WiFi配置已保存");
}

void ApplyWifiConfig()
{
    if (!OperatingSystem.IsLinux())
    {
        Console.WriteLine("非Linux系统，跳过WiFi配置应用。");
        return;
    }
    var ap = config.ApConfig;
    // 关闭热点，恢复接口管理
    Utils.RunCommand($"sudo nmcli connection down Hotspot");
    Utils.RunCommand($"sudo nmcli device set {ap.Interface} managed yes");
    // 启动WiFi连接
    Utils.RunCommand($"sudo nmcli device connect {ap.Interface}");
    Console.WriteLine("应用WiFi配置...");
}

void Reboot()
{
    if (!OperatingSystem.IsLinux())
    {
        Console.WriteLine("非Linux系统，跳过重启。");
        return;
    }
    Console.WriteLine("执行系统重启...");
    Utils.RunCommand("sudo reboot");
}

void ShowQrCodeOnDisplay(string url)
{
    if (!OperatingSystem.IsLinux())
    {
        Console.WriteLine("非Linux系统，跳过二维码显示，仅生成二维码图片。");
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
        Utils.ShowQrCode(url);
        Console.WriteLine($"请访问 {url} 配置WiFi");
        Console.WriteLine("或扫描二维码连接配置网页");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"显示二维码时出错: {ex.Message}");
        Utils.ShowQrCode(url);
    }
}
