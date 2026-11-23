using System.Security.Cryptography;
using WolvenKit.Common.Services;
using WolvenKit.Core.Compression;
using WolvenKit.RED4.Archive.Buffer;
using WolvenKit.RED4.Save;
using WolvenKit.RED4.Save.Classes;
using WolvenKit.RED4.Save.IO;
using WolvenKit.RED4.Types;
using Xunit;
using static WolvenKit.RED4.Types.Enums;

namespace CP2077SaveEditor.Tests;

/// <summary>
/// Round-trip tests against a real save. Set CP2077_TEST_SAVE to the path of a sav.dat to run them,
/// saves are not committed to the repository. Patch 2.5+ saves also cover the optimized stats format.
/// </summary>
public class SaveRoundTripTests
{
    private const string SaveVariable = "CP2077_TEST_SAVE";

    private static readonly Lazy<HashService> s_hashService = new(() =>
    {
        CompressionSettings.Get().UseOodle = false;
        return new HashService();
    });

    private static SaveFileHelper LoadSave()
    {
        var path = Environment.GetEnvironmentVariable(SaveVariable);
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Assert.Skip($"Set {SaveVariable} to a sav.dat to run this test");
        }

        _ = s_hashService.Value;

        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    private static SaveFileHelper Load(Stream stream)
    {
        using var reader = new CyberpunkSaveReader(stream);
        Assert.Equal(EFileReadErrorCodes.NoError, reader.ReadFile(out var save));
        return new SaveFileHelper { SaveFile = save };
    }

    private static SaveFileHelper Reload(SaveFileHelper saveFile)
    {
        using var stream = new MemoryStream(saveFile.Serialize());
        return Load(stream);
    }

    private static byte[] Hash(SaveFileHelper saveFile) => SHA256.HashData(saveFile.Serialize());

    private static IEnumerable<ItemData> AllItems(SaveFileHelper saveFile) =>
        saveFile.GetInventoriesContainer().SubInventories.SelectMany(x => x.Items);

    private static float ConstantTotal(gameSavedStatsData stats, gamedataStatType stat) =>
        ((ModifiersBuffer)stats.ModifiersBuffer.Data!).Entries
            .OfType<gameConstantStatModifierData_Deprecated>()
            .Where(x => x.StatType == stat && x.ModifierType == gameStatModifierType.Additive)
            .Sum(x => (float)x.Value);

    private static string Modifiers(gameSavedStatsData stats) =>
        string.Join("|", ((ModifiersBuffer)stats.ModifiersBuffer.Data!).Entries.Select(x => $"{x.GetType().Name}:{x.StatType}:{x.ModifierType}:{(x as gameConstantStatModifierData_Deprecated)?.Value}"));

    [Fact]
    public void Serializing_twice_gives_the_same_bytes()
    {
        var saveFile = LoadSave();

        Assert.Equal(Hash(saveFile), Hash(saveFile));
    }

    [Fact]
    public void Saved_file_can_be_read_back_with_the_same_content()
    {
        var saveFile = LoadSave();
        var reloaded = Reload(saveFile);

        Assert.Equal(Hash(saveFile), Hash(reloaded));
        Assert.Equal(saveFile.SaveFile.Nodes.Count, reloaded.SaveFile.Nodes.Count);
    }

    [Fact]
    public void Reading_every_stats_entry_does_not_change_the_save()
    {
        var saveFile = LoadSave();
        var before = Hash(saveFile);

        var statsMap = saveFile.GetStatsContainer();
        foreach (var key in statsMap.Keys)
        {
            Assert.NotNull(saveFile.GetStatsFromStatsObjectId(key));
        }
        foreach (var entry in statsMap.ValuesOptimized)
        {
            Assert.NotNull(saveFile.GetStatsFromStatsObjectId(new gameStatsObjectID { IdType = entry.IdType, EntityHash = entry.EntityHash }));
        }

        Assert.Equal(before, Hash(saveFile));
    }

    [Fact]
    public void Editing_an_item_does_not_change_items_that_share_its_stats_slice()
    {
        var saveFile = LoadSave();
        var statsMap = saveFile.GetStatsContainer();
        if (statsMap.ValuesOptimized.Count == 0)
        {
            Assert.Skip("Only saves from patch 2.5+ share stats slices");
        }

        // Two inventory items whose optimized stats point to the same slice
        var itemsByHash = AllItems(saveFile).GroupBy(x => InventoryHelper.GetItemIdHash(x.ItemInfo.ItemId)).ToDictionary(x => x.Key, x => x.First());
        var pair = statsMap.ValuesOptimized
            .Where(x => x.IdType == gameStatIDType.ItemID && x.BufferSliceIndices.Count == 1 && itemsByHash.ContainsKey(x.EntityHash))
            .GroupBy(x => (ushort)x.BufferSliceIndices[0])
            .FirstOrDefault(x => x.Count() >= 2)?
            .Take(2)
            .Select(x => itemsByHash[x.EntityHash])
            .ToList();
        Assert.SkipWhen(pair == null, "No two items share a stats slice in this save");

        var edited = pair![0];
        var other = pair[1];
        var otherBefore = Modifiers(saveFile.GetStatsFromItemId(other.ItemInfo.ItemId));

        Assert.True(saveFile.SetMaxTier(saveFile.GetStatsFromItemId(edited.ItemInfo.ItemId)));

        var reloaded = Reload(saveFile);
        Assert.Equal(otherBefore, Modifiers(reloaded.GetStatsFromItemId(other.ItemInfo.ItemId)));
        Assert.True(ConstantTotal(reloaded.GetStatsFromItemId(edited.ItemInfo.ItemId), gamedataStatType.Quality) >= 4);
    }

    [Fact]
    public void Max_tier_is_idempotent_and_survives_a_reload()
    {
        var saveFile = LoadSave();
        var item = AllItems(saveFile).FirstOrDefault(x => saveFile.GetStatsFromItemId(x.ItemInfo.ItemId) != null &&
                                                         ConstantTotal(saveFile.GetStatsFromItemId(x.ItemInfo.ItemId), gamedataStatType.Quality) < 4);
        Assert.SkipWhen(item == null, "No item below Tier 5 with stats in this save");

        var stats = saveFile.GetStatsFromItemId(item!.ItemInfo.ItemId);
        Assert.True(saveFile.SetMaxTier(stats));
        var afterFirst = Hash(saveFile);

        Assert.False(saveFile.SetMaxTier(stats));
        Assert.Equal(afterFirst, Hash(saveFile));

        var reloaded = Reload(saveFile).GetStatsFromItemId(item.ItemInfo.ItemId);
        Assert.True(ConstantTotal(reloaded, gamedataStatType.Quality) >= 4);
        Assert.True(ConstantTotal(reloaded, gamedataStatType.IsItemPlus) >= 2);
    }

    [Fact]
    public void Cloned_stats_are_independent_from_the_original()
    {
        var saveFile = LoadSave();
        var original = AllItems(saveFile).FirstOrDefault(x => x.ItemInfo.ItemId.RngSeed != 2 &&
                                                             saveFile.GetStatsFromItemId(x.ItemInfo.ItemId) != null &&
                                                             ConstantTotal(saveFile.GetStatsFromItemId(x.ItemInfo.ItemId), gamedataStatType.Quality) < 4);
        Assert.SkipWhen(original == null, "No suitable item with stats in this save");

        var cloneId = new gameItemID
        {
            Id = original!.ItemInfo.ItemId.Id,
            RngSeed = saveFile.CreateUniqueSeed(),
            UniqueCounter = saveFile.GetNextUniqueCounter(),
            Flags = original.ItemInfo.ItemId.Flags
        };
        saveFile.CloneStats(original.ItemInfo.ItemId, cloneId);

        var originalBefore = Modifiers(saveFile.GetStatsFromItemId(original.ItemInfo.ItemId));
        Assert.Equal(originalBefore, Modifiers(saveFile.GetStatsFromItemId(cloneId)));

        saveFile.SetMaxTier(saveFile.GetStatsFromItemId(cloneId));

        var reloaded = Reload(saveFile);
        Assert.Equal(originalBefore, Modifiers(reloaded.GetStatsFromItemId(original.ItemInfo.ItemId)));
        Assert.True(ConstantTotal(reloaded.GetStatsFromItemId(cloneId), gamedataStatType.Quality) >= 4);
    }

    [Fact]
    public void Perk_changes_use_points_and_survive_a_reload()
    {
        var saveFile = LoadSave();
        var perks = new Utils.PerkHelper(saveFile.GetPlayerDevelopmentData());
        var perk = Utils.ResourceHelper.Perks.FirstOrDefault(x => x.Attribute != "Espionage" && perks.GetLevel(x) == 0);
        Assert.SkipWhen(perk == null, "Every perk is already unlocked in this save");

        var pointsBefore = perks.GetUnspentPoints(gamedataDevelopmentPointType.Primary);
        Assert.True(perks.SetLevel(perk!, perk!.MaxLevel, chargePoints: true));
        Assert.Equal(Math.Max(0, pointsBefore - perk.MaxLevel), perks.GetUnspentPoints(gamedataDevelopmentPointType.Primary));

        var reloaded = new Utils.PerkHelper(Reload(saveFile).GetPlayerDevelopmentData());
        Assert.Equal(perk.MaxLevel, reloaded.GetLevel(perk));

        Assert.True(perks.SetLevel(perk, 0, chargePoints: true));
        Assert.Equal(0, perks.GetLevel(perk));
        Assert.Equal(pointsBefore, perks.GetUnspentPoints(gamedataDevelopmentPointType.Primary));
    }

    [Fact]
    public void Extra_cyberware_capacity_survives_a_reload()
    {
        var saveFile = LoadSave();
        var cyberware = new Utils.CyberwareHelper(saveFile);

        cyberware.SetExtraCapacity(250);
        cyberware.SetExtraCapacity(300);
        Assert.Equal(300, cyberware.GetExtraCapacity());

        var reloaded = Reload(saveFile);
        Assert.Equal(300, new Utils.CyberwareHelper(reloaded).GetExtraCapacity());

        cyberware.SetExtraCapacity(0);
        Assert.Equal(0, new Utils.CyberwareHelper(Reload(saveFile)).GetExtraCapacity());
    }

    [Fact]
    public void Equipped_quickhacks_can_be_changed_and_swapped_after_a_reload()
    {
        var saveFile = LoadSave();
        var quickhacks = new Utils.QuickhackHelper(saveFile);
        var deck = quickhacks.GetEquippedCyberdeck();
        Assert.SkipWhen(deck == null, "No equipped cyberdeck in this save");

        var slots = Utils.QuickhackHelper.GetQuickhackSlots(deck!).ToList();
        if (slots.Count == 0)
        {
            quickhacks.AddStandardSlots(deck!);
            slots = Utils.QuickhackHelper.GetQuickhackSlots(deck!).ToList();
        }

        var inventory = saveFile.GetInventory(Utils.StashHelper.PlayerInventoryId);
        var replacement = Utils.QuickhackHelper.GetOwnedQuickhacks(saveFile)
            .FirstOrDefault(item => slots.All(slot => slot.ItemInfo.ItemId.Id != item.ItemInfo.ItemId.Id));
        Assert.SkipWhen(replacement == null, "No uninstalled quickhack in V's inventory");

        if (slots.Count > 1)
        {
            var firstBefore = slots[0].ItemInfo.ItemId.Id;
            var secondBefore = slots[1].ItemInfo.ItemId.Id;
            Assert.True(Utils.QuickhackHelper.SwapQuickhacks(slots[0], slots[1]));
            Assert.Equal(secondBefore, slots[0].ItemInfo.ItemId.Id);
            Assert.Equal(firstBefore, slots[1].ItemInfo.ItemId.Id);
        }

        var target = slots[0];
        var displacedId = target.ItemInfo.ItemId.Id;
        var slotIndex = Utils.QuickhackHelper.GetSlotIndex(target)!.Value;
        Assert.True(quickhacks.SetQuickhack(deck!, target, replacement!));
        Assert.Equal(replacement!.ItemInfo.ItemId.Id, target.ItemInfo.ItemId.Id);
        Assert.DoesNotContain(replacement, inventory.Items);

        var reloaded = Reload(saveFile);
        var reloadedDeck = new Utils.QuickhackHelper(reloaded).GetEquippedCyberdeck();
        var reloadedSlot = Utils.QuickhackHelper.GetQuickhackSlots(reloadedDeck!)
            .Single(x => Utils.QuickhackHelper.GetSlotIndex(x) == slotIndex);
        Assert.Equal(replacement.ItemInfo.ItemId.Id, reloadedSlot.ItemInfo.ItemId.Id);
        if ((ulong)displacedId != 0)
        {
            Assert.Contains(reloaded.GetInventory(Utils.StashHelper.PlayerInventoryId).Items,
                item => item.ItemInfo.ItemId.Id == displacedId);
        }
    }

    [Fact]
    public void Blackwall_Gateway_items_in_reference_save_are_quickhacks()
    {
        var saveFile = LoadSave();
        var blackwallItems = AllItems(saveFile)
            .Where(item => item.ItemInfo.ItemId.Id.ResolvedText?.StartsWith(
                "Items.BlackWallProgramLvl",
                StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        Assert.NotEmpty(blackwallItems);
        Assert.All(blackwallItems, item => Assert.True(CP2077SaveEditor.Utils.QuickhackHelper.IsQuickhack(item)));
        Assert.All(blackwallItems, item => Assert.Contains(
            item,
            CP2077SaveEditor.Utils.QuickhackHelper.GetOwnedQuickhacks(saveFile)));
        var pingTiers = CP2077SaveEditor.Utils.QuickhackHelper.GetQuickhackVariants()
            .Where(x => x.Family == "Items.Ping")
            .Select(x => x.Label)
            .ToHashSet();
        Assert.Equal(11, pingTiers.Count);
        Assert.Contains("Ping (T1)", pingTiers);
        Assert.Contains("Ping (T1+)", pingTiers);
        Assert.Contains("Ping (T2)", pingTiers);
        Assert.Contains("Ping (T5)", pingTiers);
        Assert.Contains("Ping (T5+)", pingTiers);
        Assert.Contains("Ping (T5++)", pingTiers);

        var requestBackupTiers = CP2077SaveEditor.Utils.QuickhackHelper.GetQuickhackVariants()
            .Where(x => x.Family == "Items.CommsCallIn")
            .Select(x => x.Label)
            .ToHashSet();
        Assert.Equal(11, requestBackupTiers.Count);
        Assert.Contains(requestBackupTiers, x => x.EndsWith(" (T1)", StringComparison.Ordinal));
        Assert.Contains(requestBackupTiers, x => x.EndsWith(" (T5++)", StringComparison.Ordinal));
    }

    [Fact]
    public void Unowned_quickhack_catalogue_record_can_be_created_and_equipped()
    {
        var saveFile = LoadSave();
        var quickhacks = new Utils.QuickhackHelper(saveFile);
        var blackwallT1Plus = Utils.QuickhackHelper.GetQuickhackVariants()
            .Single(x => x.Family == "Items.BlackWall" && x.Tier == 0 && x.Plus == 1);
        var blackwall = Utils.QuickhackHelper.GetQuickhackVariants()
            .Single(x => x.Family == "Items.BlackWall" && x.Tier == 4 && x.Plus == 2);
        var deck = quickhacks.GetEquippedCyberdeck();
        Assert.SkipWhen(deck == null, "No equipped cyberdeck in this save");

        var inventory = saveFile.GetInventory(Utils.StashHelper.PlayerInventoryId);
        foreach (var ownedBlackwall in inventory!.Items
            .Where(x => x.ItemInfo.ItemId.Id.ResolvedText?.StartsWith("Items.BlackWallProgramLvl", StringComparison.OrdinalIgnoreCase) == true)
            .ToList())
        {
            inventory.Items.Remove(ownedBlackwall);
        }

        var slot = quickhacks.AddQuickhackSlot(deck!);
        Assert.True(quickhacks.SetQuickhack(deck!, slot, blackwallT1Plus));
        Assert.True(quickhacks.SetQuickhack(deck!, slot, blackwall));
        Assert.Equal(blackwall.ItemId, slot.ItemInfo.ItemId.Id);
        Assert.DoesNotContain(inventory.Items, x => x.ItemInfo.ItemId.Id == blackwall.ItemId);

        var reloaded = Reload(saveFile);
        var reloadedDeck = new Utils.QuickhackHelper(reloaded).GetEquippedCyberdeck();
        var installedBlackwall = Utils.QuickhackHelper.GetQuickhackSlots(reloadedDeck!)
            .Single(x => x.ItemInfo.ItemId.Id == blackwall.ItemId);
        Assert.Equal(2f, ConstantTotal(reloaded.GetStatsFromItemId(installedBlackwall.ItemInfo.ItemId),
            gamedataStatType.IsItemPlus));
        var installedVariant = Utils.QuickhackHelper.GetQuickhackVariant(installedBlackwall.ItemInfo.ItemId, reloaded);
        Assert.Equal(blackwall.Label, installedVariant.Label);
        Assert.EndsWith(" (T5++)", installedVariant.Label, StringComparison.Ordinal);
        Assert.Contains(Utils.QuickhackHelper.GetOwnedQuickhacks(reloaded),
            item => Utils.QuickhackHelper.GetQuickhackVariant(item, reloaded).Key == blackwallT1Plus.Key);
    }

    [Fact]
    public void Equipping_new_cyberware_creates_the_item_and_fills_the_slot()
    {
        var saveFile = LoadSave();
        var cyberware = new Utils.CyberwareHelper(saveFile);

        var (area, slot) = cyberware.GetCyberwareAreas()
            .SelectMany(a => a.EquipSlots.Select(s => (a, s)))
            .FirstOrDefault(x => (ulong)(x.s.ItemID?.Id ?? 0) == 0);
        Assert.SkipWhen(slot == null, "No empty cyberware slot in this save");

        var areaName = area.AreaType.ToString();
        var recordId = Utils.ResourceHelper.CyberwareAreas.First(x => x.Value == areaName).Key;
        var record = Utils.ResourceHelper.ItemClasses.GetValueOrDefault(recordId) ?? new Utils.ItemRecord { Type = "Cyberware" };
        var item = saveFile.CreateItem(recordId, record, 1);
        saveFile.GetInventory(1).Items.Add(item);
        cyberware.Equip(slot!, item);

        var reloaded = Reload(saveFile);
        var reloadedSlot = new Utils.CyberwareHelper(reloaded).GetCyberwareAreas()
            .First(x => x.AreaType.ToString() == areaName)
            .EquipSlots[area.EquipSlots.IndexOf(slot!)];
        Assert.NotNull(new Utils.CyberwareHelper(reloaded).FindItem(reloadedSlot.ItemID));
        Assert.Equal(recordId, (ulong)reloadedSlot.ItemID.Id);
    }

    [Fact]
    public void Cloned_item_with_mods_gets_new_ids_and_survives_a_reload()
    {
        var saveFile = LoadSave();
        var player = saveFile.GetInventory(Utils.StashHelper.PlayerInventoryId);
        var original = player.Items.FirstOrDefault(x => x.ItemInfo.ItemId.RngSeed != 2 &&
                                                        x.ItemSlotPart?.Children.Count > 0 &&
                                                        saveFile.GetStatsFromItemId(x.ItemInfo.ItemId) != null);
        Assert.SkipWhen(original == null, "No item with mods and stats in this save");

        var clone = saveFile.CloneItem(original!);
        player.Items.Add(clone);

        Assert.Equal(original!.ItemInfo.ItemId.Id, clone.ItemInfo.ItemId.Id);
        Assert.NotEqual((uint)original.ItemInfo.ItemId.RngSeed, (uint)clone.ItemInfo.ItemId.RngSeed);
        Assert.NotSame(original.ItemSlotPart, clone.ItemSlotPart);
        Assert.Equal(original.ItemSlotPart!.Children.Count, clone.ItemSlotPart!.Children.Count);

        var originalBefore = Modifiers(saveFile.GetStatsFromItemId(original.ItemInfo.ItemId));
        saveFile.SetMaxTier(saveFile.GetStatsFromItemId(clone.ItemInfo.ItemId));

        var reloaded = Reload(saveFile);
        var reloadedClone = reloaded.GetInventory(Utils.StashHelper.PlayerInventoryId).Items
            .Single(x => x.ItemInfo.ItemId.Id == clone.ItemInfo.ItemId.Id && x.ItemInfo.ItemId.RngSeed == clone.ItemInfo.ItemId.RngSeed &&
                         x.ItemInfo.ItemId.UniqueCounter == clone.ItemInfo.ItemId.UniqueCounter);
        Assert.Equal(original.ItemSlotPart.Children.Count, reloadedClone.ItemSlotPart!.Children.Count);
        Assert.Equal(originalBefore, Modifiers(reloaded.GetStatsFromItemId(original.ItemInfo.ItemId)));
        Assert.True(ConstantTotal(reloaded.GetStatsFromItemId(clone.ItemInfo.ItemId), gamedataStatType.Quality) >= 4);
    }

    [Fact]
    public void Moving_items_to_the_stash_and_back_keeps_them()
    {
        var saveFile = LoadSave();
        var player = saveFile.GetInventory(Utils.StashHelper.PlayerInventoryId);
        var items = player.Items.Where(Utils.StashHelper.CanMoveToStash).Take(5).ToList();
        Assert.SkipWhen(items.Count == 0, "No stashable item in this save");

        var stash = Utils.StashHelper.GetOrCreateStash(saveFile, out _);
        var stashCount = stash.Items.Count;
        var playerCount = player.Items.Count;

        var (moved, skipped) = Utils.StashHelper.MoveToStash(saveFile, player, stash, items);
        Assert.Equal(items.Count, moved);
        Assert.Equal(0, skipped);
        Assert.Equal(playerCount - moved, player.Items.Count);
        Assert.All(items, item => Assert.Empty(saveFile.GetEquipSlotsFromID(item.ItemInfo.ItemId)));

        // Weapons and clothing don't stack, so each one is its own entry in the stash
        var reloaded = Reload(saveFile);
        Assert.Equal(stashCount + moved, reloaded.GetInventory(stash.InventoryId).Items.Count);

        Assert.Equal(moved, Utils.StashHelper.MoveToInventory(stash, player, items));
        Assert.Equal(playerCount, player.Items.Count);
        Assert.Equal(stashCount, stash.Items.Count);
    }

    [Fact]
    public void Money_and_quest_items_are_never_stashed()
    {
        var saveFile = LoadSave();
        var player = saveFile.GetInventory(Utils.StashHelper.PlayerInventoryId);

        Assert.All(player.Items.Where(x => x.ItemInfo.ItemId.Id.ResolvedText == "Items.money" || x.Flags != 0),
            item => Assert.False(Utils.StashHelper.CanMoveToStash(item)));
    }

    [Fact]
    public void Player_stats_are_found()
    {
        var saveFile = LoadSave();

        Assert.NotNull(saveFile.GetStatsFromEntityId(1));
    }
}
