using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Radar.Attributes;

namespace Radar;

public class PluginCommandManager<THost> : IDisposable
{
	private readonly IDalamudPluginInterface pluginInterface;

	private readonly (string, CommandInfo)[] pluginCommands;

	private readonly THost host;

	public PluginCommandManager(THost host, IDalamudPluginInterface pluginInterface)
	{
		this.pluginInterface = pluginInterface;
		this.host = host;
		pluginCommands = (from method in host.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			where method.GetCustomAttribute<CommandAttribute>() != null
			select method).SelectMany(GetCommandInfoTuple).ToArray();
		AddCommandHandlers();
	}

	private void AddCommandHandlers()
	{
		for (int i = 0; i < pluginCommands.Length; i++)
		{
			var (command, info) = pluginCommands[i];
			Plugin.cmd.AddHandler(command, info);
		}
	}

	private void RemoveCommandHandlers()
	{
		for (int i = 0; i < pluginCommands.Length; i++)
		{
			string item = pluginCommands[i].Item1;
			Plugin.cmd.RemoveHandler(item);
		}
	}

	private IEnumerable<(string, CommandInfo)> GetCommandInfoTuple(MethodInfo method)
	{
		IReadOnlyCommandInfo.HandlerDelegate obj = (IReadOnlyCommandInfo.HandlerDelegate)Delegate.CreateDelegate(typeof(IReadOnlyCommandInfo.HandlerDelegate), host, method);
		CommandAttribute customAttribute = obj.Method.GetCustomAttribute<CommandAttribute>();
		AliasesAttribute customAttribute2 = obj.Method.GetCustomAttribute<AliasesAttribute>();
		HelpMessageAttribute customAttribute3 = obj.Method.GetCustomAttribute<HelpMessageAttribute>();
		DoNotShowInHelpAttribute customAttribute4 = obj.Method.GetCustomAttribute<DoNotShowInHelpAttribute>();
		CommandInfo item = new CommandInfo(obj)
		{
			HelpMessage = (customAttribute3?.HelpMessage ?? string.Empty),
			ShowInHelp = (customAttribute4 == null)
		};
		List<(string, CommandInfo)> list = new List<(string, CommandInfo)> { (customAttribute.Command, item) };
		if (customAttribute2 != null)
		{
			for (int i = 0; i < customAttribute2.Aliases.Length; i++)
			{
				list.Add((customAttribute2.Aliases[i], item));
			}
		}
		return list;
	}

	public void Dispose()
	{
		RemoveCommandHandlers();
	}
}
