using CP2077SaveEditor.Utils;
using WolvenKit.RED4.Save;
using WolvenKit.RED4.Save.Classes;
using WolvenKit.RED4.Types;
using Xunit;

namespace CP2077SaveEditor.Tests;

public class QuickhackHelperTests
{
    [Fact]
    public void GetQuickhackSlots_ReturnsOnlyProgramSlotsInOrder()
    {
        var deck = CreateCyberdeck(
            CreateSlot("AttachmentSlots.CyberdeckProgram3", "Items.OverheatProgram"),
            CreateSlot("AttachmentSlots.StatsShardSlot", "Items.AdvancedStatsShard"),
            CreateSlot("AttachmentSlots.CyberdeckProgram1", "Items.PingProgram"));

        var slots = QuickhackHelper.GetQuickhackSlots(deck);

        Assert.Equal(new[] { 1, 3 }, slots.Select(x => QuickhackHelper.GetSlotIndex(x)!.Value));
    }

    [Fact]
    public void IsQuickhack_RecognizesBlackwallGatewayTieredRecord()
    {
        var blackwallGateway = (TweakDBID)"Items.BlackWallProgramLvl4";

        Assert.True(QuickhackHelper.IsQuickhack(blackwallGateway));
        Assert.Contains("(T5)", QuickhackHelper.GetDisplayName(blackwallGateway));
    }

    [Fact]
    public void AddStandardSlots_PreservesExistingChildrenAndAddsMissingSlots()
    {
        var unrelated = CreateSlot("AttachmentSlots.StatsShardSlot", "Items.AdvancedStatsShard");
        var deck = CreateCyberdeck(CreateSlot("AttachmentSlots.CyberdeckProgram2", "Items.PingProgram"), unrelated);
        var helper = new QuickhackHelper(new SaveFileHelper());

        helper.AddStandardSlots(deck);

        Assert.Equal(new[] { 1, 2, 3, 4 }, QuickhackHelper.GetQuickhackSlots(deck).Select(x => QuickhackHelper.GetSlotIndex(x)!.Value));
        Assert.Contains(unrelated, deck.ItemSlotPart.Children);
        Assert.Equal(0UL, (ulong)QuickhackHelper.GetQuickhackSlots(deck)[0].ItemInfo.ItemId.Id);
    }

    [Fact]
    public void AddQuickhackSlot_UsesNextAvailableIndexAfterExistingSlots()
    {
        var deck = CreateCyberdeck(Enumerable.Range(1, 5)
            .Select(index => CreateSlot($"AttachmentSlots.CyberdeckProgram{index}", TweakDBID.Empty))
            .ToArray());
        var helper = new QuickhackHelper(new SaveFileHelper());

        var addedSlot = helper.AddQuickhackSlot(deck);

        Assert.Equal(6, QuickhackHelper.GetSlotIndex(addedSlot));
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 },
            QuickhackHelper.GetQuickhackSlots(deck).Select(x => QuickhackHelper.GetSlotIndex(x)!.Value));
        Assert.Equal(0UL, (ulong)addedSlot.ItemInfo.ItemId.Id);
    }

    [Fact]
    public void RemoveQuickhackSlot_RemovesOnlyTheSelectedSlot()
    {
        var selectedSlot = CreateSlot("AttachmentSlots.CyberdeckProgram5", TweakDBID.Empty);
        var otherSlot = CreateSlot("AttachmentSlots.CyberdeckProgram2", "Items.PingProgram");
        var deck = CreateCyberdeck(selectedSlot, otherSlot);
        var helper = new QuickhackHelper(new SaveFileHelper());

        helper.RemoveQuickhackSlot(deck, selectedSlot);

        Assert.Equal(new[] { 2 }, QuickhackHelper.GetQuickhackSlots(deck).Select(x => QuickhackHelper.GetSlotIndex(x)!.Value));
        Assert.DoesNotContain(selectedSlot, deck.ItemSlotPart.Children);
        Assert.Contains(otherSlot, deck.ItemSlotPart.Children);
    }

    [Fact]
    public void SetQuickhack_MovesOwnedItemIntoEmptySlot()
    {
        var quickhackId = (TweakDBID)"Items.BlindProgram";
        var owned = new ItemData
        {
            ItemInfo = new ItemInfo
            {
                ItemId = new gameItemID { Id = quickhackId, RngSeed = 123, UniqueCounter = 4 },
                ItemStructure = ItemStructure.None
            }
        };
        var slot = CreateSlot("AttachmentSlots.CyberdeckProgram1", TweakDBID.Empty);
        var deck = CreateCyberdeck(slot);
        var inventory = new Inventory();
        inventory.SubInventories.Add(new SubInventory
        {
            InventoryId = StashHelper.PlayerInventoryId,
            Items = { owned }
        });
        var saveFile = new SaveFileHelper
        {
            SaveFile = new CyberpunkSaveFile
            {
                Nodes = { new NodeEntry { Name = "inventory", Value = inventory } }
            }
        };
        var helper = new QuickhackHelper(saveFile);

        Assert.True(QuickhackHelper.IsQuickhack(owned));
        Assert.True(helper.SetQuickhack(deck, slot, owned));

        Assert.Equal(quickhackId, slot.ItemInfo.ItemId.Id);
        Assert.Equal(123u, (uint)slot.ItemInfo.ItemId.RngSeed);
        Assert.Empty(inventory.SubInventories[0].Items);
    }

    [Fact]
    public void SetQuickhack_EquipsBlackwallGatewayFromInventory()
    {
        var blackwallId = (TweakDBID)"Items.BlackWallProgramLvl4";
        var owned = new ItemData
        {
            ItemInfo = new ItemInfo
            {
                ItemId = new gameItemID { Id = blackwallId, RngSeed = 123, UniqueCounter = 4 },
                ItemStructure = ItemStructure.None
            }
        };
        var slot = CreateSlot("AttachmentSlots.CyberdeckProgram5", TweakDBID.Empty);
        var deck = CreateCyberdeck(slot);
        var inventory = new Inventory();
        inventory.SubInventories.Add(new SubInventory
        {
            InventoryId = StashHelper.PlayerInventoryId,
            Items = { owned }
        });
        var helper = new QuickhackHelper(new SaveFileHelper
        {
            SaveFile = new CyberpunkSaveFile
            {
                Nodes = { new NodeEntry { Name = "inventory", Value = inventory } }
            }
        });

        Assert.True(helper.SetQuickhack(deck, slot, owned));

        Assert.Equal(blackwallId, slot.ItemInfo.ItemId.Id);
        Assert.Equal(123u, (uint)slot.ItemInfo.ItemId.RngSeed);
        Assert.Empty(inventory.SubInventories[0].Items);
    }

    [Fact]
    public void SwapQuickhacks_ChangesItemsWithoutChangingSlotDefinitions()
    {
        var first = CreateSlot("AttachmentSlots.CyberdeckProgram1", "Items.PingProgram");
        var second = CreateSlot("AttachmentSlots.CyberdeckProgram2", "Items.OverheatProgram");
        var firstId = first.ItemInfo.ItemId.Id;
        var secondId = second.ItemInfo.ItemId.Id;

        Assert.True(QuickhackHelper.SwapQuickhacks(first, second));

        Assert.Equal(secondId, first.ItemInfo.ItemId.Id);
        Assert.Equal(firstId, second.ItemInfo.ItemId.Id);
        Assert.Equal("AttachmentSlots.CyberdeckProgram1", first.AttachmentSlotTdbId.ResolvedText);
        Assert.Equal("AttachmentSlots.CyberdeckProgram2", second.AttachmentSlotTdbId.ResolvedText);
    }

    private static ItemData CreateCyberdeck(params ItemSlotPart[] children) =>
        new()
        {
            ItemInfo = new ItemInfo { ItemId = new gameItemID { Id = "Items.TestCyberdeck" } },
            ItemSlotPart = new ItemSlotPart
            {
                ItemInfo = new ItemInfo { ItemId = new gameItemID { Id = "Items.TestCyberdeck" } },
                AppearanceName = "None",
                ItemAdditionalInfo = new ItemAdditionalInfo { RequiredLevel = float.MaxValue },
                Children = children.ToList()
            }
        };

    private static ItemSlotPart CreateSlot(string slotId, string itemId) =>
        CreateSlot(slotId, (TweakDBID)itemId);

    private static ItemSlotPart CreateSlot(string slotId, TweakDBID itemId) =>
        new()
        {
            ItemInfo = new ItemInfo { ItemId = new gameItemID { Id = itemId } },
            AppearanceName = "None",
            AttachmentSlotTdbId = slotId,
            ItemAdditionalInfo = new ItemAdditionalInfo { RequiredLevel = float.MaxValue }
        };
}
