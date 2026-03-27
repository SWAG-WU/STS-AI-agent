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
        _stolenCards.Remove(cardId);
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
