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
        manager.ReturnCard("strike");
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
        manager.ReturnCard("strike");
        Assert.Single(manager.GetStolenCards());
    }
}
