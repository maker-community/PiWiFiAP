<div align="center">

# 🤝 Contributing to ApWifi

**Help us make WiFi configuration on Raspberry Pi even better!**

[![Contributors](https://img.shields.io/github/contributors/maker-community/PiWiFiAP?style=for-the-badge)](https://github.com/maker-community/PiWiFiAP/graphs/contributors)
[![Good First Issues](https://img.shields.io/github/issues/maker-community/PiWiFiAP/good%20first%20issue?style=for-the-badge)](https://github.com/maker-community/PiWiFiAP/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22)

</div>

We love your input! We want to make contributing to ApWifi as easy and transparent as possible, whether it's:

- 🐛 Reporting bugs
- 🚀 Proposing new features  
- 💡 Discussing the current state of the code
- 🔧 Submitting fixes or improvements
- 🌍 Adding translations
- 📖 Improving documentation

## 🎯 Ways to Contribute

### 🐛 Bug Reports
Found a bug? Please help us fix it!

**Before reporting:**
1. Check if the issue already exists in [Issues](https://github.com/maker-community/PiWiFiAP/issues)
2. Test on the latest version
3. Gather relevant information (OS, .NET version, hardware)

**Report template:**
```markdown
**Environment:**
- Raspberry Pi Model: 
- OS Version: 
- .NET Version: 
- ApWifi Version: 

**Bug Description:**
A clear description of what the bug is.

**Steps to Reproduce:**
1. Go to '...'
2. Click on '....'
3. See error

**Expected Behavior:**
What you expected to happen.

**Screenshots:**
If applicable, add screenshots.
```

### 🚀 Feature Requests
Have an idea for improvement?

**Consider:**
- Is this feature useful for most users?
- Does it align with the project goals?
- Can it be implemented without breaking existing functionality?

### 🌍 Translations
Help make ApWifi accessible worldwide!

**Currently supported languages:**
- 🇺🇸 English (en-US)
- 🇨🇳 Chinese (zh-CN)  
- 🇩🇪 German (de-DE)
- 🇫🇷 French (fr-FR)
- 🇯🇵 Japanese (ja-JP)

**To add a new language:**
1. Create `Strings.[locale].json` in `ApWifi.App/Resources/`
2. Translate all strings from `Strings.en-US.json`
3. Test the interface with your translation
4. Submit a pull request

### 📖 Documentation
Help others use and contribute to ApWifi!

**Areas that need help:**
- API documentation
- Tutorial improvements
- Code examples
- Troubleshooting guides

## 🔧 Development Setup

### Prerequisites
- .NET 8 SDK
- Git
- (Optional) Raspberry Pi 5 for hardware testing

### Setup Steps
1. **Fork the repository**
   ```bash
   git clone https://github.com/YOUR_USERNAME/PiWiFiAP.git
   cd PiWiFiAP
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Install dependencies**
   ```bash
   dotnet restore
   ```

4. **Build the project**
   ```bash
   dotnet build
   ```

5. **Run tests**
   ```bash
   dotnet test
   ```

6. **Start development**
   ```bash
   dotnet run --project ApWifi.App
   ```

## 📝 Pull Request Process

### Before Submitting
- ✅ Code builds without warnings
- ✅ All tests pass
- ✅ Code follows project style
- ✅ Documentation updated if needed
- ✅ Screenshots added for UI changes

### PR Template
```markdown
## Description
Brief description of changes.

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Tested on development environment
- [ ] Tested on Raspberry Pi hardware
- [ ] All existing tests pass
- [ ] New tests added if applicable

## Screenshots
Include screenshots for UI changes.

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] No new warnings introduced
```

## 🎨 Code Style Guidelines

### C# Conventions
- Use PascalCase for public members
- Use camelCase for private fields
- Use meaningful variable names
- Add XML documentation for public APIs
- Follow async/await best practices

### Example:
```csharp
/// <summary>
/// Configures WiFi settings on the system
/// </summary>
/// <param name="ssid">Network SSID</param>
/// <param name="password">Network password</param>
/// <returns>True if configuration was successful</returns>
public async Task<bool> ConfigureWiFiAsync(string ssid, string password)
{
    // Implementation
}
```

### File Organization
- One class per file
- Organize using statements
- Group related functionality
- Use appropriate namespaces

## 🧪 Testing Guidelines

### Unit Tests
- Test business logic thoroughly
- Mock external dependencies
- Use descriptive test names
- Follow AAA pattern (Arrange, Act, Assert)

### Integration Tests
- Test on actual hardware when possible
- Verify network functionality
- Test multi-language support
- Validate system integration

## 📦 Release Process

### Version Numbering
We use [Semantic Versioning](https://semver.org/):
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes

### Release Checklist
- [ ] Version number updated
- [ ] CHANGELOG.md updated  
- [ ] Documentation reviewed
- [ ] Hardware testing completed
- [ ] Release notes prepared

## 🏆 Recognition

Contributors are recognized in:
- GitHub contributors list
- README.md acknowledgments
- Release notes
- Project documentation

## 💬 Getting Help

**Stuck? Need guidance?**

- 💬 [Start a Discussion](https://github.com/maker-community/PiWiFiAP/discussions)
- 📧 [Email the maintainers](mailto:gil.zhang.dev@outlook..com)
- 🐛 [Open an Issue](https://github.com/maker-community/PiWiFiAP/issues)

## 📜 License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE.txt).

---

<div align="center">

**Thank you for helping make ApWifi better! 🎉**

*Every contribution, no matter how small, makes a difference*

</div>
