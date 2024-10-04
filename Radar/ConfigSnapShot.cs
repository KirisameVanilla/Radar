using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Radar;

public class ConfigSnapShot
{
	public Configuration saved;

	public DateTime LastEdit;

	public string Name;

	private ConfigSnapShot()
	{
	}

	public static ConfigSnapShot GetSnapShot(string name, Configuration configInstance)
	{
		Configuration configuration = configInstance.CloneJson();
		configuration.DeepDungeonObjects = null;
		configuration.NpcBaseMapping = null;
		configuration.profiles = null;
		return new ConfigSnapShot
		{
			saved = configuration,
			Name = name,
			LastEdit = DateTime.Now
		};
	}

	public void RestoreSnapShot(Configuration configInstance)
	{
		Configuration savedCopy = saved.CloneJson();
		Type type = configInstance.GetType();
		foreach (var item2 in from i in type.GetFields()
			where i.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null
			select (Name: i.Name, i.GetValue(savedCopy)))
		{
			object item = item2.Item2;
			if (item2.Item2 != null)
			{
				Plugin.log.Verbose($"trying set {item2.Name} {item2.Item2}");
				type.GetField(item2.Name)?.SetValue(configInstance, item);
			}
			else
			{
				Plugin.log.Verbose(item2.Name + " NULL");
			}
		}
	}
}
