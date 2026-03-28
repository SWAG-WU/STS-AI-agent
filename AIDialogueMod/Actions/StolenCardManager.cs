using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace AIDialogueMod.Actions;

public class StolenCardManager
{
    private readonly List<CardModel> _stolenCards = new();

    public void StealCard(CardModel card)
    {
        _stolenCards.Add(card);
    }

    public List<CardModel> GetStolenCards() => new(_stolenCards);

    public void RemoveCard(CardModel card)
    {
        _stolenCards.Remove(card);
    }

    public List<CardModel> ClearAndReturnAll()
    {
        var cards = new List<CardModel>(_stolenCards);
        _stolenCards.Clear();
        return cards;
    }
}
