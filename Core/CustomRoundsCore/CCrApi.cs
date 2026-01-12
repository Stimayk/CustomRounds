using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Extensions;

namespace CustomRoundsCore;

public class CCrApi(CustomRoundsCore plugin) : ICustomRoundsApi
{
    public event Action<string, Dictionary<string, object>>? OnCustomRoundStart;
    public event Action<string, Dictionary<string, object>>? OnCustomRoundEnd;
    public event Action<CCSPlayerController, Dictionary<string, object>>? OnCustomRoundPlayerSpawn;

    public event Action<string, CCSPlayerController?>? OnSetNextRound;
    public event Action<string, CCSPlayerController?>? OnForceRoundStart;
    public event Action<string, CCSPlayerController?>? OnClearNextRound;
    public event Action<string, CCSPlayerController?>? OnStopCurrentRound;

    public event Func<string, CCSPlayerController?, bool>? CanSetNextRound;
    public event Func<string, CCSPlayerController?, bool>? CanStartRound;
    public event Func<string, CCSPlayerController?, bool>? CanClearNextRound;
    public event Func<string, CCSPlayerController?, bool>? CanStopCurrentRound;

    public bool IsCustomRound => plugin.CurrentRoundName != null;
    public bool IsNextRoundCustom => plugin.NextRoundName != null;
    public bool IsRoundEnd => plugin.IsRoundEndPhase;
    public string? CurrentRoundName => plugin.CurrentRoundName;
    public string? NextRoundName => plugin.NextRoundName;

    public bool IsRoundExists(string roundName)
    {
        return plugin.Config.Rounds.ContainsKey(roundName);
    }

    public T GetRoundSetting<T>(string roundName, string settingKey, T defaultValue)
    {
        Dictionary<string, object>? roundSettings;

        if (plugin.CurrentRoundName == roundName)
            roundSettings = plugin.GetSettingsForRound(roundName, plugin.CurrentRoundVirtualSettings);
        else if (plugin.NextRoundName == roundName)
            roundSettings = plugin.GetSettingsForRound(roundName, plugin.NextRoundVirtualSettings);
        else
            plugin.Config.Rounds.TryGetValue(roundName, out roundSettings);

        if (roundSettings == null || !roundSettings.TryGetValue(settingKey, out var value))
            return defaultValue;

        try
        {
            if (value is JsonElement jsonElement)
                return jsonElement.Deserialize<T>() ?? defaultValue;
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch { return defaultValue; }
    }

    public Dictionary<string, object>? GetCurrentRoundSettings()
    {
        return plugin.CurrentRoundName == null ? null : plugin.GetSettingsForRound(plugin.CurrentRoundName, plugin.CurrentRoundVirtualSettings);
    }

    public Dictionary<string, object>? GetNextRoundSettings()
    {
        return plugin.NextRoundName == null ? null : plugin.GetSettingsForRound(plugin.NextRoundName, plugin.NextRoundVirtualSettings);
    }

    public Dictionary<string, Dictionary<string, object>> GetFullConfig()
    {
        return plugin.Config.Rounds;
    }

    public List<string> GetAvailableRounds()
    {
        return plugin.Config.Rounds.Keys.ToList();
    }

    public bool SetNextRound(string roundName, CCSPlayerController? initiator = null)
    {
        return plugin.Config.Rounds.ContainsKey(roundName) && InternalSetNextRound(roundName, null, initiator);
    }

    public bool SetNextRound(string roundName, Dictionary<string, object> customSettings, CCSPlayerController? initiator = null)
    {
        return InternalSetNextRound(roundName, customSettings, initiator);
    }

    public bool ClearNextRound(CCSPlayerController? initiator = null)
    {
        if (plugin.NextRoundName == null) return false;

        if (initiator != null && !CheckBlockers(CanClearNextRound, plugin.NextRoundName, initiator)) return false;

        var clearedName = plugin.NextRoundName;
        plugin.NextRoundName = null;
        plugin.NextRoundVirtualSettings = null;

        OnClearNextRound?.Invoke(clearedName, initiator);
        return true;
    }

    public bool StartRound(string roundName, CCSPlayerController? initiator = null)
    {
        return SetNextRound(roundName, initiator) && InternalStartRound(roundName, initiator);
    }

    public bool StartRound(string roundName, Dictionary<string, object> customSettings, CCSPlayerController? initiator = null)
    {
        return SetNextRound(roundName, customSettings, initiator) && InternalStartRound(roundName, initiator);
    }

    public bool StopCurrentRound(CCSPlayerController? initiator = null)
    {
        if (!IsCustomRound) return false;

        var roundName = plugin.CurrentRoundName!;
        var settings = plugin.GetSettingsForRound(roundName, plugin.CurrentRoundVirtualSettings);

        if (initiator != null && !CheckBlockers(CanStopCurrentRound, roundName, initiator)) return false;

        plugin.CurrentRoundName = null;
        plugin.CurrentRoundVirtualSettings = null;

        if (settings != null)
        {
            InvokeOnCustomRoundEnd(roundName, settings);
        }

        OnStopCurrentRound?.Invoke(roundName, initiator);

        ForceEndRound();
        return true;
    }

    public void ReloadConfig()
    {
        plugin.Config.Reload();
    }

    internal void InvokeOnCustomRoundStart(string name, Dictionary<string, object> settings)
    {
        OnCustomRoundStart?.Invoke(name, settings);
    }

    internal void InvokeOnCustomRoundEnd(string name, Dictionary<string, object> settings)
    {
        OnCustomRoundEnd?.Invoke(name, settings);
    }

    internal void InvokeOnPlayerSpawn(CCSPlayerController player, Dictionary<string, object> settings)
    {
        OnCustomRoundPlayerSpawn?.Invoke(player, settings);
    }

    private static bool CheckBlockers(MulticastDelegate? delegateList, params object[] args)
    {
        if (delegateList == null) return true;
        foreach (var handler in delegateList.GetInvocationList())
        {
            if (handler is not Func<string, CCSPlayerController?, bool> func) continue;
            if (!func((string)args[0], (CCSPlayerController?)args[1])) return false;
        }

        return true;
    }

    private bool InternalSetNextRound(string roundName, Dictionary<string, object>? customSettings, CCSPlayerController? initiator)
    {
        if (initiator != null && !CheckBlockers(CanSetNextRound, roundName, initiator)) return false;

        plugin.NextRoundName = roundName;
        plugin.NextRoundVirtualSettings = customSettings;

        OnSetNextRound?.Invoke(roundName, initiator);
        return true;
    }

    private bool InternalStartRound(string roundName, CCSPlayerController? initiator)
    {
        if (initiator != null && !CheckBlockers(CanStartRound, roundName, initiator))
        {
            plugin.NextRoundName = null;
            plugin.NextRoundVirtualSettings = null;
            return false;
        }

        OnForceRoundStart?.Invoke(roundName, initiator);

        ForceEndRound();
        return true;
    }

    private static void ForceEndRound()
    {
        Server.NextFrame(() =>
        {
            var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
            gameRules?.TerminateRound(0.0f, RoundEndReason.RoundDraw);
        });
    }
}