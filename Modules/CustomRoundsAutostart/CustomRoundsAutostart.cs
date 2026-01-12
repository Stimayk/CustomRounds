using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CustomRoundsCore;

namespace CustomRoundsAutostart;

public class CustomRoundsAutostart : BasePlugin, IPluginConfig<CustomRoundsAutostartConfig>
{
    private readonly PluginCapability<ICustomRoundsApi?> _pluginCapability = new("cr:core");
    private readonly Random _random = new();
    private ICustomRoundsApi? _api;
    private int _mode;

    private int _rounds;
    private int _value;
    public override string ModuleName => "[CR] Autostart";
    public override string ModuleDescription => "";
    public override string ModuleAuthor => "E!N";
    public override string ModuleVersion => "v1.0.0";

    public CustomRoundsAutostartConfig Config { get; set; } = new();

    public void OnConfigParsed(CustomRoundsAutostartConfig config)
    {
        Config = config;
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();
        if (_api == null) return;

        _mode = Config.Mode;

        _value = _mode switch
        {
            1 or 2 => Config.Value,
            _ => _value
        };

        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
    }

    public override void Unload(bool hotReload)
    {
        DeregisterEventHandler<EventRoundStart>(OnRoundStart);
        RemoveListener<Listeners.OnMapStart>(OnMapStart);
    }

    private void OnMapStart(string mapName)
    {
        _rounds = 0;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (IsWarmup() || _api.IsCustomRound)
        {
            return HookResult.Continue;
        }

        switch (_mode)
        {
            case 1:
                _rounds++;
                if (_rounds >= _value)
                    ChooseRandom();
                break;

            case 2:
                if (_random.Next(0, 100) < _value)
                    ChooseRandom();
                break;
        }

        return HookResult.Continue;
    }

    private void ChooseRandom()
    {
        if (_api == null) return;

        var availableRounds = _api.GetAvailableRounds();

        var candidates = availableRounds
            .Where(roundName => !Config.IgnoreRounds.Contains(roundName))
            .ToList();

        if (candidates.Count == 0) return;

        var index = _random.Next(candidates.Count);
        var selectedRound = candidates[index];

        var success = _api.SetNextRound(selectedRound);

        if (success)
        {
            _rounds = 0;
        }
    }

    private static bool IsWarmup()
    {
        var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
        return gameRules is
        {
            WarmupPeriod: true
        };
    }
}