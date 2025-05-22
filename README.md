# ApWifi

> ⚠️ **Status: Unverified / 待验证**
>
> This project is in an initial implementation stage. The code has not yet been fully validated on real hardware or production environments. Use with caution and welcome feedback!

<p align="right">
  <a href="README.zh-CN.md">🇨🇳 中文说明</a>
</p>

This project is a .NET 8 solution for Raspberry Pi 5 that enables AP hotspot-based WiFi configuration. When the device is offline, it starts an access point, displays a QR code (optionally on a connected screen), and serves a local web page for WiFi setup. After configuration, the system applies the WiFi settings and reboots to connect to the specified network.

## Features
- Automatic AP hotspot mode when no network is detected (Linux only)
- QR code generation (displayed on screen if available, or as an image file)
- Local web server for WiFi SSID/password input
- WiFi configuration is applied to Raspberry Pi OS using system commands (Linux only)
- Automatic reboot and WiFi connection after configuration (Linux only)
- All AP and WiFi parameters are configurable via `appsettings.json` and Liquid templates

## Usage
1. Deploy to Raspberry Pi 5 running Raspberry Pi OS.
2. On first boot or when offline, the device starts in AP mode and displays a QR code (if a display is connected).
3. Scan the QR code or access the AP IP directly to open the configuration web page.
4. Enter WiFi SSID and password.
5. The device applies the settings and reboots (Linux only).
6. On reboot, it connects to the configured WiFi.

## Requirements
- .NET 8 SDK
- Raspberry Pi 5 with Raspberry Pi OS (for full functionality)
- .NET IoT libraries
- (Optional) Screen connected to the Pi for QR code display

## Build & Run
```sh
dotnet build
cd ApWifi.App
dotnet run
```

## Notes
- Most system configuration features (AP setup, WiFi config, reboot) only work on Linux (Raspberry Pi OS). On other platforms, these steps are skipped with a message.
- Running as root is required for network configuration and reboot on Raspberry Pi OS.
- All configuration and system file templates are managed via `appsettings.json` and Liquid templates in the `Templates/` directory.
