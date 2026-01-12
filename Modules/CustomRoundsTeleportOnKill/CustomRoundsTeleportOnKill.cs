using System.Text.Json;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CustomRoundsCore;

namespace CustomRoundsTeleportOnKill;

public class CustomRoundsTeleportOnKill : BasePlugin
{
    private readonly PluginCapability<ICustomRoundsApi?> _pluginCapability = new("cr:core");
    private ICustomRoundsApi? _api;
    private bool _tok;
    public override string ModuleName => "[CR] Teleport On Kill";
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
            if (!TryGetBool(settings, "tok")) return;
            _tok = true;
        };

        _api.OnCustomRoundEnd += (_, settings) =>
        {
            if (!TryGetBool(settings, "tok")) return;
            _tok = false;
        };

        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (!_tok) return HookResult.Continue;
        var victim = @event.Userid;
        var killer = @event.Attacker;

        var victimPos = victim?.PlayerPawn.Value?.AbsOrigin;
        if (victimPos != null)
        {
            killer?.PlayerPawn.Value?.Teleport(
                victimPos,
                killer.PlayerPawn.Value.AbsRotation,
                killer.PlayerPawn.Value.AbsVelocity
            );
        }

        return HookResult.Continue;
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