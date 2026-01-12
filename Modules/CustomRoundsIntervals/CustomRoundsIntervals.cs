using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CustomRoundsCore;

namespace CustomRoundsIntervals;

public class CustomRoundsIntervals : BasePlugin, IPluginConfig<CustomRoundsIntervalsConfig>
{
    private readonly PluginCapability<ICustomRoundsApi?> _pluginCapability = new("cr:core");
    private ICustomRoundsApi? _api;
    private int _rounds;
    public override string ModuleName => "[CR] Intervals";
    public override string ModuleDescription => "";
    public override string ModuleAuthor => "E!N";
    public override string ModuleVersion => "v1.0.0";

    public CustomRoundsIntervalsConfig Config { get; set; } = new();

    public void OnConfigParsed(CustomRoundsIntervalsConfig config)
    {
        Config = config;
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();
        if (_api is null)
            return;

        _api.CanSetNextRound += CheckCanSetNextRound;
        _api.CanStartRound += CheckCanStartRound;
        _api.OnCustomRoundStart += OnRoundStart;

        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
    }

    public override void Unload(bool hotReload)
    {
        if (_api == null) return;
        _api.CanSetNextRound -= CheckCanSetNextRound;
        _api.CanStartRound -= CheckCanStartRound;
        _api.OnCustomRoundStart -= OnRoundStart;
        DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RemoveListener<Listeners.OnMapStart>(OnMapStart);
    }

    private bool CheckCanSetNextRound(string roundName, CCSPlayerController? controller)
    {
        if (_rounds <= 0) return true;
        controller?.PrintToChat(Localizer["cr.intervals.warning", _rounds]);
        return false;
    }

    private bool CheckCanStartRound(string roundName, CCSPlayerController? controller)
    {
        if (_rounds <= 0) return true;
        controller?.PrintToChat(Localizer["cr.intervals.warning", _rounds]);
        return false;
    }

    private void OnRoundStart(string name, Dictionary<string, object> settings)
    {
        _rounds = Config.Interval;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (_rounds > 0)
        {
            _rounds--;
        }

        return HookResult.Continue;
    }

    private void OnMapStart(string mapName)
    {
        _rounds = 0;
    }
}