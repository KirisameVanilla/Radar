using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;

namespace Radar.CustomObject;

public static class DeepDungeonObjectExtension
{
    public static bool IsTrap(IGameObject obj)
    {
        if (obj.DataId == 6388 && obj.Position != Vector3.Zero)
        {
            return true;
        }
        if ((obj.DataId >= 2007182 && obj.DataId <= 2007186) || obj.DataId == 2009504)
        {
            return true;
        }
        return false;
    }

    public static bool IsAccursedHoard(IGameObject obj)
    {
        if (obj.DataId != 2007542)
        {
            return obj.DataId == 2007543;
        }
        return true;
    }

    public static bool IsSilverCoffer(IGameObject obj)
    {
        return obj.DataId == 2007357;
    }

}
