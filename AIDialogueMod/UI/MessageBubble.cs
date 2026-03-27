using Godot;

namespace AIDialogueMod.UI;

public partial class MessageBubble : HBoxContainer
{
    private readonly bool _isPlayerMessage;

    public MessageBubble(string text, bool isPlayerMessage)
    {
        _isPlayerMessage = isPlayerMessage;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        BuildUI(text);
    }

    private void BuildUI(string text)
    {
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        panel.CustomMinimumSize = new Vector2(0, 40);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);

        var label = new RichTextLabel();
        label.BbcodeEnabled = true;
        label.FitContent = true;
        label.ScrollActive = false;
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.CustomMinimumSize = new Vector2(200, 0);
        label.Text = text;

        if (_isPlayerMessage)
        {
            AddChild(panel);
            AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        }
        else
        {
            AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
            AddChild(panel);
        }

        margin.AddChild(label);
        panel.AddChild(margin);
    }
}
