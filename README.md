# ApWifi

> ✅ **Status: Tested on Raspberry Pi 5**
>
> This project has been successfully tested on Raspberry Pi 5 running Raspberry Pi OS. The AP hotspot is created with the name "RaspberryPi5-WiFiSetup" and the WiFi configuration functionality works as expected.

<p align="right">
  <a href="README.zh-CN.md">🇨🇳 中文说明</a>
</p>

This project is a .NET 8 solution for Raspberry Pi 5 that enables AP hotspot-based WiFi configuration. When the device is offline, it starts an access point with the name "RaspberryPi5-WiFiSetup", displays a QR code (optionally on a connected screen), and serves a local web page for WiFi setup. After configuration, the system applies the WiFi settings and reboots to connect to the specified network.

## Screenshots

| Screen Display | WiFi Configuration Interface |
|:---:|:---:|
| ![Network Not Connected](assets/network-not-connected.JPG) | ![WiFi Setup Form](assets/set_ssid_en.PNG) |
| **When offline**: QR code displayed on screen for WiFi configuration | **Configuration page**: Enter WiFi SSID and password |

| After Configuration | Multi-language Support |
|:---:|:---:|
| ![Configuration Success](assets/set_ssid_ok_en.PNG) | ![Chinese Interface](assets/set_ssid_zh.PNG) |
| **Success page**: Device is restarting to apply settings | **Chinese interface**: Full localization support |

| Connected State |
|:---:|
| ![Network Connected](assets/network-connected.JPG) |
| **After successful connection**: Device displays its IP address |

## Features
- ✅ **Tested**: Automatic AP hotspot mode when no network is detected (creates "RaspberryPi5-WiFiSetup" network)
- ✅ **Tested**: QR code generation and display on SPI-connected screen using .NET IoT libraries and SkiaSharp rendering
- ✅ **Tested**: Local web server for WiFi SSID/password input with responsive UI
- ✅ **Tested**: WiFi configuration applied to Raspberry Pi OS using system commands
- ✅ **Tested**: Automatic reboot and WiFi connection after configuration
- ✅ **Tested**: Multi-language support (English, Chinese, German, French, Japanese)
- All AP and WiFi parameters are configurable via `appsettings.json` and Liquid templates

## Usage
1. **Deploy** to Raspberry Pi 5 running Raspberry Pi OS.
2. **Initial Setup**: On first boot or when offline, the device automatically starts in AP mode (creates "RaspberryPi5-WiFiSetup" hotspot) and displays a QR code on the connected screen.
3. **Configuration**: Scan the QR code with your mobile device or connect to the AP and navigate to the displayed IP address to open the WiFi configuration web page.
4. **WiFi Setup**: Enter your WiFi SSID and password in the web interface (supports multiple languages).
5. **Apply Settings**: The device automatically applies the WiFi settings and reboots.
6. **Connection**: After reboot, the device connects to the configured WiFi network and displays its IP address on the screen.

## How It Works

### When Network is Disconnected
- Device starts AP hotspot named "RaspberryPi5-WiFiSetup"
- QR code is displayed on the connected screen pointing to the configuration URL
- Local web server runs on the AP network for WiFi configuration

### During WiFi Configuration
- Web interface allows input of WiFi SSID and password
- Supports multiple languages with automatic detection
- Real-time feedback and validation

### After Configuration
- WiFi settings are written to system configuration
- Device automatically reboots to apply changes
- Connects to the specified WiFi network
- Displays the assigned IP address on screen

## Requirements
- .NET 8 SDK
- **Raspberry Pi 5** with **Raspberry Pi OS** (tested and verified)
- .NET IoT libraries for hardware control
- SPI display/screen connected to the Pi for QR code display
- SkiaSharp library for image rendering and SPI transmission
- Root privileges for network configuration operations

## Tested Environment
- **Hardware**: Raspberry Pi 5
- **OS**: Raspberry Pi OS (64-bit)
- **Runtime**: .NET 8
- **Network**: Successfully tested with various WiFi networks
- **Display**: Tested with SPI-connected display using .NET IoT libraries and SkiaSharp rendering

## Build & Run

### Development
```sh
dotnet build
cd ApWifi.App
dotnet run
```

### Deployment to Raspberry Pi
1. **Build for ARM64**:
   ```sh
   dotnet publish ApWifi.App/ApWifi.App.csproj -c Release -r linux-arm64 --self-contained
   ```

2. **Upload to Raspberry Pi**:
   Upload the published files to your Raspberry Pi (e.g., to `/home/pi/ApWifi`)

3. **Configure Auto-start Service**:
   See detailed instructions in [`scripts/README.md`](scripts/README.md) for setting up the systemd service to automatically start the application on boot.

## Notes
- **Production Ready**: All system configuration features (AP setup, WiFi config, reboot) have been tested and verified on Raspberry Pi OS.
- **Root Access**: Running as root is required for network configuration and reboot operations on Raspberry Pi OS.
- **Cross-Platform**: On non-Linux platforms, system operations are skipped with informational messages for development purposes.
- **Configuration**: All settings and templates are managed via `appsettings.json` and Liquid templates in the `Templates/` directory.
- **Hotspot Name**: The AP hotspot is created with the name "RaspberryPi5-WiFiSetup" by default.
