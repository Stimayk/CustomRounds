using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CustomRoundsCore;

namespace CustomRoundsWeapons;

public class CustomRoundsWeapons : BasePlugin
{
    private static readonly string[] WeaponsList =
    [
        "weapon_ak47", "weapon_aug", "weapon_awp", "weapon_bizon", "weapon_cz75a", "weapon_deagle", "weapon_elite", "weapon_famas", "weapon_fiveseven", "weapon_g3sg1",
        "weapon_galilar",
        "weapon_glock", "weapon_hkp2000", "weapon_m249", "weapon_m4a1", "weapon_m4a1_silencer", "weapon_mac10", "weapon_mag7", "weapon_mp5sd", "weapon_mp7", "weapon_mp9",
        "weapon_negev",
        "weapon_nova", "weapon_p250", "weapon_p90", "weapon_revolver", "weapon_sawedoff", "weapon_scar20", "weapon_sg556", "weapon_ssg08", "weapon_tec9", "weapon_ump45",
        "weapon_usp_silencer", "weapon_xm1014",
        "weapon_decoy", "weapon_flashbang", "weapon_hegrenade", "weapon_incgrenade", "weapon_molotov", "weapon_smokegrenade", "item_defuser", "item_cutters", "weapon_knife"
    ];

    private readonly List<string> _currentRoundWeapons = [];
    private readonly PluginCapability<ICustomRoundsApi?> _pluginCapability = new("cr:core");

    private readonly Dictionary<ulong, PlayerLoadout> _savedLoadouts = new();

    private ICustomRoundsApi? _api;
    private bool _noKnife;
    private bool _noWeapon;

    public override string ModuleName => "[CR] Weapons";
    public override string ModuleAuthor => "E!N";
    public override string ModuleVersion => "v1.0.0";

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();
        if (_api is null)
        {
            return;
        }

        _api.OnCustomRoundStart += OnCustomRoundStart;
        _api.OnCustomRoundEnd += OnCustomRoundEnd;
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }

    public override void Unload(bool hotReload)
    {
        if (_api is null)
        {
            return;
        }

        _api.OnCustomRoundStart -= OnCustomRoundStart;
        _api.OnCustomRoundEnd -= OnCustomRoundEnd;
        DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }

    private void OnCustomRoundEnd(string name, Dictionary<string, object> settings)
    {
        _currentRoundWeapons.Clear();
        _noWeapon = false;
        _noKnife = false;
    }

    private void OnCustomRoundStart(string name, Dictionary<string, object> settings)
    {
        if (TryGetBool(settings, "no_weapon", out var now))
        {
            _noWeapon = now;
        }

        if (TryGetBool(settings, "no_knife", out var nok))
        {
            _noKnife = nok;
        }

        if (settings.TryGetValue("Weapons", out var weaponsObj) &&
            weaponsObj is JsonElement
            {
                ValueKind: JsonValueKind.Object
            } element)
        {
            foreach (var property in element.EnumerateObject())
            {
                _currentRoundWeapons.Add(property.Name);
            }
        }

        var players = Utilities.GetPlayers().Where(x => x is
        {
            IsValid: true,
            PawnIsAlive: true
        });

        foreach (var player in players)
        {
            if (_noWeapon)
            {
                SaveLoadout(player);
                player.RemoveWeapons();
            }

            if (!_noKnife)
            {
                player.GiveNamedItem("weapon_knife");
            }

            foreach (var weaponName in _currentRoundWeapons)
            {
                player.GiveNamedItem(weaponName);
            }
        }

        if (!TryGetBool(settings, "clear_map", out var cm)) return;
        if (cm)
            ClearGroundWeapons();
    }

    [GameEventHandler]
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.PawnIsAlive) return HookResult.Continue;

        if (_api is
            {
                IsCustomRound: false
            } &&
            _savedLoadouts.ContainsKey(player.SteamID))
        {
            Server.NextFrame(() => RestoreWeapons(player));
        }

        return HookResult.Continue;
    }

    private void SaveLoadout(CCSPlayerController player)
    {
        if (_savedLoadouts.ContainsKey(player.SteamID)) return;

        var loadout = new PlayerLoadout();
        var pawn = player.PlayerPawn.Value;
        if (pawn?.WeaponServices == null) return;

        foreach (var weaponHandle in pawn.WeaponServices.MyWeapons)
        {
            if (weaponHandle.Value is not { IsValid: true } weapon) continue;

            loadout.Weapons.Add(weapon.DesignerName);
        }

        _savedLoadouts[player.SteamID] = loadout;
    }

    private void RestoreWeapons(CCSPlayerController player)
    {
        if (!player.IsValid || !player.PawnIsAlive) return;
        if (!_savedLoadouts.TryGetValue(player.SteamID, out var loadout)) return;

        player.RemoveWeapons();

        foreach (var weaponName in loadout.Weapons)
        {
            player.GiveNamedItem(weaponName);
        }

        _savedLoadouts.Remove(player.SteamID);
    }

    private static void ClearGroundWeapons()
    {
        foreach (var weapons in WeaponsList)
        {
            foreach (var entity in Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(weapons))
            {
                if (entity.Entity == null) continue;
                if (entity.OwnerEntity.IsValid) continue;

                entity.Remove();
            }
        }
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

    private class PlayerLoadout
    {
        public List<string> Weapons { get; } = [];
    }
}