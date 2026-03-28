using System.Collections.Generic;
using Godot;
using AIDialogueMod.Actions;

namespace AIDialogueMod.UI;

public partial class DialoguePanel : CanvasLayer
{
    private VBoxContainer _messageContainer = null!;
    private ScrollContainer _scrollContainer = null!;
    private LineEdit _inputField = null!;
    private Button _sendButton = null!;
    private Button _abandonButton = null!;
    private Label _titleLabel = null!;
    private TypingIndicator? _typingIndicator;
    private string _characterName = "";
    private int _currentRound;
    private int _maxRounds;

    public event System.Action<string>? OnPlayerSubmit;
    public event System.Action? OnAbandon;
    public event System.Action? OnTempClose;

    public DialoguePanel() { Layer = 100; }

    public void Initialize(string characterName, int maxRounds)
    {
        _characterName = characterName;
        _maxRounds = maxRounds;
        _currentRound = 0;
        BuildUI();
    }

    private void BuildUI()
    {
        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.5f);
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        var panelContainer = new PanelContainer();
        panelContainer.SetAnchorsPreset(Control.LayoutPreset.Center);
        panelContainer.CustomMinimumSize = new Vector2(600, 450);
        panelContainer.Position = new Vector2(-300, -225);
        AddChild(panelContainer);

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 8);

        _titleLabel = new Label();
        UpdateTitle();
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _titleLabel.AddThemeFontSizeOverride("font_size", 18);
        mainVBox.AddChild(_titleLabel);
        mainVBox.AddChild(new HSeparator());

        _scrollContainer = new ScrollContainer();
        _scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _scrollContainer.CustomMinimumSize = new Vector2(0, 300);

        _messageContainer = new VBoxContainer();
        _messageContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _messageContainer.AddThemeConstantOverride("separation", 6);
        _scrollContainer.AddChild(_messageContainer);
        mainVBox.AddChild(_scrollContainer);
        mainVBox.AddChild(new HSeparator());

        var inputHBox = new HBoxContainer();
        inputHBox.AddThemeConstantOverride("separation", 8);

        _inputField = new LineEdit();
        _inputField.PlaceholderText = "输入你想说的话... / Type here...";
        _inputField.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _inputField.TextSubmitted += OnTextSubmitted;
        inputHBox.AddChild(_inputField);

        _sendButton = new Button { Text = "发送" };
        _sendButton.Pressed += OnSendPressed;
        inputHBox.AddChild(_sendButton);

        _abandonButton = new Button { Text = "放弃" };
        _abandonButton.Pressed += OnAbandonPressed;
        inputHBox.AddChild(_abandonButton);

        var tempCloseButton = new Button { Text = "临时关闭" };
        tempCloseButton.Pressed += OnTempClosePressed;
        inputHBox.AddChild(tempCloseButton);

        mainVBox.AddChild(inputHBox);
        panelContainer.AddChild(mainVBox);
    }

    public void AddPlayerMessage(string text)
    {
        _messageContainer.AddChild(new MessageBubble(text, isPlayerMessage: true));
        ScrollToBottom();
    }

    public void AddNpcMessage(string text, string emotion)
    {
        RemoveTypingIndicator();
        _messageContainer.AddChild(new MessageBubble(text, isPlayerMessage: false));
        ScrollToBottom();
    }

    public void AddActionNotification(string text)
    {
        _messageContainer.AddChild(new ActionNotification(text));
        ScrollToBottom();
    }

    public void ShowTypingIndicator()
    {
        RemoveTypingIndicator();
        _typingIndicator = new TypingIndicator();
        _messageContainer.AddChild(_typingIndicator);
        ScrollToBottom();
    }

    public void RemoveTypingIndicator()
    {
        if (_typingIndicator != null && IsInstanceValid(_typingIndicator))
        {
            _typingIndicator.QueueFree();
            _typingIndicator = null;
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputField.Editable = enabled;
        _sendButton.Disabled = !enabled;
    }

    public void SetRound(int round)
    {
        _currentRound = round;
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        _titleLabel.Text = $"与 {_characterName} 的对话 ({_currentRound}/{_maxRounds}轮)";
    }

    private void OnTextSubmitted(string text) => SubmitInput();
    private void OnSendPressed() => SubmitInput();

    private void SubmitInput()
    {
        string text = _inputField.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        _inputField.Text = "";
        OnPlayerSubmit?.Invoke(text);
    }

    private void OnAbandonPressed() => OnAbandon?.Invoke();
    private void OnTempClosePressed() => OnTempClose?.Invoke();

    private void ScrollToBottom() => CallDeferred(nameof(DeferredScrollToBottom));

    private void DeferredScrollToBottom()
    {
        _scrollContainer.ScrollVertical = (int)_scrollContainer.GetVScrollBar().MaxValue;
    }

    public void Close() => QueueFree();
}
