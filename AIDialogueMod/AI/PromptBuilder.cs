using System.Collections.Generic;
using System.Text;
using AIDialogueMod.Personality;

namespace AIDialogueMod.AI;

public class PromptBuilder
{
    public string BuildSystemPrompt(
        string characterName, CharacterType characterType,
        List<PersonalityType> personalities, string language,
        int playerHp, int playerMaxHp, int playerGold, string enemyInfo)
    {
        bool isChinese = language != "en";
        var sb = new StringBuilder();

        if (isChinese)
        {
            sb.AppendLine($"你是杀戮尖塔2中的一个{GetCharacterTypeName(characterType, language)}：{characterName}。");
            sb.AppendLine();
            sb.AppendLine("【你的性格】");
            foreach (var p in personalities)
            {
                sb.AppendLine($"{PersonalityDescriptions.GetName(p, language)}：{PersonalityDescriptions.Get(p, language)}");
            }
            sb.AppendLine();
            sb.AppendLine("【当前场景】");
            sb.AppendLine($"- 事件类型：{GetCharacterTypeName(characterType, language)}");
            sb.AppendLine($"- 玩家当前状态：{playerHp}/{playerMaxHp} HP，金币 {playerGold}");
            sb.AppendLine($"- 你的状态：{enemyInfo}");
            sb.AppendLine();
            sb.AppendLine("【对话规则】");
            sb.AppendLine("1. 始终保持角色扮演，用符合性格的语气说话，不要有固定句式，自由发挥");
            sb.AppendLine("2. 每轮回复必须严格使用以下JSON格式，不要输出任何其他内容：");
            sb.AppendLine("   {\"dialogue\":\"你的台词\",\"emotion\":\"情绪词\",\"actions\":[...],\"end_conversation\":false}");
            sb.AppendLine("3. actions只能从以下清单中选择：");
            sb.AppendLine(GetActionListText());
            sb.AppendLine("4. 每轮最多2个action，如果本轮没有效果就用 {\"type\":\"no_action\"}");
            sb.AppendLine("5. 增益/减益的duration默认为\"combat\"（本场战斗有效），只有赠送礼物等场景才用\"permanent\"");
            sb.AppendLine("6. destroy_card是不可逆的，极少使用，仅在叙事中有强烈理由时");
            sb.AppendLine("7. 你可以在任何时候设置end_conversation:true来结束对话");
            sb.AppendLine("8. 对话最多5轮，第5轮必须设置end_conversation:true并做出最终裁决");
            sb.AppendLine();
            sb.AppendLine("【语言】");
            sb.AppendLine("用中文回复");
        }
        else
        {
            sb.AppendLine($"You are a {GetCharacterTypeName(characterType, language)} in Slay the Spire 2: {characterName}.");
            sb.AppendLine();
            sb.AppendLine("[Your Personality]");
            foreach (var p in personalities)
            {
                sb.AppendLine($"{PersonalityDescriptions.GetName(p, language)}: {PersonalityDescriptions.Get(p, language)}");
            }
            sb.AppendLine();
            sb.AppendLine("[Current Scene]");
            sb.AppendLine($"- Event type: {GetCharacterTypeName(characterType, language)}");
            sb.AppendLine($"- Player status: {playerHp}/{playerMaxHp} HP, {playerGold} gold");
            sb.AppendLine($"- Your status: {enemyInfo}");
            sb.AppendLine();
            sb.AppendLine("[Dialogue Rules]");
            sb.AppendLine("1. Stay in character at all times. Speak in a tone matching your personality. No fixed patterns — be creative.");
            sb.AppendLine("2. Every reply MUST be strictly in this JSON format, no other text:");
            sb.AppendLine("   {\"dialogue\":\"your line\",\"emotion\":\"emotion_word\",\"actions\":[...],\"end_conversation\":false}");
            sb.AppendLine("3. actions must only use types from this list:");
            sb.AppendLine(GetActionListText());
            sb.AppendLine("4. Max 2 actions per round. Use {\"type\":\"no_action\"} if no effect this round.");
            sb.AppendLine("5. Buff/debuff duration defaults to \"combat\" (current battle only). Use \"permanent\" only for gifts.");
            sb.AppendLine("6. destroy_card is irreversible — use extremely rarely, only with strong narrative reason.");
            sb.AppendLine("7. You may set end_conversation:true at any time to end the dialogue.");
            sb.AppendLine("8. Max 5 rounds. Round 5 MUST set end_conversation:true with a final verdict.");
            sb.AppendLine();
            sb.AppendLine("[Language]");
            sb.AppendLine("Reply in English");
        }
        return sb.ToString();
    }

    public string BuildUserMessage(string playerInput, int currentRound, int maxRounds)
    {
        return $"[Round {currentRound}/{maxRounds}] Player says: {playerInput}";
    }

    public string BuildOpeningMessage(string language)
    {
        return language != "en"
            ? "[开场] 玩家走近了你，请根据你的性格说一句开场白。可以附带action。"
            : "[Opening] The player approaches you. Say an opening line based on your personality. You may include actions.";
    }

    private static string GetCharacterTypeName(CharacterType type, string language)
    {
        bool isChinese = language != "en";
        return type switch
        {
            CharacterType.Normal => isChinese ? "普通怪物" : "Monster",
            CharacterType.Elite => isChinese ? "精英怪物" : "Elite Monster",
            CharacterType.Boss => isChinese ? "Boss" : "Boss",
            CharacterType.Merchant => isChinese ? "商人" : "Merchant",
            CharacterType.EventNpc => isChinese ? "事件NPC" : "Event NPC",
            _ => isChinese ? "角色" : "Character",
        };
    }

    private static string GetActionListText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("   - modify_player_hp: value (±int)");
        sb.AppendLine("   - modify_player_gold: value (±int)");
        sb.AppendLine("   - modify_enemy_strength: value (±int)");
        sb.AppendLine("   - modify_enemy_hp: value (±int)");
        sb.AppendLine("   - add_player_buff: buff_id, stacks, duration (\"combat\"/\"permanent\"/\"turns:N\")");
        sb.AppendLine("   - add_player_debuff: debuff_id, stacks, duration");
        sb.AppendLine("   - add_enemy_buff: buff_id, stacks, duration");
        sb.AppendLine("   - add_enemy_debuff: debuff_id, stacks, duration");
        sb.AppendLine("   - give_card: card_id");
        sb.AppendLine("   - destroy_card: card_id or \"random\"");
        sb.AppendLine("   - steal_card: card_id or \"random\"");
        sb.AppendLine("   - return_card: card_id");
        sb.AppendLine("   - give_relic: relic_id");
        sb.AppendLine("   - skip_event (only when end_conversation:true)");
        sb.AppendLine("   - shop_discount: percentage (0-100)");
        sb.AppendLine("   - no_action");
        return sb.ToString();
    }
}
