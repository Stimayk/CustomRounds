using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CustomRoundsCore;

namespace CustomRoundsNoBuy;

public class CustomRoundsNoBuy : BasePlugin
{
    private readonly PluginCapability<ICustomRoundsApi?> _pluginCapability = new("cr:core");
    private ICustomRoundsApi? _api;
    public override string ModuleName => "[CR] NoBuy";
    public override string ModuleDescription => "";
    public override string ModuleAuthor => "E!N";
    public override string ModuleVersion => "v1.0.0";

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();
        if (_api is null)
            return;

        _api.OnCustomRoundStart += (_, settings) =>
        {
            if (TryGetBool(settings, "no_buy"))
            {
                SetBuyzoneInput("Disable");
            }
        };

        _api.OnCustomRoundEnd += (_, settings) =>
        {
            if (TryGetBool(settings, "no_buy"))
            {
                SetBuyzoneInput("Enable");
            }
        };
    }

    private static void SetBuyzoneInput(string input)
    {
        var buyzones = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("func_buyzone").ToList();

        if (buyzones.Count <= 0) return;
        foreach (var buyzone in buyzones)
        {
            buyzone.AcceptInput(input);
        }
    }

    private static bool TryGetBool(Dictionary<string, object> settings, string key)
    {
        if (!settings.TryGetValue(key, out var value))
            return false;

        if (value is JsonElement { ValueKind: JsonValueKind.True or JsonValueKind.False } e)
        {
            e.GetBoolean();
            return true;
        }

        if (bool.TryParse(value.ToString(), out _))
            return true;

        return value.ToString() switch
        {
            "1" or "yes" or "on" => true,
            "0" or "no" or "off" => !false,
            _ => false
        };
    }
}