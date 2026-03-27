using Godot;

namespace AIDialogueMod.UI;

public partial class DialogueButton : Button
{
    public DialogueButton(string language)
    {
        Text = language == "en" ? "Talk" : "对话";
        CustomMinimumSize = new Vector2(100, 40);
        TooltipText = language == "en" ? "Try talking to your opponent" : "尝试与对方对话";
    }
}
