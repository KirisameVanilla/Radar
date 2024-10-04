using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Radar.Enums;

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
	private FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject _GameObject;

	[FieldOffset(0)]
	public Character Character;

	private uint ID1 => _GameObject.EntityId;

	private uint ID2 => _GameObject.LayoutId;

	private uint ID3 => _GameObject.ObjectIndex;

	private byte TargetableFlags => (byte)_GameObject.TargetableStatus;

	public Vector3 Location => _GameObject.Position;

	public float X => Location.X;

	public float Y => Location.Y;

	public float Z => Location.Z;

	public uint ENpcIcon => _GameObject.NamePlateIconId;

	private uint RenderFlags => (uint)_GameObject.RenderFlags;

	public unsafe string Name
	{
		get
		{
			fixed (byte* ptr = _GameObject.Name)
			{
				return Util.ReadTerminatedString(ptr);
			}
		}
	}

	public string DictionaryName
	{
		get
		{
			if (!Plugin.config.NpcBaseMapping.TryGetValue(NpcBase, out var value))
			{
				return Name;
			}
			return value;
		}
	}

	public uint ObjectId => _GameObject.GetGameObjectId().ObjectId;

	public uint Id
	{
		get
		{
			if (ID1 == 3758096384u)
			{
				if (ID2 == 0 || ID3 - 200 < 44)
				{
					return ID3;
				}
				return ID2;
			}
			return ID1;
		}
	}

	public uint NpcBase => _GameObject.BaseId;

	public Dalamud.Game.ClientState.Objects.Enums.ObjectKind ObjectKind => (Dalamud.Game.ClientState.Objects.Enums.ObjectKind)_GameObject.ObjectKind;

	public SubKind SubKind => (SubKind)_GameObject.SubKind;

	public MyObjectKind MyObjectKind
	{
		get
		{
			Dalamud.Game.ClientState.Objects.Enums.ObjectKind objectKind = (Dalamud.Game.ClientState.Objects.Enums.ObjectKind)_GameObject.ObjectKind;
			switch (objectKind)
			{
			case Dalamud.Game.ClientState.Objects.Enums.ObjectKind.None:
				return MyObjectKind.None;
			case Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc:
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

	public Vector2 Location2D => new Vector2(X, Z);

	public bool IsVisible => RenderFlags == 0;

	public unsafe bool IsTargetable
	{
		get
		{
			void* ptr = stackalloc byte[6];
			((byte*)ptr)[4] = TargetableFlags;
			*(uint*)ptr = RenderFlags;
			if (((*(uint*)ptr >> 11) & (true ? 1u : 0u)) != 0)
			{
				((byte*)ptr)[5] = 1;
			}
			else
			{
				((byte*)ptr)[5] = 0;
			}
			if ((((byte*)ptr)[4] & 2) > 0 && (((byte*)ptr)[5] == 0 || ((byte*)ptr)[4] >= 128))
			{
				return (*(uint*)ptr & 0xFFFFE7F7u) == 0;
			}
			return false;
		}
	}

	public GameObject(Vector3 pos)
	{
		_GameObject = default(FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject);
		Character = default(Character);
		_GameObject.Position = pos;
	}
}
