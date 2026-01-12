using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CustomRoundsCore;

namespace CustomRoundsChat;

public class CustomRoundsChat : BasePlugin
{
    private readonly PluginCapability<ICustomRoundsApi?> _pluginCapability = new("cr:core");
    private ICustomRoundsApi? _api;
    public override string ModuleName => "[CR] Chat";
    public override string ModuleDescription => "";
    public override string ModuleAuthor => "E!N";
    public override string ModuleVersion => "v1.0.0";

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();
        if (_api is null)
            return;

        _api.OnSetNextRound += (name, controller) =>
        {
            Server.PrintToChatAll(_api.IsNextRoundCustom
                ? Localizer["cr.chat.change.next.round", name, controller?.PlayerName ?? "Server"]
                : Localizer["cr.chat.set.next.round", name, controller?.PlayerName ?? "Server"]);
        };
        _api.OnForceRoundStart += (name, controller) =>
        {
            Server.PrintToChatAll(Localizer["cr.chat.set.current.round", name, controller?.PlayerName ?? "Server"]);
        };
        _api.OnClearNextRound += (name, controller) =>
        {
            Server.PrintToChatAll(Localizer["cr.chat.clear.next.round", name, controller?.PlayerName ?? "Server"]);
        };
        _api.OnCustomRoundStart += (name, _) =>
        {
            Server.PrintToChatAll(Localizer["cr.chat.round.start", name]);
        };
        _api.OnStopCurrentRound += (name, controller) =>
        {
            Server.PrintToChatAll(Localizer["cr.chat.stop.current.round", name, controller?.PlayerName ?? "Server"]);
        };
    }
}