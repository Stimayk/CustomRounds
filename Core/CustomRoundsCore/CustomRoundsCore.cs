using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;

namespace CustomRoundsCore;

public class CustomRoundsCore : BasePlugin, IPluginConfig<CustomRoundsCoreConfig>
{
    private readonly PluginCapability<ICustomRoundsApi> _pluginCapability = new("cr:core");
    private CCrApi? _api;
    public override string ModuleName => "[CORE] Custom Rounds";
    public override string ModuleDescription => "";
    public override string ModuleAuthor => "E!N";
    public override string ModuleVersion => "v1.0.0";

    public string? CurrentRoundName { get; set; }
    public string? NextRoundName { get; set; }

    public Dictionary<string, object>? CurrentRoundVirtualSettings { get; set; }
    public Dictionary<string, object>? NextRoundVirtualSettings { get; set; }

    public bool IsRoundEndPhase { get; private set; }

    public CustomRoundsCoreConfig Config { get; set; } = new();

    public void OnConfigParsed(CustomRoundsCoreConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        _api = new CCrApi(this);
        Capabilities.RegisterPluginCapability(_pluginCapability, () => _api);

        RegisterEventHandler<EventRoundPrestart>(OnRoundPrestart);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
    }

    public override void Unload(bool hotReload)
    {
        DeregisterEventHandler<EventRoundPrestart>(OnRoundPrestart);
        DeregisterEventHandler<EventRoundStart>(OnRoundStart);
        DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
        DeregisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RemoveListener<Listeners.OnMapStart>(OnMapStart);
    }

    private void OnMapStart(string mapName)
    {
        IsRoundEndPhase = false;
        CurrentRoundName = null;
        NextRoundName = null;
        CurrentRoundVirtualSettings = null;
        NextRoundVirtualSettings = null;
    }

    private HookResult OnRoundPrestart(EventRoundPrestart @event, GameEventInfo info)
    {
        IsRoundEndPhase = false;

        if (NextRoundName != null)
        {
            CurrentRoundName = NextRoundName;
            CurrentRoundVirtualSettings = NextRoundVirtualSettings;

            NextRoundName = null;
            NextRoundVirtualSettings = null;
        }
        else
        {
            CurrentRoundName = null;
            CurrentRoundVirtualSettings = null;
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        IsRoundEndPhase = false;

        if (CurrentRoundName == null) return HookResult.Continue;

        var settings = GetSettingsForRound(CurrentRoundName, CurrentRoundVirtualSettings);
        if (settings != null)
        {
            _api?.InvokeOnCustomRoundStart(CurrentRoundName, settings);
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        IsRoundEndPhase = true;

        if (CurrentRoundName == null) return HookResult.Continue;
        var settings = GetSettingsForRound(CurrentRoundName, CurrentRoundVirtualSettings);

        if (settings != null)
        {
            _api?.InvokeOnCustomRoundEnd(CurrentRoundName, settings);
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (CurrentRoundName == null)
            return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || !player.Pawn.IsValid)
            return HookResult.Continue;

        var settings = GetSettingsForRound(CurrentRoundName, CurrentRoundVirtualSettings);
        if (settings != null)
        {
            _api?.InvokeOnPlayerSpawn(player, settings);
        }

        return HookResult.Continue;
    }

    public Dictionary<string, object>? GetSettingsForRound(string roundName, Dictionary<string, object>? virtualSettings)
    {
        return virtualSettings ?? Config.Rounds.GetValueOrDefault(roundName);
    }
}