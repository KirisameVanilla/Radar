using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Radar.Enums;
using SharpDX;

namespace Radar.UI;

public class BuildUi : IDisposable
{
	public class DeepDungeonObjectLocationEqualityComparer : IEqualityComparer<DeepDungeonObject>
	{
		public bool Equals(DeepDungeonObject x, DeepDungeonObject y)
		{
			if ((object)x == y)
			{
				return true;
			}
			if ((object)x == null)
			{
				return false;
			}
			if ((object)y == null)
			{
				return false;
			}
			if (x.Location2D.Equals(y.Location2D) && DeepDungeonTerritoryEqual.Equals(x.Territory, y.Territory))
			{
				return x.Type == y.Type;
			}
			return false;
		}

		public int GetHashCode(DeepDungeonObject obj)
		{
			return obj.Location2D.GetHashCode() ^ (int)obj.GetBg ^ (int)obj.Type;
		}
	}

	public class DeepDungeonTerritoryEqualityComparer : IEqualityComparer<ushort>
	{
		public bool Equals(ushort x, ushort y)
		{
			switch (x)
			{
			case 564:
			case 565:
				if (y != 564)
				{
					return y == 565;
				}
				return true;
			case 593:
			case 594:
			case 595:
				if (y != 593 && y != 594)
				{
					return y == 595;
				}
				return true;
			case 596:
			case 597:
			case 598:
				if (y != 596 && y != 597)
				{
					return y == 598;
				}
				return true;
			case 599:
			case 600:
				if (y != 599)
				{
					return y == 600;
				}
				return true;
			case 601:
			case 602:
				if (y != 601)
				{
					return y == 602;
				}
				return true;
			case 603:
			case 604:
			case 605:
			case 606:
			case 607:
				if (y != 603 && y != 604 && y != 605 && y != 606)
				{
					return y == 607;
				}
				return true;
			case 772:
			case 782:
				if (y != 772)
				{
					return y == 782;
				}
				return true;
			case 773:
			case 783:
				if (y != 773)
				{
					return y == 783;
				}
				return true;
			case 774:
			case 784:
				if (y != 774)
				{
					return y == 784;
				}
				return true;
			case 775:
			case 785:
				if (y != 775)
				{
					return y == 785;
				}
				return true;
			default:
				return x == y;
			}
		}

		public int GetHashCode(ushort obj)
		{
			switch (obj)
			{
			case 564:
			case 565:
				return -10;
			case 593:
			case 594:
			case 595:
				return -1;
			case 596:
			case 597:
			case 598:
				return -2;
			case 599:
			case 600:
				return -3;
			case 601:
			case 602:
				return -4;
			case 603:
			case 604:
			case 605:
			case 606:
			case 607:
				return -5;
			case 772:
			case 782:
				return -6;
			case 773:
			case 783:
				return -7;
			case 774:
			case 784:
				return -8;
			case 775:
			case 785:
				return -9;
			default:
				return obj;
			}
		}
	}

	internal enum DeepDungeonBg
	{
		notInDeepDungeon,
		f1c1,
		f1c2,
		f1c3,
		f1c4,
		f1c5,
		f1c6,
		f1c8,
		f1c9,
		f1c7,
		e3c1,
		e3c2,
		e3c3,
		e3c4,
		e3c5,
		e3c6
	}

	public bool ConfigVisible;

	private byte[] fontBytes;

	private ImFontPtr fontPtr;

	private System.Numerics.Vector2? mapOrigin = System.Numerics.Vector2.Zero;

	private float GlobalUIScale = 1f;

	private System.Numerics.Vector2[] MapPosSize = new System.Numerics.Vector2[2];

	private static System.Numerics.Vector2 MeScreenPos = ImGuiHelpers.MainViewport.GetCenter();

	private static System.Numerics.Vector3 MeWorldPos = System.Numerics.Vector3.Zero;

	internal static Matrix MatrixSingetonCache;

	internal static System.Numerics.Vector2 ViewPortSizeCache;

	private ImDrawListPtr FDL;

	private ImDrawListPtr BDL;

	private ConfigSnapShot currentProfile;

	private Dictionary<ushort, bool> _isPvpZone;

	private string newCustomObjectName = string.Empty;

	private System.Numerics.Vector4 newCustomObjectColor = System.Numerics.Vector4.One;

	private Dictionary<ushort, string> territoryIdToBg;

	private int treeLevel;

	private bool importingError;

	private string errorMessage = string.Empty;

	private HashSet<DeepDungeonObject> deepDungeonObjectsImportCache;

	private string[] _getEnumNames;

	private static Random random = new Random();

	private static Lazy<System.Numerics.Vector4> randomColor = new Lazy<System.Numerics.Vector4>(delegate
	{
		ImGui.ColorConvertHSVtoRGB((float)random.NextDouble(), 1f, 1f, out var out_r, out var out_g, out var out_b);
		return new System.Numerics.Vector4(out_r, out_g, out_b, 1f);
	});

	public const uint Red = 4278190335u;

	public const uint Magenta = 4294902015u;

	public const uint Yellow = 4278255615u;

	public const uint Green = 4278255360u;

	public const uint GrassGreen = 4278247424u;

	public const uint Cyan = 4294967040u;

	public const uint DarkCyan = 4287664128u;

	public const uint LightCyan = 4294967200u;

	public const uint Blue = 4294901760u;

	public const uint Black = 4278190080u;

	public const uint TransBlack = 2147483648u;

	public const uint Grey = 4286611584u;

	public const uint White = uint.MaxValue;

	public const ImGuiTableFlags TableFlags = ImGuiTableFlags.BordersInner | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY;

	private HashSet<System.Numerics.Vector2> HoardBlackList = new HashSet<System.Numerics.Vector2>();

	private HashSet<System.Numerics.Vector2> TrapBlacklist = new HashSet<System.Numerics.Vector2>();

	private readonly Dictionary<uint, ushort> sizeFactorDict;

	private System.Numerics.Vector2 dragPos = System.Numerics.Vector2.Zero;

	private float uvZoom1 = 1f;

	private readonly System.Numerics.Vector2 uv1 = new System.Numerics.Vector2(0f, -1f);

	private readonly System.Numerics.Vector2 uv2 = new System.Numerics.Vector2(1f, -1f);

	private readonly System.Numerics.Vector2 uv3 = new System.Numerics.Vector2(1f, 0f);

	private readonly System.Numerics.Vector2 uv4 = new System.Numerics.Vector2(0f, 0f);

	private List<(System.Numerics.Vector3 worldpos, uint fgcolor, uint bgcolor, string name)> DrawList2D { get; } = new List<(System.Numerics.Vector3, uint, uint, string)>();


	public unsafe uint MapId
	{
		get
		{
			if (*(uint*)Plugin.address.MapIdDungeon != 0)
			{
				return *(uint*)Plugin.address.MapIdDungeon;
			}
			return *(uint*)Plugin.address.MapIdWorld;
		}
	}

	private Dictionary<ushort, bool> isPvpZone => _isPvpZone ?? (_isPvpZone = Plugin.DataManager.GetExcelSheet<TerritoryType>().ToDictionary((TerritoryType i) => (ushort)i.RowId, (TerritoryType j) => j.IsPvpZone));

	private static int FontsSize => ImGui.GetIO().Fonts.Fonts.Size;

	private Dictionary<ushort, string> TerritoryIdToBg
	{
		get
		{
			if (territoryIdToBg == null)
			{
				territoryIdToBg = Plugin.DataManager.GetExcelSheet<TerritoryType>().ToDictionary((TerritoryType i) => (ushort)i.RowId, (TerritoryType j) => j?.Bg?.RawString);
				territoryIdToBg[0] = "未记录区域（数据不可用）";
			}
			return territoryIdToBg;
		}
	}

	private string[] GetEnumNames => _getEnumNames ?? (_getEnumNames = Enum.GetNames(typeof(MyObjectKind)));

	public static DeepDungeonTerritoryEqualityComparer DeepDungeonTerritoryEqual { get; set; }

	public static DeepDungeonObjectLocationEqualityComparer DeepDungeonObjectLocationEqual { get; set; }

	private List<(nint objectPointer, uint fgcolor, string title)> SpecialObjectDrawList { get; } = new List<(nint, uint, string)>();


	private float WorldToMapScale => AreaMap.MapScale * (float)(int)sizeFactorDict[Plugin.ClientState.TerritoryType] / 100f * GlobalUIScale;

	private ref float UvZoom
	{
		get
		{
			if (uvZoom1 < 1f)
			{
				dragPos = System.Numerics.Vector2.Zero;
				uvZoom1 = 1f;
			}
			return ref uvZoom1;
		}
	}

	private Dictionary<ushort, Map[]> TerritoryMapsDictionary { get; }

	private List<(Map map, string texpath, ISharedImmediateTexture texture)> CurrentTerritoryMaps { get; set; } = new List<(Map, string, ISharedImmediateTexture)>();


	public BuildUi()
	{
		sizeFactorDict = Plugin.DataManager.GetExcelSheet<TerritoryType>().ToDictionary((TerritoryType k) => k.RowId, (TerritoryType v) => v.Map.Value.SizeFactor);
		DeepDungeonTerritoryEqual = new DeepDungeonTerritoryEqualityComparer();
		DeepDungeonObjectLocationEqual = new DeepDungeonObjectLocationEqualityComparer();
		TerritoryMapsDictionary = (from i in Plugin.DataManager.GetExcelSheet<Map>()
			group i by i.TerritoryType?.Value?.RowId into i
			where i.Key.HasValue && i.Key != 0
			select i).ToDictionary((IGrouping<uint?, Map> i) => (ushort)i.Key.Value, (IGrouping<uint?, Map> j) => j.ToArray());
		TryGetCurrentMapTex(Plugin.ClientState.TerritoryType);
		Plugin.ClientState.TerritoryChanged += TerritoryChanged;
		Plugin.pi.UiBuilder.OpenConfigUi += UiBuilder_OnOpenConfigUi;
		Plugin.pi.UiBuilder.Draw += UiBuilder_OnBuildUi;
	}

	private void UiBuilder_OnOpenConfigUi()
	{
		ConfigVisible = !ConfigVisible;
	}

	private void TerritoryChanged(ushort territoryId)
	{
		Plugin.log.Information($"territory changed to: {territoryId}");
		TryGetCurrentMapTex(territoryId);
		TrapBlacklist.Clear();
		HoardBlackList.Clear();
	}

	public void Dispose()
	{
		Plugin.pi.UiBuilder.OpenConfigUi -= UiBuilder_OnOpenConfigUi;
		Plugin.pi.UiBuilder.Draw -= UiBuilder_OnBuildUi;
		Plugin.ClientState.TerritoryChanged -= TerritoryChanged;
		DisposeMapTextures();
	}

	private unsafe void UiBuilder_OnBuildUi()
	{
		bool flag = false;
		try
		{
			if (Plugin.ClientState.TerritoryType != 0)
			{
				flag = isPvpZone[Plugin.ClientState.TerritoryType];
			}
		}
		catch (Exception)
		{
		}
		if (!flag)
		{
			FFXIVClientStructs.FFXIV.Client.Game.Camera* controlCamera = CameraManager.Instance()->GetActiveCamera();
			FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera* renderCamera = ((controlCamera != null) ? controlCamera->SceneCamera.RenderCamera : null);
			if (renderCamera != null)
			{
				Matrix4x4 view = renderCamera->ViewMatrix;
				Matrix4x4 proj = renderCamera->ProjectionMatrix;
				MatrixSingetonCache = Matrix4x4ToSharpDX(view * proj);
				Device* device = Device.Instance();
				ViewPortSizeCache = new System.Numerics.Vector2(device->Width, device->Height);
				FDL = ImGui.GetForegroundDrawList(ImGui.GetMainViewport());
				BDL = ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());
				RefreshMeScreenPos();
				RefreshMeWorldPos();
				if (Plugin.config.DeepDungeon_EnableTrapView && Plugin.condition[ConditionFlag.InDeepDungeon])
				{
					DrawDeepDungeonObjects();
				}
				bool num = FontsSize > 2;
				if (num && Plugin.config.Overlay3D_UseLargeFont)
				{
					ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[2]);
				}
				if (Plugin.GameObjectList != null)
				{
					EnumerateAllObjects();
				}
				if (num && Plugin.config.Overlay3D_UseLargeFont)
				{
					ImGui.PopFont();
				}
				if (num && Plugin.config.Overlay2D_UseLargeFont)
				{
					ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[2]);
				}
				if (Plugin.config.Overlay2D_Enabled)
				{
					DrawMapOverlay();
				}
				if (Plugin.config.ExternalMap_Enabled)
				{
					DrawExternalMap();
				}
				if (num && Plugin.config.Overlay2D_UseLargeFont)
				{
					ImGui.PopFont();
				}
				if (num && Plugin.config.OverlayHint_LargeFont)
				{
					ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[2]);
				}
				DrawSpecialObjectTipWindows();
				if (num && Plugin.config.OverlayHint_LargeFont)
				{
					ImGui.PopFont();
				}
			}
		}
		if (ConfigVisible)
		{
			DrawConfig();
		}
		DrawList2D.Clear();
		static Matrix Matrix4x4ToSharpDX(Matrix4x4 ma)
		{
			return new Matrix(ma.M11, ma.M12, ma.M13, ma.M14, ma.M21, ma.M22, ma.M23, ma.M24, ma.M31, ma.M32, ma.M33, ma.M34, ma.M41, ma.M42, ma.M43, ma.M44);
		}
	}

	private unsafe void EnumerateAllObjects()
	{
		for (int i = 1; i < 424; i++)
		{
			if (Plugin.GameObjectList[i] != null)
			{
				GameObject* o2 = Plugin.GameObjectList[i];
				CheckEachObject(o2);
			}
		}
		unsafe void CheckEachObject(GameObject* o)
		{
			if (Plugin.condition[ConditionFlag.InDeepDungeon])
			{
				AddDeepDungeonObjectRecord(o);
			}
			uint fgColor = uint.MaxValue;
			uint bgColor = 3204448256u;
			bool flag = TryAddSpecialObjectsToDrawList(o, ref fgColor, ref bgColor);
			if (Plugin.config.Overlay2D_Enabled || Plugin.config.Overlay3D_Enabled)
			{
				if (!flag)
				{
					if (!Plugin.config.Overlay_ShowKinds[(int)o->MyObjectKind] || (Plugin.config.Overlay_OnlyShowTargetable && (!o->IsTargetable || o->ObjectKind == ObjectKind.MountType)))
					{
						return;
					}
					fgColor = ImGui.ColorConvertFloat4ToU32(Plugin.config.KindColors[(int)o->MyObjectKind]);
					bgColor = ImGui.ColorConvertFloat4ToU32(Plugin.config.KindColorsBg[(int)o->MyObjectKind]);
				}
				ISharedImmediateTexture icon = null;
				if (Plugin.config.Overlay3D_Enabled)
				{
					DrawObject3D(o, fgColor, bgColor, Plugin.config.OverlayHint_ShowSpecialObjectLine && flag, icon);
				}
				if (Plugin.config.Overlay2D_Enabled)
				{
					AddObjectTo2DDrawList(o, fgColor, bgColor, icon);
				}
			}
		}
	}

	private static unsafe bool TryGetEnpcIcon(GameObject* o, out ISharedImmediateTexture enpcIcon)
	{
		enpcIcon = null;
		if (o->ObjectKind == ObjectKind.EventNpc && o->ENpcIcon != 0 && Plugin.EnpcIcons != null && Plugin.EnpcIcons.TryGetValue(o->ENpcIcon, out var value))
		{
			enpcIcon = value;
			return true;
		}
		return false;
	}

	private static unsafe void RefreshMeScreenPos()
	{
		try
		{
			Util.WorldToScreenEx(Core.Me->Location, out var screenPos, out var _, ImGui.GetMainViewport().Pos);
			MeScreenPos = screenPos;
		}
		catch
		{
		}
	}

	private static unsafe void RefreshMeWorldPos()
	{
		try
		{
			GameObject* gameObjectList = *Plugin.GameObjectList;
			if (gameObjectList != null)
			{
				MeWorldPos = gameObjectList->Location;
			}
		}
		catch
		{
		}
	}

    /*
	private unsafe void UiBuilder_OnBuildFonts()
	{
		fixed (byte* ptr = fontBytes)
		{
			fixed (ushort* ptr2 = GlyphRangesChinese.GlyphRanges)
			{
				ImFontConfigPtr font_cfg = ImGuiNative.ImFontConfig_ImFontConfig();
				font_cfg.MergeMode = true;
				font_cfg.PixelSnapH = true;
				fontPtr = ImGui.GetIO().Fonts.AddFontFromMemoryTTF((nint)ptr, fontBytes.Length, 24f, font_cfg, (nint)ptr2);
			}
		}
	}
    */

	private static void Config2D()
	{
		ImGui.TextWrapped("在游戏平面地图上显示物体信息叠加层。");
		ImGui.Checkbox("启用2D覆盖", ref Plugin.config.Overlay2D_Enabled);
		ImGui.Checkbox("显示自己##Overlay2D_ShowCenter", ref Plugin.config.Overlay2D_ShowCenter);
		ImGui.Checkbox("显示辅助圈(25m|125m)", ref Plugin.config.Overlay2D_ShowAssist);
		ref int overlay2D_DetailLevel = ref Plugin.config.Overlay2D_DetailLevel;
		DetailLevel overlay2D_DetailLevel2 = (DetailLevel)Plugin.config.Overlay2D_DetailLevel;
		ImGui.SliderInt("信息显示级别##2d", ref overlay2D_DetailLevel, 0, 2, overlay2D_DetailLevel2.ToString());
		ImGui.Separator();
		ImGui.Checkbox("启用外置地图##externalmap", ref Plugin.config.ExternalMap_Enabled);
		ImGui.Checkbox("锁定位置大小##externalmap", ref Plugin.config.ExternalMap_LockSizePos);
		ImGui.Checkbox("点击穿透##externalmap", ref Plugin.config.ExternalMap_ClickThrough);
		ImGui.Checkbox("显示地图信息##externalmap", ref Plugin.config.ExternalMap_ShowMapInfo);
		ImGui.SliderFloat("地图透明度##externalmap", ref Plugin.config.ExternalMap_MapAlpha, 0f, 1f);
		ImGui.SliderFloat("背景透明度##externalmap", ref Plugin.config.ExternalMap_BgAlpha, 0f, 1f);
		ref int externalMap_Mode = ref Plugin.config.ExternalMap_Mode;
		MapMode externalMap_Mode2 = (MapMode)Plugin.config.ExternalMap_Mode;
		ImGui.SliderInt("地图模式##externalmap", ref externalMap_Mode, 0, 2, externalMap_Mode2.ToString());
		ImGui.Separator();
		ImGui.TextUnformatted("名牌设置");
		if (ImGui.GetIO().Fonts.Fonts.Size > 2)
		{
			ImGui.Checkbox("大字体##2D", ref Plugin.config.Overlay2D_UseLargeFont);
		}
		ImGui.Checkbox("文字描边##Overlay2D_TextStroke", ref Plugin.config.Overlay2D_TextStroke);
		ImGui.Separator();
		ImGui.TextUnformatted("标识设置");
		ImGui.SliderFloat("标识大小##Overlay2D_DotSize", ref Plugin.config.Overlay2D_DotSize, 3f, 15f);
		ImGui.SliderFloat("标识描边宽度##Overlay2D_DotStroke", ref Plugin.config.Overlay2D_DotStroke, 0f, 5f);
	}

	private static void Config3D()
	{
		ImGui.TextWrapped("在游戏世界空间显示物体信息叠加层。");
		ImGui.Checkbox("启用3D覆盖", ref Plugin.config.Overlay3D_Enabled);
		ImGui.Checkbox("显示屏幕外物体", ref Plugin.config.Overlay3D_ShowOffscreen);
		ImGui.Checkbox("显示当前目标线", ref Plugin.config.Overlay3D_DrawObjectLineCurrentTarget);
		ImGui.Checkbox("显示以你为目标的目标线", ref Plugin.config.Overlay3D_DrawObjectLineTargetingYou);
		ImGui.Checkbox("显示所有物体目标线", ref Plugin.config.Overlay3D_DrawObjectLineAll);
		ref int overlay3D_DetailLevel = ref Plugin.config.Overlay3D_DetailLevel;
		DetailLevel overlay3D_DetailLevel2 = (DetailLevel)Plugin.config.Overlay3D_DetailLevel;
		ImGui.SliderInt("信息显示级别##3d", ref overlay3D_DetailLevel, 0, 2, overlay3D_DetailLevel2.ToString());
		ImGui.Separator();
		ImGui.TextUnformatted("名牌设置");
		if (ImGui.GetIO().Fonts.Fonts.Size > 2)
		{
			ImGui.Checkbox("大字体##3D", ref Plugin.config.Overlay3D_UseLargeFont);
		}
		ImGui.Checkbox("名牌居中显示##3D", ref Plugin.config.Overlay3D_CenterAlign);
		ImGui.SliderFloat("名牌圆角", ref Plugin.config.Overlay3D_NamePlateRound, 0f, 10f);
		ImGui.SliderFloat("名牌背景透明度##3D", ref Plugin.config.Overlay3D_NamePlateBgAlpha, 0f, 1f);
		ImGui.Separator();
		ImGui.TextUnformatted("标识设置");
		if (ImGui.RadioButton("方形", Plugin.config.Overlay3D_RingType == RingSegmentsType.Quad))
		{
			Plugin.config.Overlay3D_RingType = RingSegmentsType.Quad;
		}
		ImGui.SameLine();
		if (ImGui.RadioButton("六边形", Plugin.config.Overlay3D_RingType == RingSegmentsType.Hexagon))
		{
			Plugin.config.Overlay3D_RingType = RingSegmentsType.Hexagon;
		}
		ImGui.SameLine();
		if (ImGui.RadioButton("圆形", Plugin.config.Overlay3D_RingType == RingSegmentsType.Circle))
		{
			Plugin.config.Overlay3D_RingType = RingSegmentsType.Circle;
		}
		ImGui.DragFloat2("边框保留宽度", ref Plugin.config.Overlay3D_ClampVector2, 0.1f, 0f, 1000f);
		ImGui.SliderFloat("屏幕内标识大小", ref Plugin.config.Overlay3D_RingSize, 2f, 50f);
		ImGui.SliderFloat("边缘标识大小", ref Plugin.config.Overlay3D_ArrowSize, 5f, 50f);
		ImGui.SliderFloat("边缘标识粗细", ref Plugin.config.Overlay3D_ArrorThickness, 0.5f, 50f);
		ImGui.SliderFloat("标识描边宽度", ref Plugin.config.Overlay3D_IconStrokeThickness, 0f, 10f);
	}

	private void MobHuntAndCustomObjects()
	{
		ImGui.TextWrapped("用单独的提示窗口显示狩猎怪和自定义名称的物体。\n需要显示的物体名可以在下方自行添加。");
		ImGui.Checkbox("显示狩猎怪", ref Plugin.config.OverlayHint_MobHuntView);
		ImGui.Checkbox("显示自定义物体", ref Plugin.config.OverlayHint_CustomObjectView);
		ImGui.Separator();
		if (ImGui.GetIO().Fonts.Fonts.Size > 2)
		{
			ImGui.Checkbox("大字体##hints", ref Plugin.config.OverlayHint_LargeFont);
		}
		ImGui.Checkbox("显示目标线(3D)##specialObjects", ref Plugin.config.OverlayHint_ShowSpecialObjectLine);
		ImGui.Checkbox("鼠标悬停在窗口时按Alt打开地图链接", ref Plugin.config.OverlayHint_OpenMapLinkOnAlt);
		ImGui.DragFloat2("提示窗口位置", ref Plugin.config.WindowPos, 1f, 0f, 10000f);
		ImGui.SliderFloat("提示窗口边框宽度", ref Plugin.config.OverlayHint_BorderSize, 0f, 5f);
		ImGui.SliderFloat("窗口背景透明度##overlayHint", ref Plugin.config.OverlayHint_BgAlpha, 0f, 1f);
		if (!ImGui.BeginTable("CustomObjectTable", 4, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.PadOuterX))
		{
			return;
		}
		ImGui.TableSetupScrollFreeze(0, 1);
		ImGui.TableSetupColumn("自定义物体名");
		ImGui.TableSetupColumn("颜色");
		ImGui.TableSetupColumn("添加/删除");
		ImGui.TableHeadersRow();
		foreach (KeyValuePair<string, CustomObjectValue> customHighlightObject in Plugin.config.customHighlightObjects)
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			bool v = customHighlightObject.Value.Enabled;
			if (ImGui.Checkbox(customHighlightObject.Key + "##highlightObject", ref v))
			{
				Plugin.config.customHighlightObjects[customHighlightObject.Key] = customHighlightObject.Value with
				{
					Enabled = v
				};
				break;
			}
			ImGui.TableNextColumn();
			System.Numerics.Vector4 originalColor = customHighlightObject.Value.Color;
			ImguiUtil.ColorPickerWithPalette(customHighlightObject.Key.GetHashCode(), string.Empty, ref originalColor, ImGuiColorEditFlags.None);
			if (originalColor != customHighlightObject.Value.Color)
			{
				Plugin.config.customHighlightObjects[customHighlightObject.Key] = customHighlightObject.Value with
				{
					Color = originalColor
				};
				break;
			}
			ImGui.TableNextColumn();
			if (ImguiUtil.IconButton(FontAwesomeIcon.Trash, customHighlightObject.Key + "##delete"))
			{
				Plugin.config.customHighlightObjects.Remove(customHighlightObject.Key);
				break;
			}
		}
		ImGui.TableNextRow();
		ImGui.TableNextColumn();
		ImGui.SetNextItemWidth(-1f);
		bool num = ImGui.InputTextWithHint("##newName", "要添加的物体名，留空添加当前目标名", ref newCustomObjectName, 255u, ImGuiInputTextFlags.EnterReturnsTrue);
		ImGui.TableNextColumn();
		ImguiUtil.ColorPickerWithPalette(99999, string.Empty, ref newCustomObjectColor, ImGuiColorEditFlags.None);
		ImGui.TableNextColumn();
		bool flag = ImguiUtil.IconButton(FontAwesomeIcon.Plus, "##newcustomobjectentry");
		ImGui.TableNextColumn();
		if (num || flag)
		{
			if (string.IsNullOrWhiteSpace(newCustomObjectName))
			{
				IGameObject target = Plugin.TargetManager.Target;
				if (target != null)
				{
					newCustomObjectName = target.Name.TextValue;
				}
			}
			else
			{
				Plugin.config.customHighlightObjects[newCustomObjectName] = new CustomObjectValue
				{
					Color = newCustomObjectColor,
					Enabled = true
				};
				newCustomObjectName = string.Empty;
			}
		}
		ImGui.EndTable();
	}

	private void ConfigDeepDungeonRecord()
	{
		ImGui.TextWrapped("记录并显示本机深层迷宫攻略过程中出现过的陷阱与宝藏位置。\n你也可以导出自己的记录并与他人共享情报。");
		ImGui.Checkbox("深层迷宫实体显示", ref Plugin.config.DeepDungeon_EnableTrapView);
		ImGui.Checkbox("显示计数", ref Plugin.config.DeepDungeon_ShowObjectCount);
		ImGui.Spacing();
		ImGui.SliderFloat("最远显示距离", ref Plugin.config.DeepDungeon_ObjectShowDistance, 15f, 500f, Plugin.config.DeepDungeon_ObjectShowDistance.ToString("##.0m"), ImGuiSliderFlags.Logarithmic);
		ImGui.Separator();
		if (ImGui.Button("导出当前记录点到剪贴板"))
		{
			Plugin.log.Information("exporting...");
			Plugin.log.Information($"exported {(from i in Plugin.config.DeepDungeonObjects
				group i by i.Territory).Count()} territories, {Plugin.config.DeepDungeonObjects.Count((DeepDungeonObject i) => i.Type == DeepDungeonType.Trap)} traps, {Plugin.config.DeepDungeonObjects.Count((DeepDungeonObject i) => i.Type == DeepDungeonType.AccursedHoard)} hoards.");
			ImGui.SetClipboardText(Plugin.config.DeepDungeonObjects.ToCompressedString());
		}
		if (deepDungeonObjectsImportCache == null || !deepDungeonObjectsImportCache.Any())
		{
			ImGui.SameLine();
			if (ImGui.Button("从剪贴板导入已有的记录点"))
			{
				importingError = false;
				try
				{
					HashSet<DeepDungeonObject> source = ImGui.GetClipboardText().DecompressStringToObject<HashSet<DeepDungeonObject>>();
					if (source.Any())
					{
						deepDungeonObjectsImportCache = source;
					}
				}
				catch (Exception ex)
				{
					importingError = true;
					Plugin.log.Warning(ex, "error when importing deep dungeon object list.");
					errorMessage = ex.Message;
				}
			}
			if (importingError)
			{
				ImGui.TextColored(new System.Numerics.Vector4(1f, 0f, 0f, 1f), "导入发生错误，请检查导入的字符串和日志。");
				ImGui.TextColored(new System.Numerics.Vector4(1f, 0f, 0f, 1f), errorMessage);
			}
			return;
		}
		ImGui.SameLine();
		if (ImGui.Button("正在准备导入..."))
		{
			deepDungeonObjectsImportCache = null;
			Plugin.log.Debug("user canceled importing task.");
			return;
		}
		bool flag = ImGui.SliderInt("树视图展开级别", ref treeLevel, 1, 4, getformat(treeLevel));
		IEnumerable<IGrouping<ushort, DeepDungeonObject>> source2 = from i in deepDungeonObjectsImportCache
			group i by i.Territory;
		string arg = string.Join(", ", from i in source2
			select i.Key.ToString() into i
			orderby i
			select i);
		IGrouping<string, DeepDungeonObject>[] array = (from i in deepDungeonObjectsImportCache
			group i by TerritoryIdToBg[i.Territory] into i
			orderby i.Key
			select i).ToArray();
		ImGui.TextWrapped($"将要导入 {array.Length} 个区域的 {deepDungeonObjectsImportCache.Count} 条记录。({arg})\n包含 {array.Select((IGrouping<string, DeepDungeonObject> i) => (from j in i
			where j.Type == DeepDungeonType.Trap
			group j by j.Location2D).Count()).Sum()} 个陷阱位置，{array.Select((IGrouping<string, DeepDungeonObject> i) => (from j in i
			where j.Type == DeepDungeonType.AccursedHoard
			group j by j.Location2D).Count()).Sum()} 个宝藏位置。");
		if (ImGui.BeginChild("ddobjecttreeview", new System.Numerics.Vector2(-1f, (0f - ImGui.GetFrameHeightWithSpacing()) * 2f), border: true))
		{
			IGrouping<string, DeepDungeonObject>[] array2 = array;
			foreach (IGrouping<string, DeepDungeonObject> grouping in array2)
			{
				if (flag)
				{
					ImGui.SetNextItemOpen(treeLevel > 1);
				}
				if (!ImGui.TreeNodeEx(grouping.Key + "##DDTerritoryKey", ImGuiTreeNodeFlags.Framed))
				{
					continue;
				}
				foreach (IGrouping<DeepDungeonType, DeepDungeonObject> item in from i in grouping
					group i by i.Type into i
					orderby i.Key
					select i)
				{
					if (flag)
					{
						ImGui.SetNextItemOpen(treeLevel > 2);
					}
					if (!ImGui.TreeNodeEx($"{item.Key} ({(from i in item
						group i by i.Location2D).Count()})##{grouping.Key}", ImGuiTreeNodeFlags.SpanAvailWidth))
					{
						continue;
					}
					foreach (IGrouping<System.Numerics.Vector2, DeepDungeonObject> item2 in from i in item
						group i by i.Location2D into i
						orderby i.Count() descending
						select i)
					{
						if (flag)
						{
							ImGui.SetNextItemOpen(treeLevel > 3);
						}
						if (!ImGui.TreeNodeEx($"{item2.Key} ({item2.Count()})##{item.Key}{grouping.Key}", ImGuiTreeNodeFlags.SpanAvailWidth))
						{
							continue;
						}
						foreach (DeepDungeonObject item3 in item2.OrderBy((DeepDungeonObject i) => i.InstanceId))
						{
							ImGui.TextUnformatted($"{item3.Territory} : {item3.Base} : {item3.InstanceId:X}");
						}
						ImGui.TreePop();
					}
					ImGui.TreePop();
				}
				ImGui.TreePop();
			}
			ImGui.EndChild();
		}
		ImGui.TextColored(new System.Numerics.Vector4(1f, 0.8f, 0f, 1f), "确认后数据将合并到本机记录且不可撤销，请确认数据来源可靠。要继续吗？");
		ImGui.Spacing();
		if (ImGui.Button("取消导入##importDecline"))
		{
			deepDungeonObjectsImportCache = null;
			Plugin.log.Debug("user canceled importing task.");
			return;
		}
		ImGui.SameLine();
		if (ImGui.Button("确认导入##importAccept"))
		{
			int count = Plugin.config.DeepDungeonObjects.Count;
			Plugin.config.DeepDungeonObjects.UnionWith(deepDungeonObjectsImportCache);
			deepDungeonObjectsImportCache = null;
			int num = Plugin.config.DeepDungeonObjects.Count - count;
			Plugin.log.Information($"imported {num} deep dungeon object records.");
		}
		static string getformat(int input)
		{
			return input switch
			{
				0 => "默认", 
				1 => "全部折叠", 
				2 => "展开到物体类型", 
				3 => "展开到物体位置", 
				4 => "全部展开", 
				_ => "invalid", 
			};
		}
	}

	private void ConfigObjectKind()
	{
		ImGui.TextWrapped("按物体类别过滤显示。");
		if (ImGui.Button("全选"))
		{
			for (int i = 0; i < Plugin.config.Overlay_ShowKinds.Length; i++)
			{
				Plugin.config.Overlay_ShowKinds[i] = true;
			}
		}
		ImGui.SameLine();
		if (ImGui.Button("全不选"))
		{
			for (int j = 0; j < Plugin.config.Overlay_ShowKinds.Length; j++)
			{
				Plugin.config.Overlay_ShowKinds[j] = false;
			}
		}
		ImGui.SameLine();
		if (ImGui.Button("反选"))
		{
			for (int k = 0; k < Plugin.config.Overlay_ShowKinds.Length; k++)
			{
				Plugin.config.Overlay_ShowKinds[k] = !Plugin.config.Overlay_ShowKinds[k];
			}
		}
		ImGui.SameLine();
		if (ImGui.Button("玩家"))
		{
			for (int l = 0; l < Plugin.config.Overlay_ShowKinds.Length; l++)
			{
				Plugin.config.Overlay_ShowKinds[l] = false;
			}
			Plugin.config.Overlay_ShowKinds[3] = true;
		}
		ImGui.SameLine();
		if (ImGui.Button("NPC"))
		{
			for (int m = 0; m < Plugin.config.Overlay_ShowKinds.Length; m++)
			{
				Plugin.config.Overlay_ShowKinds[m] = false;
			}
			Plugin.config.Overlay_ShowKinds[4] = true;
			Plugin.config.Overlay_ShowKinds[5] = true;
			Plugin.config.Overlay_ShowKinds[6] = true;
			Plugin.config.Overlay_ShowKinds[7] = true;
			Plugin.config.Overlay_ShowKinds[9] = true;
		}
		ImGui.SameLine();
		ImGui.Checkbox("只显示可选中物体", ref Plugin.config.Overlay_OnlyShowTargetable);
		string[] getEnumNames = GetEnumNames;
		if (ImGui.BeginTable("ObjectKindTable", 3, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.ScrollY))
		{
			ImGui.TableSetupScrollFreeze(0, 1);
			ImGui.TableSetupColumn("物体类别");
			ImGui.TableSetupColumn("前景色");
			ImGui.TableSetupColumn("背景色");
			ImGui.TableHeadersRow();
			for (int n = 1; n < getEnumNames.Length; n++)
			{
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.Checkbox(getEnumNames[n] + "##ObjectKindCheckbox", ref Plugin.config.Overlay_ShowKinds[n]);
				ImGui.TableNextColumn();
				ImguiUtil.ColorPickerWithPalette(n, string.Empty, ref Plugin.config.KindColors[n], ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview);
				ImGui.TableNextColumn();
				ImguiUtil.ColorPickerWithPalette(int.MaxValue - n, string.Empty, ref Plugin.config.KindColorsBg[n], ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview);
			}
			ImGui.EndTable();
		}
	}

	private void DrawConfig()
	{
		ImGui.SetNextWindowSize(new System.Numerics.Vector2(480f, 640f), ImGuiCond.FirstUseEver);
		if (ImGui.Begin("Radar config###Radar config", ref ConfigVisible) && ImGui.BeginTabBar("tabbar", ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.AutoSelectNewTabs))
		{
			if (ImGui.BeginTabItem("显示类别"))
			{
				if (ImGui.BeginChild("显示类别childwindow"))
				{
					ConfigObjectKind();
					ImGui.EndChild();
				}
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("特殊物体"))
			{
				if (ImGui.BeginChild("狩猎&自定义childwindow"))
				{
					MobHuntAndCustomObjects();
					ImGui.EndChild();
				}
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("2D覆盖"))
			{
				if (ImGui.BeginChild("config2dchild"))
				{
					Config2D();
					ImGui.EndChild();
				}
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("3D覆盖"))
			{
				if (ImGui.BeginChild("config3dchild"))
				{
					Config3D();
					ImGui.EndChild();
				}
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("深层迷宫记录"))
			{
				if (ImGui.BeginChild("ConfigDeepDungeonRecord"))
				{
					ConfigDeepDungeonRecord();
					ImGui.EndChild();
				}
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("预设"))
			{
				if (ImGui.BeginChild("Profiles") && ImGui.BeginTable("ProfilesTable", 3, ImGuiTableFlags.PadOuterX, new System.Numerics.Vector2(-1f, ImGui.GetWindowSize().Y - ImGui.GetFrameHeightWithSpacing())))
				{
					ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
					ImGui.TableNextColumn();
					ImGui.TextUnformatted("预设名");
					ImGui.TableNextColumn();
					ImGui.TextUnformatted("最后保存时间");
					ImGui.TableNextColumn();
					ImGui.TextUnformatted("读取 / 覆盖 / 删除");
					int num = -1;
					for (int i = 0; i < Plugin.config.profiles.Count; i++)
					{
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ConfigSnapShot configSnapShot = Plugin.config.profiles[i];
						string input = configSnapShot.Name;
						ImGui.Selectable($"##selectable{i}", configSnapShot == currentProfile, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.AllowItemOverlap, new System.Numerics.Vector2(0f, ImGui.GetFrameHeight()));
						ImGui.SameLine();
						ImGui.SetCursorPosX(0f);
						ImGui.SetNextItemWidth(-1f);
						if (ImGui.InputText($"##name{i}", ref input, 255u, ImGuiInputTextFlags.AutoSelectAll))
						{
							Plugin.config.profiles[i].Name = input;
						}
						ImGui.TableNextColumn();
						ImGui.TextUnformatted($"{configSnapShot.LastEdit:f}");
						ImGui.TableNextColumn();
						System.Numerics.Vector2 size = new System.Numerics.Vector2(ImGui.GetFrameHeight() * 1.5f, ImGui.GetFrameHeight());
						if (ImguiUtil.IconButton(FontAwesomeIcon.Upload, $"loadbutton{i}", size))
						{
							currentProfile = configSnapShot;
							configSnapShot.RestoreSnapShot(Plugin.config);
						}
						ImGui.SameLine();
						ImguiUtil.IconButton(FontAwesomeIcon.Download, $"savebutton{i}", size);
						if (ImGui.IsItemHovered())
						{
							if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
							{
								Plugin.config.profiles[i] = ConfigSnapShot.GetSnapShot(configSnapShot.Name, Plugin.config);
							}
							ImGui.BeginTooltip();
							ImGui.TextUnformatted("Double click to overwrite");
							ImGui.EndTooltip();
						}
						ImGui.SameLine();
						ImguiUtil.IconButton(FontAwesomeIcon.Trash, $"deletebutton{i}", size);
						if (ImGui.IsItemHovered())
						{
							if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
							{
								num = i;
							}
							ImGui.BeginTooltip();
							ImGui.TextUnformatted("Double click to delete");
							ImGui.EndTooltip();
						}
					}
					if (num != -1)
					{
						Plugin.config.profiles.RemoveAt(num);
					}
					ImGui.EndTable();
					if (ImGui.Button("保存当前设置为新预设"))
					{
						Plugin.config.profiles.Add(ConfigSnapShot.GetSnapShot(DateTime.Now.ToString("G"), Plugin.config));
					}
				}
				ImGui.EndChild();
				ImGui.EndTabItem();
			}
			ImGui.EndTabBar();
		}
		ImGui.End();
	}

	internal static DeepDungeonBg GetDeepDungeonBg(ushort territory)
	{
		switch (territory)
		{
		case 561:
			return DeepDungeonBg.f1c1;
		case 562:
			return DeepDungeonBg.f1c2;
		case 563:
			return DeepDungeonBg.f1c3;
		case 564:
		case 565:
			return DeepDungeonBg.f1c4;
		case 593:
		case 594:
		case 595:
			return DeepDungeonBg.f1c5;
		case 596:
		case 597:
		case 598:
			return DeepDungeonBg.f1c6;
		case 599:
		case 600:
			return DeepDungeonBg.f1c8;
		case 601:
		case 602:
			return DeepDungeonBg.f1c9;
		case 603:
		case 604:
		case 605:
		case 606:
		case 607:
			return DeepDungeonBg.f1c7;
		case 770:
			return DeepDungeonBg.e3c1;
		case 771:
			return DeepDungeonBg.e3c2;
		case 772:
		case 782:
			return DeepDungeonBg.e3c3;
		case 773:
		case 783:
			return DeepDungeonBg.e3c4;
		case 774:
		case 784:
			return DeepDungeonBg.e3c5;
		case 775:
		case 785:
			return DeepDungeonBg.e3c6;
		default:
			return DeepDungeonBg.notInDeepDungeon;
		}
	}

	private unsafe void AddDeepDungeonObjectRecord(GameObject* o)
	{
		if (DeepDungeonObjectExtension.IsSilverCoffer(o))
		{
			TrapBlacklist.Add(o->Location2D);
		}
		if (DeepDungeonObjectExtension.IsAccursedHoard(o))
		{
			DeepDungeonObject deepDungeonObject = new DeepDungeonObject
			{
				Type = DeepDungeonType.AccursedHoard,
				Base = o->NpcBase,
				InstanceId = o->ObjectId,
				Location = o->Location,
				Territory = Plugin.ClientState.TerritoryType
			};
			if (Plugin.config.DeepDungeonObjects.Add(deepDungeonObject))
			{
				Plugin.log.Information($"New AccursedHoard recorded! {deepDungeonObject}");
			}
		}
		if (DeepDungeonObjectExtension.IsTrap(o) && !TrapBlacklist.Contains(o->Location2D))
		{
			DeepDungeonObject deepDungeonObject2 = new DeepDungeonObject
			{
				Type = DeepDungeonType.Trap,
				Base = o->NpcBase,
				InstanceId = o->ObjectId,
				Location = o->Location,
				Territory = Plugin.ClientState.TerritoryType
			};
			if (Plugin.config.DeepDungeonObjects.Add(deepDungeonObject2))
			{
				Plugin.log.Information($"New Trap recorded! {deepDungeonObject2}");
			}
		}
	}

	private void DrawDeepDungeonObjects()
	{
		foreach (IGrouping<DeepDungeonObject, DeepDungeonObject> item in Plugin.config.DeepDungeonObjects.Where((DeepDungeonObject i) => i.Territory != 0 && i.GetBg == GetDeepDungeonBg(Plugin.ClientState.TerritoryType) && i.Location.Distance2D(MeWorldPos.Convert()) < Plugin.config.DeepDungeon_ObjectShowDistance).GroupBy((DeepDungeonObject i) => i, DeepDungeonObjectLocationEqual))
		{
			System.Numerics.Vector2 ringCenter;
			if (item.Key.Type == DeepDungeonType.Trap)
			{
				if (Plugin.config.DeepDungeon_ShowObjectCount)
				{
					ImDrawListPtr bDL = BDL;
					System.Numerics.Vector3 location = item.Key.Location;
					string text = $"{item.Count()}";
					ringCenter = default(System.Numerics.Vector2);
					bDL.DrawRingWorldWithText(location, 0.5f, 24, 2f, 4278190335u, text, ringCenter);
				}
				else
				{
					BDL.DrawRingWorld(item.Key.Location, 0.5f, 24, 2f, 4278190335u, out ringCenter);
				}
			}
			if (item.Key.Type == DeepDungeonType.AccursedHoard)
			{
				if (Plugin.config.DeepDungeon_ShowObjectCount)
				{
					BDL.DrawRingWorldWithText(item.Key.Location + new System.Numerics.Vector3(0f, 0.1f, 0f), 0.5f, 24, 2f, 4278255615u, $"{item.Count()}", new System.Numerics.Vector2(0f, 0f - ImGui.GetTextLineHeight()));
				}
				else
				{
					BDL.DrawRingWorld(item.Key.Location + new System.Numerics.Vector3(0f, 0.1f, 0f), 0.5f, 24, 2f, 4278255615u, out ringCenter);
				}
			}
		}
	}

	private unsafe bool TryAddSpecialObjectsToDrawList(GameObject* o, ref uint fgColor, ref uint bgColor)
	{
		if (Plugin.config.OverlayHint_CustomObjectView && Plugin.config.customHighlightObjects.TryGetValue(o->DictionaryName, out var value) && value.Enabled)
		{
			SpecialObjectDrawList.Add(((nint)o, ImGui.ColorConvertFloat4ToU32(value.Color), $"{o->MyObjectKind.ToString().ToUpper()} {((o->NpcBase != 0) ? o->NpcBase.ToString() : string.Empty)}\nLv.{o->Character.CharacterData.Level} {o->DictionaryName}"));
			fgColor = ImGui.ColorConvertFloat4ToU32(value.Color);
			return true;
		}
		if (Plugin.config.OverlayHint_MobHuntView && o->ObjectKind == ObjectKind.BattleNpc)
		{
			if (NotoriousMonsters.SRankLazy.Value.Contains(o->NpcBase))
			{
				SpecialObjectDrawList.Add(((nint)o, 4278190335u, $"S RANK NOTORIOUS MONSTER\nLv.{o->Character.CharacterData.Level} {o->DictionaryName}"));
				fgColor = 4278190335u;
				return true;
			}
			if (NotoriousMonsters.ARankLazy.Value.Contains(o->NpcBase))
			{
				SpecialObjectDrawList.Add(((nint)o, 4278255615u, $"A RANK NOTORIOUS MONSTER\nLv.{o->Character.CharacterData.Level} {o->DictionaryName}"));
				fgColor = 4278255615u;
				return true;
			}
			if (NotoriousMonsters.BRankLazy.Value.Contains(o->NpcBase))
			{
				SpecialObjectDrawList.Add(((nint)o, 4278255360u, $"B RANK NOTORIOUS MONSTER\nLv.{o->Character.CharacterData.Level} {o->DictionaryName}"));
				fgColor = 4278255360u;
				return true;
			}
			if (NotoriousMonsters.ListEMobs.Contains(o->Character.NameId))
			{
				SpecialObjectDrawList.Add(((nint)o, 4294967040u, $"EUREKA ELEMENTAL\nLv.{o->Character.CharacterData.Level} {o->DictionaryName}"));
				fgColor = 4294967040u;
				return true;
			}
			if (o->NpcBase == 882)
			{
				SpecialObjectDrawList.Add(((nint)o, 4294901760u, $"ODIN\nLv.{o->Character.CharacterData.Level} {o->DictionaryName}"));
				fgColor = 4294901760u;
				return true;
			}
		}
		return false;
	}

	private unsafe void AddObjectTo2DDrawList(GameObject* a, uint fgcolor, uint bgcolor, ISharedImmediateTexture icon = null)
	{
		string item = null;
		switch (Plugin.config.Overlay2D_DetailLevel)
		{
		case 1:
			item = (string.IsNullOrEmpty(a->DictionaryName) ? $"{a->ObjectKind} {a->NpcBase}" : a->DictionaryName);
			break;
		case 2:
			item = (string.IsNullOrEmpty(a->DictionaryName) ? $"{a->ObjectKind} {a->NpcBase}" : $"{a->DictionaryName}\u3000{a->Location.Distance2D(MeWorldPos):F2}m");
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case 0:
			break;
		}
		DrawList2D.Add((a->Location, fgcolor, bgcolor, item));
	}

	private unsafe void DrawObject3D(GameObject* obj, uint fgcolor, uint bgcolor, bool drawLine, ISharedImmediateTexture icon = null)
	{
		bool flag = false;
		string text = null;
		switch (Plugin.config.Overlay3D_DetailLevel)
		{
		case 0:
			flag = true;
			break;
		case 1:
			text = (string.IsNullOrEmpty(obj->DictionaryName) ? $"{obj->ObjectKind} {obj->NpcBase}" : obj->DictionaryName);
			break;
		case 2:
			text = (string.IsNullOrEmpty(obj->DictionaryName) ? $"{obj->ObjectKind} {obj->NpcBase}" : obj->DictionaryName) + $"\t{obj->Location.Distance2D(MeWorldPos):F2}m";
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		if (Plugin.config.Overlay3D_DrawObjectLineAll)
		{
			drawLine = true;
		}
		else
		{
			if (Plugin.config.Overlay3D_DrawObjectLineCurrentTarget && Plugin.TargetManager.Target?.Address == (nint?)obj)
			{
				drawLine = true;
			}
			if (Plugin.config.Overlay3D_DrawObjectLineTargetingYou && Core.Me != null && ((ulong)obj->Character.TargetId == Core.Me->Id || (ulong)obj->Character.LookAt.Controller.Params[0].TargetParam.TargetId == Core.Me->Id))
			{
				drawLine = true;
			}
		}
		System.Numerics.Vector3 location = obj->Location;
		System.Numerics.Vector2 size = ImGuiHelpers.MainViewport.Size;
		System.Numerics.Vector2 pos = ImGuiHelpers.MainViewport.Pos;
		_ = MeScreenPos - ImGuiHelpers.MainViewport.GetCenter();
		ImGuiHelpers.MainViewport.GetCenter();
		System.Numerics.Vector2 screenPos;
		float Z;
		bool flag2 = Util.WorldToScreenEx(location, out screenPos, out Z, System.Numerics.Vector2.Zero, 200f, 100f);
		if (flag2 && Z < 0f)
		{
			screenPos -= MeScreenPos;
			screenPos /= size;
			screenPos = screenPos.Normalize();
			screenPos *= size;
			screenPos += MeScreenPos;
		}
		else
		{
			screenPos += pos;
		}
		System.Numerics.Vector2 vector = screenPos;
		_ = Plugin.config.Overlay3D_ClampVector2;
		System.Numerics.Vector2 overlay3D_ClampVector = Plugin.config.Overlay3D_ClampVector2;
		bool flag3 = false;
		if (Plugin.config.Overlay3D_ShowOffscreen && Vector2Intersect.GetBorderClampedVector2(screenPos, overlay3D_ClampVector, out var clampedPos))
		{
			screenPos = clampedPos;
			flag3 = true;
		}
		if (drawLine)
		{
			BDL.AddLine(MeScreenPos, vector, fgcolor);
		}
		if (flag3 || flag)
		{
			if (flag3)
			{
				System.Numerics.Vector2 rotation = vector - ImGui.GetMainViewport().GetCenter();
				float thickness = Math.Min(Plugin.config.Overlay3D_RingSize * 2f, Plugin.config.Overlay3D_ArrorThickness);
				if (Plugin.config.Overlay3D_IconStrokeThickness != 0f)
				{
					BDL.DrawArrow(screenPos, Plugin.config.Overlay3D_ArrowSize, fgcolor, bgcolor, rotation, thickness, Plugin.config.Overlay3D_IconStrokeThickness);
				}
				else
				{
					BDL.DrawArrow(screenPos, Plugin.config.Overlay3D_ArrowSize, fgcolor, rotation, thickness);
				}
			}
			else
			{
				BDL.DrawCircleOutlined(screenPos, fgcolor, bgcolor);
			}
		}
		else if (flag2 && Z > 0f)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				System.Numerics.Vector4 namePlateBgcolor = ImGui.ColorConvertU32ToFloat4(bgcolor);
				namePlateBgcolor.W = Plugin.config.Overlay3D_NamePlateBgAlpha;
				BDL.DrawTextWithBorderBg(screenPos, text, fgcolor, ImGui.GetColorU32(namePlateBgcolor), Plugin.config.Overlay3D_CenterAlign);
			}
			if (icon != null)
			{
				BDL.DrawIcon(screenPos - new System.Numerics.Vector2(0f, 10f), icon);
			}
			BDL.AddCircleFilled(screenPos, 4f, fgcolor, 4);
		}
	}

	private unsafe void DrawSpecialObjectTipWindows()
	{
		ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, Plugin.config.OverlayHint_BorderSize);
		System.Numerics.Vector2 windowPos = Plugin.config.WindowPos;
		foreach (var item4 in SpecialObjectDrawList.OrderBy(((nint objectPointer, uint fgcolor, string title) i) => ((GameObject*)i.objectPointer)->Location.Distance(MeWorldPos)))
		{
			nint item = item4.objectPointer;
			uint item2 = item4.fgcolor;
			string item3 = item4.title;
			GameObject* ptr = (GameObject*)item;
			ImGui.PushStyleColor(ImGuiCol.Border, item2);
			ImGui.SetNextWindowBgAlpha(Plugin.config.OverlayHint_BgAlpha);
			ImGui.SetNextWindowPos(windowPos);
			windowPos.Y += 15f;
			if (!ImGui.Begin((long)ptr + "static", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus))
			{
				continue;
			}
			System.Numerics.Vector2 pos2 = ImGui.GetWindowPos() + ImGui.GetCursorPos() + new System.Numerics.Vector2(ImGui.GetTextLineHeight(), ImGui.GetTextLineHeight());
			ImGui.GetWindowDrawList().DrawArrow(pos2, ImGui.GetTextLineHeightWithSpacing() * 0.618f, item2, (ptr->Location2D - MeWorldPos.ToVector2()).Normalize().Rotate(0f - Plugin.HRotation), 5f);
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetTextLineHeight() + ImGui.GetTextLineHeightWithSpacing());
			ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(item2), item3 ?? "");
			ImGui.Separator();
			string text = string.Empty;
			if (ptr->ObjectKind == ObjectKind.BattleNpc || ptr->ObjectKind == ObjectKind.Player)
			{
				text += $"{ptr->Character.CharacterData.Health:N0}/{ptr->Character.CharacterData.MaxHealth:N0}\t{(float)ptr->Character.CharacterData.Health / (float)ptr->Character.CharacterData.MaxHealth:P}\n";
			}
			float num = ptr->Y - MeWorldPos.Y;
			ImGui.TextUnformatted(string.Format("{0}{1:F2}m\t{2}{3:F2}m", text, ptr->Location.Distance2D(MeWorldPos), (num == 0f) ? "" : ((num > 0f) ? "↑" : "↓"), Math.Abs(num)));
			windowPos += new System.Numerics.Vector2(0f, ImGui.GetWindowSize().Y);
			if (Plugin.config.OverlayHint_OpenMapLinkOnAlt && ImGui.GetIO().KeyAlt && ImGui.IsMouseHoveringRect(ImGui.GetWindowPos(), ImGui.GetWindowSize() + ImGui.GetWindowPos()))
			{
				try
				{
					Plugin.Gui.OpenMapWithMapLink(new MapLinkPayload(Plugin.ClientState.TerritoryType, MapId, (int)(ptr->Location.X * 1000f), (int)(ptr->Location.Z * 1000f)));
				}
				catch (Exception)
				{
					Plugin.log.Debug("no map available in this area!");
				}
			}
			ImGui.PopStyleColor();
			ImGui.End();
		}
		ImGui.PopStyleVar(2);
		SpecialObjectDrawList.Clear();
	}

	private void DrawMapOverlay()
	{
		RefreshMapOrigin();
		System.Numerics.Vector2? vector = mapOrigin;
		if (!vector.HasValue)
		{
			return;
		}
		System.Numerics.Vector2 valueOrDefault = vector.GetValueOrDefault();
		if (!(valueOrDefault != System.Numerics.Vector2.Zero) || Plugin.ClientState.TerritoryType == 0)
		{
			return;
		}
		BDL.PushClipRect(MapPosSize[0], MapPosSize[1]);
		foreach (var item in DrawList2D)
		{
			System.Numerics.Vector2 pos = WorldToMap(valueOrDefault, item.worldpos);
			BDL.DrawMapTextDot(pos, item.name, item.fgcolor, item.bgcolor);
		}
		if (Plugin.config.Overlay2D_ShowCenter)
		{
			BDL.DrawMapTextDot(valueOrDefault, "ME", 4294967040u, 4278190080u);
		}
		if (Plugin.config.Overlay2D_ShowAssist)
		{
			BDL.AddCircle(valueOrDefault, WorldToMapScale * 25f, 4294967040u, 0, 1f);
			BDL.AddCircle(valueOrDefault, WorldToMapScale * 125f, 4286611584u, 0, 1f);
			BDL.AddLine(valueOrDefault, valueOrDefault - new System.Numerics.Vector2(0f, WorldToMapScale * 25f).Rotate((float)Math.PI / 4f + Plugin.HRotation), 4294967040u, 1f);
			BDL.AddLine(valueOrDefault, valueOrDefault - new System.Numerics.Vector2(0f, WorldToMapScale * 25f).Rotate(-(float)Math.PI / 4f + Plugin.HRotation), 4294967040u, 1f);
		}
		BDL.PopClipRect();
	}

	private System.Numerics.Vector2 WorldToMap(System.Numerics.Vector2 origin, System.Numerics.Vector3 worldVector3)
	{
		System.Numerics.Vector2 vector = (worldVector3.ToVector2() - MeWorldPos.ToVector2()) * WorldToMapScale;
		return origin + vector;
	}

	private unsafe void RefreshMapOrigin()
	{
		mapOrigin = null;
		if (!AreaMap.MapVisible)
		{
			return;
		}
		AtkUnitBase* areaMapAddon = AreaMap.AreaMapAddon;
		GlobalUIScale = areaMapAddon->Scale;
		if (areaMapAddon->UldManager.NodeListCount <= 4)
		{
			return;
		}
		AtkComponentNode* ptr = (AtkComponentNode*)areaMapAddon->UldManager.NodeList[3];
		AtkResNode atkResNode = ptr->AtkResNode;
		if (ptr->Component->UldManager.NodeListCount < 233)
		{
			return;
		}
		for (int i = 6; i < ptr->Component->UldManager.NodeListCount - 1; i++)
		{
			if (!ptr->Component->UldManager.NodeList[i]->IsVisible())
			{
				continue;
			}
			AtkComponentNode* ptr2 = (AtkComponentNode*)ptr->Component->UldManager.NodeList[i];
			AtkImageNode* ptr3 = (AtkImageNode*)ptr2->Component->UldManager.NodeList[4];
			string text = null;
			if (ptr3->PartsList != null && ptr3->PartId <= ptr3->PartsList->PartCount)
			{
				AtkUldAsset* uldAsset = ptr3->PartsList->Parts[(int)ptr3->PartId].UldAsset;
				if (uldAsset->AtkTexture.TextureType == TextureType.Resource)
				{
					StdString fileName = uldAsset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName;
					text = Path.GetFileName(fileName.ToString());
				}
			}
			if (text == "060443.tex" || text == "060443_hr1.tex")
			{
				AtkComponentNode* ptr4 = (AtkComponentNode*)ptr->Component->UldManager.NodeList[i];
				Plugin.log.Verbose($"node found {i}");
				AtkResNode atkResNode2 = ptr4->AtkResNode;
				System.Numerics.Vector2 vector = new System.Numerics.Vector2(areaMapAddon->X, areaMapAddon->Y);
				mapOrigin = ImGui.GetMainViewport().Pos + vector + (new System.Numerics.Vector2(atkResNode.X, atkResNode.Y) + new System.Numerics.Vector2(atkResNode2.X, atkResNode2.Y) + new System.Numerics.Vector2(atkResNode2.OriginX, atkResNode2.OriginY)) * GlobalUIScale;
				MapPosSize[0] = ImGui.GetMainViewport().Pos + vector + new System.Numerics.Vector2(atkResNode.X, atkResNode.Y) * GlobalUIScale;
				MapPosSize[1] = ImGui.GetMainViewport().Pos + vector + new System.Numerics.Vector2(atkResNode.X, atkResNode.Y) + new System.Numerics.Vector2((int)atkResNode.Width, (int)atkResNode.Height) * GlobalUIScale;
				break;
			}
		}
	}

	internal unsafe void DrawExternalMap()
	{
		if (Core.Me == null)
		{
			return;
		}
		Map map = new Map
		{
			SizeFactor = 100,
			OffsetX = 0,
			OffsetY = 0
		};
		IDalamudTextureWrap textureWrap = null;
		try
		{
			int num = CurrentTerritoryMaps.FindIndex(((Map map, string texpath, ISharedImmediateTexture texture) i) => i.map.RowId == MapId);
			if (num != -1)
			{
				map = CurrentTerritoryMaps[num].map;
				textureWrap = CurrentTerritoryMaps[num].texture.GetWrapOrDefault();
			}
		}
		catch (Exception exception)
		{
			Plugin.log.Error(exception, "error when get map");
		}
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
		ImGui.SetNextWindowSizeConstraints(new System.Numerics.Vector2(150f, 150f), new System.Numerics.Vector2(float.MaxValue, float.MaxValue), delegate(ImGuiSizeCallbackData* data)
		{
			float num4 = Math.Max(data->DesiredSize.X, data->DesiredSize.Y);
			data->DesiredSize = new System.Numerics.Vector2(num4, num4);
		});
		ImGuiWindowFlags imGuiWindowFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoDocking;
		if (Plugin.config.ExternalMap_ClickThrough)
		{
			imGuiWindowFlags |= ImGuiWindowFlags.NoMouseInputs;
		}
		if (Plugin.config.ExternalMap_LockSizePos)
		{
			imGuiWindowFlags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
		}
		ImGui.SetNextWindowBgAlpha(Plugin.config.ExternalMap_BgAlpha);
		System.Numerics.Vector2 IMGUI_windowcenter;
		System.Numerics.Vector2 MAP_offset;
		float MAP_SizeFactor;
		if (ImGui.Begin("maptex", imGuiWindowFlags))
		{
			ImDrawListPtr windowDrawList = ImGui.GetWindowDrawList();
			windowDrawList.ChannelsSplit(3);
			float windowContentRegionWidth = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
			new System.Numerics.Vector2(windowContentRegionWidth, windowContentRegionWidth);
			float num2 = windowContentRegionWidth / 2048f;
			System.Numerics.Vector2 windowPos = ImGui.GetWindowPos();
			System.Numerics.Vector2[] array = Square4(windowPos, ImGui.GetWindowWidth());
			IMGUI_windowcenter = ImGui.GetWindowPos() + new System.Numerics.Vector2(windowContentRegionWidth, windowContentRegionWidth) / 2f;
			MAP_offset = new System.Numerics.Vector2(map.OffsetX, map.OffsetY);
			MAP_SizeFactor = (float)(int)map.SizeFactor / 100f * num2;
			windowDrawList.ChannelsSetCurrent(1);
			if (Plugin.config.ExternalMap_ShowMapInfo)
			{
				string text = $" {windowContentRegionWidth / (MAP_SizeFactor * UvZoom) / 2f:F2}m X: {MeWorldPos.X:N3} Y: {MeWorldPos.Y:N3} Z: {MeWorldPos.Z:N3} ";
				System.Numerics.Vector2 vector = ImGui.CalcTextSize(text);
				System.Numerics.Vector2 vector2 = ImGui.GetWindowSize() - vector;
				windowDrawList.AddRectFilled(vector2 + windowPos, windowPos + ImGui.GetWindowSize(), 2147483648u);
				ImGui.SetCursorPos(vector2);
				ImGui.TextColored(System.Numerics.Vector4.One, text);
			}
			if (!Plugin.config.ExternalMap_ClickThrough)
			{
				ImGui.SetCursorPos(new System.Numerics.Vector2(5f, 5f));
				if (ImguiUtil.IconButton((Plugin.config.ExternalMap_Mode == 0) ? FontAwesomeIcon.Expand : ((Plugin.config.ExternalMap_Mode == 1) ? FontAwesomeIcon.Crosshairs : FontAwesomeIcon.LocationArrow), "ToggleSnap", new System.Numerics.Vector2(25f, 25f)))
				{
					Plugin.config.ExternalMap_Mode++;
					Plugin.config.ExternalMap_Mode = Plugin.config.ExternalMap_Mode % 3;
				}
				ImGui.SetCursorPosX(5f);
				if (ImguiUtil.IconButton(FontAwesomeIcon.PlusCircle, "zoom++", new System.Numerics.Vector2(25f, 25f)))
				{
					UvZoom *= 1.1f;
				}
				ImGui.SetCursorPosX(5f);
				if (ImguiUtil.IconButton(FontAwesomeIcon.MinusCircle, "zoom--", new System.Numerics.Vector2(25f, 25f)))
				{
					UvZoom *= 0.9f;
				}
			}
			windowDrawList.ChannelsSetCurrent(0);
			if (Plugin.config.ExternalMap_Mode != 0)
			{
				Square4(System.Numerics.Vector2.Zero, windowContentRegionWidth);
				for (int j = 0; j < 4; j++)
				{
					ref System.Numerics.Vector2 reference = ref array[j];
					reference -= (MeWorldPos.ToVector2() + MAP_offset) * MAP_SizeFactor;
					if (Plugin.config.ExternalMap_Mode == 2)
					{
						reference = reference.Rotate(0f - Plugin.HRotation, IMGUI_windowcenter);
					}
					reference = reference.Zoom(UvZoom, IMGUI_windowcenter);
				}
				try
				{
					windowDrawList.AddImageQuad(textureWrap.ImGuiHandle, array[0], array[1], array[2], array[3], uv1, uv2, uv3, uv4, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1f, 1f, 1f, Plugin.config.ExternalMap_MapAlpha)));
				}
				catch (Exception)
				{
				}
				foreach (var item in DrawList2D)
				{
					System.Numerics.Vector2 pos = WorldToMap(item.worldpos);
					windowDrawList.DrawMapTextDot(pos, item.name, item.fgcolor, item.bgcolor);
				}
				if (Plugin.config.Overlay2D_ShowCenter)
				{
					windowDrawList.DrawMapTextDot(IMGUI_windowcenter, (Plugin.config.Overlay2D_DetailLevel > 0) ? "ME" : null, 4294967040u, 4278190080u);
					if (Plugin.config.Overlay2D_ShowAssist)
					{
						float num3 = ((Plugin.config.ExternalMap_Mode == 2) ? 0f : (0f - Plugin.HRotation));
						windowDrawList.PathArcTo(IMGUI_windowcenter, MAP_SizeFactor * 25f * UvZoom, num3 - (float)Math.PI / 2f - (float)Math.PI / 4f, num3 - (float)Math.PI / 4f, 24);
						windowDrawList.PathLineTo(IMGUI_windowcenter);
						windowDrawList.PathStroke(4294967040u, ImDrawFlags.Closed, 2f);
					}
				}
			}
			else
			{
				for (int k = 0; k < 4; k++)
				{
					ref System.Numerics.Vector2 reference2 = ref array[k];
					reference2 = reference2.Zoom(UvZoom, IMGUI_windowcenter + dragPos);
				}
				try
				{
					windowDrawList.AddImageQuad(textureWrap.ImGuiHandle, array[0], array[1], array[2], array[3], uv1, uv2, uv3, uv4, ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1f, 1f, 1f, Plugin.config.ExternalMap_MapAlpha)));
				}
				catch (Exception)
				{
				}
				foreach (var item2 in DrawList2D)
				{
					System.Numerics.Vector2 pos2 = WorldToMapNoSnap(item2.worldpos);
					windowDrawList.DrawMapTextDot(pos2, item2.name, item2.fgcolor, item2.bgcolor);
				}
				if (Plugin.config.Overlay2D_ShowCenter)
				{
					System.Numerics.Vector2 vector3 = WorldToMapNoSnap(MeWorldPos);
					windowDrawList.DrawMapTextDot(vector3, (Plugin.config.Overlay2D_DetailLevel > 0) ? "ME" : null, 4294967040u, 4278190080u);
					if (Plugin.config.Overlay2D_ShowAssist)
					{
						windowDrawList.PathArcTo(vector3, MAP_SizeFactor * 25f * UvZoom, 0f - Plugin.HRotation - (float)Math.PI / 2f - (float)Math.PI / 4f, 0f - Plugin.HRotation - (float)Math.PI / 4f, 24);
						windowDrawList.PathLineTo(vector3);
						windowDrawList.PathStroke(4294967040u, ImDrawFlags.Closed, 2f);
					}
				}
			}
			if (ImGui.IsWindowHovered())
			{
				if (ImGui.IsMouseDown(ImGuiMouseButton.Right) || ImGui.IsMouseDown(ImGuiMouseButton.Middle))
				{
					dragPos -= ImGui.GetIO().MouseDelta / UvZoom;
					if (Plugin.config.ExternalMap_Mode != 0)
					{
						dragPos = (MeWorldPos.ToVector2() + MAP_offset) * MAP_SizeFactor;
						Plugin.config.ExternalMap_Mode = 0;
					}
				}
				UvZoom += UvZoom * ImGui.GetIO().MouseWheel * 0.1f;
			}
			windowDrawList.ChannelsMerge();
			ImGui.End();
		}
		ImGui.PopStyleVar();
		System.Numerics.Vector2 WorldToMap(System.Numerics.Vector3 worldPos)
		{
			System.Numerics.Vector2 vector4 = (worldPos - MeWorldPos).ToVector2() * MAP_SizeFactor;
			if (Plugin.config.ExternalMap_Mode == 2)
			{
				vector4 = vector4.Rotate(0f - Plugin.HRotation);
			}
			return IMGUI_windowcenter + vector4 * UvZoom;
		}
		System.Numerics.Vector2 WorldToMapNoSnap(System.Numerics.Vector3 worldPos)
		{
			return IMGUI_windowcenter + (worldPos.ToVector2() + MAP_offset) * MAP_SizeFactor * UvZoom - dragPos * (UvZoom - 1f);
		}
	}

	private static System.Numerics.Vector2[] Square4(System.Numerics.Vector2 ltPos = default(System.Numerics.Vector2), float size = 1f)
	{
		return new System.Numerics.Vector2[4]
		{
			ltPos,
			ltPos + new System.Numerics.Vector2(size, 0f),
			ltPos + new System.Numerics.Vector2(size, size),
			ltPos + new System.Numerics.Vector2(0f, size)
		};
	}

	private void TryGetCurrentMapTex(ushort e)
	{
		DisposeMapTextures();
		Task.Run(delegate
		{
			if (TerritoryMapsDictionary.TryGetValue(e, out var value))
			{
				try
				{
					CurrentTerritoryMaps = value.Select(delegate(Map i)
					{
						try
						{
							string rawString = i.Id.RawString;
							string text = "ui/map/" + rawString + "/" + rawString.Replace("_", string.Empty).Replace("/", string.Empty) + "_m.tex";
							Plugin.log.Information("Loading map tex... " + $"rowid: {i.RowId} " + "idstring: " + rawString + ", placename: [" + i.PlaceNameRegion?.Value?.Name?.RawString + "/" + i.PlaceName?.Value?.Name?.RawString + "/" + i.PlaceNameSub?.Value?.Name?.RawString + "], texpath: " + text);
							ISharedImmediateTexture fromGame = Plugin.textures.GetFromGame(text);
							return (i: i, text: text, imGuiTexture: fromGame);
						}
						catch (Exception exception2)
						{
							Plugin.log.Warning(exception2, $"error when getting map tex: {i.RowId} {i.Id?.RawString}");
							return ((Map i, string text, ISharedImmediateTexture imGuiTexture))(i: i, text: "", imGuiTexture: null);
						}
					}).ToList();
					return;
				}
				catch (Exception exception)
				{
					Plugin.log.Warning(exception, "error when getting map tex");
					return;
				}
			}
			Plugin.log.Information($"no map found for territory {e}");
		});
	}

	private void DisposeMapTextures()
	{
		CurrentTerritoryMaps.Clear();
	}
}
