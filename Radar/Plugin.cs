using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface.Textures;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Radar.Attributes;
using Radar.UI;

namespace Radar;

public class Plugin : IDalamudPlugin, IDisposable
{
	private delegate void sub_14089C400(long a1, int a2, int a3, float a4, float a5, int a6);

	internal PluginCommandManager<Plugin> CommandManager;

	internal BuildUi Ui;

	public const uint EmptyObjectID = 3758096384u;

	public static unsafe GameObject** GameObjectList;

	internal static Dictionary<uint, ISharedImmediateTexture> EnpcIcons;

	private static int Savetimer;

	[PluginService]
	internal static IDalamudPluginInterface pi { get; private set; }

	[PluginService]
	internal static IClientState ClientState { get; private set; }

	[PluginService]
	internal static IFramework Framework { get; private set; }

	[PluginService]
	internal static IGameGui Gui { get; private set; }

	[PluginService]
	internal static IChatGui ChatGui { get; private set; }

	[PluginService]
	internal static ISigScanner SigScanner { get; private set; }

	[PluginService]
	internal static IDataManager DataManager { get; private set; }

	[PluginService]
	internal static ITargetManager TargetManager { get; private set; }

	[PluginService]
	internal static ICondition condition { get; private set; }

	[PluginService]
	internal static ICommandManager cmd { get; private set; }

	[PluginService]
	internal static ITextureProvider textures { get; private set; }

	[PluginService]
	internal static IObjectTable objects { get; private set; }

	[PluginService]
	internal static IGameInteropProvider hook { get; private set; }

	[PluginService]
	internal static IPluginLog log { get; private set; }

	internal static Configuration config { get; private set; }

	public static ProcessModule MainModule => SigScanner.Module;

	public static nint ImageBase => SigScanner.Module.BaseAddress;

	public string Name => "Radar";

	public unsafe Plugin()
	{
		config = ((Configuration)pi.GetPluginConfig()) ?? new Configuration();
		config.Initialize(pi);
		GameObjectList = (GameObject**)objects.Address;
		Framework.Update += Framework_OnUpdateEvent;
		SetupResources();
		Ui = new BuildUi();
		CommandManager = new PluginCommandManager<Plugin>(this, pi);
		if (pi.Reason != PluginLoadReason.Boot)
		{
			Ui.ConfigVisible = true;
		}
	}

	public void Initialize()
	{
	}

	private static void SetupResources()
	{
        ExcelSheet<EventIconPriority> EventIconPrioritySheet = DataManager.GetExcelSheet<EventIconPriority>();

        Task.Run(delegate
		{
			try
			{
				EnpcIcons = (from i in EventIconPrioritySheet?.SelectMany((EventIconPriority i) => i.Icon)
					where i != 0
					select i).ToDictionary((uint i) => i, delegate(uint j)
				{
					ITextureProvider textureProvider = textures;
					GameIconLookup lookup = new GameIconLookup(j, itemHq: false, hiRes: false, DataManager.Language);
					return textureProvider.GetFromGameIcon(in lookup);
				});
			}
			catch (Exception exception)
			{
				log.Error(exception, "error when loading enpc icons.");
			}
		});
	}

	private void Framework_OnUpdateEvent(IFramework framework)
	{
		Savetimer++;
		if (Savetimer % 3600 != 0)
		{
			return;
		}
		Savetimer = 0;
		Task.Run(delegate
		{
			try
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				config.Save();
				log.Verbose($"config saved in {stopwatch.Elapsed.TotalMilliseconds}ms.");
			}
			catch (Exception exception)
			{
				log.Warning(exception, "error when saving config");
			}
		});
	}

	[Command("/radar")]
	[HelpMessage("Open radar config window")]
	public void ConfigCommand2(string command, string args)
	{
		BuildUi ui = Ui;
		ui.ConfigVisible = !ui.ConfigVisible;
	}

	[Command("/rmap")]
	[HelpMessage("Toggle external map")]
	public void ToggleMap(string command, string args)
	{
		Configuration configuration = config;
		configuration.ExternalMap_Enabled = !configuration.ExternalMap_Enabled;
		ChatGui.Print("[Radar] External Map " + (config.ExternalMap_Enabled ? "Enabled" : "Disabled") + ".");
	}

	[Command("/rhunt")]
	[HelpMessage("Toggle hunt mobs overlay")]
	public void ShowHunt(string command, string args)
	{
		Configuration configuration = config;
		configuration.OverlayHint_MobHuntView = !configuration.OverlayHint_MobHuntView;
		ChatGui.Print("[Radar] Hunt view " + (config.OverlayHint_MobHuntView ? "Enabled" : "Disabled") + ".");
	}

	[Command("/rfinder")]
	[HelpMessage("Toggle custom object overlay")]
	public void ShowSpecial(string command, string args)
	{
		Configuration configuration = config;
		configuration.OverlayHint_CustomObjectView = !configuration.OverlayHint_CustomObjectView;
		ChatGui.Print("[Radar] Custom object view " + (config.OverlayHint_CustomObjectView ? "Enabled" : "Disabled") + ".");
	}

	[Command("/r2d")]
	[HelpMessage("Toggle 2D overlay")]
	public void Toggle2D(string command, string args)
	{
		Configuration configuration = config;
		configuration.Overlay2D_Enabled = !configuration.Overlay2D_Enabled;
		ChatGui.Print("[Radar] 2D overlay " + (config.Overlay2D_Enabled ? "Enabled" : "Disabled") + ".");
	}

	[Command("/r3d")]
	[HelpMessage("Toggle 3D overlay")]
	public void Toggle3D(string command, string args)
	{
		Configuration configuration = config;
		configuration.Overlay3D_Enabled = !configuration.Overlay3D_Enabled;
		ChatGui.Print("[Radar] 3D overlay " + (config.Overlay3D_Enabled ? "Enabled" : "Disabled") + ".");
	}

	[Command("/rpreset")]
	[HelpMessage("/rpreset <preset name> → load named preset\n/rpreset save → save current config as preset")]
	public void LoadPreset(string command, string args)
	{
		if (args == "save")
		{
			string text = DateTime.Now.ToString("G");
			config.profiles.Add(ConfigSnapShot.GetSnapShot(text, config));
			ChatGui.Print("[Radar] config snapchot saved as \"" + text + "\".");
			return;
		}
		ConfigSnapShot configSnapShot = config.profiles.OrderBy((ConfigSnapShot i) => i.LastEdit).LastOrDefault((ConfigSnapShot i) => i.Name == args);
		if (configSnapShot != null)
		{
			ChatGui.Print("[Radar] loading preset \"" + configSnapShot.Name + "\".");
			configSnapShot.RestoreSnapShot(config);
		}
		else
		{
			ChatGui.PrintError("[Radar] no preset named \"" + args + "\" found.");
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			Framework.Update -= Framework_OnUpdateEvent;
			CommandManager.Dispose();
			Ui.Dispose();
			pi.SavePluginConfig(config);
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
