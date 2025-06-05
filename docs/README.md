<div align="center">

# 📖 ApWifi Documentation

**Comprehensive documentation for the ApWifi project**

[![Documentation](https://img.shields.io/badge/Documentation-Complete-success?style=for-the-badge)](README.md)
[![API Docs](https://img.shields.io/badge/API-Reference-blue?style=for-the-badge)](README.md)

</div>

This directory contains additional documentation for the ApWifi project, including technical details, architecture notes, and development guides.

## 📚 Available Documentation

| Document | Description | Target Audience |
|----------|-------------|-----------------|
| [`MULTILANG_README.md`](MULTILANG_README.md) | 🌍 **Multi-language Support Guide** | Developers adding new languages |
| [`REFACTORING_SUMMARY.md`](REFACTORING_SUMMARY.md) | 🔧 **Code Refactoring Documentation** | Contributors and maintainers |

## 🏗️ Architecture Overview

ApWifi follows a modular architecture designed for embedded IoT applications:

```
┌─────────────────────────────────────────────────────────────┐
│                      ApWifi Architecture                     │
├─────────────────────────────────────────────────────────────┤
│  Web Interface (Liquid Templates + Localization)            │
├─────────────────────────────────────────────────────────────┤
│  Application Layer (Program.cs + Services)                  │
├─────────────────────────────────────────────────────────────┤
│  Hardware Layer (SPI Display + Network Management)          │
├─────────────────────────────────────────────────────────────┤
│  System Layer (WiFi Config + Systemd Service)               │
└─────────────────────────────────────────────────────────────┘
```

### 🧩 Core Components

- **🖥️ Display Management**: SkiaSharp rendering + .NET IoT SPI communication
- **🌐 Network Management**: AP hotspot creation + WiFi configuration
- **🌍 Localization**: Multi-language support with automatic detection
- **⚙️ Configuration**: JSON-based settings with Liquid templating
- **🔧 System Integration**: Direct OS commands for network setup

## 🔧 Development Guidelines

### Code Style
- Follow standard C# conventions
- Use async/await for I/O operations
- Implement proper error handling and logging
- Add XML documentation for public APIs

### Testing
- Write unit tests for core functionality
- Test on actual Raspberry Pi hardware
- Verify multi-language support
- Test network configuration scenarios

### Documentation
- Update README files for significant changes
- Document new configuration options
- Include code examples for new features
- Maintain screenshot documentation

## 🚀 Getting Started for Contributors

1. **Clone the repository**
   ```bash
   git clone https://github.com/maker-community/PiWiFiAP.git
   ```

2. **Set up development environment**
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Run tests**
   ```bash
   dotnet test
   ```

4. **Start development server**
   ```bash
   dotnet run --project ApWifi.App
   ```

## 🤝 Contributing

We welcome contributions! Please see the main [README.md](../README.md) for contribution guidelines.

### Areas for Contribution
- 🌍 **Translations**: Add support for new languages
- 🔧 **Hardware Support**: Extend to other display types
- 📱 **UI/UX**: Improve web interface design
- 🐛 **Bug Fixes**: Report and fix issues
- 📖 **Documentation**: Improve guides and examples

---

<div align="center">

*For more detailed information, explore the individual documentation files or check the main [README.md](../README.md)*

</div>
