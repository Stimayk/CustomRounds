using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CustomRoundsCore;

namespace CustomRoundsSC;

public class CustomRoundsSc : BasePlugin
{
    private readonly PluginCapability<ICustomRoundsApi?> _pluginCapability = new("cr:core");
    private ICustomRoundsApi? _api;
    public override string ModuleName => "[CR] Server Commands";
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
            if (TryGetString(settings, "cmd", out var cmd))
            {
                Server.ExecuteCommand(cmd);
            }
        };

        _api.OnCustomRoundEnd += (_, settings) =>
        {
            if (TryGetString(settings, "cmd_end", out var cmdEnd))
            {
                Server.ExecuteCommand(cmdEnd);
            }
        };
    }

    private static bool TryGetString(Dictionary<string, object> settings, string key, out string result)
    {
        result = string.Empty;

        if (!settings.TryGetValue(key, out var value))
            return false;

        switch (value)
        {
            case string str:
                result = str;
                return true;
            case JsonElement
            {
                ValueKind: JsonValueKind.String
            } e:
                result = e.GetString() ?? string.Empty;
                return true;
            case JsonElement e:
                result = e.ToString();
                return true;
            default:
                result = value.ToString() ?? string.Empty;
                return true;
        }
    }
}