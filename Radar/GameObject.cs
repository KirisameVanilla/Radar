using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Radar.Enums;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace Radar;

[StructLayout(LayoutKind.Explicit, Pack = 2)]
public struct GameObject
{
	public enum mySpecialObjectType
	{
		maplink = 100,
		treasureMap = 110
	}

	[FieldOffset(0)]
	private FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject gameObject;

	[FieldOffset(0)]
	public Character Character;

	private uint EntityId => gameObject.EntityId;

	private uint LayoutId => gameObject.LayoutId;

	private uint ObjectIndex => gameObject.ObjectIndex;

	public byte TargetableStatus => (byte)gameObject.TargetableStatus;

	public Vector3 Position => gameObject.Position;

	public float X => Position.X;

	public float Y => Position.Y;

	public float Z => Position.Z;

	public uint ENpcIcon => gameObject.NamePlateIconId;

	public uint RenderFlags => (uint)gameObject.RenderFlags;

	public unsafe string Name
	{
		get
		{
			fixed (byte* ptr = gameObject.Name)
			{
				return Util.ReadTerminatedString(ptr);
			}
		}
	}

	public string DictionaryName
	{
		get
		{
			if (!Plugin.config.NpcBaseMapping.TryGetValue(BaseId, out var value))
			{
				return Name;
			}
			return value;
		}
	}

	public uint ObjectId => gameObject.GetGameObjectId().ObjectId;

	public uint BaseId => gameObject.BaseId;

	public ObjectKind ObjectKind => (ObjectKind)gameObject.ObjectKind;

	public SubKind SubKind => (SubKind)gameObject.SubKind;

	public MyObjectKind MyObjectKind
	{
		get
		{
			ObjectKind objectKind = (ObjectKind)gameObject.ObjectKind;
			switch (objectKind)
			{
			case ObjectKind.None:
				return MyObjectKind.None;
			case ObjectKind.BattleNpc:
				if (SubKind == SubKind.Pet)
				{
					return MyObjectKind.Pet;
				}
				if (SubKind == SubKind.Chocobo)
				{
					return MyObjectKind.Chocobo;
				}
				break;
			}
			return (MyObjectKind)(objectKind + 2);
		}
	}

	public Vector2 Location2D => new(X, Z);

    public GameObject(Vector3 pos)
	{
		gameObject = default;
		Character = default;
		gameObject.Position = pos;
	}
}
