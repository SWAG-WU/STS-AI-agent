# AI Dialogue Mod Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Slay the Spire 2 mod that injects a "Talk" button into all non-campfire events, allowing players to engage in AI-powered dialogue with monsters/NPCs that produces real-time game effects.

**Architecture:** HarmonyLib Postfix patches inject UI buttons after game screens load. A DialogueManager orchestrates the flow: PersonalityGenerator creates NPC traits, PromptBuilder assembles context, AIService calls the LLM API, ResponseParser extracts structured JSON, ActionValidator checks safety rules, and ActionExecutor applies effects to the game. A Godot CanvasLayer-based DialoguePanel renders the chat UI.

**Tech Stack:** C# / .NET 9.0, Godot 4.5 (Godot.NET.Sdk), HarmonyLib (0Harmony.dll bundled with STS2), System.Net.Http, System.Text.Json, xUnit (tests)

---

## File Map

```
AIDialogueMod/
├── AIDialogueMod.csproj
├── mod_manifest.json
├── mod_image.png
├── Plugin.cs
├── Config/
│   └── ModConfig.cs
├── Personality/
│   ├── PersonalityType.cs
│   ├── PersonalityGenerator.cs
│   └── PersonalityDescriptions.cs
├── AI/
│   ├── AIResponse.cs
│   ├── AIService.cs
│   ├── PromptBuilder.cs
│   └── ResponseParser.cs
├── Actions/
│   ├── ActionType.cs
│   ├── GameAction.cs
│   ├── ActionValidator.cs
│   ├── ActionExecutor.cs
│   └── StolenCardManager.cs
├── UI/
│   ├── DialogueButton.cs
│   ├── DialoguePanel.cs
│   ├── MessageBubble.cs
│   ├── ActionNotification.cs
│   ├── TypingIndicator.cs
│   └── ConfigPanel.cs
├── DialogueManager.cs
├── Patches/
│   ├── CombatUIPatch.cs
│   ├── ShopUIPatch.cs
│   ├── EventUIPatch.cs
│   └── RestSiteFilter.cs
└── pck_src/
    ├── mod_manifest.json
    └── themes/
        └── dialogue_theme.tres

AIDialogueMod.Tests/
├── AIDialogueMod.Tests.csproj
├── Personality/
│   └── PersonalityGeneratorTests.cs
├── AI/
│   ├── ResponseParserTests.cs
│   └── PromptBuilderTests.cs
├── Actions/
│   ├── ActionValidatorTests.cs
│   └── StolenCardManagerTests.cs
└── Config/
    └── ModConfigTests.cs
```

---

## Task 1: Project Scaffolding

**Files:**
- Create: `AIDialogueMod/AIDialogueMod.csproj`
- Create: `AIDialogueMod/mod_manifest.json`
- Create: `AIDialogueMod/Plugin.cs`
- Create: `AIDialogueMod.Tests/AIDialogueMod.Tests.csproj`

- [ ] **Step 1: Create the Godot mod project file**

```xml
<!-- AIDialogueMod/AIDialogueMod.csproj -->
<Project Sdk="Godot.NET.Sdk/4.5.1">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <RootNamespace>AIDialogueMod</RootNamespace>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="sts2">
      <HintPath>sts2.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create mod manifest**

```json
// AIDialogueMod/mod_manifest.json
{
    "pck_name": "AIDialogueMod",
    "name": "AI Dialogue Mod",
    "author": "ModAuthor",
    "description": "Talk your way through events! AI-powered dialogue with monsters and NPCs that affects gameplay in real-time.",
    "version": "0.1.0"
}
```

- [ ] **Step 3: Create the mod entry point**

```csharp
// AIDialogueMod/Plugin.cs
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace AIDialogueMod;

[ModInitializer("Initialize")]
public static class Plugin
{
    private static Harmony? _harmony;

    public static void Initialize()
    {
        try
        {
            _harmony = new Harmony("com.modauthor.aidialogueMod");
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Log.Warn("[AIDialogueMod] Mod loaded successfully.");
        }
        catch (System.Exception ex)
        {
            Log.Warn($"[AIDialogueMod] Failed to initialize: {ex.Message}");
        }
    }
}
```

- [ ] **Step 4: Create the test project**

```xml
<!-- AIDialogueMod.Tests/AIDialogueMod.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\AIDialogueMod\AIDialogueMod.csproj" />
  </ItemGroup>
</Project>
```

Note: The test project uses standard .NET SDK (not Godot SDK) so it can run without the Godot engine. Tests only cover pure logic classes that don't depend on Godot or STS2 types.

- [ ] **Step 5: Verify project structure compiles**

Run: `cd AIDialogueMod && dotnet build`
Expected: Build succeeds (or warnings about missing sts2.dll — that's OK for now, we'll copy it from the game later)

- [ ] **Step 6: Commit**

```bash
git add AIDialogueMod/ AIDialogueMod.Tests/
git commit -m "feat: scaffold mod project with entry point and test project"
```

---

## Task 2: Configuration System

**Files:**
- Create: `AIDialogueMod/Config/ModConfig.cs`
- Create: `AIDialogueMod.Tests/Config/ModConfigTests.cs`

- [ ] **Step 1: Write failing tests for ModConfig**

```csharp
// AIDialogueMod.Tests/Config/ModConfigTests.cs
using AIDialogueMod.Config;
using Xunit;

namespace AIDialogueMod.Tests.Config;

public class ModConfigTests
{
    [Fact]
    public void Default_config_has_empty_api_key()
    {
        var config = new ModConfig();
        Assert.Equal("", config.ApiKey);
    }

    [Fact]
    public void Default_config_language_is_chinese()
    {
        var config = new ModConfig();
        Assert.Equal("zh", config.Language);
    }

    [Fact]
    public void Default_config_provider_is_claude()
    {
        var config = new ModConfig();
        Assert.Equal("claude", config.Provider);
    }

    [Fact]
    public void IsConfigured_returns_false_when_api_key_empty()
    {
        var config = new ModConfig();
        Assert.False(config.IsConfigured);
    }

    [Fact]
    public void IsConfigured_returns_true_when_api_key_set()
    {
        var config = new ModConfig { ApiKey = "sk-test-key" };
        Assert.True(config.IsConfigured);
    }

    [Fact]
    public void Serialize_and_deserialize_roundtrip()
    {
        var config = new ModConfig
        {
            ApiKey = "sk-test",
            ApiUrl = "https://api.example.com",
            Provider = "gpt",
            Model = "gpt-4",
            Language = "en"
        };

        string json = config.ToJson();
        var restored = ModConfig.FromJson(json);

        Assert.Equal(config.ApiKey, restored.ApiKey);
        Assert.Equal(config.ApiUrl, restored.ApiUrl);
        Assert.Equal(config.Provider, restored.Provider);
        Assert.Equal(config.Model, restored.Model);
        Assert.Equal(config.Language, restored.Language);
    }

    [Fact]
    public void FromJson_returns_default_on_invalid_json()
    {
        var config = ModConfig.FromJson("not valid json{{{");
        Assert.Equal("", config.ApiKey);
        Assert.Equal("zh", config.Language);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd AIDialogueMod.Tests && dotnet test --filter "ModConfigTests"`
Expected: FAIL — `ModConfig` type does not exist

- [ ] **Step 3: Implement ModConfig**

```csharp
// AIDialogueMod/Config/ModConfig.cs
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIDialogueMod.Config;

public class ModConfig
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AIDialogueMod"
    );
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = "";

    [JsonPropertyName("apiUrl")]
    public string ApiUrl { get; set; } = "";

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "claude";

    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("language")]
    public string Language { get; set; } = "zh";

    [JsonIgnore]
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public string GetEffectiveApiUrl()
    {
        if (!string.IsNullOrWhiteSpace(ApiUrl))
            return ApiUrl;

        return Provider switch
        {
            "claude" => "https://api.anthropic.com/v1/messages",
            "gpt" => "https://api.openai.com/v1/chat/completions",
            _ => ApiUrl
        };
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    public static ModConfig FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ModConfig>(json, JsonOptions) ?? new ModConfig();
        }
        catch
        {
            return new ModConfig();
        }
    }

    public static ModConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                return FromJson(json);
            }
        }
        catch
        {
            // Fall through to default
        }
        return new ModConfig();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            File.WriteAllText(ConfigPath, ToJson());
        }
        catch
        {
            // Silently fail — config not saved is not fatal
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd AIDialogueMod.Tests && dotnet test --filter "ModConfigTests"`
Expected: All 7 tests PASS

- [ ] **Step 5: Commit**

```bash
git add AIDialogueMod/Config/ AIDialogueMod.Tests/Config/
git commit -m "feat: add configuration system with JSON persistence"
```

---

## Task 3: Personality Types & Descriptions

**Files:**
- Create: `AIDialogueMod/Personality/PersonalityType.cs`
- Create: `AIDialogueMod/Personality/PersonalityDescriptions.cs`

- [ ] **Step 1: Create personality type enum**

```csharp
// AIDialogueMod/Personality/PersonalityType.cs
namespace AIDialogueMod.Personality;

public enum PersonalityType
{
    Aggressive,   // 暴躁
    Cunning,      // 狡诈
    Greedy,       // 贪财
    Calm,         // 冷静
    Cowardly,     // 胆小
    WarAverse,    // 怯战
    Weak,         // 软弱
    Kind,         // 善良
    Gentle,       // 温和
    Generous      // 慷慨
}

public enum CharacterType
{
    Normal,       // 普通怪
    Elite,        // 精英怪
    Boss,         // Boss
    Merchant,     // 商人
    EventNpc      // 随机事件NPC
}
```

- [ ] **Step 2: Create bilingual personality descriptions**

```csharp
// AIDialogueMod/Personality/PersonalityDescriptions.cs
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
```

- [ ] **Step 3: Commit**

```bash
git add AIDialogueMod/Personality/PersonalityType.cs AIDialogueMod/Personality/PersonalityDescriptions.cs
git commit -m "feat: add personality types with bilingual descriptions"
```

---

## Task 4: Personality Generator

**Files:**
- Create: `AIDialogueMod/Personality/PersonalityGenerator.cs`
- Create: `AIDialogueMod.Tests/Personality/PersonalityGeneratorTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// AIDialogueMod.Tests/Personality/PersonalityGeneratorTests.cs
using AIDialogueMod.Personality;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AIDialogueMod.Tests.Personality;

public class PersonalityGeneratorTests
{
    [Fact]
    public void Generate_returns_exactly_two_personalities()
    {
        var gen = new PersonalityGenerator(seed: 42);
        var result = gen.Generate(CharacterType.Normal);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Generate_returns_two_different_personalities()
    {
        var gen = new PersonalityGenerator(seed: 42);
        var result = gen.Generate(CharacterType.Normal);
        Assert.NotEqual(result[0], result[1]);
    }

    [Fact]
    public void Boss_always_has_calm_or_cunning()
    {
        var gen = new PersonalityGenerator(seed: 0);
        for (int i = 0; i < 100; i++)
        {
            var result = gen.Generate(CharacterType.Boss);
            Assert.True(
                result.Contains(PersonalityType.Calm) || result.Contains(PersonalityType.Cunning),
                $"Boss got {result[0]} and {result[1]} — expected Calm or Cunning"
            );
        }
    }

    [Fact]
    public void Merchant_always_has_greedy()
    {
        var gen = new PersonalityGenerator(seed: 0);
        for (int i = 0; i < 100; i++)
        {
            var result = gen.Generate(CharacterType.Merchant);
            Assert.Contains(PersonalityType.Greedy, result);
        }
    }

    [Fact]
    public void Distribution_over_many_rolls_includes_rare_types()
    {
        var gen = new PersonalityGenerator(seed: 123);
        var counts = new Dictionary<PersonalityType, int>();

        for (int i = 0; i < 10000; i++)
        {
            var result = gen.Generate(CharacterType.Normal);
            foreach (var p in result)
                counts[p] = counts.GetValueOrDefault(p) + 1;
        }

        // Generous (~3%) should appear but less than Aggressive (~15%)
        Assert.True(counts.ContainsKey(PersonalityType.Generous), "Generous never appeared in 10000 rolls");
        Assert.True(counts.ContainsKey(PersonalityType.Aggressive), "Aggressive never appeared");
        Assert.True(counts[PersonalityType.Aggressive] > counts[PersonalityType.Generous],
            "Aggressive should appear more often than Generous");
    }

    [Fact]
    public void Elite_reduces_positive_personality_weight()
    {
        var gen = new PersonalityGenerator(seed: 456);
        int normalKindCount = 0;
        int eliteKindCount = 0;

        for (int i = 0; i < 10000; i++)
        {
            var normalResult = gen.Generate(CharacterType.Normal);
            if (normalResult.Contains(PersonalityType.Kind)) normalKindCount++;

            var eliteResult = gen.Generate(CharacterType.Elite);
            if (eliteResult.Contains(PersonalityType.Kind)) eliteKindCount++;
        }

        Assert.True(normalKindCount > eliteKindCount,
            $"Elite kind({eliteKindCount}) should appear less than normal kind({normalKindCount})");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd AIDialogueMod.Tests && dotnet test --filter "PersonalityGeneratorTests"`
Expected: FAIL — `PersonalityGenerator` type does not exist

- [ ] **Step 3: Implement PersonalityGenerator**

```csharp
// AIDialogueMod/Personality/PersonalityGenerator.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIDialogueMod.Personality;

public class PersonalityGenerator
{
    private readonly Random _random;

    // Base weights for each personality type
    private static readonly Dictionary<PersonalityType, int> BaseWeights = new()
    {
        [PersonalityType.Aggressive] = 150,
        [PersonalityType.Cunning] = 150,
        [PersonalityType.Greedy] = 150,
        [PersonalityType.Calm] = 130,
        [PersonalityType.Cowardly] = 120,
        [PersonalityType.WarAverse] = 80,
        [PersonalityType.Weak] = 70,
        [PersonalityType.Kind] = 50,
        [PersonalityType.Gentle] = 50,
        [PersonalityType.Generous] = 30,
    };

    // Positive personalities (whose weight gets reduced for elites/bosses)
    private static readonly HashSet<PersonalityType> PositiveTypes = new()
    {
        PersonalityType.Cowardly,
        PersonalityType.WarAverse,
        PersonalityType.Weak,
        PersonalityType.Kind,
        PersonalityType.Gentle,
        PersonalityType.Generous,
    };

    // Extra-positive personalities (further reduced for bosses)
    private static readonly HashSet<PersonalityType> HighValueTypes = new()
    {
        PersonalityType.Kind,
        PersonalityType.Generous,
        PersonalityType.Weak,
    };

    public PersonalityGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public List<PersonalityType> Generate(CharacterType characterType)
    {
        return characterType switch
        {
            CharacterType.Boss => GenerateBoss(),
            CharacterType.Merchant => GenerateMerchant(),
            CharacterType.Elite => GenerateWithModifiers(positiveMultiplier: 0.5, highValueMultiplier: 1.0),
            _ => GenerateWithModifiers(positiveMultiplier: 1.0, highValueMultiplier: 1.0),
        };
    }

    private List<PersonalityType> GenerateBoss()
    {
        // Boss: fixed Calm or Cunning, second is random with reduced positive weights
        var first = _random.Next(2) == 0 ? PersonalityType.Calm : PersonalityType.Cunning;
        var weights = BuildWeights(positiveMultiplier: 0.5, highValueMultiplier: 0.5);
        weights.Remove(first); // Don't pick the same one twice
        var second = PickWeighted(weights);
        return new List<PersonalityType> { first, second };
    }

    private List<PersonalityType> GenerateMerchant()
    {
        // Merchant: fixed Greedy, second is random
        var weights = BuildWeights(positiveMultiplier: 1.0, highValueMultiplier: 1.0);
        weights.Remove(PersonalityType.Greedy);
        var second = PickWeighted(weights);
        return new List<PersonalityType> { PersonalityType.Greedy, second };
    }

    private List<PersonalityType> GenerateWithModifiers(double positiveMultiplier, double highValueMultiplier)
    {
        var weights = BuildWeights(positiveMultiplier, highValueMultiplier);
        var first = PickWeighted(weights);
        weights.Remove(first);
        var second = PickWeighted(weights);
        return new List<PersonalityType> { first, second };
    }

    private Dictionary<PersonalityType, int> BuildWeights(double positiveMultiplier, double highValueMultiplier)
    {
        var weights = new Dictionary<PersonalityType, int>();
        foreach (var (type, baseWeight) in BaseWeights)
        {
            double w = baseWeight;
            if (PositiveTypes.Contains(type))
                w *= positiveMultiplier;
            if (HighValueTypes.Contains(type))
                w *= highValueMultiplier;
            weights[type] = Math.Max(1, (int)w);
        }
        return weights;
    }

    private PersonalityType PickWeighted(Dictionary<PersonalityType, int> weights)
    {
        int total = weights.Values.Sum();
        int roll = _random.Next(total);
        int cumulative = 0;
        foreach (var (type, weight) in weights)
        {
            cumulative += weight;
            if (roll < cumulative)
                return type;
        }
        return weights.Keys.Last();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd AIDialogueMod.Tests && dotnet test --filter "PersonalityGeneratorTests"`
Expected: All 6 tests PASS

- [ ] **Step 5: Commit**

```bash
git add AIDialogueMod/Personality/PersonalityGenerator.cs AIDialogueMod.Tests/Personality/
git commit -m "feat: add weighted personality generator with character-type modifiers"
```

---

## Task 5: Action Types & Data Models

**Files:**
- Create: `AIDialogueMod/Actions/ActionType.cs`
- Create: `AIDialogueMod/Actions/GameAction.cs`
- Create: `AIDialogueMod/AI/AIResponse.cs`

- [ ] **Step 1: Define action type enum**

```csharp
// AIDialogueMod/Actions/ActionType.cs
namespace AIDialogueMod.Actions;

public enum ActionType
{
    ModifyPlayerHp,
    ModifyPlayerGold,
    ModifyEnemyStrength,
    ModifyEnemyHp,
    AddPlayerBuff,
    AddPlayerDebuff,
    AddEnemyBuff,
    AddEnemyDebuff,
    GiveCard,
    DestroyCard,
    StealCard,
    ReturnCard,
    GiveRelic,
    SkipEvent,
    ShopDiscount,
    NoAction,
}
```

- [ ] **Step 2: Define the GameAction data model**

```csharp
// AIDialogueMod/Actions/GameAction.cs
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
```

- [ ] **Step 3: Define the AI response model**

```csharp
// AIDialogueMod/AI/AIResponse.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;
using AIDialogueMod.Actions;

namespace AIDialogueMod.AI;

public class AIResponse
{
    [JsonPropertyName("dialogue")]
    public string Dialogue { get; set; } = "";

    [JsonPropertyName("emotion")]
    public string Emotion { get; set; } = "neutral";

    [JsonPropertyName("actions")]
    public List<GameAction> Actions { get; set; } = new();

    [JsonPropertyName("end_conversation")]
    public bool EndConversation { get; set; } = false;
}
```

- [ ] **Step 4: Commit**

```bash
git add AIDialogueMod/Actions/ActionType.cs AIDialogueMod/Actions/GameAction.cs AIDialogueMod/AI/AIResponse.cs
git commit -m "feat: add action types, GameAction data model, and AIResponse model"
```

---

## Task 6: Action Validator

**Files:**
- Create: `AIDialogueMod/Actions/ActionValidator.cs`
- Create: `AIDialogueMod.Tests/Actions/ActionValidatorTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// AIDialogueMod.Tests/Actions/ActionValidatorTests.cs
using AIDialogueMod.Actions;
using AIDialogueMod.AI;
using AIDialogueMod.Personality;
using System.Collections.Generic;
using Xunit;

namespace AIDialogueMod.Tests.Actions;

public class ActionValidatorTests
{
    private readonly List<PersonalityType> _normalPersonalities = new()
    {
        PersonalityType.Calm, PersonalityType.Greedy
    };

    [Fact]
    public void Unknown_action_type_is_filtered_out()
    {
        var actions = new List<GameAction>
        {
            new() { Type = "unknown_action" },
            new() { Type = "no_action" }
        };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false);
        Assert.Single(result);
        Assert.Equal("no_action", result[0].Type);
    }

    [Fact]
    public void Max_two_actions_per_round()
    {
        var actions = new List<GameAction>
        {
            new() { Type = "modify_player_hp", Value = -5 },
            new() { Type = "modify_player_gold", Value = -10 },
            new() { Type = "modify_enemy_strength", Value = 3 },
        };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Skip_event_blocked_when_conversation_not_ending()
    {
        var actions = new List<GameAction>
        {
            new() { Type = "skip_event" }
        };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false);
        Assert.Empty(result);
    }

    [Fact]
    public void Skip_event_allowed_when_conversation_ending()
    {
        var actions = new List<GameAction>
        {
            new() { Type = "skip_event" }
        };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: true);
        Assert.Single(result);
    }

    [Fact]
    public void Give_relic_blocked_without_generous_personality()
    {
        var actions = new List<GameAction>
        {
            new() { Type = "give_relic", RelicId = "some_relic" }
        };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false);
        Assert.Empty(result);
    }

    [Fact]
    public void Give_relic_allowed_with_generous_personality()
    {
        var personalities = new List<PersonalityType>
        {
            PersonalityType.Generous, PersonalityType.Calm
        };
        var actions = new List<GameAction>
        {
            new() { Type = "give_relic", RelicId = "some_relic" }
        };
        var result = ActionValidator.Validate(actions, personalities, isConversationEnding: false);
        Assert.Single(result);
    }

    [Fact]
    public void Destroy_card_blocked_without_aggressive_or_cunning()
    {
        var actions = new List<GameAction>
        {
            new() { Type = "destroy_card", CardId = "strike" }
        };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false);
        Assert.Empty(result);
    }

    [Fact]
    public void Destroy_card_allowed_with_aggressive_personality()
    {
        var personalities = new List<PersonalityType>
        {
            PersonalityType.Aggressive, PersonalityType.Greedy
        };
        var actions = new List<GameAction>
        {
            new() { Type = "destroy_card", CardId = "strike" }
        };
        var result = ActionValidator.Validate(actions, personalities, isConversationEnding: false);
        Assert.Single(result);
    }

    [Fact]
    public void Hp_modification_clamped_to_minimum_1()
    {
        var actions = new List<GameAction>
        {
            new() { Type = "modify_player_hp", Value = -999 }
        };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false, playerHp: 10);
        Assert.Single(result);
        Assert.Equal(-9, result[0].Value); // 10 - 9 = 1 HP remaining
    }

    [Fact]
    public void Gold_modification_clamped_to_zero()
    {
        var actions = new List<GameAction>
        {
            new() { Type = "modify_player_gold", Value = -500 }
        };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false, playerGold: 100);
        Assert.Single(result);
        Assert.Equal(-100, result[0].Value); // Can't go below 0
    }

    [Fact]
    public void No_action_always_passes()
    {
        var actions = new List<GameAction>
        {
            new() { Type = "no_action" }
        };
        var result = ActionValidator.Validate(actions, _normalPersonalities, isConversationEnding: false);
        Assert.Single(result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd AIDialogueMod.Tests && dotnet test --filter "ActionValidatorTests"`
Expected: FAIL — `ActionValidator` type does not exist

- [ ] **Step 3: Implement ActionValidator**

```csharp
// AIDialogueMod/Actions/ActionValidator.cs
using System;
using System.Collections.Generic;
using System.Linq;
using AIDialogueMod.Personality;

namespace AIDialogueMod.Actions;

public static class ActionValidator
{
    private const int MaxActionsPerRound = 2;

    public static List<GameAction> Validate(
        List<GameAction> actions,
        List<PersonalityType> personalities,
        bool isConversationEnding,
        int playerHp = int.MaxValue,
        int playerGold = int.MaxValue)
    {
        var validated = new List<GameAction>();

        foreach (var action in actions)
        {
            if (validated.Count >= MaxActionsPerRound)
                break;

            var parsedType = action.ParseType();
            if (parsedType == null)
                continue; // Unknown action type — skip

            if (!PassesRestrictions(parsedType.Value, personalities, isConversationEnding))
                continue;

            ClampValues(action, parsedType.Value, playerHp, playerGold);
            validated.Add(action);
        }

        return validated;
    }

    private static bool PassesRestrictions(
        ActionType type,
        List<PersonalityType> personalities,
        bool isConversationEnding)
    {
        switch (type)
        {
            case ActionType.SkipEvent:
                return isConversationEnding;

            case ActionType.GiveRelic:
                return personalities.Contains(PersonalityType.Generous);

            case ActionType.DestroyCard:
                return personalities.Contains(PersonalityType.Aggressive)
                    || personalities.Contains(PersonalityType.Cunning);

            default:
                return true;
        }
    }

    private static void ClampValues(GameAction action, ActionType type, int playerHp, int playerGold)
    {
        switch (type)
        {
            case ActionType.ModifyPlayerHp:
                if (action.Value < 0 && playerHp != int.MaxValue)
                {
                    // Ensure player HP doesn't drop below 1
                    int maxDamage = playerHp - 1;
                    action.Value = Math.Max(action.Value, -maxDamage);
                }
                break;

            case ActionType.ModifyPlayerGold:
                if (action.Value < 0 && playerGold != int.MaxValue)
                {
                    // Ensure gold doesn't go below 0
                    action.Value = Math.Max(action.Value, -playerGold);
                }
                break;

            case ActionType.ShopDiscount:
                action.Percentage = Math.Clamp(action.Percentage, 0, 100);
                break;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd AIDialogueMod.Tests && dotnet test --filter "ActionValidatorTests"`
Expected: All 11 tests PASS

- [ ] **Step 5: Commit**

```bash
git add AIDialogueMod/Actions/ActionValidator.cs AIDialogueMod.Tests/Actions/
git commit -m "feat: add action validator with safety clamping and personality restrictions"
```

---

## Task 7: Stolen Card Manager

**Files:**
- Create: `AIDialogueMod/Actions/StolenCardManager.cs`
- Create: `AIDialogueMod.Tests/Actions/StolenCardManagerTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// AIDialogueMod.Tests/Actions/StolenCardManagerTests.cs
using AIDialogueMod.Actions;
using Xunit;

namespace AIDialogueMod.Tests.Actions;

public class StolenCardManagerTests
{
    [Fact]
    public void StealCard_adds_to_stolen_list()
    {
        var manager = new StolenCardManager();
        manager.StealCard("strike");
        Assert.Contains("strike", manager.GetStolenCards());
    }

    [Fact]
    public void ReturnCard_removes_from_stolen_list()
    {
        var manager = new StolenCardManager();
        manager.StealCard("strike");
        manager.ReturnCard("strike");
        Assert.DoesNotContain("strike", manager.GetStolenCards());
    }

    [Fact]
    public void ReturnCard_for_unstolen_card_does_nothing()
    {
        var manager = new StolenCardManager();
        manager.ReturnCard("strike"); // nothing stolen
        Assert.Empty(manager.GetStolenCards());
    }

    [Fact]
    public void Clear_returns_all_stolen_cards()
    {
        var manager = new StolenCardManager();
        manager.StealCard("strike");
        manager.StealCard("defend");
        var returned = manager.ClearAndReturnAll();
        Assert.Equal(2, returned.Count);
        Assert.Contains("strike", returned);
        Assert.Contains("defend", returned);
        Assert.Empty(manager.GetStolenCards());
    }

    [Fact]
    public void Multiple_steals_of_same_card_tracked_separately()
    {
        var manager = new StolenCardManager();
        manager.StealCard("strike");
        manager.StealCard("strike");
        Assert.Equal(2, manager.GetStolenCards().Count);
        manager.ReturnCard("strike"); // returns one
        Assert.Single(manager.GetStolenCards());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd AIDialogueMod.Tests && dotnet test --filter "StolenCardManagerTests"`
Expected: FAIL — `StolenCardManager` type does not exist

- [ ] **Step 3: Implement StolenCardManager**

```csharp
// AIDialogueMod/Actions/StolenCardManager.cs
using System.Collections.Generic;

namespace AIDialogueMod.Actions;

public class StolenCardManager
{
    private readonly List<string> _stolenCards = new();

    public void StealCard(string cardId)
    {
        _stolenCards.Add(cardId);
    }

    public void ReturnCard(string cardId)
    {
        _stolenCards.Remove(cardId); // Removes first occurrence
    }

    public List<string> GetStolenCards()
    {
        return new List<string>(_stolenCards);
    }

    public List<string> ClearAndReturnAll()
    {
        var cards = new List<string>(_stolenCards);
        _stolenCards.Clear();
        return cards;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd AIDialogueMod.Tests && dotnet test --filter "StolenCardManagerTests"`
Expected: All 5 tests PASS

- [ ] **Step 5: Commit**

```bash
git add AIDialogueMod/Actions/StolenCardManager.cs AIDialogueMod.Tests/Actions/StolenCardManagerTests.cs
git commit -m "feat: add stolen card manager for temporary card removal tracking"
```

---

## Task 8: Response Parser

**Files:**
- Create: `AIDialogueMod/AI/ResponseParser.cs`
- Create: `AIDialogueMod.Tests/AI/ResponseParserTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// AIDialogueMod.Tests/AI/ResponseParserTests.cs
using AIDialogueMod.AI;
using Xunit;

namespace AIDialogueMod.Tests.AI;

public class ResponseParserTests
{
    [Fact]
    public void Parses_valid_json_response()
    {
        string json = """
        {
            "dialogue": "哼，又一个不自量力的冒险者",
            "emotion": "threatening",
            "actions": [{"type": "modify_enemy_strength", "value": 3}],
            "end_conversation": false
        }
        """;
        var result = ResponseParser.Parse(json);
        Assert.Equal("哼，又一个不自量力的冒险者", result.Dialogue);
        Assert.Equal("threatening", result.Emotion);
        Assert.Single(result.Actions);
        Assert.Equal("modify_enemy_strength", result.Actions[0].Type);
        Assert.Equal(3, result.Actions[0].Value);
        Assert.False(result.EndConversation);
    }

    [Fact]
    public void Extracts_json_from_surrounding_text()
    {
        string messy = """
        Sure, here's my response:
        {"dialogue": "Hello!", "emotion": "friendly", "actions": [], "end_conversation": false}
        That's my reply.
        """;
        var result = ResponseParser.Parse(messy);
        Assert.Equal("Hello!", result.Dialogue);
    }

    [Fact]
    public void Returns_fallback_on_completely_invalid_input()
    {
        string garbage = "This is not JSON at all";
        var result = ResponseParser.Parse(garbage);
        Assert.Equal("This is not JSON at all", result.Dialogue);
        Assert.Empty(result.Actions);
        Assert.False(result.EndConversation);
    }

    [Fact]
    public void Returns_fallback_on_empty_input()
    {
        var result = ResponseParser.Parse("");
        Assert.NotNull(result);
        Assert.Empty(result.Actions);
    }

    [Fact]
    public void Handles_missing_actions_field()
    {
        string json = """{"dialogue": "Hi", "emotion": "neutral"}""";
        var result = ResponseParser.Parse(json);
        Assert.Equal("Hi", result.Dialogue);
        Assert.Empty(result.Actions);
    }

    [Fact]
    public void Handles_end_conversation_true()
    {
        string json = """
        {"dialogue": "Goodbye!", "emotion": "calm", "actions": [{"type": "skip_event"}], "end_conversation": true}
        """;
        var result = ResponseParser.Parse(json);
        Assert.True(result.EndConversation);
        Assert.Equal("skip_event", result.Actions[0].Type);
    }

    [Fact]
    public void Extracts_json_with_nested_braces()
    {
        string text = """
        Here is my response: {"dialogue": "I'll give you {power}!", "emotion": "kind", "actions": [{"type": "no_action"}], "end_conversation": false}
        """;
        var result = ResponseParser.Parse(text);
        Assert.Contains("power", result.Dialogue);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd AIDialogueMod.Tests && dotnet test --filter "ResponseParserTests"`
Expected: FAIL — `ResponseParser` type does not exist

- [ ] **Step 3: Implement ResponseParser**

```csharp
// AIDialogueMod/AI/ResponseParser.cs
using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AIDialogueMod.AI;

public static class ResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static AIResponse Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new AIResponse { Dialogue = "" };

        // Try direct parse first
        var result = TryDeserialize(raw.Trim());
        if (result != null)
            return result;

        // Try to extract JSON object from surrounding text
        string? extracted = ExtractJsonObject(raw);
        if (extracted != null)
        {
            result = TryDeserialize(extracted);
            if (result != null)
                return result;
        }

        // Fallback: treat entire text as dialogue, no actions
        return new AIResponse
        {
            Dialogue = raw.Trim(),
            Emotion = "neutral",
        };
    }

    private static AIResponse? TryDeserialize(string json)
    {
        try
        {
            var response = JsonSerializer.Deserialize<AIResponse>(json, JsonOptions);
            if (response != null && !string.IsNullOrEmpty(response.Dialogue))
                return response;
        }
        catch
        {
            // Not valid JSON
        }
        return null;
    }

    private static string? ExtractJsonObject(string text)
    {
        // Find the first '{' and match to its closing '}'
        int start = text.IndexOf('{');
        if (start < 0)
            return null;

        int depth = 0;
        bool inString = false;
        bool escaped = false;

        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
                continue;

            if (c == '{') depth++;
            else if (c == '}') depth--;

            if (depth == 0)
                return text.Substring(start, i - start + 1);
        }

        return null;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd AIDialogueMod.Tests && dotnet test --filter "ResponseParserTests"`
Expected: All 7 tests PASS

- [ ] **Step 5: Commit**

```bash
git add AIDialogueMod/AI/ResponseParser.cs AIDialogueMod.Tests/AI/
git commit -m "feat: add response parser with JSON extraction and fallback handling"
```

---

## Task 9: Prompt Builder

**Files:**
- Create: `AIDialogueMod/AI/PromptBuilder.cs`
- Create: `AIDialogueMod.Tests/AI/PromptBuilderTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// AIDialogueMod.Tests/AI/PromptBuilderTests.cs
using AIDialogueMod.AI;
using AIDialogueMod.Personality;
using System.Collections.Generic;
using Xunit;

namespace AIDialogueMod.Tests.AI;

public class PromptBuilderTests
{
    [Fact]
    public void System_prompt_contains_character_name()
    {
        var builder = new PromptBuilder();
        string prompt = builder.BuildSystemPrompt(
            characterName: "熔岩巨兽",
            characterType: CharacterType.Elite,
            personalities: new List<PersonalityType> { PersonalityType.Aggressive, PersonalityType.Greedy },
            language: "zh",
            playerHp: 50, playerMaxHp: 80, playerGold: 100,
            enemyInfo: "HP 120, 攻击力 18"
        );
        Assert.Contains("熔岩巨兽", prompt);
    }

    [Fact]
    public void System_prompt_contains_personality_descriptions()
    {
        var builder = new PromptBuilder();
        string prompt = builder.BuildSystemPrompt(
            characterName: "Lava Beast",
            characterType: CharacterType.Elite,
            personalities: new List<PersonalityType> { PersonalityType.Aggressive, PersonalityType.Greedy },
            language: "en",
            playerHp: 50, playerMaxHp: 80, playerGold: 100,
            enemyInfo: "HP 120, Strength 18"
        );
        Assert.Contains("Aggressive", prompt);
        Assert.Contains("Greedy", prompt);
        Assert.Contains("fiery temper", prompt);
    }

    [Fact]
    public void System_prompt_contains_player_state()
    {
        var builder = new PromptBuilder();
        string prompt = builder.BuildSystemPrompt(
            characterName: "Test",
            characterType: CharacterType.Normal,
            personalities: new List<PersonalityType> { PersonalityType.Calm, PersonalityType.Kind },
            language: "zh",
            playerHp: 50, playerMaxHp: 80, playerGold: 100,
            enemyInfo: "HP 60"
        );
        Assert.Contains("50/80", prompt);
        Assert.Contains("100", prompt);
    }

    [Fact]
    public void System_prompt_contains_json_format_instruction()
    {
        var builder = new PromptBuilder();
        string prompt = builder.BuildSystemPrompt(
            characterName: "Test",
            characterType: CharacterType.Normal,
            personalities: new List<PersonalityType> { PersonalityType.Calm, PersonalityType.Kind },
            language: "zh",
            playerHp: 50, playerMaxHp: 80, playerGold: 100,
            enemyInfo: "HP 60"
        );
        Assert.Contains("dialogue", prompt);
        Assert.Contains("actions", prompt);
        Assert.Contains("end_conversation", prompt);
    }

    [Fact]
    public void System_prompt_contains_action_list()
    {
        var builder = new PromptBuilder();
        string prompt = builder.BuildSystemPrompt(
            characterName: "Test",
            characterType: CharacterType.Normal,
            personalities: new List<PersonalityType> { PersonalityType.Calm, PersonalityType.Kind },
            language: "zh",
            playerHp: 50, playerMaxHp: 80, playerGold: 100,
            enemyInfo: "HP 60"
        );
        Assert.Contains("modify_player_hp", prompt);
        Assert.Contains("skip_event", prompt);
        Assert.Contains("steal_card", prompt);
    }

    [Fact]
    public void English_prompt_uses_english_language_tag()
    {
        var builder = new PromptBuilder();
        string prompt = builder.BuildSystemPrompt(
            characterName: "Test",
            characterType: CharacterType.Normal,
            personalities: new List<PersonalityType> { PersonalityType.Calm, PersonalityType.Kind },
            language: "en",
            playerHp: 50, playerMaxHp: 80, playerGold: 100,
            enemyInfo: "HP 60"
        );
        Assert.Contains("English", prompt);
    }

    [Fact]
    public void BuildUserMessage_wraps_player_input()
    {
        var builder = new PromptBuilder();
        string msg = builder.BuildUserMessage("大哥饶命！", currentRound: 2, maxRounds: 5);
        Assert.Contains("大哥饶命！", msg);
        Assert.Contains("2", msg);
        Assert.Contains("5", msg);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd AIDialogueMod.Tests && dotnet test --filter "PromptBuilderTests"`
Expected: FAIL — `PromptBuilder` type does not exist

- [ ] **Step 3: Implement PromptBuilder**

```csharp
// AIDialogueMod/AI/PromptBuilder.cs
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AIDialogueMod.Personality;

namespace AIDialogueMod.AI;

public class PromptBuilder
{
    public string BuildSystemPrompt(
        string characterName,
        CharacterType characterType,
        List<PersonalityType> personalities,
        string language,
        int playerHp,
        int playerMaxHp,
        int playerGold,
        string enemyInfo)
    {
        bool isChinese = language != "en";
        var sb = new StringBuilder();

        // Role
        if (isChinese)
        {
            sb.AppendLine($"你是杀戮尖塔2中的一个{GetCharacterTypeName(characterType, language)}：{characterName}。");
            sb.AppendLine();

            // Personality
            sb.AppendLine("【你的性格】");
            foreach (var p in personalities)
            {
                string name = PersonalityDescriptions.GetName(p, language);
                string desc = PersonalityDescriptions.Get(p, language);
                sb.AppendLine($"{name}：{desc}");
            }
            sb.AppendLine();

            // Scene
            sb.AppendLine("【当前场景】");
            sb.AppendLine($"- 事件类型：{GetCharacterTypeName(characterType, language)}");
            sb.AppendLine($"- 玩家当前状态：{playerHp}/{playerMaxHp} HP，金币 {playerGold}");
            sb.AppendLine($"- 你的状态：{enemyInfo}");
            sb.AppendLine();

            // Rules
            sb.AppendLine("【对话规则】");
            sb.AppendLine("1. 始终保持角色扮演，用符合性格的语气说话，不要有固定句式，自由发挥");
            sb.AppendLine("2. 每轮回复必须严格使用以下JSON格式，不要输出任何其他内容：");
            sb.AppendLine("""   {"dialogue":"你的台词","emotion":"情绪词","actions":[...],"end_conversation":false}""");
            sb.AppendLine("3. actions只能从以下清单中选择：");
            sb.AppendLine(GetActionListText(isChinese));
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
                string name = PersonalityDescriptions.GetName(p, language);
                string desc = PersonalityDescriptions.Get(p, language);
                sb.AppendLine($"{name}: {desc}");
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
            sb.AppendLine("""   {"dialogue":"your line","emotion":"emotion_word","actions":[...],"end_conversation":false}""");
            sb.AppendLine("3. actions must only use types from this list:");
            sb.AppendLine(GetActionListText(isChinese));
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
        if (language != "en")
            return "[开场] 玩家走近了你，请根据你的性格说一句开场白。可以附带action。";
        else
            return "[Opening] The player approaches you. Say an opening line based on your personality. You may include actions.";
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

    private static string GetActionListText(bool isChinese)
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd AIDialogueMod.Tests && dotnet test --filter "PromptBuilderTests"`
Expected: All 7 tests PASS

- [ ] **Step 5: Commit**

```bash
git add AIDialogueMod/AI/PromptBuilder.cs AIDialogueMod.Tests/AI/PromptBuilderTests.cs
git commit -m "feat: add bilingual prompt builder with full action list and rules"
```

---

## Task 10: AI Service (HTTP Client)

**Files:**
- Create: `AIDialogueMod/AI/AIService.cs`

This module makes actual HTTP calls to LLM APIs, so it cannot be meaningfully unit tested without mocking the network. We test it indirectly through integration.

- [ ] **Step 1: Implement AIService**

```csharp
// AIDialogueMod/AI/AIService.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AIDialogueMod.Config;

namespace AIDialogueMod.AI;

public class AIService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly ModConfig _config;

    public AIService(ModConfig config)
    {
        _config = config;
    }

    public async Task<string> SendMessageAsync(string systemPrompt, List<ChatMessage> conversationHistory)
    {
        try
        {
            return _config.Provider switch
            {
                "claude" => await SendClaudeAsync(systemPrompt, conversationHistory),
                "gpt" => await SendOpenAIAsync(systemPrompt, conversationHistory),
                _ => await SendOpenAIAsync(systemPrompt, conversationHistory), // Default to OpenAI-compatible format
            };
        }
        catch (TaskCanceledException)
        {
            return ""; // Timeout
        }
        catch (Exception)
        {
            return ""; // Any error
        }
    }

    private async Task<string> SendClaudeAsync(string systemPrompt, List<ChatMessage> messages)
    {
        var body = new
        {
            model = string.IsNullOrEmpty(_config.Model) ? "claude-sonnet-4-20250514" : _config.Model,
            max_tokens = 512,
            system = systemPrompt,
            messages = messages,
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _config.GetEffectiveApiUrl())
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", _config.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var response = await Http.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? "";
    }

    private async Task<string> SendOpenAIAsync(string systemPrompt, List<ChatMessage> messages)
    {
        var allMessages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };
        foreach (var msg in messages)
        {
            allMessages.Add(new { role = msg.Role, content = msg.Content });
        }

        var body = new
        {
            model = string.IsNullOrEmpty(_config.Model) ? "gpt-4o" : _config.Model,
            max_tokens = 512,
            messages = allMessages,
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _config.GetEffectiveApiUrl())
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");

        var response = await Http.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }
}

public class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}
```

- [ ] **Step 2: Commit**

```bash
git add AIDialogueMod/AI/AIService.cs
git commit -m "feat: add AI service with Claude and OpenAI-compatible API support"
```

---

## Task 11: Dialogue Manager (State Machine)

**Files:**
- Create: `AIDialogueMod/DialogueManager.cs`

This is the orchestrator that ties all modules together. It depends on Godot types for async/signal handling, so it's tested through integration rather than unit tests.

- [ ] **Step 1: Implement DialogueManager**

```csharp
// AIDialogueMod/DialogueManager.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIDialogueMod.Actions;
using AIDialogueMod.AI;
using AIDialogueMod.Config;
using AIDialogueMod.Personality;
using MegaCrit.Sts2.Core.Logging;

namespace AIDialogueMod;

public enum DialogueState
{
    Idle,
    WaitingForPlayer,
    WaitingForAI,
    Ended,
}

public class DialogueManager
{
    public const int MaxRounds = 5;

    private readonly AIService _aiService;
    private readonly PromptBuilder _promptBuilder;
    private readonly PersonalityGenerator _personalityGenerator;
    private readonly StolenCardManager _stolenCardManager;

    private string _systemPrompt = "";
    private List<ChatMessage> _conversationHistory = new();
    private List<PersonalityType> _currentPersonalities = new();
    private int _currentRound;

    public DialogueState State { get; private set; } = DialogueState.Idle;
    public int CurrentRound => _currentRound;
    public List<PersonalityType> CurrentPersonalities => _currentPersonalities;
    public StolenCardManager StolenCards => _stolenCardManager;

    // Events for UI binding
    public event Action<string, string>? OnNpcMessage;          // dialogue, emotion
    public event Action<List<GameAction>>? OnActionsExecuted;   // validated actions
    public event Action? OnConversationEnded;
    public event Action? OnWaitingForAI;

    public DialogueManager(ModConfig config)
    {
        _aiService = new AIService(config);
        _promptBuilder = new PromptBuilder();
        _personalityGenerator = new PersonalityGenerator();
        _stolenCardManager = new StolenCardManager();
    }

    public async Task StartDialogue(
        string characterName,
        CharacterType characterType,
        string language,
        int playerHp, int playerMaxHp, int playerGold,
        string enemyInfo)
    {
        try
        {
            _currentRound = 0;
            _conversationHistory = new List<ChatMessage>();
            _currentPersonalities = _personalityGenerator.Generate(characterType);

            _systemPrompt = _promptBuilder.BuildSystemPrompt(
                characterName, characterType, _currentPersonalities,
                language, playerHp, playerMaxHp, playerGold, enemyInfo
            );

            // Round 0: AI opening line
            State = DialogueState.WaitingForAI;
            OnWaitingForAI?.Invoke();

            string openingPrompt = _promptBuilder.BuildOpeningMessage(language);
            _conversationHistory.Add(new ChatMessage { Role = "user", Content = openingPrompt });

            string rawResponse = await _aiService.SendMessageAsync(_systemPrompt, _conversationHistory);
            HandleAIResponse(rawResponse, isOpening: true);
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] StartDialogue failed: {ex.Message}");
            EndConversation();
        }
    }

    public async Task SendPlayerMessage(string message, int playerHp, int playerGold)
    {
        if (State != DialogueState.WaitingForPlayer)
            return;

        try
        {
            _currentRound++;
            State = DialogueState.WaitingForAI;
            OnWaitingForAI?.Invoke();

            string userMsg = _promptBuilder.BuildUserMessage(message, _currentRound, MaxRounds);
            _conversationHistory.Add(new ChatMessage { Role = "user", Content = userMsg });

            string rawResponse = await _aiService.SendMessageAsync(_systemPrompt, _conversationHistory);
            HandleAIResponse(rawResponse, isOpening: false, playerHp: playerHp, playerGold: playerGold);
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] SendPlayerMessage failed: {ex.Message}");
            // Show a fallback message rather than crashing
            OnNpcMessage?.Invoke(
                _systemPrompt.Contains("中文") ? "（对方沉默了一会...）" : "(They are silent for a moment...)",
                "neutral"
            );
            State = DialogueState.WaitingForPlayer;

            if (_currentRound >= MaxRounds)
                EndConversation();
        }
    }

    public void AbandonDialogue()
    {
        EndConversation();
    }

    private void HandleAIResponse(string rawResponse, bool isOpening, int playerHp = int.MaxValue, int playerGold = int.MaxValue)
    {
        AIResponse response;

        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            // API failure fallback
            response = new AIResponse
            {
                Dialogue = _systemPrompt.Contains("中文") ? "（对方沉默了一会...）" : "(They are silent for a moment...)",
                Emotion = "neutral",
            };
        }
        else
        {
            response = ResponseParser.Parse(rawResponse);
        }

        // Add AI response to history
        _conversationHistory.Add(new ChatMessage
        {
            Role = "assistant",
            Content = rawResponse
        });

        // Validate actions
        bool isEnding = response.EndConversation || _currentRound >= MaxRounds;
        var validatedActions = ActionValidator.Validate(
            response.Actions, _currentPersonalities, isEnding, playerHp, playerGold
        );

        // Fire events
        OnNpcMessage?.Invoke(response.Dialogue, response.Emotion);

        if (validatedActions.Count > 0)
            OnActionsExecuted?.Invoke(validatedActions);

        // State transition
        if (isEnding)
        {
            EndConversation();
        }
        else
        {
            State = DialogueState.WaitingForPlayer;
        }
    }

    private void EndConversation()
    {
        State = DialogueState.Ended;
        OnConversationEnded?.Invoke();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add AIDialogueMod/DialogueManager.cs
git commit -m "feat: add dialogue manager state machine orchestrating the full conversation flow"
```

---

## Task 12: UI — Message Bubble & Action Notification

**Files:**
- Create: `AIDialogueMod/UI/MessageBubble.cs`
- Create: `AIDialogueMod/UI/ActionNotification.cs`
- Create: `AIDialogueMod/UI/TypingIndicator.cs`

These are Godot UI components — tested visually in-game.

- [ ] **Step 1: Implement MessageBubble**

```csharp
// AIDialogueMod/UI/MessageBubble.cs
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
        // Panel for the message bubble
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

        if (_isPlayerMessage)
        {
            label.Text = text;
            // Player on left: add spacer on right to push left
            AddChild(panel);
            AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        }
        else
        {
            label.Text = text;
            // NPC on right: add spacer on left to push right
            AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
            AddChild(panel);
        }

        margin.AddChild(label);
        panel.AddChild(margin);
    }
}
```

- [ ] **Step 2: Implement ActionNotification**

```csharp
// AIDialogueMod/UI/ActionNotification.cs
using Godot;

namespace AIDialogueMod.UI;

public partial class ActionNotification : CenterContainer
{
    public ActionNotification(string text)
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var label = new Label();
        label.Text = $"⚡ {text}";
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.8f, 0.2f)); // Yellow/orange
        label.AddThemeFontSizeOverride("font_size", 14);

        AddChild(label);
    }
}
```

- [ ] **Step 3: Implement TypingIndicator**

```csharp
// AIDialogueMod/UI/TypingIndicator.cs
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

        // Push to right side (NPC side)
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
```

- [ ] **Step 4: Commit**

```bash
git add AIDialogueMod/UI/MessageBubble.cs AIDialogueMod/UI/ActionNotification.cs AIDialogueMod/UI/TypingIndicator.cs
git commit -m "feat: add message bubble, action notification, and typing indicator UI components"
```

---

## Task 13: UI — Dialogue Panel

**Files:**
- Create: `AIDialogueMod/UI/DialoguePanel.cs`

- [ ] **Step 1: Implement DialoguePanel**

```csharp
// AIDialogueMod/UI/DialoguePanel.cs
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

    public DialoguePanel()
    {
        Layer = 100; // Above game UI
    }

    public void Initialize(string characterName, int maxRounds)
    {
        _characterName = characterName;
        _maxRounds = maxRounds;
        _currentRound = 0;
        BuildUI();
    }

    private void BuildUI()
    {
        // Semi-transparent background overlay
        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.5f);
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        // Main panel
        var panelContainer = new PanelContainer();
        panelContainer.SetAnchorsPreset(Control.LayoutPreset.Center);
        panelContainer.CustomMinimumSize = new Vector2(600, 450);
        panelContainer.Position = new Vector2(-300, -225);
        AddChild(panelContainer);

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 8);

        // Title bar
        _titleLabel = new Label();
        UpdateTitle();
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _titleLabel.AddThemeFontSizeOverride("font_size", 18);
        mainVBox.AddChild(_titleLabel);

        // Separator
        mainVBox.AddChild(new HSeparator());

        // Scrollable message area
        _scrollContainer = new ScrollContainer();
        _scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _scrollContainer.CustomMinimumSize = new Vector2(0, 300);

        _messageContainer = new VBoxContainer();
        _messageContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _messageContainer.AddThemeConstantOverride("separation", 6);
        _scrollContainer.AddChild(_messageContainer);
        mainVBox.AddChild(_scrollContainer);

        // Separator
        mainVBox.AddChild(new HSeparator());

        // Input area
        var inputHBox = new HBoxContainer();
        inputHBox.AddThemeConstantOverride("separation", 8);

        _inputField = new LineEdit();
        _inputField.PlaceholderText = "输入你想说的话... / Type here...";
        _inputField.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _inputField.TextSubmitted += OnTextSubmitted;
        inputHBox.AddChild(_inputField);

        _sendButton = new Button();
        _sendButton.Text = "发送";
        _sendButton.Pressed += OnSendPressed;
        inputHBox.AddChild(_sendButton);

        _abandonButton = new Button();
        _abandonButton.Text = "放弃";
        _abandonButton.Pressed += OnAbandonPressed;
        inputHBox.AddChild(_abandonButton);

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

    private void OnTextSubmitted(string text)
    {
        SubmitInput();
    }

    private void OnSendPressed()
    {
        SubmitInput();
    }

    private void SubmitInput()
    {
        string text = _inputField.Text.Trim();
        if (string.IsNullOrEmpty(text))
            return;

        _inputField.Text = "";
        OnPlayerSubmit?.Invoke(text);
    }

    private void OnAbandonPressed()
    {
        OnAbandon?.Invoke();
    }

    private void ScrollToBottom()
    {
        // Defer to next frame so layout is updated
        CallDeferred(nameof(DeferredScrollToBottom));
    }

    private void DeferredScrollToBottom()
    {
        _scrollContainer.ScrollVertical = (int)_scrollContainer.GetVScrollBar().MaxValue;
    }

    public void Close()
    {
        QueueFree();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add AIDialogueMod/UI/DialoguePanel.cs
git commit -m "feat: add dialogue panel with scrollable chat, input field, and send/abandon buttons"
```

---

## Task 14: UI — Dialogue Button & Config Panel

**Files:**
- Create: `AIDialogueMod/UI/DialogueButton.cs`
- Create: `AIDialogueMod/UI/ConfigPanel.cs`

- [ ] **Step 1: Implement DialogueButton**

```csharp
// AIDialogueMod/UI/DialogueButton.cs
using Godot;

namespace AIDialogueMod.UI;

public partial class DialogueButton : Button
{
    public DialogueButton(string language)
    {
        Text = language == "en" ? "Talk" : "对话";
        CustomMinimumSize = new Vector2(100, 40);
        TooltipText = language == "en"
            ? "Try talking to your opponent"
            : "尝试与对方对话";
    }
}
```

- [ ] **Step 2: Implement ConfigPanel**

```csharp
// AIDialogueMod/UI/ConfigPanel.cs
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

    public ConfigPanel(ModConfig config)
    {
        _config = config;
        Layer = 110; // Above dialogue panel
        BuildUI();
    }

    private void BuildUI()
    {
        // Background overlay
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

        // Title
        var title = new Label();
        title.Text = "AI Dialogue Mod - Settings";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(title);

        // Provider
        vbox.AddChild(CreateLabel("API Provider:"));
        _providerOption = new OptionButton();
        _providerOption.AddItem("Claude", 0);
        _providerOption.AddItem("GPT (OpenAI)", 1);
        _providerOption.AddItem("Custom", 2);
        _providerOption.Selected = _config.Provider switch
        {
            "claude" => 0,
            "gpt" => 1,
            _ => 2,
        };
        vbox.AddChild(_providerOption);

        // API Key
        vbox.AddChild(CreateLabel("API Key:"));
        _apiKeyInput = new LineEdit();
        _apiKeyInput.PlaceholderText = "sk-...";
        _apiKeyInput.Text = _config.ApiKey;
        _apiKeyInput.Secret = true;
        vbox.AddChild(_apiKeyInput);

        // API URL (optional)
        vbox.AddChild(CreateLabel("API URL (optional, for custom endpoints):"));
        _apiUrlInput = new LineEdit();
        _apiUrlInput.PlaceholderText = "https://api.example.com/v1/...";
        _apiUrlInput.Text = _config.ApiUrl;
        vbox.AddChild(_apiUrlInput);

        // Model (optional)
        vbox.AddChild(CreateLabel("Model (optional):"));
        _modelInput = new LineEdit();
        _modelInput.PlaceholderText = "Leave empty for default";
        _modelInput.Text = _config.Model;
        vbox.AddChild(_modelInput);

        // Language
        vbox.AddChild(CreateLabel("Language / 语言:"));
        _languageOption = new OptionButton();
        _languageOption.AddItem("中文", 0);
        _languageOption.AddItem("English", 1);
        _languageOption.Selected = _config.Language == "en" ? 1 : 0;
        vbox.AddChild(_languageOption);

        // Buttons
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
        _config.Provider = _providerOption.Selected switch
        {
            0 => "claude",
            1 => "gpt",
            _ => "custom",
        };
        _config.ApiKey = _apiKeyInput.Text.Trim();
        _config.ApiUrl = _apiUrlInput.Text.Trim();
        _config.Model = _modelInput.Text.Trim();
        _config.Language = _languageOption.Selected == 1 ? "en" : "zh";

        _config.Save();
        OnConfigSaved?.Invoke();
        QueueFree();
    }

    private void OnCancelPressed()
    {
        OnConfigCancelled?.Invoke();
        QueueFree();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add AIDialogueMod/UI/DialogueButton.cs AIDialogueMod/UI/ConfigPanel.cs
git commit -m "feat: add dialogue trigger button and API configuration panel"
```

---

## Task 15: Action Executor (Game Integration)

**Files:**
- Create: `AIDialogueMod/Actions/ActionExecutor.cs`

This module interacts with STS2 game types from `sts2.dll`. The exact API calls depend on what's available in the decompiled DLL. This implementation uses placeholder method names that will need to be adjusted after inspecting `sts2.dll` with ILSpy.

- [ ] **Step 1: Implement ActionExecutor**

```csharp
// AIDialogueMod/Actions/ActionExecutor.cs
using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;

namespace AIDialogueMod.Actions;

public class ActionExecutor
{
    private readonly StolenCardManager _stolenCardManager;

    public ActionExecutor(StolenCardManager stolenCardManager)
    {
        _stolenCardManager = stolenCardManager;
    }

    /// <summary>
    /// Execute a list of validated game actions.
    /// Returns human-readable descriptions of what happened (for UI notifications).
    /// </summary>
    public List<string> Execute(List<GameAction> actions, string language)
    {
        var notifications = new List<string>();
        bool isChinese = language != "en";

        foreach (var action in actions)
        {
            try
            {
                string? notification = ExecuteSingle(action, isChinese);
                if (notification != null)
                    notifications.Add(notification);
            }
            catch (Exception ex)
            {
                Log.Warn($"[AIDialogueMod] Action execution failed for {action.Type}: {ex.Message}");
            }
        }

        return notifications;
    }

    private string? ExecuteSingle(GameAction action, bool isChinese)
    {
        var type = action.ParseType();
        if (type == null)
            return null;

        // NOTE: The actual game API calls below are placeholders.
        // After decompiling sts2.dll with ILSpy, replace these with the real API.
        // The method signatures will look something like:
        //   MegaCrit.Sts2.Core.Game.Player.CurrentHp
        //   MegaCrit.Sts2.Core.Game.Player.Gold
        //   MegaCrit.Sts2.Core.Combat.CombatManager.CurrentEnemy
        // For now, these just log and return notification text.

        return type.Value switch
        {
            ActionType.ModifyPlayerHp => ExecuteModifyPlayerHp(action, isChinese),
            ActionType.ModifyPlayerGold => ExecuteModifyPlayerGold(action, isChinese),
            ActionType.ModifyEnemyStrength => ExecuteModifyEnemyStrength(action, isChinese),
            ActionType.ModifyEnemyHp => ExecuteModifyEnemyHp(action, isChinese),
            ActionType.AddPlayerBuff => ExecuteAddBuff(action, true, true, isChinese),
            ActionType.AddPlayerDebuff => ExecuteAddBuff(action, true, false, isChinese),
            ActionType.AddEnemyBuff => ExecuteAddBuff(action, false, true, isChinese),
            ActionType.AddEnemyDebuff => ExecuteAddBuff(action, false, false, isChinese),
            ActionType.GiveCard => ExecuteGiveCard(action, isChinese),
            ActionType.DestroyCard => ExecuteDestroyCard(action, isChinese),
            ActionType.StealCard => ExecuteStealCard(action, isChinese),
            ActionType.ReturnCard => ExecuteReturnCard(action, isChinese),
            ActionType.GiveRelic => ExecuteGiveRelic(action, isChinese),
            ActionType.SkipEvent => ExecuteSkipEvent(isChinese),
            ActionType.ShopDiscount => ExecuteShopDiscount(action, isChinese),
            ActionType.NoAction => null,
            _ => null,
        };
    }

    private string ExecuteModifyPlayerHp(GameAction action, bool isChinese)
    {
        // TODO: Replace with actual game API
        // Example: GameManager.Instance.Player.ModifyHp(action.Value);
        Log.Warn($"[AIDialogueMod] ModifyPlayerHp: {action.Value}");

        if (action.Value > 0)
            return isChinese ? $"玩家恢复了 {action.Value} HP" : $"Player healed {action.Value} HP";
        else
            return isChinese ? $"玩家受到了 {-action.Value} 点伤害" : $"Player took {-action.Value} damage";
    }

    private string ExecuteModifyPlayerGold(GameAction action, bool isChinese)
    {
        Log.Warn($"[AIDialogueMod] ModifyPlayerGold: {action.Value}");

        if (action.Value > 0)
            return isChinese ? $"获得了 {action.Value} 金币" : $"Gained {action.Value} gold";
        else
            return isChinese ? $"失去了 {-action.Value} 金币" : $"Lost {-action.Value} gold";
    }

    private string ExecuteModifyEnemyStrength(GameAction action, bool isChinese)
    {
        Log.Warn($"[AIDialogueMod] ModifyEnemyStrength: {action.Value}");

        if (action.Value > 0)
            return isChinese ? $"怪物攻击力 +{action.Value}" : $"Enemy strength +{action.Value}";
        else
            return isChinese ? $"怪物攻击力 {action.Value}" : $"Enemy strength {action.Value}";
    }

    private string ExecuteModifyEnemyHp(GameAction action, bool isChinese)
    {
        Log.Warn($"[AIDialogueMod] ModifyEnemyHp: {action.Value}");

        if (action.Value > 0)
            return isChinese ? $"怪物恢复了 {action.Value} HP" : $"Enemy healed {action.Value} HP";
        else
            return isChinese ? $"怪物受到了 {-action.Value} 点伤害" : $"Enemy took {-action.Value} damage";
    }

    private string ExecuteAddBuff(GameAction action, bool isPlayer, bool isBuff, bool isChinese)
    {
        string id = isBuff ? (action.BuffId ?? "unknown") : (action.DebuffId ?? "unknown");
        string target = isPlayer
            ? (isChinese ? "玩家" : "Player")
            : (isChinese ? "怪物" : "Enemy");
        string effect = isBuff
            ? (isChinese ? "增益" : "buff")
            : (isChinese ? "减益" : "debuff");

        Log.Warn($"[AIDialogueMod] Add{(isBuff ? "Buff" : "Debuff")} to {(isPlayer ? "player" : "enemy")}: {id} x{action.Stacks} ({action.Duration})");

        return isChinese
            ? $"{target}获得{effect}：{id} x{action.Stacks}（{FormatDuration(action.Duration, true)}）"
            : $"{target} gained {effect}: {id} x{action.Stacks} ({FormatDuration(action.Duration, false)})";
    }

    private string ExecuteGiveCard(GameAction action, bool isChinese)
    {
        string cardId = action.CardId ?? "unknown";
        Log.Warn($"[AIDialogueMod] GiveCard: {cardId}");
        return isChinese ? $"获得卡牌：{cardId}" : $"Received card: {cardId}";
    }

    private string ExecuteDestroyCard(GameAction action, bool isChinese)
    {
        string cardId = action.CardId ?? "random";
        Log.Warn($"[AIDialogueMod] DestroyCard: {cardId}");
        return isChinese ? $"卡牌被永久销毁：{cardId}" : $"Card permanently destroyed: {cardId}";
    }

    private string ExecuteStealCard(GameAction action, bool isChinese)
    {
        string cardId = action.CardId ?? "random";
        _stolenCardManager.StealCard(cardId);
        Log.Warn($"[AIDialogueMod] StealCard: {cardId}");
        return isChinese ? $"卡牌被暂时夺取：{cardId}（事件结束后归还）" : $"Card temporarily stolen: {cardId} (returned after event)";
    }

    private string ExecuteReturnCard(GameAction action, bool isChinese)
    {
        string cardId = action.CardId ?? "";
        _stolenCardManager.ReturnCard(cardId);
        Log.Warn($"[AIDialogueMod] ReturnCard: {cardId}");
        return isChinese ? $"卡牌已归还：{cardId}" : $"Card returned: {cardId}";
    }

    private string ExecuteGiveRelic(GameAction action, bool isChinese)
    {
        string relicId = action.RelicId ?? "unknown";
        Log.Warn($"[AIDialogueMod] GiveRelic: {relicId}");
        return isChinese ? $"获得遗物：{relicId}" : $"Received relic: {relicId}";
    }

    private string ExecuteSkipEvent(bool isChinese)
    {
        Log.Warn("[AIDialogueMod] SkipEvent");
        return isChinese ? "事件被跳过！" : "Event skipped!";
    }

    private string ExecuteShopDiscount(GameAction action, bool isChinese)
    {
        Log.Warn($"[AIDialogueMod] ShopDiscount: {action.Percentage}%");
        return isChinese ? $"商店全场 {action.Percentage}% 折扣！" : $"Shop {action.Percentage}% discount!";
    }

    private static string FormatDuration(string duration, bool isChinese)
    {
        if (duration == "combat")
            return isChinese ? "本场战斗" : "this combat";
        if (duration == "permanent")
            return isChinese ? "永久" : "permanent";
        if (duration.StartsWith("turns:") && int.TryParse(duration[6..], out int turns))
            return isChinese ? $"{turns}回合" : $"{turns} turns";
        return duration;
    }

    /// <summary>
    /// Call when an event/combat ends to return all stolen cards.
    /// </summary>
    public List<string> OnEventEnd(string language)
    {
        var returned = _stolenCardManager.ClearAndReturnAll();
        var notifications = new List<string>();
        bool isChinese = language != "en";

        foreach (var cardId in returned)
        {
            Log.Warn($"[AIDialogueMod] Auto-returning stolen card: {cardId}");
            // TODO: Replace with actual game API to add card back to deck
            notifications.Add(isChinese ? $"被偷的卡牌已归还：{cardId}" : $"Stolen card returned: {cardId}");
        }

        return notifications;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add AIDialogueMod/Actions/ActionExecutor.cs
git commit -m "feat: add action executor with placeholder game API calls and notification text"
```

---

## Task 16: Harmony Patches

**Files:**
- Create: `AIDialogueMod/Patches/CombatUIPatch.cs`
- Create: `AIDialogueMod/Patches/ShopUIPatch.cs`
- Create: `AIDialogueMod/Patches/EventUIPatch.cs`
- Create: `AIDialogueMod/Patches/RestSiteFilter.cs`

These patches require knowledge of exact class/method names from `sts2.dll`. The structure below uses placeholder target types. After decompiling `sts2.dll` with ILSpy, replace `TARGET_CLASS` and `TARGET_METHOD` with the real names.

- [ ] **Step 1: Implement CombatUIPatch**

```csharp
// AIDialogueMod/Patches/CombatUIPatch.cs
using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using AIDialogueMod.Config;
using AIDialogueMod.UI;

namespace AIDialogueMod.Patches;

/// <summary>
/// Injects a "Talk" button next to the "End Turn" button in combat.
/// TODO: After decompiling sts2.dll, replace the HarmonyPatch attribute
/// with the actual combat UI class and its initialization method.
/// Example: [HarmonyPatch(typeof(CombatUI), "OnReady")]
/// </summary>
// [HarmonyPatch(typeof(TARGET_COMBAT_UI_CLASS), "TARGET_METHOD")]
public static class CombatUIPatch
{
    public static void Postfix(object __instance)
    {
        try
        {
            var config = ModConfig.Load();
            if (!config.IsConfigured)
                return;

            // __instance should be a Godot Node (the combat UI root)
            if (__instance is not Node uiRoot)
                return;

            // TODO: Find the actual "End Turn" button node path by inspecting the scene tree
            // Example: var endTurnBtn = uiRoot.GetNode<Button>("EndTurnButton");
            // Then add our button as a sibling

            var dialogueBtn = new DialogueButton(config.Language);
            dialogueBtn.Pressed += () => OnDialoguePressed(config);

            // TODO: Add button to the correct parent in the scene tree
            // Example: endTurnBtn.GetParent().AddChild(dialogueBtn);

            Log.Warn("[AIDialogueMod] Combat dialogue button injected.");
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] CombatUIPatch failed: {ex.Message}");
            // Fail silently — don't break the game
        }
    }

    private static void OnDialoguePressed(ModConfig config)
    {
        try
        {
            if (!config.IsConfigured)
            {
                // Show config panel
                var sceneTree = Engine.GetMainLoop() as SceneTree;
                var configPanel = new ConfigPanel(config);
                sceneTree?.Root.AddChild(configPanel);
                return;
            }

            // TODO: Get character name, type, player/enemy stats from game API
            // Example:
            // var enemy = CombatManager.Instance.CurrentEnemy;
            // var player = GameManager.Instance.Player;

            var manager = new DialogueManager(config);
            var panel = new DialoguePanel();
            var sceneTree2 = Engine.GetMainLoop() as SceneTree;
            sceneTree2?.Root.AddChild(panel);

            panel.Initialize("Unknown Enemy", DialogueManager.MaxRounds);

            // Wire events
            var executor = new Actions.ActionExecutor(manager.StolenCards);

            panel.OnPlayerSubmit += async (text) =>
            {
                panel.AddPlayerMessage(text);
                panel.SetInputEnabled(false);
                panel.ShowTypingIndicator();
                // TODO: Get real playerHp and playerGold from game
                await manager.SendPlayerMessage(text, playerHp: 50, playerGold: 100);
            };

            panel.OnAbandon += () =>
            {
                manager.AbandonDialogue();
                executor.OnEventEnd(config.Language);
                panel.Close();
            };

            manager.OnNpcMessage += (dialogue, emotion) =>
            {
                panel.AddNpcMessage(dialogue, emotion);
                panel.SetRound(manager.CurrentRound);
                panel.SetInputEnabled(true);
            };

            manager.OnActionsExecuted += (actions) =>
            {
                var notifications = executor.Execute(actions, config.Language);
                foreach (var n in notifications)
                    panel.AddActionNotification(n);
            };

            manager.OnWaitingForAI += () =>
            {
                panel.ShowTypingIndicator();
                panel.SetInputEnabled(false);
            };

            manager.OnConversationEnded += () =>
            {
                executor.OnEventEnd(config.Language);
                panel.SetInputEnabled(false);
                // TODO: Apply skip_event if that action was in the final round
            };

            // Start the dialogue (AI sends opening line)
            _ = manager.StartDialogue(
                characterName: "Unknown Enemy", // TODO: real name
                characterType: Personality.CharacterType.Normal, // TODO: real type
                language: config.Language,
                playerHp: 50, playerMaxHp: 80, playerGold: 100, // TODO: real values
                enemyInfo: "HP 60" // TODO: real info
            );
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] OnDialoguePressed failed: {ex.Message}");
        }
    }
}
```

- [ ] **Step 2: Implement ShopUIPatch**

```csharp
// AIDialogueMod/Patches/ShopUIPatch.cs
using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using AIDialogueMod.Config;
using AIDialogueMod.UI;

namespace AIDialogueMod.Patches;

/// <summary>
/// Injects a "Talk" button into the shop UI.
/// TODO: Replace HarmonyPatch target after inspecting sts2.dll.
/// </summary>
// [HarmonyPatch(typeof(TARGET_SHOP_UI_CLASS), "TARGET_METHOD")]
public static class ShopUIPatch
{
    public static void Postfix(object __instance)
    {
        try
        {
            var config = ModConfig.Load();
            if (!config.IsConfigured)
                return;

            if (__instance is not Node uiRoot)
                return;

            var dialogueBtn = new DialogueButton(config.Language);
            // TODO: Add to the correct position in shop UI
            // The flow is the same as CombatUIPatch.OnDialoguePressed
            // but with CharacterType.Merchant

            Log.Warn("[AIDialogueMod] Shop dialogue button injected.");
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] ShopUIPatch failed: {ex.Message}");
        }
    }
}
```

- [ ] **Step 3: Implement EventUIPatch**

```csharp
// AIDialogueMod/Patches/EventUIPatch.cs
using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using AIDialogueMod.Config;
using AIDialogueMod.UI;

namespace AIDialogueMod.Patches;

/// <summary>
/// Injects a "Try talking..." option into random event choice lists.
/// TODO: Replace HarmonyPatch target after inspecting sts2.dll.
/// </summary>
// [HarmonyPatch(typeof(TARGET_EVENT_UI_CLASS), "TARGET_METHOD")]
public static class EventUIPatch
{
    public static void Postfix(object __instance)
    {
        try
        {
            var config = ModConfig.Load();
            if (!config.IsConfigured)
                return;

            if (__instance is not Node uiRoot)
                return;

            var dialogueBtn = new DialogueButton(config.Language);
            dialogueBtn.Text = config.Language == "en" ? "Try talking..." : "尝试对话...";
            // TODO: Add to event option list
            // CharacterType.EventNpc

            Log.Warn("[AIDialogueMod] Event dialogue option injected.");
        }
        catch (Exception ex)
        {
            Log.Warn($"[AIDialogueMod] EventUIPatch failed: {ex.Message}");
        }
    }
}
```

- [ ] **Step 4: Implement RestSiteFilter**

```csharp
// AIDialogueMod/Patches/RestSiteFilter.cs
namespace AIDialogueMod.Patches;

/// <summary>
/// Ensures no dialogue button is injected at campfire/rest sites.
/// This is a safeguard — since we use Postfix patches on specific UI classes,
/// we simply don't patch the rest site UI class.
///
/// If the rest site shares a base class with other events that we DO patch,
/// add a check here:
///
/// public static bool IsRestSite(object instance)
/// {
///     // TODO: Check the actual type after inspecting sts2.dll
///     return instance.GetType().Name.Contains("RestSite")
///         || instance.GetType().Name.Contains("Campfire");
/// }
/// </summary>
public static class RestSiteFilter
{
    public static bool IsRestSite(string eventId)
    {
        // TODO: After inspecting sts2.dll, add all rest site event IDs
        return eventId.Contains("rest", System.StringComparison.OrdinalIgnoreCase)
            || eventId.Contains("campfire", System.StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add AIDialogueMod/Patches/
git commit -m "feat: add Harmony patch stubs for combat, shop, event UI injection"
```

---

## Task 17: Godot Resources & Packaging

**Files:**
- Create: `AIDialogueMod/pck_src/mod_manifest.json`
- Create: `AIDialogueMod/pck_src/themes/dialogue_theme.tres`

- [ ] **Step 1: Copy mod_manifest.json to pck_src**

```json
// AIDialogueMod/pck_src/mod_manifest.json
{
    "pck_name": "AIDialogueMod",
    "name": "AI Dialogue Mod",
    "author": "ModAuthor",
    "description": "Talk your way through events! AI-powered dialogue with monsters and NPCs that affects gameplay in real-time.",
    "version": "0.1.0"
}
```

- [ ] **Step 2: Create a basic Godot theme resource**

```
; AIDialogueMod/pck_src/themes/dialogue_theme.tres
[gd_resource type="Theme" format=3]

[resource]
default_font_size = 16
```

Note: This is a minimal theme. After setting up the Godot editor project, you can visually edit this theme to add panel styles, font colors, and button styles that match the STS2 aesthetic.

- [ ] **Step 3: Commit**

```bash
git add AIDialogueMod/pck_src/
git commit -m "feat: add Godot pck resources with mod manifest and base theme"
```

---

## Task 18: Run All Tests & Final Verification

- [ ] **Step 1: Run the complete test suite**

Run: `cd AIDialogueMod.Tests && dotnet test -v normal`
Expected: All tests PASS (ModConfigTests: 7, PersonalityGeneratorTests: 6, ActionValidatorTests: 11, StolenCardManagerTests: 5, ResponseParserTests: 7, PromptBuilderTests: 7 = **43 tests total**)

- [ ] **Step 2: Verify the mod project builds**

Run: `cd AIDialogueMod && dotnet build`
Expected: Build succeeds (warnings about missing sts2.dll are expected — the DLL must be copied from the game installation)

- [ ] **Step 3: Commit any final fixes**

```bash
git add -A
git commit -m "chore: final verification — all tests passing"
```

---

## Next Steps (Post-Plan, Not Tasks)

These items require access to the actual game and `sts2.dll`:

1. **Decompile `sts2.dll`** with ILSpy to find:
   - Combat UI class name and its "End Turn" button path
   - Shop UI class name
   - Event UI class name and option list structure
   - Rest site / campfire class name
   - Player HP/Gold/MaxHP accessors
   - Enemy name/HP/strength accessors
   - Card/Relic/Buff APIs

2. **Replace all TODO placeholders** in:
   - `Patches/CombatUIPatch.cs` — real `[HarmonyPatch]` target and game state reading
   - `Patches/ShopUIPatch.cs` — real target
   - `Patches/EventUIPatch.cs` — real target
   - `Actions/ActionExecutor.cs` — real game API calls

3. **Build & test in-game**:
   - Copy `sts2.dll` to project root
   - Build the DLL
   - Export the `.pck` from Godot editor
   - Copy both to `mods/AIDialogueMod/`
   - Launch the game and verify
