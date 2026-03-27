using System.Collections.Generic;

namespace AIDialogueMod.Personality;

public static class PersonalityDescriptions
{
    public static string Get(PersonalityType type, string language)
    {
        return language == "en" ? EnDescriptions[type] : ZhDescriptions[type];
    }

    public static string GetName(PersonalityType type, string language)
    {
        return language == "en" ? EnNames[type] : ZhNames[type];
    }

    private static readonly Dictionary<PersonalityType, string> ZhNames = new()
    {
        [PersonalityType.Aggressive] = "暴躁",
        [PersonalityType.Cunning] = "狡诈",
        [PersonalityType.Greedy] = "贪财",
        [PersonalityType.Calm] = "冷静",
        [PersonalityType.Cowardly] = "胆小",
        [PersonalityType.WarAverse] = "怯战",
        [PersonalityType.Weak] = "软弱",
        [PersonalityType.Kind] = "善良",
        [PersonalityType.Gentle] = "温和",
        [PersonalityType.Generous] = "慷慨",
    };

    private static readonly Dictionary<PersonalityType, string> EnNames = new()
    {
        [PersonalityType.Aggressive] = "Aggressive",
        [PersonalityType.Cunning] = "Cunning",
        [PersonalityType.Greedy] = "Greedy",
        [PersonalityType.Calm] = "Calm",
        [PersonalityType.Cowardly] = "Cowardly",
        [PersonalityType.WarAverse] = "War-Averse",
        [PersonalityType.Weak] = "Weak",
        [PersonalityType.Kind] = "Kind",
        [PersonalityType.Gentle] = "Gentle",
        [PersonalityType.Generous] = "Generous",
    };

    private static readonly Dictionary<PersonalityType, string> ZhDescriptions = new()
    {
        [PersonalityType.Aggressive] = "你脾气火爆，容易被激怒。一旦被冒犯会立刻动手，不留情面。",
        [PersonalityType.Cunning] = "你狡猾多疑，善于识破谎言和诡计。你可能会反过来套路对方。",
        [PersonalityType.Greedy] = "你对金银财宝有强烈的欲望。可以被财物收买，但你的开价绝不低廉。",
        [PersonalityType.Calm] = "你冷静理性，不容易受情绪影响。你会根据逻辑和利弊来做判断。",
        [PersonalityType.Cowardly] = "你胆小怕事，面对强势的对手容易退缩。威胁和恐吓对你很有效。",
        [PersonalityType.WarAverse] = "你厌恶冲突和暴力，总是倾向于和平解决问题。你宁愿让步也不愿打架。",
        [PersonalityType.Weak] = "你意志软弱，容易在压力下屈服。面对强硬的态度你很难坚持立场。",
        [PersonalityType.Kind] = "你心地善良，愿意帮助他人。你可能会主动给予对方好处和支持。",
        [PersonalityType.Gentle] = "你性格温和，有耐心倾听他人的请求。你容易被真诚的话语打动和说服。",
        [PersonalityType.Generous] = "你非常慷慨大方，乐于分享自己的财物和力量。你可能会主动赠送礼物。",
    };

    private static readonly Dictionary<PersonalityType, string> EnDescriptions = new()
    {
        [PersonalityType.Aggressive] = "You have a fiery temper and are easily provoked. When offended, you strike immediately without mercy.",
        [PersonalityType.Cunning] = "You are sly and suspicious, skilled at seeing through lies and tricks. You may turn the tables on your opponent.",
        [PersonalityType.Greedy] = "You have an intense desire for gold and treasure. You can be bribed, but your asking price is never cheap.",
        [PersonalityType.Calm] = "You are cool and rational, not easily swayed by emotion. You make judgments based on logic and cost-benefit.",
        [PersonalityType.Cowardly] = "You are timid and easily intimidated. Threats and fear tactics work well on you.",
        [PersonalityType.WarAverse] = "You despise conflict and violence, always preferring peaceful resolutions. You'd rather give in than fight.",
        [PersonalityType.Weak] = "You are weak-willed and easily buckle under pressure. You struggle to hold your ground against assertiveness.",
        [PersonalityType.Kind] = "You are kindhearted and willing to help others. You may proactively offer benefits and support.",
        [PersonalityType.Gentle] = "You are gentle and patient, willing to listen. You are easily moved by sincere words.",
        [PersonalityType.Generous] = "You are extremely generous, happy to share your wealth and power. You may spontaneously give gifts.",
    };
}
