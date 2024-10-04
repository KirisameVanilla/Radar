using Newtonsoft.Json;

namespace System;

public static class ObjectExtensions
{
	public static T CloneJson<T>(this T source)
	{
		if (source == null)
		{
			return default(T);
		}
		return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
	}
}
