using CounterStrikeSharp.API.Core;

namespace CustomRoundsCore;

public class CustomRoundsCoreConfig : BasePluginConfig
{
    public Dictionary<string, Dictionary<string, object>> Rounds { get; set; } = new()
    {
        ["OnlyHead"] = new Dictionary<string, object>
        {
            ["hp"] = 500,
            ["only_headshot"] = true
        },
        ["KnifeRound"] = new Dictionary<string, object>
        {
            ["hp"] = 35,
            ["gravity"] = 0.8
        }
    };
}