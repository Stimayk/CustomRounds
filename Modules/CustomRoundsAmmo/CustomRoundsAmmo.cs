using System.Text.Json;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Cvars;
using CustomRoundsCore;

namespace CustomRoundsAmmo;

public class CustomRoundsAmmo : BasePlugin
{
    private static int _default = -1;
    private readonly PluginCapability<ICustomRoundsApi?> _pluginCapability = new("cr:core");
    private ICustomRoundsApi? _api;

    private ConVar? _cvar;
    public override string ModuleName => "[CR] Ammo";
    public override string ModuleDescription => "";
    public override string ModuleAuthor => "E!N";
    public override string ModuleVersion => "v1.0.0";

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();
        if (_api is null)
            return;

        _cvar = ConVar.Find("sv_infinite_ammo");

        _api.OnCustomRoundStart += OnCustomRoundStart;
        _api.OnCustomRoundEnd += OnCustomRoundEnd;
    }

    public override void Unload(bool hotReload)
    {
        if (_api is null)
            return;
        _api.OnCustomRoundStart -= OnCustomRoundStart;
        _api.OnCustomRoundEnd -= OnCustomRoundEnd;
    }

    private void OnCustomRoundStart(string name, Dictionary<string, object> settings)
    {
        if (!TryGetInt(settings, "ammo", out var ammo)) return;
        _default = _cvar?.GetPrimitiveValue<int>() ?? 0;
        _cvar?.SetValue(ammo);
    }

    private void OnCustomRoundEnd(string name, Dictionary<string, object> settings)
    {
        if (!TryGetInt(settings, "ammo", out _)) return;
        if (_default == -1) return;
        _cvar?.SetValue(0);
        _default = -1;
    }

    private static bool TryGetInt(Dictionary<string, object> settings, string key, out int result)
    {
        result = 0;

        if (!settings.TryGetValue(key, out var value))
            return false;

        if (value is JsonElement { ValueKind: JsonValueKind.Number } e &&
            e.TryGetInt32(out result))
            return true;

        return int.TryParse(value.ToString(), out result);
    }
}