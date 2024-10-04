using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using ImGuiNET;

namespace Radar;

internal class ReflectionConfig
{
	private FieldInfo[] boolConfigFieldInfos;

	private Configuration config;

	internal ReflectionConfig(Configuration configuration)
	{
		config = configuration;
		boolConfigFieldInfos = (from i in configuration.GetType().GetFields()
			where i.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null && i.FieldType == typeof(bool) && i.Name.ContainsIgnoreCase("show")
			select i).ToArray();
	}

	public void DrawBoolConfigs()
	{
		FieldInfo[] array = boolConfigFieldInfos;
		foreach (FieldInfo fieldInfo in array)
		{
			bool v = (bool)fieldInfo.GetValue(config);
			if (ImGui.Checkbox(fieldInfo.Name, ref v))
			{
				fieldInfo.SetValue(config, v);
				config.Save();
			}
		}
	}
}
