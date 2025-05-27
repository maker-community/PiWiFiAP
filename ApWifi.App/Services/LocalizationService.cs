using System.Globalization;
using System.Text.Json;

namespace ApWifi.App.Services;

public class LocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _strings = new();
    private string _currentLanguage = "zh-CN";

    public LocalizationService()
    {
        LoadLanguages();
    }

    private void LoadLanguages()
    {
        var resourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        if (!Directory.Exists(resourcesPath))
        {
            Directory.CreateDirectory(resourcesPath);
            return;
        }

        var langFiles = Directory.GetFiles(resourcesPath, "Strings.*.json");
        foreach (var file in langFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var langCode = fileName.Replace("Strings.", "");
            
            try
            {
                var jsonContent = File.ReadAllText(file);
                var strings = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                if (strings != null)
                {
                    _strings[langCode] = strings;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading language file {file}: {ex.Message}");
            }
        }

        // 设置默认语言
        if (_strings.Count > 0 && !_strings.ContainsKey(_currentLanguage))
        {
            _currentLanguage = _strings.Keys.First();
        }
    }

    public string GetString(string key, params object[] args)
    {
        if (_strings.TryGetValue(_currentLanguage, out var currentStrings) && 
            currentStrings.TryGetValue(key, out var value))
        {
            return args.Length > 0 ? string.Format(value, args) : value;
        }

        // 回退到中文
        if (_currentLanguage != "zh-CN" && 
            _strings.TryGetValue("zh-CN", out var fallbackStrings) && 
            fallbackStrings.TryGetValue(key, out var fallbackValue))
        {
            return args.Length > 0 ? string.Format(fallbackValue, args) : fallbackValue;
        }

        // 回退到英文
        if (_currentLanguage != "en-US" && 
            _strings.TryGetValue("en-US", out var enStrings) && 
            enStrings.TryGetValue(key, out var enValue))
        {
            return args.Length > 0 ? string.Format(enValue, args) : enValue;
        }

        return key; // 如果都找不到，返回键名
    }

    public void SetLanguage(string languageCode)
    {
        if (_strings.ContainsKey(languageCode))
        {
            _currentLanguage = languageCode;
        }
    }

    public string GetCurrentLanguage() => _currentLanguage;

    public List<string> GetAvailableLanguages() => _strings.Keys.ToList();    
    public string GetLanguageDisplayName(string languageCode)
    {
        return languageCode switch
        {
            "zh-CN" => "中文",
            "en-US" => "English",
            "ja-JP" => "日本語",
            "fr-FR" => "Français",
            "de-DE" => "Deutsch",
            _ => languageCode
        };
    }

    public Dictionary<string, string> GetAllStrings()
    {
        return _strings.TryGetValue(_currentLanguage, out var strings) ? strings : new Dictionary<string, string>();
    }
}
