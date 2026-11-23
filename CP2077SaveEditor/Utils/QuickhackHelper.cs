using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CP2077SaveEditor.ModSupport;
using WolvenKit.RED4.Archive.Buffer;
using WolvenKit.RED4.Save.Classes;
using WolvenKit.RED4.Types;
using static WolvenKit.RED4.Types.Enums;

namespace CP2077SaveEditor.Utils;

public sealed class QuickhackHelper
{
    public const int StandardQuickhackSlotCount = 4;

    public sealed record QuickhackVariant(TweakDBID ItemId, string Family, int Tier, int Plus, string Label)
    {
        public (string Family, int Tier, int Plus) Key => (Family, Tier, Plus);
    }

    private sealed record QuickhackRecord(TweakDBID Id, string Family, int BaseTier, int BasePlus);

    private static readonly Regex s_quickhackRecord = new(
        @"(?:(?:Lvl(?<tier>[0-5])(?<levelPlus>PlusPlus|Plus)?Program)|(?:ProgramLvl(?<tier>[0-5]))|Program)(?<endPlus>PlusPlus|Plus)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex s_quickhackSuffix = new(
        @"(?:Lvl[0-5](?:PlusPlus|Plus)?Program|ProgramLvl[0-5]|Program)(?:PlusPlus|Plus)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Lazy<Dictionary<string, string>> s_quickhackFamilyNames = new(() =>
        ResourceHelper.ItemClasses.Keys
            .Select(id => (TweakDBID)id)
            .Where(IsQuickhack)
            .Select(id => (Family: GetQuickhackFamily(id), Name: ResourceHelper.GetDisplayName(id)))
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Family, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().Name, StringComparer.OrdinalIgnoreCase));

    private readonly SaveFileHelper _saveFile;

    public QuickhackHelper(SaveFileHelper saveFile)
    {
        _saveFile = saveFile ?? throw new ArgumentNullException(nameof(saveFile));
    }

    public ItemData GetEquippedCyberdeck()
    {
        var area = _saveFile.GetEquipAreas().FirstOrDefault(x => x.AreaType == gamedataEquipmentArea.SystemReplacementCW);
        var slot = area?.EquipSlots?.FirstOrDefault(x => x.ItemID != null && (ulong)x.ItemID.Id != 0);
        if (slot == null)
        {
            return null;
        }

        return _saveFile.GetInventory(StashHelper.PlayerInventoryId)?.Items
            .FirstOrDefault(x => IsSameItem(x.ItemInfo.ItemId, slot.ItemID));
    }

    public QuickhackLoadoutSystem GetModLoadoutSystem() =>
        _saveFile.GetScriptableContainer().Chunks.OfType<QuickhackLoadoutSystem>().FirstOrDefault();

    public static IReadOnlyList<ItemSlotPart> GetQuickhackSlots(ItemData cyberdeck) =>
        cyberdeck?.ItemSlotPart?.Children?
            .Where(x => GetSlotIndex(x) is not null)
            .OrderBy(GetSlotIndex)
            .ToList() ?? new List<ItemSlotPart>();

    public static int? GetSlotIndex(ItemSlotPart slot)
    {
        var slotName = slot?.AttachmentSlotTdbId.ResolvedText;
        const string prefix = "AttachmentSlots.CyberdeckProgram";
        if (slotName == null || !slotName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(slotName.Substring(prefix.Length), out var slotIndex) || slotIndex < 1)
        {
            return null;
        }

        return slotIndex;
    }

    public static bool IsQuickhack(TweakDBID itemId)
    {
        var itemRecord = ResourceHelper.ItemClasses.GetValueOrDefault((ulong)itemId);
        var idText = itemId.ResolvedText;
        return itemRecord?.Type == "Part" &&
               idText != null &&
               idText.StartsWith("Items.", StringComparison.OrdinalIgnoreCase) &&
               !idText.Equals("Items.CyberdeckProgram", StringComparison.OrdinalIgnoreCase) &&
               s_quickhackRecord.IsMatch(idText);
    }

    public static bool IsQuickhack(ItemData item) =>
        item != null && IsQuickhack(item.ItemInfo.ItemId.Id);

    public static IReadOnlyList<ItemData> GetOwnedQuickhacks(SaveFileHelper saveFile) =>
        saveFile.GetInventory(StashHelper.PlayerInventoryId)?.Items
            .Where(IsQuickhack)
            .OrderBy(x => GetDisplayName(x.ItemInfo.ItemId.Id), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x.ItemInfo.ItemId.Id.ResolvedText, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<ItemData>();

    public static IReadOnlyList<TweakDBID> GetQuickhackCatalogue() =>
        ResourceHelper.ItemClasses.Keys
            .Select(id => (TweakDBID)id)
            .Where(IsQuickhack)
            .OrderBy(GetDisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x.ResolvedText, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static IReadOnlyList<QuickhackVariant> GetQuickhackVariants()
    {
        var records = GetQuickhackCatalogue()
            .Select(id => new QuickhackRecord(id, GetQuickhackFamily(id), GetRecordTier(id), GetRecordPlus(id)))
            .Where(x => x.BaseTier >= 0)
            .GroupBy(x => x.Family, StringComparer.OrdinalIgnoreCase);
        var variants = new List<QuickhackVariant>();

        foreach (var family in records)
        {
            var candidates = family.ToList();
            var name = GetQuickhackName(candidates[0].Id, family.Key);
            for (var tier = 0; tier <= 4; tier++)
            {
                var maxPlus = tier == 4 ? 2 : 1;
                for (var plus = 0; plus <= maxPlus; plus++)
                {
                    var matchingRecord = candidates.FirstOrDefault(x => x.BaseTier == tier && x.BasePlus == plus);
                    var source = matchingRecord ?? SelectSourceRecord(candidates, tier);
                    variants.Add(new QuickhackVariant(
                        source.Id,
                        family.Key,
                        tier,
                        plus,
                        $"{name} (T{tier + 1}{new string('+', plus)})"));
                }
            }
        }

        return variants
            .OrderBy(x => x.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static string GetDisplayName(TweakDBID itemId)
    {
        var name = GetQuickhackName(itemId, GetQuickhackFamily(itemId));
        var tier = GetRecordTier(itemId);
        return tier >= 0
            ? $"{name} (T{tier + 1}{new string('+', GetRecordPlus(itemId))})"
            : name;
    }

    public static string GetDisplayName(ItemData item, SaveFileHelper saveFile) =>
        GetQuickhackVariant(item, saveFile).Label;

    public static QuickhackVariant GetQuickhackVariant(ItemData item, SaveFileHelper saveFile)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(saveFile);
        return GetQuickhackVariant(item.ItemInfo.ItemId, saveFile);
    }

    public static QuickhackVariant GetQuickhackVariant(gameItemID itemId, SaveFileHelper saveFile)
    {
        ArgumentNullException.ThrowIfNull(itemId);
        ArgumentNullException.ThrowIfNull(saveFile);

        var id = itemId.Id;
        var family = GetQuickhackFamily(id);
        var tier = Math.Clamp(GetRecordTier(id) + (int)Math.Round(GetStatTotal(saveFile, itemId, gamedataStatType.Quality)), 0, 4);
        var plus = Math.Clamp(GetRecordPlus(id) + (int)Math.Round(GetStatTotal(saveFile, itemId, gamedataStatType.IsItemPlus)), 0, 2);
        var name = GetQuickhackName(id, family);
        return new QuickhackVariant(id, family, tier, plus, $"{name} (T{tier + 1}{new string('+', plus)})");
    }

    public void AddStandardSlots(ItemData cyberdeck)
    {
        ArgumentNullException.ThrowIfNull(cyberdeck);
        cyberdeck.ItemSlotPart ??= CreateRootSlotPart(cyberdeck.ItemInfo.ItemId);

        var existingSlotIndices = GetQuickhackSlots(cyberdeck)
            .Select(GetSlotIndex)
            .Where(x => x.HasValue)
            .Select(x => x.Value)
            .ToHashSet();

        for (var index = 1; index <= StandardQuickhackSlotCount; index++)
        {
            if (!existingSlotIndices.Contains(index))
            {
                cyberdeck.ItemSlotPart.Children.Add(CreateEmptyQuickhackSlot(index));
            }
        }
    }

    public ItemSlotPart AddQuickhackSlot(ItemData cyberdeck)
    {
        ArgumentNullException.ThrowIfNull(cyberdeck);
        cyberdeck.ItemSlotPart ??= CreateRootSlotPart(cyberdeck.ItemInfo.ItemId);

        var existingSlotIndices = GetQuickhackSlots(cyberdeck)
            .Select(GetSlotIndex)
            .Where(x => x.HasValue)
            .Select(x => x.Value)
            .ToHashSet();
        var nextIndex = 1;
        while (existingSlotIndices.Contains(nextIndex))
        {
            if (nextIndex == int.MaxValue)
            {
                throw new InvalidOperationException("No additional quickhack slot indices are available.");
            }

            nextIndex++;
        }

        var slot = CreateEmptyQuickhackSlot(nextIndex);
        cyberdeck.ItemSlotPart.Children.Add(slot);
        return slot;
    }

    public void RemoveQuickhackSlot(ItemData cyberdeck, ItemSlotPart slot)
    {
        ArgumentNullException.ThrowIfNull(cyberdeck);
        ArgumentNullException.ThrowIfNull(slot);

        if (!GetQuickhackSlots(cyberdeck).Contains(slot))
        {
            throw new ArgumentException("The selected slot does not belong to this cyberdeck.", nameof(slot));
        }

        if ((ulong)(slot.ItemInfo?.ItemId?.Id ?? 0) != 0)
        {
            SetQuickhack(cyberdeck, slot, (ItemData)null);
        }

        if (!cyberdeck.ItemSlotPart.Children.Remove(slot))
        {
            throw new InvalidOperationException("The selected quickhack slot could not be removed.");
        }
    }

    public bool SetQuickhack(ItemData cyberdeck, ItemSlotPart slot, ItemData replacement)
    {
        ArgumentNullException.ThrowIfNull(cyberdeck);
        ArgumentNullException.ThrowIfNull(slot);

        var slots = GetQuickhackSlots(cyberdeck);
        if (!slots.Contains(slot))
        {
            throw new ArgumentException("The selected slot does not belong to this cyberdeck.", nameof(slot));
        }

        var inventory = _saveFile.GetInventory(StashHelper.PlayerInventoryId)
            ?? throw new InvalidOperationException("V's inventory is missing from this save.");
        var currentItemId = slot.ItemInfo?.ItemId;
        var currentId = currentItemId?.Id ?? 0;

        if (replacement != null)
        {
            if (!IsQuickhack(replacement))
            {
                throw new ArgumentException("The replacement item is not a quickhack.", nameof(replacement));
            }

            if (!inventory.Items.Contains(replacement))
            {
                throw new ArgumentException("The replacement quickhack is not in V's inventory.", nameof(replacement));
            }

            if (IsSameItem(currentItemId, replacement.ItemInfo.ItemId))
            {
                return false;
            }

            if (slots.Any(x => !ReferenceEquals(x, slot) &&
                               x.ItemInfo?.ItemId?.Id == replacement.ItemInfo.ItemId.Id))
            {
                throw new InvalidOperationException("That quickhack is already installed in another slot.");
            }
        }
        else if ((ulong)currentId == 0)
        {
            return false;
        }

        ItemData returnedQuickhack = null;
        if ((ulong)currentId != 0 && !inventory.Items.Any(x => IsSameItem(x.ItemInfo.ItemId, currentItemId)))
        {
            var itemRecord = ResourceHelper.ItemClasses.GetValueOrDefault((ulong)currentId) ?? new ItemRecord { Type = "Part" };
            returnedQuickhack = _saveFile.CreateItem(currentId, itemRecord, 1);
            returnedQuickhack.ItemInfo.ItemId = CloneItemId(currentItemId);
            returnedQuickhack.ItemInfo.ItemStructure = slot.ItemInfo?.ItemStructure ?? ItemStructure.None;
        }

        if (replacement != null && !inventory.Items.Remove(replacement))
        {
            throw new InvalidOperationException("The replacement quickhack could not be removed from V's inventory.");
        }

        if (returnedQuickhack != null)
        {
            inventory.Items.Add(returnedQuickhack);
        }

        slot.ItemInfo ??= new ItemInfo();
        slot.ItemInfo.ItemId = replacement == null ? new gameItemID() : CloneItemId(replacement.ItemInfo.ItemId);
        slot.ItemInfo.ItemStructure = replacement?.ItemInfo.ItemStructure ?? ItemStructure.None;
        return true;
    }

    public bool SetQuickhack(ItemData cyberdeck, ItemSlotPart slot, QuickhackVariant replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        if (replacement.Tier is < 0 or > 4 ||
            replacement.Plus < 0 ||
            replacement.Plus > (replacement.Tier == 4 ? 2 : 1))
        {
            throw new ArgumentException("The selected quickhack tier is invalid.", nameof(replacement));
        }

        if (!IsQuickhack(replacement.ItemId))
        {
            throw new ArgumentException("The replacement record is not a quickhack.", nameof(replacement));
        }

        var inventory = _saveFile.GetInventory(StashHelper.PlayerInventoryId)
            ?? throw new InvalidOperationException("V's inventory is missing from this save.");
        var itemRecord = ResourceHelper.ItemClasses.GetValueOrDefault((ulong)replacement.ItemId)
            ?? throw new InvalidOperationException("Quickhack item data is missing from the editor's item catalogue.");
        var newItem = _saveFile.CreateItem(replacement.ItemId, itemRecord, 1);
        var qualityDelta = replacement.Tier - GetRecordTier(replacement.ItemId);
        var plusDelta = replacement.Plus - GetRecordPlus(replacement.ItemId);
        if (qualityDelta != 0 || plusDelta != 0)
        {
            var stats = _saveFile.CreateStatData(newItem.ItemInfo.ItemId);
            AddQuickhackTierModifier(stats, gamedataStatType.Quality, qualityDelta);
            AddQuickhackTierModifier(stats, gamedataStatType.IsItemPlus, plusDelta);
        }

        inventory.Items.Add(newItem);
        try
        {
            return SetQuickhack(cyberdeck, slot, newItem);
        }
        finally
        {
            inventory.Items.Remove(newItem);
        }
    }

    private static QuickhackRecord SelectSourceRecord(List<QuickhackRecord> candidates, int targetTier)
    {
        var records = candidates.Where(x => x.BasePlus == 0).ToList();
        if (records.Count == 0)
        {
            records = candidates;
        }

        return records.Where(x => x.BaseTier <= targetTier)
                   .OrderByDescending(x => x.BaseTier)
                   .FirstOrDefault()
               ?? records.OrderBy(x => x.BaseTier).First();
    }

    private static int GetRecordTier(TweakDBID itemId)
    {
        var quality = ResourceHelper.ItemClasses.GetValueOrDefault((ulong)itemId)?.Quality;
        var baseQuality = quality?
            .Replace("PlusPlus", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Plus", "", StringComparison.OrdinalIgnoreCase);
        return baseQuality switch
        {
            "Common" => 0,
            "Uncommon" => 1,
            "Rare" => 2,
            "Epic" => 3,
            "Legendary" => 4,
            _ => -1
        };
    }

    private static int GetRecordPlus(TweakDBID itemId)
    {
        var itemRecord = ResourceHelper.ItemClasses.GetValueOrDefault((ulong)itemId);
        var qualityPlus = itemRecord?.Quality?.EndsWith("PlusPlus", StringComparison.OrdinalIgnoreCase) == true
            ? 2
            : itemRecord?.Quality?.EndsWith("Plus", StringComparison.OrdinalIgnoreCase) == true
                ? 1
                : 0;
        var idText = itemId.ResolvedText;
        var match = idText == null ? Match.Empty : s_quickhackRecord.Match(idText);
        var idPlus = match.Groups["levelPlus"].Value.Equals("PlusPlus", StringComparison.OrdinalIgnoreCase) ||
                     match.Groups["endPlus"].Value.Equals("PlusPlus", StringComparison.OrdinalIgnoreCase)
            ? 2
            : match.Groups["levelPlus"].Success || match.Groups["endPlus"].Success
                ? 1
                : 0;
        return Math.Clamp(Math.Max(Math.Max(qualityPlus, idPlus), itemRecord?.IsItemPlus ?? 0), 0, 2);
    }

    private static string GetQuickhackFamily(TweakDBID itemId)
    {
        var idText = itemId.ResolvedText;
        var match = idText == null ? Match.Empty : s_quickhackSuffix.Match(idText);
        return match.Success ? idText.Substring(0, match.Index) : idText ?? itemId.ToString();
    }

    private static string GetQuickhackName(TweakDBID itemId, string family)
    {
        var displayName = ResourceHelper.GetDisplayName(itemId);
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        if (s_quickhackFamilyNames.Value.TryGetValue(family, out displayName))
        {
            return displayName;
        }

        return family.StartsWith("Items.", StringComparison.OrdinalIgnoreCase)
            ? family.Substring("Items.".Length)
            : family;
    }

    private static float GetStatTotal(SaveFileHelper saveFile, gameItemID itemId, gamedataStatType stat)
    {
        var modifiers = saveFile.GetStatsFromItemId(itemId)?.ModifiersBuffer?.Data as ModifiersBuffer;
        return modifiers?.Entries
            .OfType<gameConstantStatModifierData_Deprecated>()
            .Where(x => x.StatType == stat && x.ModifierType == gameStatModifierType.Additive)
            .Sum(x => (float)x.Value) ?? 0;
    }

    private static void AddQuickhackTierModifier(gameSavedStatsData stats, gamedataStatType stat, int value)
    {
        if (value == 0)
        {
            return;
        }

        var modifier = new gameConstantStatModifierData_Deprecated
        {
            ModifierType = gameStatModifierType.Additive,
            StatType = stat,
            Value = value
        };
        var modifiers = stats.ModifiersBuffer.Data as ModifiersBuffer;
        if (modifiers == null)
        {
            modifiers = new ModifiersBuffer();
            stats.ModifiersBuffer.Data = modifiers;
        }
        modifiers.Entries.Add(modifier);
    }

    public static bool SwapQuickhacks(ItemSlotPart first, ItemSlotPart second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (ReferenceEquals(first, second))
        {
            return false;
        }

        (first.ItemInfo, second.ItemInfo) = (second.ItemInfo, first.ItemInfo);
        return true;
    }

    private static ItemSlotPart CreateEmptyQuickhackSlot(int index) =>
        new()
        {
            ItemInfo = new ItemInfo { ItemId = new gameItemID(), ItemStructure = ItemStructure.None },
            AppearanceName = "None",
            AttachmentSlotTdbId = $"AttachmentSlots.CyberdeckProgram{index}",
            ItemAdditionalInfo = new ItemAdditionalInfo { RequiredLevel = float.MaxValue }
        };

    private static ItemSlotPart CreateRootSlotPart(gameItemID itemId) =>
        new()
        {
            ItemInfo = new ItemInfo { ItemId = CloneItemId(itemId) },
            AppearanceName = "None",
            ItemAdditionalInfo = new ItemAdditionalInfo { RequiredLevel = float.MaxValue }
        };

    private static gameItemID CloneItemId(gameItemID source) =>
        new()
        {
            Id = source.Id,
            RngSeed = source.RngSeed,
            UniqueCounter = source.UniqueCounter,
            Flags = source.Flags
        };

    private static bool IsSameItem(gameItemID first, gameItemID second) =>
        first != null && second != null &&
        first.Id == second.Id &&
        first.RngSeed == second.RngSeed &&
        first.UniqueCounter == second.UniqueCounter;
}
