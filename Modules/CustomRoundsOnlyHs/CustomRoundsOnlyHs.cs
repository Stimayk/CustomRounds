using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Cvars;
using CustomRoundsCore;

namespace CustomRoundsOnlyHs;

public class CustomRoundsOnlyHs : BasePlugin
{
    private readonly PluginCapability<ICustomRoundsApi?> _pluginCapability = new("cr:core");
    private ICustomRoundsApi? _api;
    public override string ModuleName => "[CR] Only HS";
    public override string ModuleDescription => "";
    public override string ModuleAuthor => "E!N";
    public override string ModuleVersion => "v1.1.0";

    private ConVar? _cvar;
    private static int _default = -1;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();
        if (_api is null)
            return;

        _cvar = ConVar.Find("mp_damage_headshot_only");
        
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
        if (!TryGetBool(settings, "only_hs", out var mode)) return;
        _default = _cvar?.GetPrimitiveValue<int>() ?? 0;
        _cvar?.SetValue(mode);
    }
    
    private void OnCustomRoundEnd(string name, Dictionary<string, object> settings)
    {
        if (!TryGetBool(settings, "only_hs", out var mode)) return;
        if (_default == -1) return;
        _cvar?.SetValue(_default);
        _default = -1;
    }

    private static bool TryGetBool(Dictionary<string, object> settings, string key, out bool result)
    {
        result = false;

        if (!settings.TryGetValue(key, out var value))
            return false;

        if (value is JsonElement { ValueKind: JsonValueKind.True or JsonValueKind.False } e)
        {
            result = e.GetBoolean();
            return true;
        }

        if (bool.TryParse(value.ToString(), out result))
            return true;

        return value.ToString() switch
        {
            "1" or "yes" or "on" => result = true,
            "0" or "no" or "off" => !(result = false),
            _ => false
        };
    }
}