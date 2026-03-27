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
                $"Boss got {result[0]} and {result[1]}"
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
        Assert.True(counts.ContainsKey(PersonalityType.Generous));
        Assert.True(counts.ContainsKey(PersonalityType.Aggressive));
        Assert.True(counts[PersonalityType.Aggressive] > counts[PersonalityType.Generous]);
    }

    [Fact]
    public void Elite_reduces_positive_personality_weight()
    {
        var gen = new PersonalityGenerator(seed: 456);
        int normalKindCount = 0, eliteKindCount = 0;
        for (int i = 0; i < 10000; i++)
        {
            if (gen.Generate(CharacterType.Normal).Contains(PersonalityType.Kind)) normalKindCount++;
            if (gen.Generate(CharacterType.Elite).Contains(PersonalityType.Kind)) eliteKindCount++;
        }
        Assert.True(normalKindCount > eliteKindCount);
    }
}
