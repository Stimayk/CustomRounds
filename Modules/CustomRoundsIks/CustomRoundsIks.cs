using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Menu;
using CustomRoundsCore;
using IksAdminApi;

namespace CustomRoundsIks;

public class CustomRoundsIks : BasePlugin, IPluginConfig<CustomRoundsIksConfig>
{
    private readonly PluginCapability<ICustomRoundsApi?> _pluginCapability = new("cr:core");
    private ICustomRoundsApi? _api;
    public override string ModuleName => "[CR] IKS";
    public override string ModuleDescription => "";
    public override string ModuleAuthor => "E!N";
    public override string ModuleVersion => "v1.0.0";

    public CustomRoundsIksConfig Config { get; set; } = new();

    public void OnConfigParsed(CustomRoundsIksConfig config)
    {
        Config = config;
    }

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        _api = _pluginCapability.Get();
        AdminModule.Api.MenuOpenPre += OnMenuOpenPre;
        AdminModule.Api.RegisterPermission("other.cr", Config.AdminPermissionFlags);
    }

    public override void Unload(bool hotReload)
    {
        AdminModule.Api.MenuOpenPre -= OnMenuOpenPre;
        AdminModule.Api.RegistredPermissions.Remove("other.cr");
    }

    private HookResult OnMenuOpenPre(CCSPlayerController player, IDynamicMenu menu, IMenu gameMenu)
    {
        if (menu.Id != "iksadmin:menu:main" || _api == null)
        {
            return HookResult.Continue;
        }

        menu.AddMenuOption("cr", Localizer["MenuOption.CustomRounds"], (_, _) => { OpenCrMainMenu(player, menu); },
            viewFlags: AdminUtils.GetCurrentPermissionFlags("other.cr"));

        return HookResult.Continue;
    }

    private void OpenCrMainMenu(CCSPlayerController caller, IDynamicMenu? backMenu = null!)
    {
        if (_api == null) return;

        var menu = AdminModule.Api.CreateMenu(
            "cr.main",
            Localizer["MenuTitle.Main"],
            backMenu: backMenu
        );

        menu.AddMenuOption("cr.choose", Localizer["MenuOption.ChooseRound"], (p, _) =>
        {
            OpenRoundListMenu(p, menu);
        });

        if (_api.IsCustomRound)
        {
            menu.AddMenuOption("cr.stop", Localizer["MenuOption.StopCurrent"], (p, _) =>
            {
                if (_api.IsCustomRound)
                {
                    _api.StopCurrentRound(p);
                    p.Print(Localizer["Msg.RoundStopped"]);
                }
                else
                {
                    p.Print(Localizer["Msg.NoCustomRoundActive"]);
                }

                OpenCrMainMenu(p, backMenu);
            });
        }

        if (_api.IsNextRoundCustom)
        {
            menu.AddMenuOption("cr.cancel_next", Localizer["MenuOption.CancelNext"], (p, _) =>
            {
                if (_api.IsNextRoundCustom)
                {
                    _api.ClearNextRound(p);
                    p.Print(Localizer["Msg.NextRoundCancelled"]);
                }
                else
                {
                    p.Print(Localizer["Msg.NoNextRoundSet"]);
                }

                OpenCrMainMenu(p, backMenu);
            });
        }

        menu.Open(caller);
    }

    private void OpenRoundListMenu(CCSPlayerController caller, IDynamicMenu backMenu)
    {
        if (_api == null) return;

        var menu = AdminModule.Api.CreateMenu(
            "cr.list",
            Localizer["MenuTitle.ChooseRound"],
            backMenu: backMenu
        );

        var rounds = _api.GetAvailableRounds();

        foreach (var roundName in rounds)
        {
            menu.AddMenuOption($"cr.round.{roundName}", roundName, (p, _) =>
            {
                OpenRoundActionMenu(p, roundName, menu);
            });
        }

        menu.Open(caller);
    }

    private void OpenRoundActionMenu(CCSPlayerController caller, string roundName, IDynamicMenu backMenu)
    {
        if (_api == null) return;

        var menu = AdminModule.Api.CreateMenu(
            "cr.action",
            Localizer["MenuTitle.RoundAction", roundName],
            backMenu: backMenu
        );

        menu.AddMenuOption("cr.start_now", Localizer["MenuOption.StartNow"], (p, _) =>
        {
            if (_api.IsRoundEnd)
            {
                p.Print(Localizer["Msg.ErrorRoundEnd"]);
                return;
            }

            if (IsWarmup())
            {
                p.Print(Localizer["Msg.ErrorWarmup"]);
                return;
            }

            if (_api.StartRound(roundName, p))
            {
                p.Print(Localizer["Msg.RoundStarted", roundName]);
                p.CloseMenu();
            }
            else
            {
                p.Print(Localizer["Msg.ErrorStartFailed"]);
            }
        });

        menu.AddMenuOption("cr.set_next", Localizer["MenuOption.SetNext"], (p, _) =>
        {
            if (_api.SetNextRound(roundName, p))
            {
                p.Print(Localizer["Msg.NextRoundSet", roundName]);
                OpenCrMainMenu(p, backMenu);
            }
            else
            {
                p.Print(Localizer["Msg.ErrorSetNextFailed"]);
            }
        });

        menu.Open(caller);
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