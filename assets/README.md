# Assets - Screenshots and Images

This directory contains screenshots and images demonstrating the ApWifi system in action on Raspberry Pi 5.

## File Descriptions

### System Status Screenshots

| File | Description |
|------|-------------|
| `network-not-connected.JPG` | Raspberry Pi 5 screen showing QR code when device is offline and in AP hotspot mode |
| `network-connected.JPG` | Raspberry Pi 5 screen displaying the device's IP address after successful WiFi connection |

### Web Interface Screenshots

#### English Interface
| File | Description |
|------|-------------|
| `set_ssid_en.PNG` | English WiFi configuration page showing SSID and password input form |
| `set_ssid_ok_en.PNG` | English success page showing restart notification after WiFi configuration |

#### Chinese Interface  
| File | Description |
|------|-------------|
| `set_ssid_zh.PNG` | Chinese WiFi configuration page showing SSID and password input form |
| `set_ssid_ok_zh.PNG` | Chinese success page showing restart notification after WiFi configuration |

## Usage in Documentation

These images are referenced in the main README files to demonstrate:
- The QR code display functionality when the device is offline (rendered via SkiaSharp and displayed on SPI screen)
- The web interface for WiFi configuration in multiple languages
- The success confirmation and restart process
- The final connected state with IP address display (shown on SPI screen)

All images show actual testing results on Raspberry Pi 5 running Raspberry Pi OS with the ApWifi system successfully creating the "RaspberryPi5-WiFiSetup" hotspot and managing WiFi configuration. The display output is rendered using SkiaSharp library and transmitted to an SPI-connected screen via .NET IoT libraries.