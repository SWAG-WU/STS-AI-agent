using System.Text.Json.Serialization;

namespace AIDialogueMod.Actions;

public class GameAction
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "no_action";

    [JsonPropertyName("value")]
    public int Value { get; set; } = 0;

    [JsonPropertyName("buff_id")]
    public string? BuffId { get; set; }

    [JsonPropertyName("debuff_id")]
    public string? DebuffId { get; set; }

    [JsonPropertyName("stacks")]
    public int Stacks { get; set; } = 0;

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = "combat";

    [JsonPropertyName("card_id")]
    public string? CardId { get; set; }

    [JsonPropertyName("relic_id")]
    public string? RelicId { get; set; }

    [JsonPropertyName("percentage")]
    public int Percentage { get; set; } = 0;

    public ActionType? ParseType()
    {
        return Type switch
        {
            "modify_player_hp" => ActionType.ModifyPlayerHp,
            "modify_player_gold" => ActionType.ModifyPlayerGold,
            "modify_enemy_strength" => ActionType.ModifyEnemyStrength,
            "modify_enemy_hp" => ActionType.ModifyEnemyHp,
            "add_player_buff" => ActionType.AddPlayerBuff,
            "add_player_debuff" => ActionType.AddPlayerDebuff,
            "add_enemy_buff" => ActionType.AddEnemyBuff,
            "add_enemy_debuff" => ActionType.AddEnemyDebuff,
            "give_card" => ActionType.GiveCard,
            "destroy_card" => ActionType.DestroyCard,
            "steal_card" => ActionType.StealCard,
            "return_card" => ActionType.ReturnCard,
            "give_relic" => ActionType.GiveRelic,
            "skip_event" => ActionType.SkipEvent,
            "shop_discount" => ActionType.ShopDiscount,
            "no_action" => ActionType.NoAction,
            _ => null,
        };
    }
}
