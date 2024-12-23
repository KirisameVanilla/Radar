using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Radar;

public class Plugin : IDalamudPlugin
{
    private readonly Radar radar;
    private ConfigUi ConfigUi { get; init; }
    private readonly WindowSystem windowSystem = new("Radar");

    private static int SaveTimer;

	[PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; }

	[PluginService] internal static IClientState ClientState { get; private set; }

	[PluginService] internal static IFramework Framework { get; private set; }

	[PluginService] internal static IGameGui Gui { get; private set; }

	[PluginService] internal static IChatGui ChatGui { get; private set; }

	[PluginService] internal static IDataManager DataManager { get; private set; }

	[PluginService] internal static ITargetManager TargetManager { get; private set; }

	[PluginService] internal static ICondition Condition { get; private set; }

	[PluginService] internal static ICommandManager CommandManager { get; private set; }

	[PluginService] internal static ITextureProvider TextureProvider { get; private set; }

	[PluginService] internal static IObjectTable ObjectTable { get; private set; }

	[PluginService] internal static IPluginLog PluginLog { get; private set; }
    internal static Configuration Configuration { get; private set; }

	public Plugin()
	{
        Configuration = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
        Configuration.Initialize(PluginInterface);
        Framework.Update += Framework_OnUpdateEvent;
        radar = new Radar();
        ConfigUi = new ConfigUi();
        windowSystem.AddWindow(ConfigUi);
        if (PluginInterface.Reason != PluginLoadReason.Boot)
        { 
            ToggleMainUi();
        }

        CommandManager.AddHandler("/radar", new CommandInfo(OnCommand)
        {
            HelpMessage = """
                          Opens the Radar config window.
                          /radar map → Toggle external map
                          /radar hunt → Toggle hunt mobs overlay
                          /radar 2d → Toggle 2D overlay
                          /radar 3d → Toggle 3D overlay
                          /radar custom → Toggle custom object overlay
                          """,
            ShowInHelp = true,
        });

        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
    }

    private void OnCommand(string command, string arguments)
    {
        if (arguments.Length == 0)
        {
            ToggleMainUi();
            return;
        }
        
        var argumentsSplit = arguments.Split(' ');
        switch (argumentsSplit[0])
        {
            case "map":
            {
                Configuration.ExternalMap_Enabled = !Configuration.ExternalMap_Enabled;
                ChatGui.Print("[Radar] External Map " + (Configuration.ExternalMap_Enabled ? "Enabled" : "Disabled") + ".");
                break;
            }
            case "hunt":
            {
                Configuration.OverlayHint_MobHuntView = !Configuration.OverlayHint_MobHuntView;
                ChatGui.Print("[Radar] Hunt view " + (Configuration.OverlayHint_MobHuntView ? "Enabled" : "Disabled") + ".");
                break;
            }
            case "custom":
            {
                Configuration.OverlayHint_CustomObjectView = !Configuration.OverlayHint_CustomObjectView;
                ChatGui.Print("[Radar] Custom object view " + (Configuration.OverlayHint_CustomObjectView ? "Enabled" : "Disabled") + ".");
                break;
            }
            case "2d":
            {
                Configuration.Overlay2D_Enabled = !Configuration.Overlay2D_Enabled;
                ChatGui.Print("[Radar] 2D overlay " + (Configuration.Overlay2D_Enabled ? "Enabled" : "Disabled") + ".");
                break;
            }
            case "3d":
            {
                Configuration.Overlay3D_Enabled = !Configuration.Overlay3D_Enabled;
                ChatGui.Print("[Radar] 3D overlay " + (Configuration.Overlay3D_Enabled ? "Enabled" : "Disabled") + ".");
                break;
            }
        }
    }
	private static void Framework_OnUpdateEvent(IFramework framework)
	{
		SaveTimer++;
		if (SaveTimer % 3600 != 0)
		{
			return;
		}
		SaveTimer = 0;
		Task.Run(delegate
		{
			try
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				Configuration.Save();
				PluginLog.Verbose($"config saved in {stopwatch.Elapsed.TotalMilliseconds}ms.");
			}
			catch (Exception exception)
			{
				PluginLog.Warning(exception, "error when saving config");
			}
		});
	}
	public void Dispose()
    {
        CommandManager.RemoveHandler("/radar");
        windowSystem.RemoveAllWindows();
        Framework.Update -= Framework_OnUpdateEvent;
        radar.Dispose();
        ConfigUi.Dispose();
        PluginInterface.SavePluginConfig(Configuration);
	}

    private void DrawUi() => windowSystem.Draw();
    public void ToggleMainUi() => ConfigUi.Toggle();
}
