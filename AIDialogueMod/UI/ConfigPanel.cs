using Godot;
using AIDialogueMod.Config;

namespace AIDialogueMod.UI;

public partial class ConfigPanel : CanvasLayer
{
    private OptionButton _providerOption = null!;
    private LineEdit _apiKeyInput = null!;
    private LineEdit _apiUrlInput = null!;
    private LineEdit _modelInput = null!;
    private OptionButton _languageOption = null!;
    private ModConfig _config;

    public event System.Action? OnConfigSaved;
    public event System.Action? OnConfigCancelled;

    public ConfigPanel(ModConfig config) { _config = config; Layer = 110; BuildUI(); }

    private void BuildUI()
    {
        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.6f);
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(450, 350);
        panel.Position = new Vector2(-225, -175);
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);

        var title = new Label { Text = "AI Dialogue Mod - Settings" };
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(title);

        vbox.AddChild(CreateLabel("API Provider:"));
        _providerOption = new OptionButton();
        _providerOption.AddItem("Claude", 0);
        _providerOption.AddItem("GPT (OpenAI)", 1);
        _providerOption.AddItem("Custom", 2);
        _providerOption.Selected = _config.Provider switch { "claude" => 0, "gpt" => 1, _ => 2 };
        vbox.AddChild(_providerOption);

        vbox.AddChild(CreateLabel("API Key:"));
        _apiKeyInput = new LineEdit { PlaceholderText = "sk-...", Text = _config.ApiKey, Secret = true };
        vbox.AddChild(_apiKeyInput);

        vbox.AddChild(CreateLabel("API URL (optional):"));
        _apiUrlInput = new LineEdit { PlaceholderText = "https://api.example.com/v1/...", Text = _config.ApiUrl };
        vbox.AddChild(_apiUrlInput);

        vbox.AddChild(CreateLabel("Model (optional):"));
        _modelInput = new LineEdit { PlaceholderText = "Leave empty for default", Text = _config.Model };
        vbox.AddChild(_modelInput);

        vbox.AddChild(CreateLabel("Language / 语言:"));
        _languageOption = new OptionButton();
        _languageOption.AddItem("中文", 0);
        _languageOption.AddItem("English", 1);
        _languageOption.Selected = _config.Language == "en" ? 1 : 0;
        vbox.AddChild(_languageOption);

        var btnHBox = new HBoxContainer();
        btnHBox.AddThemeConstantOverride("separation", 12);
        btnHBox.Alignment = BoxContainer.AlignmentMode.Center;

        var saveBtn = new Button { Text = "Save / 保存" };
        saveBtn.Pressed += OnSavePressed;
        btnHBox.AddChild(saveBtn);

        var cancelBtn = new Button { Text = "Cancel / 取消" };
        cancelBtn.Pressed += OnCancelPressed;
        btnHBox.AddChild(cancelBtn);

        vbox.AddChild(btnHBox);
        panel.AddChild(vbox);
    }

    private Label CreateLabel(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 14);
        return label;
    }

    private void OnSavePressed()
    {
        _config.Provider = _providerOption.Selected switch { 0 => "claude", 1 => "gpt", _ => "custom" };
        _config.ApiKey = _apiKeyInput.Text.Trim();
        _config.ApiUrl = _apiUrlInput.Text.Trim();
        _config.Model = _modelInput.Text.Trim();
        _config.Language = _languageOption.Selected == 1 ? "en" : "zh";
        _config.Save();
        OnConfigSaved?.Invoke();
        QueueFree();
    }

    private void OnCancelPressed() { OnConfigCancelled?.Invoke(); QueueFree(); }
}
