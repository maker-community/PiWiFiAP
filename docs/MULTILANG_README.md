## 多语言支持实现说明

### 新增功能

本更新为树莓派WiFi配置模板添加了完整的多语言支持，包括以下功能：

#### 1. 多语言资源文件
- **中文 (zh-CN)**: `Resources/Strings.zh-CN.json`
- **英文 (en-US)**: `Resources/Strings.en-US.json`
- **日文 (ja-JP)**: `Resources/Strings.ja-JP.json`
- **法文 (fr-FR)**: `Resources/Strings.fr-FR.json`
- **德文 (de-DE)**: `Resources/Strings.de-DE.json`

#### 2. 本地化服务 (`LocalizationService`)
- 动态加载语言资源文件
- 支持语言切换
- 提供回退机制（中文 → 英文 → 键名）
- 统一的字符串获取接口

#### 3. 界面更新
- 在WiFi配置页面添加语言切换下拉菜单
- 所有界面文本都通过资源文件动态加载
- 支持URL参数语言切换 (`?lang=en-US`)
- 表单提交时保持语言设置

#### 4. 模板更新
- `wifi_form.liquid`: 添加语言切换器和多语言支持
- `wifi_success.liquid`: 支持多语言消息显示
- 使用Liquid模板语法进行本地化渲染

### 使用方法

#### 语言切换
1. **通过界面**: 使用页面顶部的语言选择下拉菜单
2. **通过URL**: 访问 `http://设备IP:5000/?lang=en-US` 等

#### 支持的语言代码
- `zh-CN`: 中文（简体）
- `en-US`: English
- `ja-JP`: 日本語
- `fr-FR`: Français
- `de-DE`: Deutsch

#### 添加新语言
1. 在 `Resources/` 目录下创建新的语言文件，如 `Strings.es-ES.json`
2. 复制现有语言文件的结构并翻译内容
3. 在 `LocalizationService.cs` 的 `GetLanguageDisplayName` 方法中添加新语言的显示名称
4. 更新项目文件 `ApWifi.App.csproj` 包含新的资源文件

### 技术实现

#### 核心组件
```csharp
LocalizationService localizationService = new();
```

#### 模板渲染
```csharp
var context = new TemplateContext();
context.SetValue("strings", localizationService.GetAllStrings());
context.SetValue("currentLanguage", localizationService.GetCurrentLanguage());
context.SetValue("languages", availableLanguages);
```

#### 语言切换处理
```csharp
var langParam = req.Query["lang"].FirstOrDefault();
if (!string.IsNullOrEmpty(langParam))
{
    localizationService.SetLanguage(langParam);
}
```

### 配置说明

项目文件中通过以下配置确保资源文件正确复制到输出目录：

```xml
<Content Include="Resources\*.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

### 兼容性

- 完全向后兼容现有功能
- 默认语言为中文 (zh-CN)
- 在无法找到指定语言时自动回退
- 保持原有的配置逻辑和重启流程
