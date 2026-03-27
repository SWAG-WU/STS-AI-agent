using Godot;

namespace AIDialogueMod.UI;

public partial class ActionNotification : CenterContainer
{
    public ActionNotification(string text)
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var label = new Label();
        label.Text = $"\u26a1 {text}";
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.8f, 0.2f));
        label.AddThemeFontSizeOverride("font_size", 14);
        AddChild(label);
    }
}
