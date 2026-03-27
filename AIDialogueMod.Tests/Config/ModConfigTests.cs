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
