# ApWifi（中文说明）

> ⚠️ **项目状态：待验证 / Unverified**
>
> 本项目目前为初步实现，尚未在真实硬件或生产环境中充分验证。请谨慎使用，欢迎反馈和建议！

<p align="right">
  <a href="README.md">🇺🇸 English</a>
</p>

本项目是为树莓派5设计的.NET 8解决方案，实现了基于AP热点的WiFi配置功能。当设备无网络时自动开启AP热点，生成二维码（如有屏幕则显示），并启动本地Web页面用于WiFi配置。配置完成后自动写入系统并重启连接WiFi。

## 功能特性
- 无网络时自动进入AP热点模式（仅Linux/树莓派OS下生效）
- 生成二维码（如有屏幕则显示，否则生成图片文件）
- 本地Web服务器用于WiFi SSID/密码输入
- WiFi配置通过系统命令写入树莓派OS（仅Linux下生效）
- 配置完成后自动重启并连接WiFi（仅Linux下生效）
- 所有AP和WiFi参数均可通过`appsettings.json`和Liquid模板灵活配置

## 使用方法
1. 部署到运行树莓派OS的树莓派5。
2. 首次启动或离线时，设备自动进入AP模式并显示二维码（如有屏幕）。
3. 扫码或直接访问AP IP进入配置页面。
4. 输入WiFi名称和密码。
5. 设备应用配置并自动重启（仅Linux下）。
6. 重启后自动连接配置的WiFi。

## 依赖环境
- .NET 8 SDK
- 树莓派5 + 树莓派OS（完整功能需Linux）
- .NET IoT库
- （可选）连接屏幕用于二维码显示

## 构建与运行
```sh
dotnet build
cd ApWifi.App
dotnet run
```

## 注意事项
- 绝大多数系统配置（AP、WiFi、重启）仅在Linux/树莓派OS下有效，其他平台仅输出提示不执行。
- 网络配置和重启等操作需root权限。
- 所有配置和系统文件模板均通过`appsettings.json`和`Templates/`目录下的Liquid模板管理。
