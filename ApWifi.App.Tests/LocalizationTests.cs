using NUnit.Framework;
using ApWifi.App.Services;

namespace ApWifi.App.Tests;

[TestFixture]
public class LocalizationTests
{
    private LocalizationService _localizationService;

    [SetUp]
    public void Setup()
    {
        _localizationService = new LocalizationService();
    }    [Test]
    public void GetString_ValidKey_ReturnsTranslation()
    {
        // Arrange
        string key = "WifiConfiguration";

        // Act
        string result = _localizationService.GetString(key, "zh-CN");

        // Assert
        Assert.That(result, Is.EqualTo("树莓派 WiFi 配置"));
    }    [Test]
    public void GetString_ValidKeyEnglish_ReturnsTranslation()
    {
        // Arrange
        string key = "WifiConfiguration";

        // Act
        string result = _localizationService.GetString(key, "en-US");

        // Assert
        Assert.That(result, Is.EqualTo("Raspberry Pi WiFi Configuration"));
    }    [Test]
    public void GetString_ValidKeyJapanese_ReturnsTranslation()
    {
        // Arrange
        string key = "WifiConfiguration";

        // Act
        string result = _localizationService.GetString(key, "ja-JP");

        // Assert
        Assert.That(result, Is.EqualTo("Raspberry Pi WiFi 設定"));
    }

    [Test]
    public void GetString_InvalidKey_ReturnsKey()
    {
        // Arrange
        string key = "nonexistent_key";

        // Act
        string result = _localizationService.GetString(key, "zh-CN");

        // Assert
        Assert.That(result, Is.EqualTo(key));
    }    [Test]
    public void GetString_InvalidLanguage_FallsBackToChinese()
    {
        // Arrange
        string key = "WifiConfiguration";

        // Act
        string result = _localizationService.GetString(key, "invalid-LANG");

        // Assert
        Assert.That(result, Is.EqualTo("树莓派 WiFi 配置"));
    }    [Test]
    public void SetLanguage_ValidLanguage_UpdatesCurrentLanguage()
    {
        // Arrange
        string language = "en-US";

        // Act
        _localizationService.SetLanguage(language);
        string result = _localizationService.GetString("WifiConfiguration");

        // Assert
        Assert.That(result, Is.EqualTo("Raspberry Pi WiFi Configuration"));
    }[Test]
    public void GetAvailableLanguages_ReturnsAllLanguages()
    {
        // Act
        var languages = _localizationService.GetAvailableLanguages();

        // Assert
        Assert.That(languages.Count, Is.EqualTo(5));
        Assert.That(languages, Does.Contain("zh-CN"));
        Assert.That(languages, Does.Contain("en-US"));
        Assert.That(languages, Does.Contain("ja-JP"));
        Assert.That(languages, Does.Contain("fr-FR"));
        Assert.That(languages, Does.Contain("de-DE"));
    }    [Test]
    public void GetString_AllLanguagesForSameKey_ReturnsDifferentTranslations()
    {
        // Arrange
        string key = "SaveAndRebootButton";

        // Act & Assert
        Assert.That(_localizationService.GetString(key, "zh-CN"), Is.EqualTo("保存并重启"));
        Assert.That(_localizationService.GetString(key, "en-US"), Is.EqualTo("Save and Reboot"));
        Assert.That(_localizationService.GetString(key, "ja-JP"), Is.EqualTo("保存して再起動"));
        Assert.That(_localizationService.GetString(key, "fr-FR"), Is.EqualTo("Sauvegarder et redémarrer"));
        Assert.That(_localizationService.GetString(key, "de-DE"), Is.EqualTo("Speichern und neu starten"));
    }
}
