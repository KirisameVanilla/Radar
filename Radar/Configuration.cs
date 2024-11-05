using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Radar.CustomObject;
using static Radar.RadarEnum;

namespace Radar;

public class Configuration : IPluginConfiguration
{
	public bool Config_ShowActorTable;

	public bool ActorTable_ShowIndex;

	public bool ActorTable_ShowPointerVtbl;

	public bool Overlay_ForceVisible;

	public bool[] Overlay_ShowKinds = Enumerable.Repeat(element: true, 20).ToArray();

	public bool Overlay_FrontlineGcView;

	public bool Overlay_GcAllyAsGreen;

	public bool Overlay_OnlyShowTargetable = true;

	public bool OverlayHint_MobHuntView = true;

    public bool OverlayHintShowRankS = true;

    public bool OverlayHintShowRankA = true;

    public bool OverlayHintShowRankB = true;

    public bool OverlayHint_CustomObjectView = true;

	public bool OverlayHint_OpenMapLinkOnAlt = true;

	public bool OverlayHint_ShowSpecialObjectLine = true;

	public bool OverlayHint_LargeFont;

	public float OverlayHint_BorderSize = 2f;

	public float OverlayHint_BgAlpha = 0.667f;

	public bool ExternalMap_Enabled;

	public float ExternalMap_MapAlpha = 1f;

	public float ExternalMap_BgAlpha = 0.75f;

	public bool ExternalMap_LockSizePos;

	public bool ExternalMap_ClickThrough;

	public bool ExternalMap_ShowMapInfo = true;

	public int ExternalMap_Mode = 1;

	public bool Overlay2D_Enabled = true;

	public int Overlay2D_DetailLevel = 1;

	public bool Overlay2D_TextStroke = true;

	public bool Overlay2D_ShowCenter = true;

	public bool Overlay2D_ShowAssist = true;

	public float Overlay2D_DotSize = 5f;

	public float Overlay2D_DotStroke = 1f;

	public Vector4 Overlay2D_StrokeColor = new(0f, 0f, 0f, 1f);

	public bool Overlay2D_UseLargeFont;

	public bool Overlay3D_Enabled = true;

	public int Overlay3D_DetailLevel = 1;

	public bool Overlay3D_DrawObjectLineAll;

	public bool Overlay3D_DrawObjectLineCurrentTarget;

	public bool Overlay3D_DrawObjectLineTargetingYou;

	public float Overlay3D_ObjectLineThickness = 1f;

	public bool Overlay3D_ShowOffscreen = true;

	public bool Overlay3D_UseLargeFont;

	public bool Overlay3D_CenterAlign = true;

	public float Overlay3D_NamePlateRound = 2f;

	public float Overlay3D_NamePlateBgAlpha = 0.667f;

	public RingSegmentsType Overlay3D_RingType;

	public Vector4[] KindColors = Enumerable.Repeat(Vector4.One, 20).ToArray();

	public Vector4[] KindColorsBg = Enumerable.Repeat(new Vector4(0f, 0f, 0f, 0.75f), 20).ToArray();

	public Dictionary<uint, string> NpcBaseMapping = new()
    {
		[2007358u] = "宝箱（金）",
		[2007357u] = "宝箱（银）",
		[2006020u] = "宝箱（拟态怪）",
		[2007542u] = "埋藏的宝藏",
		[2007188u] = "转移石冢",
		[2007182u] = "地雷",
		[2007183u] = "诱饵陷阱",
		[2007184u] = "弱化陷阱",
		[2007185u] = "妨碍陷阱",
		[2007186u] = "蛙变陷阱",
		[2009504u] = "獭獭陷阱"
	};

	public Vector2 Overlay3D_ClampVector2 = new(40f, 40f);

	public float Overlay3D_RingSize = 6f;

	public float Overlay3D_ArrowSize = 10f;

	public float Overlay3D_ArrorThickness = 4f;

	public float Overlay3D_IconStrokeThickness = 3f;

	public Vector2 WindowPos = new(50f, 50f);

	public Dictionary<string, CustomObjectValue> customHighlightObjects = new ()
	{
		["转移灯笼"] = new CustomObjectValue
		{
			Color = new Vector4(0f, 1f, 1f, 1f),
			Enabled = true
		},
		["转移石冢"] = new CustomObjectValue
		{
			Color = new Vector4(0f, 1f, 1f, 1f),
			Enabled = true
		}
	};

	public HashSet<DeepDungeonObject> DeepDungeonObjects = new();

	public float DeepDungeon_ObjectShowDistance = 100f;

	public bool DeepDungeon_EnableTrapView = true;

	public bool DeepDungeon_ShowObjectCount;

	public List<ConfigSnapShot> profiles = new();

	[JsonIgnore]
	private IDalamudPluginInterface pluginInterface;

	public int Version { get; set; }

	public void Initialize(IDalamudPluginInterface pluginInterface)
	{
		this.pluginInterface = pluginInterface;
	}

	public void Save()
	{
		pluginInterface.SavePluginConfig(this);
	}
}
