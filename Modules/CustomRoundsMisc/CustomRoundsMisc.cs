using System.Drawing;
using System.Globalization;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CustomRoundsCore;

namespace CustomRoundsMisc;

public sealed class CustomRoundsMisc : BasePlugin
{
    private readonly PluginCapability<ICustomRoundsApi?> _pluginCapability = new("cr:core");
    private ICustomRoundsApi? _api;

    public override string ModuleName => "[CR] Misc";
    public override string ModuleDescription => string.Empty;
    public override string ModuleAuthor => "E!N";
    public override string ModuleVersion => "v1.0.0";

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();
        if (_api is null)
            return;

        _api.OnCustomRoundPlayerSpawn += OnCustomRoundPlayerSpawn;
        _api.OnCustomRoundEnd += (_, settings) => ResetAllPlayers(settings);
    }

    public override void Unload(bool hotReload)
    {
        if (_api is null)
            return;

        _api.OnCustomRoundPlayerSpawn -= OnCustomRoundPlayerSpawn;
    }

    private static void OnCustomRoundPlayerSpawn(CCSPlayerController player, Dictionary<string, object> settings)
    {
        Server.NextFrame(() =>
        {
            if (player is not { IsValid: true, PlayerPawn.IsValid: true })
                return;

            var pawn = player.PlayerPawn.Value;
            if (pawn is null || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return;

            if (TryGetFloat(settings, "gravity", out var gravity))
            {
                pawn.GravityScale = gravity;
            }

            if (TryGetFloat(settings, "speed", out var speed))
            {
                pawn.VelocityModifier = speed;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
            }

            if (TryGetInt(settings, "invisibility", out var alpha))
            {
                alpha = Math.Clamp(alpha, 0, 255);
                pawn.Render = Color.FromArgb(alpha, pawn.Render);
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            }

            if (TryGetInt(settings, "hp", out var hp))
            {
                pawn.MaxHealth = hp;
                pawn.Health = hp;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }

            if (TryGetInt(settings, "armor", out var armor))
            {
                pawn.ArmorValue = armor;
            }

            if (TryGetBool(settings, "helmet", out var hasHelmet) &&
                pawn.ItemServices is not null)
            {
                new CCSPlayer_ItemServices(pawn.ItemServices.Handle).HasHelmet = hasHelmet;
            }
        });
    }

    private static void ResetAllPlayers(Dictionary<string, object> settings)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player is not { IsValid: true, PlayerPawn.IsValid: true })
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn is null)
                continue;

            if (TryGetInt(settings, "invisibility", out _))
            {
                pawn.Render = Color.White;
                Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            }

            if (TryGetFloat(settings, "gravity", out _))
            {
                pawn.GravityScale = 1.0f;
            }

            if (TryGetFloat(settings, "speed", out _))
            {
                pawn.VelocityModifier = 1.0f;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }
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

    private static bool TryGetFloat(Dictionary<string, object> settings, string key, out float result)
    {
        result = 1.0f;

        if (!settings.TryGetValue(key, out var value))
            return false;

        if (value is JsonElement { ValueKind: JsonValueKind.Number } e &&
            e.TryGetSingle(out result))
            return true;

        return float.TryParse(
            value.ToString(),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out result
        );
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