namespace Radar;

public static class RadarEnum
{
    public enum MyObjectKind
{
	None,
	Pet,
	Chocobo,
	Player,
	BattleNpc,
	EventNpc,
	Treasure,
	Aetheryte,
	GatheringPoint,
	EventObj,
	MountType,
	Companion,
	Retainer,
	Area,
	Housing,
	Cutscene,
	CardStand,
	Unknown
}

public enum MapMode
{ 
    Free,
    Snap,
    SnapRotate
}

public enum DetailLevel
{
    仅图标,
    仅物体名,
    物体名距离,
    详细信息
}

public enum RingSegmentsType
{
    Quad = 4,
    Hexagon = 6,
    Circle = 0
}

public enum DeepDungeonType
{
    Trap,
    AccursedHoard,
    Wall,
    Room,
    Mob,
    CairnOfReturn,
    CairnOfPassage
}
}

