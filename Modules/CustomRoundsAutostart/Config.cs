using CounterStrikeSharp.API.Core;

namespace CustomRoundsAutostart;

public class CustomRoundsAutostartConfig : BasePluginConfig
{
    public int Mode { get; set; } = 1;
    public int Value { get; set; } = 6;
    public List<string> IgnoreRounds { get; set; } = [];
}