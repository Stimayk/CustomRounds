using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CustomRoundsCore;

namespace CustomRoundsNoZoom;

public class CustomRoundsNoZoom : BasePlugin
{
    private readonly PluginCapability<ICustomRoundsApi?> _pluginCapability = new("cr:core");
    private ICustomRoundsApi? _api;
    private bool _nz;
    public override string ModuleName => "[CR] NoZoom";
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
            if (TryGetBool(settings, "nz"))
            {
                _nz = true;
            }
        };

        _api.OnCustomRoundEnd += (_, settings) =>
        {
            if (TryGetBool(settings, "nz"))
            {
                _nz = false;
            }
        };

        RegisterListener<Listeners.OnTick>(() =>
        {
            if (!_nz) return;
            foreach (var player in GetOnlinePlayers().Where(p => p.PawnIsAlive))
            {
                CheckZoom(player);
            }
        });
    }

    public override void Unload(bool hotReload)
    {
        RemoveListener<Listeners.OnTick>(() =>
        {
            if (!_nz) return;
            foreach (var player in GetOnlinePlayers().Where(p => p.PawnIsAlive))
            {
                CheckZoom(player);
            }
        });
    }

    private static void CheckZoom(CCSPlayerController player)
    {
        var activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon?.DesignerName != null &&
            (activeWeapon.DesignerName.Contains("weapon_ssg08") ||
             activeWeapon.DesignerName.Contains("weapon_awp") ||
             activeWeapon.DesignerName.Contains("weapon_scar20") ||
             activeWeapon.DesignerName.Contains("weapon_g3sg1") ||
             activeWeapon.DesignerName.Contains("weapon_sg556") ||
             activeWeapon.DesignerName.Contains("weapon_aug")))
        {
            activeWeapon.NextSecondaryAttackTick = Server.TickCount + 500;
        }
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