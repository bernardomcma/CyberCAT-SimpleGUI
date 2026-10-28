using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using WolvenKit.RED4.Types;

namespace CP2077SaveEditor.Utils;

public class ItemRecord
{
    public string Type { get; set; }
    public bool IsSingleInstance { get; set; }
    public int IsItemPlus { get; set; }
    public List<SlotPartRecord> SlotParts { get; set; }
}

public class SlotPartRecord
{
    public ulong ItemPartPreset { get; set; }
    public ulong Slot { get; set; }
}

public class FastTravelRecord
{
    public string PointRecord { get; set; }
    public string MarkerRef { get; set; }
}

public record ModifierGroup(string Name, uint CRC, List<string> StatTypes);

public static class ResourceHelper
{
    public static readonly Dictionary<ulong, ItemRecord> ItemClasses = JsonSerializer.Deserialize<Dictionary<ulong, ItemRecord>>(Properties.Resources.ItemClasses);
    public static readonly List<FastTravelRecord> FastTravelRecords = JsonSerializer.Deserialize<List<FastTravelRecord>>(Properties.Resources.FastTravel);
    public static readonly List<ModifierGroup> ModifierGroups = JsonSerializer.Deserialize<List<ModifierGroup>>(Properties.Resources.ModifierGroups);

    // In-game names of items and vehicles, keyed by TweakDBID hash
    public static readonly Dictionary<ulong, string> DisplayNames = LoadDisplayNames();

    public static string GetDisplayName(TweakDBID id) => DisplayNames.GetValueOrDefault((ulong)id);

    private static Dictionary<ulong, string> LoadDisplayNames()
    {
        var allLanguages = JsonSerializer.Deserialize<Dictionary<string, Dictionary<ulong, string>>>(Properties.Resources.DisplayNames);

        // English as the base, so records without a translation still get a name
        var names = new Dictionary<ulong, string>(allLanguages["en-us"]);

        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
        {
            "pt" => "pt-br",
            _ => null
        };

        if (language != null && allLanguages.TryGetValue(language, out var localized))
        {
            foreach (var (id, name) in localized)
            {
                names[id] = name;
            }
        }

        return names;
    }
}