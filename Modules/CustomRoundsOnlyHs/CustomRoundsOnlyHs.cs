using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CustomRoundsCore;

namespace CustomRoundsOnlyHs;

public class CustomRoundsOnlyHs : BasePlugin
{
    private readonly PluginCapability<ICustomRoundsApi?> _pluginCapability = new("cr:core");
    private ICustomRoundsApi? _api;
    private bool _onlyhs;
    public override string ModuleName => "[CR] Only HS";
    public override string ModuleDescription => "";
    public override string ModuleAuthor => "E!N";
    public override string ModuleVersion => "v1.0.0";

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();
        if (_api is null)
            return;

        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        _api.OnCustomRoundEnd += (_, _) => _onlyhs = false;
    }

    public override void Unload(bool hotReload)
    {
        DeregisterEventHandler<EventRoundStart>(OnRoundStart);
        DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (_api is
            not
            {
                IsCustomRound: true
            }) return HookResult.Continue;

        var settings = _api.GetCurrentRoundSettings();
        if (settings is null) return HookResult.Continue;

        if (TryGetBool(settings, "only_hs", out var mode))
        {
            _onlyhs = mode;
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        if (!_onlyhs) return HookResult.Continue;
        var victim = @event.Userid;
        var damage = @event.DmgHealth;
        var hitgroup = (HitGroup_t)@event.Hitgroup;

        if (hitgroup != HitGroup_t.HITGROUP_HEAD)
            RestoreHealth(victim!, damage);
        return HookResult.Continue;
    }

    private static void RestoreHealth(CCSPlayerController victim, float damage)
    {
        var playerPawn = victim.PlayerPawn.Value;
        if (playerPawn == null || !playerPawn.IsValid) return;
        var newHealth = playerPawn.Health + damage;

        if (newHealth > 100)
            newHealth = 100;

        playerPawn.Health = (int)newHealth;
        Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
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