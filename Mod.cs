using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;
using Path = System.IO.Path;

namespace HardcoreStandardProfile;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.bbbbbilly.hardcorestandardprofile";
    public string Name { get; init; } = "HardcoreStandardProfile";
    public string Author { get; init; } = "BBBBilly";
    public List<string>? Contributors { get; init; } = null;
    public SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.2");
    public List<string>? Incompatibilities { get; init; } = null;
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = null;
    public string? Url { get; init; } = null;
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class HardcoreStandardProfilePatcher(
    TemplateTable templateTable,
    ModHelper modHelper,
    ICloner cloner
) : IOnLoad
{
    private const string VkboBagTpl = "5ab8ee7786f7742d8f33f0b9";
    private const string WaistPouchTpl = "5732ee6a24597719ae0c0281";

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        ApplyVkboBag();
        ApplyWaistPouch();
        ApplyProfile();

        return Task.CompletedTask;
    }

    private void ApplyVkboBag()
    {
        if (!templateTable.Items.TryGetValue(new MongoId(VkboBagTpl), out var item)) return;

        var props = item.Properties;
        if (props?.Grids == null) return;

        var grids = props.Grids.ToList();
        if (grids.Count == 0) return;

        grids[0].Properties!.CellsH = 3;
        grids[0].Properties!.CellsV = 4;
    }

    private void ApplyWaistPouch()
    {
        if (!templateTable.Items.TryGetValue(new MongoId(WaistPouchTpl), out var item)) return;

        var props = item.Properties;
        if (props == null) return;

        props.ExaminedByDefault = true;
        props.SizeWidth = 1;
        props.SizeHeight = 2;

        if (props.Grids == null) return;

        var grids = props.Grids.ToList();
        if (grids.Count == 0) return;

        grids[0].Properties!.CellsH = 1;
        grids[0].Properties!.CellsV = 2;
    }

    private void ApplyProfile()
    {
        if (!templateTable.Profiles.TryGetValue("Standard", out var standard)) return;

        var profileSides = cloner.Clone(standard);

        var modDir = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        var bearInv = modHelper.GetJsonDataFromFile<BotBaseInventory>(
            modDir, Path.Combine("data", "bear_inventory.json"));
        var usecInv = modHelper.GetJsonDataFromFile<BotBaseInventory>(
            modDir, Path.Combine("data", "usec_inventory.json"));

        if (bearInv == null || usecInv == null) return;

        profileSides.Bear!.Character!.Inventory = bearInv;
        profileSides.Usec!.Character!.Inventory = usecInv;

        templateTable.Profiles["Standard"] = profileSides;

        var toRemove = templateTable.Profiles.Keys
            .Where(k => !string.Equals(k, "Standard", StringComparison.Ordinal))
            .ToList();

        foreach (var key in toRemove)
        {
            templateTable.Profiles.Remove(key);
        }
    }
}