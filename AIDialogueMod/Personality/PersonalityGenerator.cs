using System;
using System.Collections.Generic;
using System.Linq;

namespace AIDialogueMod.Personality;

public class PersonalityGenerator
{
    private readonly Random _random;

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

    private static readonly HashSet<PersonalityType> PositiveTypes = new()
    {
        PersonalityType.Cowardly, PersonalityType.WarAverse, PersonalityType.Weak,
        PersonalityType.Kind, PersonalityType.Gentle, PersonalityType.Generous,
    };

    private static readonly HashSet<PersonalityType> HighValueTypes = new()
    {
        PersonalityType.Kind, PersonalityType.Generous, PersonalityType.Weak,
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
        var first = _random.Next(2) == 0 ? PersonalityType.Calm : PersonalityType.Cunning;
        var weights = BuildWeights(positiveMultiplier: 0.5, highValueMultiplier: 0.5);
        weights.Remove(first);
        var second = PickWeighted(weights);
        return new List<PersonalityType> { first, second };
    }

    private List<PersonalityType> GenerateMerchant()
    {
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
            if (PositiveTypes.Contains(type)) w *= positiveMultiplier;
            if (HighValueTypes.Contains(type)) w *= highValueMultiplier;
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
            if (roll < cumulative) return type;
        }
        return weights.Keys.Last();
    }
}
