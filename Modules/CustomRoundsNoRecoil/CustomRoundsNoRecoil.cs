using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CustomRoundsCore;

namespace CustomRoundsNoRecoil;

public class CustomRoundsNoRecoil : BasePlugin
{
    private readonly PluginCapability<ICustomRoundsApi?> _pluginCapability = new("cr:core");
    private ICustomRoundsApi? _api;
    private bool _nr;
    public override string ModuleName => "[CR] NoRecoil";
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
            if (!TryGetBool(settings, "nr")) return;
            _nr = true;
            if (_nr)
            {
                Server.ExecuteCommand("weapon_accuracy_nospread 1");
            }
        };

        _api.OnCustomRoundEnd += (_, settings) =>
        {
            if (!TryGetBool(settings, "nr")) return;
            _nr = false;
            if (_nr)
            {
                Server.ExecuteCommand("weapon_accuracy_nospread 0");
            }
        };

        RegisterListener<Listeners.OnTick>(() =>
        {
            if (!_nr) return;
            foreach (var player in GetOnlinePlayers().Where(p => p.PawnIsAlive))
            {
                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid || pawn.CameraServices == null) continue;
                pawn.AimPunchTickBase = 0;
                pawn.AimPunchTickFraction = 0f;
                pawn.CameraServices.CsViewPunchAngleTick = 0;
                pawn.CameraServices.CsViewPunchAngleTickRatio = 0f;
            }
        });
    }

    public override void Unload(bool hotReload)
    {
        RemoveListener<Listeners.OnTick>(() =>
        {
            if (!_nr) return;
            foreach (var player in GetOnlinePlayers().Where(p => p.PawnIsAlive))
            {
                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid || pawn.CameraServices == null) continue;
                pawn.AimPunchTickBase = 0;
                pawn.AimPunchTickFraction = 0f;
                pawn.CameraServices.CsViewPunchAngleTick = 0;
                pawn.CameraServices.CsViewPunchAngleTickRatio = 0f;
            }
        });
    }

    private static List<CCSPlayerController> GetOnlinePlayers()
    {
        return Utilities.GetPlayers().Where(p => p is
        {
            IsValid: true,
            IsBot: false,
            Connected: PlayerConnectedState.PlayerConnected
        }).ToList();
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