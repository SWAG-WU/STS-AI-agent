using Godot;

namespace AIDialogueMod.UI;

public partial class TypingIndicator : HBoxContainer
{
    private Label _dotsLabel;
    private double _elapsed;
    private int _dotCount;

    public TypingIndicator()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        _dotsLabel = new Label();
        _dotsLabel.Text = "...";
        _dotsLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        AddChild(_dotsLabel);
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (_elapsed >= 0.4)
        {
            _elapsed = 0;
            _dotCount = (_dotCount + 1) % 4;
            _dotsLabel.Text = new string('.', _dotCount + 1);
        }
    }
}
