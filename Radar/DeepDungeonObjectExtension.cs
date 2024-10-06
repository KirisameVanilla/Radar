using System.Numerics;

namespace Radar;

public static class DeepDungeonObjectExtension
{
	public static unsafe bool IsTrap(GameObject* obj)
	{
		if (obj->BaseId == 6388 && obj->Position != Vector3.Zero)
		{
			return true;
		}
		if ((obj->BaseId >= 2007182 && obj->BaseId <= 2007186) || obj->BaseId == 2009504)
		{
			return true;
		}
		return false;
	}

	public static unsafe bool IsAccursedHoard(GameObject* obj)
	{
		if (obj->BaseId != 2007542)
		{
			return obj->BaseId == 2007543;
		}
		return true;
	}

	public static unsafe bool IsSilverCoffer(GameObject* obj)
	{
		return obj->BaseId == 2007357;
	}
}
