using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;
using ImGuiNET;
using Lumina.Excel;
using Radar.CustomObject;
using SharpDX;
using static Radar.RadarEnum;
using Map = Lumina.Excel.GeneratedSheets.Map;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
using TerritoryType = Lumina.Excel.GeneratedSheets.TerritoryType;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Radar.UI;

public class MainUi : IDisposable
{
	public class DeepDungeonObjectLocationEqualityComparer : IEqualityComparer<DeepDungeonObject>
	{
		public bool Equals(DeepDungeonObject x, DeepDungeonObject y)
		{
			if ((object)x == y)
			{
				return true;
			}
			if (x is null || y is null)
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
            return obj switch
            {
                564 or 565 => -10,
                593 or 594 or 595 => -1,
                596 or 597 or 598 => -2,
                599 or 600 => -3,
                601 or 602 => -4,
                603 or 604 or 605 or 606 or 607 => -5,
                772 or 782 => -6,
                773 or 783 => -7,
                774 or 784 => -8,
                775 or 785 => -9,
                _ => obj,
            };
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

	private Vector2? mapOrigin = Vector2.Zero;

	private float globalUiScale = 1f;

	private Vector2[] mapPosSize = new Vector2[2];

	private static Vector2 MeScreenPos = ImGuiHelpers.MainViewport.GetCenter();

	private static Vector3 MeWorldPos = Vector3.Zero;

	internal static Matrix MatrixSingetonCache;

	internal static Vector2 ViewPortSizeCache;

    private ImDrawListPtr foregroundDrawList;

	private ImDrawListPtr backgroundDrawList;

	private ConfigSnapShot currentProfile;

	private Dictionary<ushort, bool> isPvpZoneDict;

	private string newCustomObjectName = string.Empty;

	private Vector4 newCustomObjectColor = Vector4.One;

	private Dictionary<ushort, string> territoryIdToBg;

	private int treeLevel;

	private bool importingError;

	private string errorMessage = string.Empty;

	private HashSet<DeepDungeonObject> deepDungeonObjectsImportCache;

	private string[] getEnumNames;

    #region ColorsVector4

    private static Vector4 RedVector4 = new(1f, 0f, 0f, 1f);

    #endregion

    private HashSet<Vector2> hoardBlackList = new();

	private HashSet<Vector2> trapBlacklist = new();

	private readonly Dictionary<uint, ushort> sizeFactorDict;

	private Vector2 dragPos = Vector2.Zero;

	private float uvZoom1 = 1f;

	private readonly Vector2 uv1 = new(0f, -1f);

	private readonly Vector2 uv2 = new(1f, -1f);

	private readonly Vector2 uv3 = new(1f, 0f);

	private readonly Vector2 uv4 = new(0f, 0f);

	private List<(Vector3 worldpos, uint fgcolor, uint bgcolor, string name)> DrawList2D { get; } = new();

    #region ExcelSheets
    private static readonly ExcelSheet<TerritoryType> TerritoryTypeSheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
    private static readonly ExcelSheet<Map> MapSheet = Plugin.DataManager.GetExcelSheet<Map>();
    #endregion

	private Dictionary<ushort, bool> IsPvpZone => isPvpZoneDict ??= TerritoryTypeSheet.ToDictionary((i) => (ushort)i.RowId, (j) => j.IsPvpZone);

	private static int FontsSize => ImGui.GetIO().Fonts.Fonts.Size;
    
	private Dictionary<ushort, string> TerritoryIdToBg
	{
		get
		{
			if (territoryIdToBg == null)
			{
				territoryIdToBg = TerritoryTypeSheet.ToDictionary((i) => (ushort)i.RowId, (j) => j?.Bg?.RawString);
				territoryIdToBg[0] = "未记录区域（数据不可用）";
			}
			return territoryIdToBg;
		}
	}

	private string[] GetEnumNames => getEnumNames ??= Enum.GetNames(typeof(MyObjectKind));

	public static DeepDungeonTerritoryEqualityComparer DeepDungeonTerritoryEqual { get; set; }

	public static DeepDungeonObjectLocationEqualityComparer DeepDungeonObjectLocationEqual { get; set; }

    private List<(IGameObject obj, uint fgcolor, string title)> SpecialObjectDrawList { get; } = new();

	private float WorldToMapScale => AreaMap.MapScale * sizeFactorDict[Plugin.ClientState.TerritoryType] / 100f * globalUiScale;

	private ref float UvZoom
	{
		get
		{
			if (uvZoom1 < 1f)
			{
				dragPos = Vector2.Zero;
				uvZoom1 = 1f;
			}
			return ref uvZoom1;
		}
	}

    private float rotation;

	public MainUi()
	{
		sizeFactorDict = TerritoryTypeSheet.ToDictionary(k => k.RowId, v => v.Map.Value.SizeFactor);
		DeepDungeonTerritoryEqual = new DeepDungeonTerritoryEqualityComparer();
		DeepDungeonObjectLocationEqual = new DeepDungeonObjectLocationEqualityComparer();
		Plugin.ClientState.TerritoryChanged += TerritoryChanged;
		Plugin.PluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OnOpenConfigUi;
		Plugin.PluginInterface.UiBuilder.Draw += UiBuilder_OnBuildUi;
	}

	private void UiBuilder_OnOpenConfigUi()
	{
		ConfigVisible = !ConfigVisible;
	}

	private void TerritoryChanged(ushort territoryId)
	{
		Plugin.PluginLog.Information($"territory changed to: {territoryId}");
		trapBlacklist.Clear();
		hoardBlackList.Clear();
	}

	public void Dispose()
	{
		Plugin.PluginInterface.UiBuilder.OpenConfigUi -= UiBuilder_OnOpenConfigUi;
		Plugin.PluginInterface.UiBuilder.Draw -= UiBuilder_OnBuildUi;
		Plugin.ClientState.TerritoryChanged -= TerritoryChanged;
        GC.SuppressFinalize(this);
	}

	private unsafe void UiBuilder_OnBuildUi()
	{
		var isPvpZone = false;

        if (Plugin.ClientState.TerritoryType != 0 && IsPvpZone.TryGetValue(Plugin.ClientState.TerritoryType, out var value))
        {
            isPvpZone = value;
        }

		if (!isPvpZone)
		{
			FFXIVClientStructs.FFXIV.Client.Game.Camera* controlCamera = CameraManager.Instance()->GetActiveCamera();
			FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera* renderCamera = ((controlCamera != null) ? controlCamera->SceneCamera.RenderCamera : null);
			if (renderCamera != null)
			{
				Matrix4x4 view = renderCamera->ViewMatrix;
				Matrix4x4 proj = renderCamera->ProjectionMatrix;
				MatrixSingetonCache = Matrix4x4ToSharpDX(view * proj);
				Device* device = Device.Instance();
				ViewPortSizeCache = new Vector2(device->Width, device->Height);
				foregroundDrawList = ImGui.GetForegroundDrawList(ImGui.GetMainViewport());
				backgroundDrawList = ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());
				RefreshMeScreenPos();
				RefreshMeWorldPos();
				if (Plugin.Configuration.DeepDungeon_EnableTrapView && Plugin.Condition[ConditionFlag.InDeepDungeon])
				{
					DrawDeepDungeonObjects();
				}
				bool num = FontsSize > 2;
				if (num && Plugin.Configuration.Overlay3D_UseLargeFont)
				{
					ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[2]);
				}
				if (Plugin.ObjectTable != null)
				{
					EnumerateAllObjects();
				}
				if (num && Plugin.Configuration.Overlay3D_UseLargeFont)
				{
					ImGui.PopFont();
				}
				if (num && Plugin.Configuration.Overlay2D_UseLargeFont)
				{
					ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[2]);
				}
				if (Plugin.Configuration.Overlay2D_Enabled)
				{
					DrawMapOverlay();
				}
				if (Plugin.Configuration.ExternalMap_Enabled)
				{
					DrawExternalMap();
				}
				if (num && Plugin.Configuration.Overlay2D_UseLargeFont)
				{
					ImGui.PopFont();
				}
				if (num && Plugin.Configuration.OverlayHint_LargeFont)
				{
					ImGui.PushFont(ImGui.GetIO().Fonts.Fonts[2]);
				}
				DrawSpecialObjectTipWindows();
				if (num && Plugin.Configuration.OverlayHint_LargeFont)
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
        return;

		static Matrix Matrix4x4ToSharpDX(Matrix4x4 ma)
		{
			return new Matrix(ma.M11, ma.M12, ma.M13, ma.M14, ma.M21, ma.M22, ma.M23, ma.M24, ma.M31, ma.M32, ma.M33, ma.M34, ma.M41, ma.M42, ma.M43, ma.M44);
		}
	}

    private void EnumerateAllObjects()
    {
        foreach (var obj in Plugin.ObjectTable)
        {
            if (obj is not null && !obj.Equals(Plugin.ObjectTable.First()))
            {
                CheckEachObject(obj);
            }
        }

        return;


        void CheckEachObject(IGameObject o)
        {
            var myObjectKind = Util.GetMyObjectKind(o);

            if (Plugin.Condition[ConditionFlag.InDeepDungeon])
            {
                AddDeepDungeonObjectRecord(o);
            }
            var fgColor = uint.MaxValue;
            var bgColor = 3204448256u;
            var flag = TryAddSpecialObjectsToDrawList(o, ref fgColor, ref bgColor);
            if (Plugin.Configuration.Overlay2D_Enabled || Plugin.Configuration.Overlay3D_Enabled)
            {
                if (!flag)
                {
                    if (!Plugin.Configuration.Overlay_ShowKinds[(int)myObjectKind] || (Plugin.Configuration.Overlay_OnlyShowTargetable && (!o.IsTargetable || o.ObjectKind == ObjectKind.MountType)))
                    {
                        return;
                    }
                    fgColor = ImGui.ColorConvertFloat4ToU32(Plugin.Configuration.KindColors[(int)myObjectKind]);
                    bgColor = ImGui.ColorConvertFloat4ToU32(Plugin.Configuration.KindColorsBg[(int)myObjectKind]);
                }
                ISharedImmediateTexture icon = null;
                if (Plugin.Configuration.Overlay3D_Enabled)
                {
                    DrawObject3D(o, fgColor, bgColor, Plugin.Configuration.OverlayHint_ShowSpecialObjectLine && flag, icon);
                }
                if (Plugin.Configuration.Overlay2D_Enabled)
                {
                    AddObjectTo2DDrawList(o, fgColor, bgColor);
                }
            }
        }
    }


    private static void RefreshMeScreenPos()
	{
        // This is the position of your character on the Screen
        if (Plugin.ClientState.LocalPlayer == null) { return; }
        Util.WorldToScreenEx(Plugin.ClientState.LocalPlayer.Position, out var screenPos, out _, ImGui.GetMainViewport().Pos);
        MeScreenPos = screenPos;
	}

	private static void RefreshMeWorldPos()
	{
        // This is the position of your character in the Game
        if (Plugin.ClientState.LocalPlayer == null) { return; }
        var me = Plugin.ObjectTable.First();
        if (me != null) { 
            MeWorldPos = me.Position;
        }
	}

	private static void Config2D()
	{
		ImGui.TextWrapped("在游戏平面地图上显示物体信息叠加层。");
		ImGui.Checkbox("启用2D覆盖", ref Plugin.Configuration.Overlay2D_Enabled);
		ImGui.Checkbox("显示自己##Overlay2D_ShowCenter", ref Plugin.Configuration.Overlay2D_ShowCenter);
		ImGui.Checkbox("显示辅助圈(25m|125m)", ref Plugin.Configuration.Overlay2D_ShowAssist);
		ref int overlay2DDetailLevel = ref Plugin.Configuration.Overlay2D_DetailLevel;
		DetailLevel overlay2DDetailLevel2 = (DetailLevel)Plugin.Configuration.Overlay2D_DetailLevel;
		ImGui.SliderInt("信息显示级别##2d", ref overlay2DDetailLevel, 0, 2, overlay2DDetailLevel2.ToString());
		ImGui.Separator();
		ImGui.Checkbox("启用外置地图##externalMap", ref Plugin.Configuration.ExternalMap_Enabled);
		ImGui.Checkbox("锁定位置大小##externalMap", ref Plugin.Configuration.ExternalMap_LockSizePos);
		ImGui.Checkbox("点击穿透##externalMap", ref Plugin.Configuration.ExternalMap_ClickThrough);
		ImGui.Checkbox("显示地图信息##externalMap", ref Plugin.Configuration.ExternalMap_ShowMapInfo);
		ImGui.SliderFloat("地图透明度##externalMap", ref Plugin.Configuration.ExternalMap_MapAlpha, 0f, 1f);
		ImGui.SliderFloat("背景透明度##externalMap", ref Plugin.Configuration.ExternalMap_BgAlpha, 0f, 1f);
		ref int externalMapMode = ref Plugin.Configuration.ExternalMap_Mode;
		MapMode externalMapMode2 = (MapMode)Plugin.Configuration.ExternalMap_Mode;
		ImGui.SliderInt("地图模式##externalMap", ref externalMapMode, 0, 2, externalMapMode2.ToString());
		ImGui.Separator();
		ImGui.TextUnformatted("名牌设置");
		if (ImGui.GetIO().Fonts.Fonts.Size > 2)
		{
			ImGui.Checkbox("大字体##2D", ref Plugin.Configuration.Overlay2D_UseLargeFont);
		}
		ImGui.Checkbox("文字描边##Overlay2D_TextStroke", ref Plugin.Configuration.Overlay2D_TextStroke);
		ImGui.Separator();
		ImGui.TextUnformatted("标识设置");
		ImGui.SliderFloat("标识大小##Overlay2D_DotSize", ref Plugin.Configuration.Overlay2D_DotSize, 3f, 15f);
		ImGui.SliderFloat("标识描边宽度##Overlay2D_DotStroke", ref Plugin.Configuration.Overlay2D_DotStroke, 0f, 5f);
	}

	private static void Config3D()
	{
		ImGui.TextWrapped("在游戏世界空间显示物体信息叠加层。");
		ImGui.Checkbox("启用3D覆盖", ref Plugin.Configuration.Overlay3D_Enabled);
		ImGui.Checkbox("显示屏幕外物体", ref Plugin.Configuration.Overlay3D_ShowOffscreen);
		ImGui.Checkbox("显示当前目标线", ref Plugin.Configuration.Overlay3D_DrawObjectLineCurrentTarget);
		ImGui.Checkbox("显示以你为目标的目标线", ref Plugin.Configuration.Overlay3D_DrawObjectLineTargetingYou);
		ImGui.Checkbox("显示所有物体目标线", ref Plugin.Configuration.Overlay3D_DrawObjectLineAll);
		ref int overlay3DDetailLevel = ref Plugin.Configuration.Overlay3D_DetailLevel;
		DetailLevel overlay3DDetailLevel2 = (DetailLevel)Plugin.Configuration.Overlay3D_DetailLevel;
		ImGui.SliderInt("信息显示级别##3d", ref overlay3DDetailLevel, 0, 2, overlay3DDetailLevel2.ToString());
		ImGui.Separator();
		ImGui.TextUnformatted("名牌设置");
		if (ImGui.GetIO().Fonts.Fonts.Size > 2)
		{
			ImGui.Checkbox("大字体##3D", ref Plugin.Configuration.Overlay3D_UseLargeFont);
		}
		ImGui.Checkbox("名牌居中显示##3D", ref Plugin.Configuration.Overlay3D_CenterAlign);
		ImGui.SliderFloat("名牌圆角", ref Plugin.Configuration.Overlay3D_NamePlateRound, 0f, 10f);
		ImGui.SliderFloat("名牌背景透明度##3D", ref Plugin.Configuration.Overlay3D_NamePlateBgAlpha, 0f, 1f);
		ImGui.Separator();
		ImGui.TextUnformatted("标识设置");
		if (ImGui.RadioButton("方形", Plugin.Configuration.Overlay3D_RingType == RingSegmentsType.Quad))
		{
			Plugin.Configuration.Overlay3D_RingType = RingSegmentsType.Quad;
		}
		ImGui.SameLine();
		if (ImGui.RadioButton("六边形", Plugin.Configuration.Overlay3D_RingType == RingSegmentsType.Hexagon))
		{
			Plugin.Configuration.Overlay3D_RingType = RingSegmentsType.Hexagon;
		}
		ImGui.SameLine();
		if (ImGui.RadioButton("圆形", Plugin.Configuration.Overlay3D_RingType == RingSegmentsType.Circle))
		{
			Plugin.Configuration.Overlay3D_RingType = RingSegmentsType.Circle;
		}
		ImGui.DragFloat2("边框保留宽度", ref Plugin.Configuration.Overlay3D_ClampVector2, 0.1f, 0f, 1000f);
		ImGui.SliderFloat("屏幕内标识大小", ref Plugin.Configuration.Overlay3D_RingSize, 2f, 50f);
		ImGui.SliderFloat("边缘标识大小", ref Plugin.Configuration.Overlay3D_ArrowSize, 5f, 50f);
		ImGui.SliderFloat("边缘标识粗细", ref Plugin.Configuration.Overlay3D_ArrorThickness, 0.5f, 50f);
		ImGui.SliderFloat("标识描边宽度", ref Plugin.Configuration.Overlay3D_IconStrokeThickness, 0f, 10f);
	}

	private void MobHuntAndCustomObjects()
	{
		ImGui.TextWrapped("用单独的提示窗口显示狩猎怪和自定义名称的物体。\n需要显示的物体名可以在下方自行添加。");
		ImGui.Checkbox("显示狩猎怪", ref Plugin.Configuration.OverlayHint_MobHuntView);
        if (Plugin.Configuration.OverlayHint_MobHuntView)
        {
            ImGui.SameLine();
            ImGui.Checkbox("显示S怪", ref Plugin.Configuration.OverlayHintShowRankS);
            ImGui.SameLine();
            ImGui.Checkbox("显示A怪", ref Plugin.Configuration.OverlayHintShowRankA);
            ImGui.SameLine();
            ImGui.Checkbox("显示B怪", ref Plugin.Configuration.OverlayHintShowRankB);
        }
        ImGui.Separator();
		ImGui.Checkbox("显示自定义物体", ref Plugin.Configuration.OverlayHint_CustomObjectView);
		ImGui.Separator();
		if (ImGui.GetIO().Fonts.Fonts.Size > 2)
		{
			ImGui.Checkbox("大字体##hints", ref Plugin.Configuration.OverlayHint_LargeFont);
		}
		ImGui.Checkbox("显示目标线(3D)##specialObjects", ref Plugin.Configuration.OverlayHint_ShowSpecialObjectLine);
		ImGui.Checkbox("鼠标悬停在窗口时按Alt打开地图链接", ref Plugin.Configuration.OverlayHint_OpenMapLinkOnAlt);
		ImGui.DragFloat2("提示窗口位置", ref Plugin.Configuration.WindowPos, 1f, 0f, 10000f);
		ImGui.SliderFloat("提示窗口边框宽度", ref Plugin.Configuration.OverlayHint_BorderSize, 0f, 5f);
		ImGui.SliderFloat("窗口背景透明度##overlayHint", ref Plugin.Configuration.OverlayHint_BgAlpha, 0f, 1f);
		if (!ImGui.BeginTable("CustomObjectTable", 4, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.PadOuterX))
		{
			return;
		}
		ImGui.TableSetupScrollFreeze(0, 1);
		ImGui.TableSetupColumn("自定义物体名");
		ImGui.TableSetupColumn("颜色");
		ImGui.TableSetupColumn("添加/删除");
		ImGui.TableHeadersRow();
		foreach (KeyValuePair<string, CustomObjectValue> customHighlightObject in Plugin.Configuration.customHighlightObjects)
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			bool v = customHighlightObject.Value.Enabled;
			if (ImGui.Checkbox(customHighlightObject.Key + "##highlightObject", ref v))
			{
				Plugin.Configuration.customHighlightObjects[customHighlightObject.Key] = customHighlightObject.Value with
				{
					Enabled = v
				};
				break;
			}
			ImGui.TableNextColumn();
			Vector4 originalColor = customHighlightObject.Value.Color;
			ImguiUtil.ColorPickerWithPalette(customHighlightObject.Key.GetHashCode(), string.Empty, ref originalColor, ImGuiColorEditFlags.None);
			if (originalColor != customHighlightObject.Value.Color)
			{
				Plugin.Configuration.customHighlightObjects[customHighlightObject.Key] = customHighlightObject.Value with
				{
					Color = originalColor
				};
				break;
			}
			ImGui.TableNextColumn();
			if (ImguiUtil.IconButton(FontAwesomeIcon.Trash, customHighlightObject.Key + "##delete"))
			{
				Plugin.Configuration.customHighlightObjects.Remove(customHighlightObject.Key);
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
		bool flag = ImguiUtil.IconButton(FontAwesomeIcon.Plus, "##newCustomObjectEntry");
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
				Plugin.Configuration.customHighlightObjects[newCustomObjectName] = new CustomObjectValue
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
		ImGui.Checkbox("深层迷宫实体显示", ref Plugin.Configuration.DeepDungeon_EnableTrapView);
		ImGui.Checkbox("显示计数", ref Plugin.Configuration.DeepDungeon_ShowObjectCount);
		ImGui.Spacing();
		ImGui.SliderFloat("最远显示距离", ref Plugin.Configuration.DeepDungeon_ObjectShowDistance, 15f, 500f, Plugin.Configuration.DeepDungeon_ObjectShowDistance.ToString("##.0m"), ImGuiSliderFlags.Logarithmic);
		ImGui.Separator();
		if (ImGui.Button("导出当前记录点到剪贴板"))
		{
			Plugin.PluginLog.Information("exporting...");
			Plugin.PluginLog.Information($"exported {(from i in Plugin.Configuration.DeepDungeonObjects
				group i by i.Territory).Count()} territories, {Plugin.Configuration.DeepDungeonObjects.Count(i => i.Type == DeepDungeonType.Trap)} traps, {Plugin.Configuration.DeepDungeonObjects.Count(i => i.Type == DeepDungeonType.AccursedHoard)} hoards.");
			ImGui.SetClipboardText(Plugin.Configuration.DeepDungeonObjects.ToCompressedString());
		}
		if (deepDungeonObjectsImportCache == null || deepDungeonObjectsImportCache.Count == 0)
		{
			ImGui.SameLine();
			if (ImGui.Button("从剪贴板导入已有的记录点"))
			{
				importingError = false;
				try
				{
					HashSet<DeepDungeonObject> source = ImGui.GetClipboardText().DecompressStringToObject<HashSet<DeepDungeonObject>>();
					if (source.Count != 0)
					{
						deepDungeonObjectsImportCache = source;
					}
				}
				catch (Exception ex)
				{
					importingError = true;
					Plugin.PluginLog.Warning(ex, "error when importing deep dungeon object list.");
					errorMessage = ex.Message;
				}
			}
			if (importingError)
			{
				ImGui.TextColored(RedVector4, "导入发生错误，请检查导入的字符串和日志。");
				ImGui.TextColored(RedVector4, errorMessage);
			}
			return;
		}
		ImGui.SameLine();
		if (ImGui.Button("正在准备导入..."))
		{
			deepDungeonObjectsImportCache = null;
			Plugin.PluginLog.Debug("user canceled importing task.");
			return;
		}
		bool flag = ImGui.SliderInt("树视图展开级别", ref treeLevel, 1, 4, GetFormat(treeLevel));
		IEnumerable<IGrouping<ushort, DeepDungeonObject>> source2 = from i in deepDungeonObjectsImportCache
			group i by i.Territory;

        string arg = string.Join(", ", source2
                                       .Select(i => i.Key.ToString())// 选择 Key 并将其转换为字符串
                                       .OrderBy(i => i));// 按照字符串排序
        IGrouping<string, DeepDungeonObject>[] array = deepDungeonObjectsImportCache
                                                       .GroupBy(i => TerritoryIdToBg[i.Territory])// 根据 TerritoryIdToBg[i.Territory] 进行分组
                                                       .OrderBy(i => i.Key)// 按 Key 排序
                                                       .ToArray();// 转换为数组
        ImGui.TextWrapped($"将要导入 {array.Length} 个区域的 {deepDungeonObjectsImportCache.Count} 条记录。({arg})\n包含 {
            array.Select(i => (from j in i
			where j.Type == DeepDungeonType.Trap
			group j by j.Location2D).Count()).Sum()} 个陷阱位置，{array.Select(i => (from j in i
			where j.Type == DeepDungeonType.AccursedHoard
			group j by j.Location2D).Count()).Sum()} 个宝藏位置。");
		if (ImGui.BeginChild("deepDungeonObjectTreeview", new Vector2(-1f, (0f - ImGui.GetFrameHeightWithSpacing()) * 2f), border: true))
		{
			foreach (IGrouping<string, DeepDungeonObject> grouping in array)
			{
				if (flag)
				{
					ImGui.SetNextItemOpen(treeLevel > 1);
				}
				if (!ImGui.TreeNodeEx(grouping.Key + "##deepDungeonTerritoryKey", ImGuiTreeNodeFlags.Framed))
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
					foreach (IGrouping<Vector2, DeepDungeonObject> item2 in from i in item
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
						foreach (DeepDungeonObject item3 in item2.OrderBy(i => i.InstanceId))
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
		ImGui.TextColored(new Vector4(1f, 0.8f, 0f, 1f), "确认后数据将合并到本机记录且不可撤销，请确认数据来源可靠。要继续吗？");
		ImGui.Spacing();
		if (ImGui.Button("取消导入##importDecline"))
		{
			deepDungeonObjectsImportCache = null;
			Plugin.PluginLog.Debug("user canceled importing task.");
			return;
		}
		ImGui.SameLine();
		if (ImGui.Button("确认导入##importAccept"))
		{
			int count = Plugin.Configuration.DeepDungeonObjects.Count;
			Plugin.Configuration.DeepDungeonObjects.UnionWith(deepDungeonObjectsImportCache);
			deepDungeonObjectsImportCache = null;
			int num = Plugin.Configuration.DeepDungeonObjects.Count - count;
			Plugin.PluginLog.Information($"imported {num} deep dungeon object records.");
		}
        return;

		static string GetFormat(int input)
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
			for (int i = 0; i < Plugin.Configuration.Overlay_ShowKinds.Length; i++)
			{
				Plugin.Configuration.Overlay_ShowKinds[i] = true;
			}
		}
		ImGui.SameLine();
		if (ImGui.Button("全不选"))
		{
			for (int j = 0; j < Plugin.Configuration.Overlay_ShowKinds.Length; j++)
			{
				Plugin.Configuration.Overlay_ShowKinds[j] = false;
			}
		}
		ImGui.SameLine();
		if (ImGui.Button("反选"))
		{
			for (int k = 0; k < Plugin.Configuration.Overlay_ShowKinds.Length; k++)
			{
				Plugin.Configuration.Overlay_ShowKinds[k] = !Plugin.Configuration.Overlay_ShowKinds[k];
			}
		}
		ImGui.SameLine();
		if (ImGui.Button("玩家"))
		{
			for (int l = 0; l < Plugin.Configuration.Overlay_ShowKinds.Length; l++)
			{
				Plugin.Configuration.Overlay_ShowKinds[l] = false;
			}
			Plugin.Configuration.Overlay_ShowKinds[3] = true;
		}
		ImGui.SameLine();
		if (ImGui.Button("NPC"))
		{
			for (int m = 0; m < Plugin.Configuration.Overlay_ShowKinds.Length; m++)
			{
				Plugin.Configuration.Overlay_ShowKinds[m] = false;
			}
			Plugin.Configuration.Overlay_ShowKinds[4] = true;
			Plugin.Configuration.Overlay_ShowKinds[5] = true;
			Plugin.Configuration.Overlay_ShowKinds[6] = true;
			Plugin.Configuration.Overlay_ShowKinds[7] = true;
			Plugin.Configuration.Overlay_ShowKinds[9] = true;
		}
		ImGui.SameLine();
		ImGui.Checkbox("只显示可选中物体", ref Plugin.Configuration.Overlay_OnlyShowTargetable);
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
				ImGui.Checkbox(getEnumNames[n] + "##ObjectKindCheckbox", ref Plugin.Configuration.Overlay_ShowKinds[n]);
				ImGui.TableNextColumn();
				ImguiUtil.ColorPickerWithPalette(n, string.Empty, ref Plugin.Configuration.KindColors[n], ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview);
				ImGui.TableNextColumn();
				ImguiUtil.ColorPickerWithPalette(int.MaxValue - n, string.Empty, ref Plugin.Configuration.KindColorsBg[n], ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview);
			}
			ImGui.EndTable();
		}
	}

	private void DrawConfig()
	{
		ImGui.SetNextWindowSize(new Vector2(480f, 640f), ImGuiCond.FirstUseEver);
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
				if (ImGui.BeginChild("Profiles") && ImGui.BeginTable("ProfilesTable", 3, ImGuiTableFlags.PadOuterX, new Vector2(-1f, ImGui.GetWindowSize().Y - ImGui.GetFrameHeightWithSpacing())))
				{
					ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
					ImGui.TableNextColumn();
					ImGui.TextUnformatted("预设名");
					ImGui.TableNextColumn();
					ImGui.TextUnformatted("最后保存时间");
					ImGui.TableNextColumn();
					ImGui.TextUnformatted("读取 / 覆盖 / 删除");
					var num = -1;
					for (int i = 0; i < Plugin.Configuration.profiles.Count; i++)
					{
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ConfigSnapShot configSnapShot = Plugin.Configuration.profiles[i];
						string input = configSnapShot.Name;
						ImGui.Selectable($"##selectable{i}", configSnapShot == currentProfile, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.AllowItemOverlap, new Vector2(0f, ImGui.GetFrameHeight()));
						ImGui.SameLine();
						ImGui.SetCursorPosX(0f);
						ImGui.SetNextItemWidth(-1f);
						if (ImGui.InputText($"##name{i}", ref input, 255u, ImGuiInputTextFlags.AutoSelectAll))
						{
							Plugin.Configuration.profiles[i].Name = input;
						}
						ImGui.TableNextColumn();
						ImGui.TextUnformatted($"{configSnapShot.LastEdit:f}");
						ImGui.TableNextColumn();
						Vector2 size = new Vector2(ImGui.GetFrameHeight() * 1.5f, ImGui.GetFrameHeight());
						if (ImguiUtil.IconButton(FontAwesomeIcon.Upload, $"loadbutton{i}", size))
						{
							currentProfile = configSnapShot;
							configSnapShot.RestoreSnapShot(Plugin.Configuration);
						}
						ImGui.SameLine();
						ImguiUtil.IconButton(FontAwesomeIcon.Download, $"savebutton{i}", size);
						if (ImGui.IsItemHovered())
						{
							if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
							{
								Plugin.Configuration.profiles[i] = ConfigSnapShot.GetSnapShot(configSnapShot.Name, Plugin.Configuration);
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
						Plugin.Configuration.profiles.RemoveAt(num);
					}
					ImGui.EndTable();
					if (ImGui.Button("保存当前设置为新预设"))
					{
						Plugin.Configuration.profiles.Add(ConfigSnapShot.GetSnapShot(DateTime.Now.ToString("G"), Plugin.Configuration));
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
        return territory switch
        {
            561 => DeepDungeonBg.f1c1,
            562 => DeepDungeonBg.f1c2,
            563 => DeepDungeonBg.f1c3,
            564 or 565 => DeepDungeonBg.f1c4,
            593 or 594 or 595 => DeepDungeonBg.f1c5,
            596 or 597 or 598 => DeepDungeonBg.f1c6,
            599 or 600 => DeepDungeonBg.f1c8,
            601 or 602 => DeepDungeonBg.f1c9,
            603 or 604 or 605 or 606 or 607 => DeepDungeonBg.f1c7,
            770 => DeepDungeonBg.e3c1,
            771 => DeepDungeonBg.e3c2,
            772 or 782 => DeepDungeonBg.e3c3,
            773 or 783 => DeepDungeonBg.e3c4,
            774 or 784 => DeepDungeonBg.e3c5,
            775 or 785 => DeepDungeonBg.e3c6,
            _ => DeepDungeonBg.notInDeepDungeon,
        };
    }

    private void AddDeepDungeonObjectRecord(IGameObject o)
    {
        if (DeepDungeonObjectExtension.IsSilverCoffer(o))
        {
            trapBlacklist.Add(new Vector2(o.Position.X, o.Position.Z));
        }
        if (DeepDungeonObjectExtension.IsAccursedHoard(o))
        {
            DeepDungeonObject deepDungeonObject = new DeepDungeonObject
            {
                Type = DeepDungeonType.AccursedHoard,
                Base = o.DataId,
                InstanceId = (uint)o.GameObjectId,
                Location = o.Position,
                Territory = Plugin.ClientState.TerritoryType
            };
            if (Plugin.Configuration.DeepDungeonObjects.Add(deepDungeonObject))
            {
                Plugin.PluginLog.Information($"New AccursedHoard recorded! {deepDungeonObject}");
            }
        }
        if (DeepDungeonObjectExtension.IsTrap(o) && !trapBlacklist.Contains(new Vector2(o.Position.X, o.Position.Z)))
        {
            DeepDungeonObject deepDungeonObject2 = new DeepDungeonObject
            {
                Type = DeepDungeonType.Trap,
                Base = o.DataId,
                InstanceId = (uint)o.GameObjectId,
                Location = o.Position,
                Territory = Plugin.ClientState.TerritoryType
            };
            if (Plugin.Configuration.DeepDungeonObjects.Add(deepDungeonObject2))
            {
                Plugin.PluginLog.Information($"New Trap recorded! {deepDungeonObject2}");
            }
        }
    }


    private void DrawDeepDungeonObjects()
	{
		foreach (IGrouping<DeepDungeonObject, DeepDungeonObject> item in Plugin.Configuration.DeepDungeonObjects.Where(i => i.Territory != 0 && 
                          i.GetBg == GetDeepDungeonBg(Plugin.ClientState.TerritoryType) && 
                          i.Location.Distance2D(MeWorldPos.Convert()) < Plugin.Configuration.DeepDungeon_ObjectShowDistance).GroupBy((DeepDungeonObject i) => i, DeepDungeonObjectLocationEqual))
		{
			Vector2 ringCenter;
			if (item.Key.Type == DeepDungeonType.Trap)
			{
				if (Plugin.Configuration.DeepDungeon_ShowObjectCount)
				{
					ImDrawListPtr bDL = backgroundDrawList;
					Vector3 location = item.Key.Location;
					string text = $"{item.Count()}";
					ringCenter = default;
					bDL.DrawRingWorldWithText(location, 0.5f, 24, 2f, 4278190335u, text, ringCenter);
				}
				else
				{
					backgroundDrawList.DrawRingWorld(item.Key.Location, 0.5f, 24, 2f, 4278190335u, out ringCenter);
				}
			}
			if (item.Key.Type == DeepDungeonType.AccursedHoard)
			{
				if (Plugin.Configuration.DeepDungeon_ShowObjectCount)
				{
					backgroundDrawList.DrawRingWorldWithText(item.Key.Location + new Vector3(0f, 0.1f, 0f), 0.5f, 24, 2f, 4278255615u, $"{item.Count()}", new Vector2(0f, 0f - ImGui.GetTextLineHeight()));
				}
				else
				{
					backgroundDrawList.DrawRingWorld(item.Key.Location + new Vector3(0f, 0.1f, 0f), 0.5f, 24, 2f, 4278255615u, out ringCenter);
				}
			}
		}
	}

    private bool TryAddSpecialObjectsToDrawList(IGameObject obj, ref uint fgColor, ref uint bgColor)
    {
        string dictionaryName = obj.Name.ToString();
        var myObjectKind = Util.GetMyObjectKind(obj);

        if (Plugin.Configuration.NpcBaseMapping.ContainsKey(obj.DataId))
        {
            // 只是用于显示Name属性相同的obj的不同真实名字
            Plugin.Configuration.NpcBaseMapping.TryGetValue(obj.DataId, out dictionaryName);
        }

        ICharacter objCharacter = obj as ICharacter;
        if (Plugin.Configuration.OverlayHint_CustomObjectView && Plugin.Configuration.customHighlightObjects.TryGetValue(dictionaryName, out var value) && value.Enabled)
        {
            SpecialObjectDrawList.Add((obj, ImGui.ColorConvertFloat4ToU32(value.Color), $"{myObjectKind.ToString().ToUpper()} {((obj.DataId != 0) ? obj.DataId.ToString() : string.Empty)}\nLv.{objCharacter?.Level} {dictionaryName}"));
            fgColor = ImGui.ColorConvertFloat4ToU32(value.Color);
            return true;
        }
        if (Plugin.Configuration.OverlayHint_MobHuntView && obj.ObjectKind == ObjectKind.BattleNpc)
        {
            if (objCharacter is null) return false;
            if (NotoriousMonsters.SRankLazy.Value.Contains(obj.DataId) && Plugin.Configuration.OverlayHintShowRankS)
            {
                SpecialObjectDrawList.Add((obj, 4278190335u, $"S RANK NOTORIOUS MONSTER\nLv.{objCharacter.Level} {dictionaryName}"));
                fgColor = 4278190335u;
                return true;
            }
            if (NotoriousMonsters.ARankLazy.Value.Contains(obj.DataId) && Plugin.Configuration.OverlayHintShowRankA)
            {
                SpecialObjectDrawList.Add((obj, 4278255615u, $"A RANK NOTORIOUS MONSTER\nLv.{objCharacter.Level} {dictionaryName}"));
                fgColor = 4278255615u;
                return true;
            }
            if (NotoriousMonsters.BRankLazy.Value.Contains(obj.DataId) && Plugin.Configuration.OverlayHintShowRankB)
            {
                SpecialObjectDrawList.Add((obj, 4278255360u, $"B RANK NOTORIOUS MONSTER\nLv.{objCharacter.Level} {dictionaryName}"));
                fgColor = 4278255360u;
                return true;
            }
            if (NotoriousMonsters.ListEurekaMobs.Contains(objCharacter.NameId))
            {
                SpecialObjectDrawList.Add((obj, 4294967040u, $"EUREKA ELEMENTAL\nLv.{objCharacter.Level} {dictionaryName}"));
                fgColor = 4294967040u;
                return true;
            }
            if (obj.DataId == 882)
            {
                SpecialObjectDrawList.Add((obj, 4294901760u, $"ODIN\nLv.{objCharacter.Level} {dictionaryName}"));
                fgColor = 4294901760u;
                return true;
            }
        }
        return false;
    }

    private void AddObjectTo2DDrawList(IGameObject a, uint foregroundColor, uint backgroundColor)
    {
        string dictionaryName = a.Name.ToString();
        if (Plugin.Configuration.NpcBaseMapping.ContainsKey(a.DataId))
        {
            Plugin.Configuration.NpcBaseMapping.TryGetValue(a.DataId, out dictionaryName);
        }

        string item = null;
        switch (Plugin.Configuration.Overlay2D_DetailLevel)
        {
            case 1:
                item = (string.IsNullOrEmpty(dictionaryName) ? $"{a.ObjectKind} {a.DataId}" : dictionaryName);
                break;
            case 2:
                item = (string.IsNullOrEmpty(dictionaryName) ? $"{a.ObjectKind} {a.DataId}" : $"{dictionaryName}\u3000{a.Position.Distance2D(MeWorldPos):F2}m");
                break;
            default:
                throw new ArgumentOutOfRangeException();
            case 0:
                break;
        }
        DrawList2D.Add((a.Position, foregroundColor, backgroundColor, item));
    }

    private void DrawObject3D(IGameObject obj, uint foregroundColor, uint bgcolor, bool drawLine, ISharedImmediateTexture icon = null)
    {
        string dictionaryName = obj.Name.ToString();
        if (Plugin.Configuration.NpcBaseMapping.ContainsKey(obj.DataId))
        {
            Plugin.Configuration.NpcBaseMapping.TryGetValue(obj.DataId, out dictionaryName);
        }
        bool flag = false;
        string text = null;
        switch (Plugin.Configuration.Overlay3D_DetailLevel)
        {
            case 0:
                flag = true;
                break;
            case 1:
                text = (string.IsNullOrEmpty(dictionaryName) ? $"{obj.ObjectKind} {obj.DataId}" : dictionaryName);
                break;
            case 2:
                text = (string.IsNullOrEmpty(dictionaryName) ? $"{obj.ObjectKind} {obj.DataId}" : dictionaryName) + $"\t{obj.Position.Distance2D(MeWorldPos):F2}m";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        if (Plugin.Configuration.Overlay3D_DrawObjectLineAll)
        {
            drawLine = true;
        }
        else
        {
            if (Plugin.Configuration.Overlay3D_DrawObjectLineCurrentTarget && Plugin.TargetManager.Target?.Address == (nint?)obj.Address)
            {
                drawLine = true;
            }
            if (Plugin.Configuration.Overlay3D_DrawObjectLineTargetingYou && Plugin.ClientState.LocalPlayer != null && obj.TargetObject!=null && (obj.TargetObject.EntityId == Plugin.ClientState.LocalPlayer.EntityId ))
            {
                drawLine = true;
            }
        }
        Vector3 location = obj.Position;
        var size = ImGuiHelpers.MainViewport.Size;
        var pos = ImGuiHelpers.MainViewport.Pos;
        _ = MeScreenPos - ImGuiHelpers.MainViewport.GetCenter();
        ImGuiHelpers.MainViewport.GetCenter();
        bool flag2 = Util.WorldToScreenEx(location, out var screenPos, out var z, Vector2.Zero, 200f, 100f);
        if (flag2 && z < 0f)
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
        var screenPosVector = screenPos;
        _ = Plugin.Configuration.Overlay3D_ClampVector2;
        var overlay3DClampVector2 = Plugin.Configuration.Overlay3D_ClampVector2;
        var flag3 = false;
        if (Plugin.Configuration.Overlay3D_ShowOffscreen && Util.GetBorderClampedVector2(screenPos, overlay3DClampVector2, out var clampedPos))
        {
            screenPos = clampedPos;
            flag3 = true;
        }
        if (drawLine)
        {
            backgroundDrawList.AddLine(MeScreenPos, screenPosVector, foregroundColor);
        }
        if (flag3 || flag)
        {
            if (flag3)
            {
                var _rotation = screenPosVector - ImGui.GetMainViewport().GetCenter();
                float thickness = Math.Min(Plugin.Configuration.Overlay3D_RingSize * 2f, Plugin.Configuration.Overlay3D_ArrorThickness);
                if (Plugin.Configuration.Overlay3D_IconStrokeThickness != 0f)
                {
                    backgroundDrawList.DrawArrow(screenPos, Plugin.Configuration.Overlay3D_ArrowSize, foregroundColor, bgcolor, _rotation, thickness, Plugin.Configuration.Overlay3D_IconStrokeThickness);
                }
                else
                {
                    backgroundDrawList.DrawArrow(screenPos, Plugin.Configuration.Overlay3D_ArrowSize, foregroundColor, _rotation, thickness);
                }
            }
            else
            {
                backgroundDrawList.DrawCircleOutlined(screenPos, foregroundColor, bgcolor);
            }
        }
        else if (flag2 && z > 0f)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                Vector4 nameplateBackgroundColor = ImGui.ColorConvertU32ToFloat4(bgcolor);
                nameplateBackgroundColor.W = Plugin.Configuration.Overlay3D_NamePlateBgAlpha;
                backgroundDrawList.DrawTextWithBorderBg(screenPos, text, foregroundColor, ImGui.GetColorU32(nameplateBackgroundColor), Plugin.Configuration.Overlay3D_CenterAlign);
            }
            if (icon != null)
            {
                backgroundDrawList.DrawIcon(screenPos - new Vector2(0f, 10f), icon);
            }
            backgroundDrawList.AddCircleFilled(screenPos, 4f, foregroundColor, 4);
        }
    }

    private void DrawSpecialObjectTipWindows()
    {
        //特殊物体名牌
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, Plugin.Configuration.OverlayHint_BorderSize);
        var windowPos = Plugin.Configuration.WindowPos;
        foreach (var specialObjectTuple in SpecialObjectDrawList.OrderBy(i => i.obj.Position.Distance(MeWorldPos)))
        {
            var thisGameObject = specialObjectTuple.obj;
            var fgcolor = specialObjectTuple.fgcolor;
            var nameString = specialObjectTuple.title;
            // 不能用thisGameObject.Address，会在后面获取NameId的时候炸游戏
            if (thisGameObject is not ICharacter objCharacter) return;
            // ICharacter objCharacter = *(ICharacter*)(&thisGameObject);
            ImGui.PushStyleColor(ImGuiCol.Border, fgcolor);
            ImGui.SetNextWindowBgAlpha(Plugin.Configuration.OverlayHint_BgAlpha);
            ImGui.SetNextWindowPos(windowPos);
            if (!ImGui.Begin( $"{thisGameObject.Name.TextValue} {thisGameObject.EntityId}", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus))
            {
                continue;
            }
            windowPos.Y += 15f;
            var pos2 = ImGui.GetWindowPos() + ImGui.GetCursorPos() + new Vector2(ImGui.GetTextLineHeight(), ImGui.GetTextLineHeight());
            if (Plugin.ClientState.LocalPlayer == null)
            {
                return;
            }
            rotation = AdjustRotationToHRotation(Plugin.ClientState.LocalPlayer.Rotation);

            // 指示相对方向的箭头
            ImGui.GetWindowDrawList().DrawArrow(pos2, ImGui.GetTextLineHeightWithSpacing() * 0.618f, fgcolor, (new Vector2(thisGameObject.Position.X, thisGameObject.Position.Z) - MeWorldPos.ToVector2()).Normalize().Rotate(0f - rotation), 5f);
            
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetTextLineHeight() + ImGui.GetTextLineHeightWithSpacing());
            ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(fgcolor), nameString ?? "");
            ImGui.Separator();
            var text = string.Empty;
            if (thisGameObject.ObjectKind is ObjectKind.BattleNpc or ObjectKind.Player)
            {
                var currentHp = objCharacter.CurrentHp;
                var maxHp = objCharacter.MaxHp;
                var percent = currentHp * 1.0 / maxHp;
                text += $"{currentHp:N0}/{maxHp:N0}\t{percent:P}\n";
            }
            var distanceY = thisGameObject.Position.Y - MeWorldPos.Y;
            var direction = (double.Abs(distanceY)<0.1f) ? "" : ((distanceY > 0f) ? "↑" : "↓");
            ImGui.TextUnformatted($"{text}{thisGameObject.Position.Distance2D(MeWorldPos):F2}m\t{direction}{Math.Abs(distanceY):F2}");
            windowPos += new Vector2(0f, ImGui.GetWindowSize().Y);
            if (Plugin.Configuration.OverlayHint_OpenMapLinkOnAlt && ImGui.GetIO().KeyAlt && ImGui.IsMouseHoveringRect(ImGui.GetWindowPos(), ImGui.GetWindowSize() + ImGui.GetWindowPos()))
            {
                try
                {
                    Plugin.Gui.OpenMapWithMapLink(new MapLinkPayload(Plugin.ClientState.TerritoryType, Plugin.ClientState.MapId, (int)(thisGameObject.Position.X * 1000f), (int)(thisGameObject.Position.Z * 1000f)));
                }
                catch (Exception)
                {
                    Plugin.PluginLog.Debug("no map available in this area!");
                }
            }
            ImGui.PopStyleColor();
            ImGui.End();
        }
        ImGui.PopStyleVar(2);
        SpecialObjectDrawList.Clear();
    }

    private static float AdjustRotationToHRotation(float angle)
    {
        /*
         * akira use HRotation at first, which is a little different from LocalPlayer.Rotation
         * this func is to compute HRotation based on Rotation
         * so that I don't have to adjust every place where HRotation is used
         */
        if (angle>0) angle -= (float)Math.PI;
        else if(angle<0) angle += (float)Math.PI;
        return angle;
    }

	private void DrawMapOverlay()
	{
		RefreshMapOrigin();
		Vector2? vector = mapOrigin;
		if (!vector.HasValue)
		{
			return;
		}
		Vector2 valueOrDefault = vector.GetValueOrDefault();
		if (!(valueOrDefault != Vector2.Zero) || Plugin.ClientState.TerritoryType == 0)
		{
			return;
		}
		backgroundDrawList.PushClipRect(mapPosSize[0], mapPosSize[1]);
		foreach (var item in DrawList2D)
		{
			Vector2 pos = WorldToMap(valueOrDefault, item.worldpos);
			backgroundDrawList.DrawMapTextDot(pos, item.name, item.fgcolor, item.bgcolor);
		}
		if (Plugin.Configuration.Overlay2D_ShowCenter)
		{
			backgroundDrawList.DrawMapTextDot(valueOrDefault, "ME", 4294967040u, 4278190080u);
		}
		if (Plugin.Configuration.Overlay2D_ShowAssist && Plugin.ClientState.LocalPlayer!=null)
        {
            rotation = AdjustRotationToHRotation(Plugin.ClientState.LocalPlayer.Rotation);
            backgroundDrawList.AddCircle(valueOrDefault, WorldToMapScale * 25f, 4294967040u, 0, 1f);
			backgroundDrawList.AddCircle(valueOrDefault, WorldToMapScale * 125f, 4286611584u, 0, 1f);
			backgroundDrawList.AddLine(valueOrDefault, 
                                       valueOrDefault - new Vector2(0f, WorldToMapScale * 25f).Rotate(((float)Math.PI / 4f) + rotation), 
                                       4294967040u, 
                                       1f);
            backgroundDrawList.AddLine(valueOrDefault,
                                       valueOrDefault - new Vector2(0f, WorldToMapScale * 25f).Rotate((-(float)Math.PI / 4f) + rotation),
                                       4294967040u,
                                       1f);
        }
		backgroundDrawList.PopClipRect();
	}

	private Vector2 WorldToMap(Vector2 origin, Vector3 worldVector3)
	{
		Vector2 vector = (worldVector3.ToVector2() - MeWorldPos.ToVector2()) * WorldToMapScale;
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
		globalUiScale = areaMapAddon->Scale;
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
				Plugin.PluginLog.Verbose($"node found {i}");
				AtkResNode atkResNode2 = ptr4->AtkResNode;
				Vector2 vector = new Vector2(areaMapAddon->X, areaMapAddon->Y);
				mapOrigin = ImGui.GetMainViewport().Pos + vector + ((new Vector2(atkResNode.X, atkResNode.Y) + new Vector2(atkResNode2.X, atkResNode2.Y) + new Vector2(atkResNode2.OriginX, atkResNode2.OriginY)) * globalUiScale);
				mapPosSize[0] = ImGui.GetMainViewport().Pos + vector + (new Vector2(atkResNode.X, atkResNode.Y) * globalUiScale);
				mapPosSize[1] = ImGui.GetMainViewport().Pos + vector + new Vector2(atkResNode.X, atkResNode.Y) + (new Vector2((int)atkResNode.Width, atkResNode.Height) * globalUiScale);
				break;
			}
		}
	}

	internal unsafe void DrawExternalMap()
	{
		if (Plugin.ClientState.LocalPlayer == null)
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

        map = MapSheet.GetRow(Plugin.ClientState.MapId);
        if (map is null)
        {
            Plugin.PluginLog.Error("error when get map");
            return;
        }
        var rawString = map.Id.RawString;
        string texturePath = "ui/map/" + rawString + "/" + rawString?.Replace("_", string.Empty).Replace("/", string.Empty) + "_m.tex";
        ISharedImmediateTexture fromGame = Plugin.TextureProvider.GetFromGame(texturePath);
        textureWrap = fromGame.GetWrapOrDefault();

        
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
		ImGui.SetNextWindowSizeConstraints(new Vector2(150f, 150f), new Vector2(float.MaxValue, float.MaxValue), delegate(ImGuiSizeCallbackData* data)
		{
			float num4 = Math.Max(data->DesiredSize.X, data->DesiredSize.Y);
			data->DesiredSize = new Vector2(num4, num4);
		});
		ImGuiWindowFlags imGuiWindowFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoDocking;
		if (Plugin.Configuration.ExternalMap_ClickThrough)
		{
			imGuiWindowFlags |= ImGuiWindowFlags.NoMouseInputs;
		}
		if (Plugin.Configuration.ExternalMap_LockSizePos)
		{
			imGuiWindowFlags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
		}
		ImGui.SetNextWindowBgAlpha(Plugin.Configuration.ExternalMap_BgAlpha);
		Vector2 imGuiWindowCenter;
		Vector2 mapOffset;
		float mapSizeFactor;
		if (ImGui.Begin("mapTexture", imGuiWindowFlags))
		{
			ImDrawListPtr windowDrawList = ImGui.GetWindowDrawList();
			windowDrawList.ChannelsSplit(3);
			float windowContentRegionWidth = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
			float num2 = windowContentRegionWidth / 2048f;
			Vector2 windowPos = ImGui.GetWindowPos();
			Vector2[] array = Square4(windowPos, ImGui.GetWindowWidth());
			imGuiWindowCenter = ImGui.GetWindowPos() + (new Vector2(windowContentRegionWidth, windowContentRegionWidth) / 2f);
			mapOffset = new Vector2(map.OffsetX, map.OffsetY);
			mapSizeFactor = map.SizeFactor / 100f * num2;
			windowDrawList.ChannelsSetCurrent(1);
			if (Plugin.Configuration.ExternalMap_ShowMapInfo)
            {
                var text = $" {windowContentRegionWidth / (mapSizeFactor * UvZoom) / 2f:F2}m X: {MeWorldPos.X:N3} Y: {MeWorldPos.Y:N3} Z: {MeWorldPos.Z:N3} ";
				var textSize = ImGui.CalcTextSize(text);
				var leftPos = ImGui.GetWindowSize() - textSize;
				windowDrawList.AddRectFilled(leftPos + windowPos, windowPos + ImGui.GetWindowSize(), 2147483648u);
				ImGui.SetCursorPos(leftPos);
				ImGui.TextColored(Vector4.One, text);
			}
			if (!Plugin.Configuration.ExternalMap_ClickThrough)
			{
				ImGui.SetCursorPos(new Vector2(5f, 5f));
				if (ImguiUtil.IconButton((Plugin.Configuration.ExternalMap_Mode == 0) ? FontAwesomeIcon.Expand : ((Plugin.Configuration.ExternalMap_Mode == 1) ? FontAwesomeIcon.Crosshairs : FontAwesomeIcon.LocationArrow), "ToggleSnap", new Vector2(25f, 25f)))
				{
					Plugin.Configuration.ExternalMap_Mode++;
					Plugin.Configuration.ExternalMap_Mode = Plugin.Configuration.ExternalMap_Mode % 3;
				}
				ImGui.SetCursorPosX(5f);
				if (ImguiUtil.IconButton(FontAwesomeIcon.PlusCircle, "zoom++", new Vector2(25f, 25f)))
				{
					UvZoom *= 1.1f;
				}
				ImGui.SetCursorPosX(5f);
				if (ImguiUtil.IconButton(FontAwesomeIcon.MinusCircle, "zoom--", new Vector2(25f, 25f)))
				{
					UvZoom *= 0.9f;
				}
			}
			windowDrawList.ChannelsSetCurrent(0);
			if (Plugin.Configuration.ExternalMap_Mode != 0)
			{
				Square4(Vector2.Zero, windowContentRegionWidth);
				for (int j = 0; j < 4; j++)
				{
					ref Vector2 reference = ref array[j];
					reference -= (MeWorldPos.ToVector2() + mapOffset) * mapSizeFactor;
					if (Plugin.Configuration.ExternalMap_Mode == 2)
					{
                        rotation = AdjustRotationToHRotation(Plugin.ClientState.LocalPlayer.Rotation);
                        reference = reference.Rotate(0f - rotation, imGuiWindowCenter);
					}
					reference = reference.Zoom(UvZoom, imGuiWindowCenter);
				}

                if (textureWrap is null) { return; }
				windowDrawList.AddImageQuad(textureWrap.ImGuiHandle, array[0], array[1], array[2], array[3], uv1, uv2, uv3, uv4, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, Plugin.Configuration.ExternalMap_MapAlpha)));
				foreach (var item in DrawList2D)
				{
					var positionOfItem = WorldToMap(item.worldpos);
					windowDrawList.DrawMapTextDot(positionOfItem, item.name, item.fgcolor, item.bgcolor);
				}
				if (Plugin.Configuration.Overlay2D_ShowCenter)
				{
					windowDrawList.DrawMapTextDot(imGuiWindowCenter, (Plugin.Configuration.Overlay2D_DetailLevel > 0) ? "ME" : null, 4294967040u, 4278190080u);
					if (Plugin.Configuration.Overlay2D_ShowAssist)
					{
                        rotation = AdjustRotationToHRotation(Plugin.ClientState.LocalPlayer.Rotation);
                        var num3 = ((Plugin.Configuration.ExternalMap_Mode == 2) ? 0f : (0f - rotation));
						windowDrawList.PathArcTo(imGuiWindowCenter, mapSizeFactor * 25f * UvZoom, num3 - ((float)Math.PI / 2f) - ((float)Math.PI / 4f), num3 - ((float)Math.PI / 4f), 24);
						windowDrawList.PathLineTo(imGuiWindowCenter);
						windowDrawList.PathStroke(4294967040u, ImDrawFlags.Closed, 2f);
					}
				}
			}
			else
			{
				for (int k = 0; k < 4; k++)
				{
					ref var reference2 = ref array[k];
					reference2 = reference2.Zoom(UvZoom, imGuiWindowCenter + dragPos);
				}
                if (textureWrap is null) { return; }
                windowDrawList.AddImageQuad(textureWrap.ImGuiHandle, array[0], array[1], array[2], array[3], uv1, uv2, uv3, uv4, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, Plugin.Configuration.ExternalMap_MapAlpha)));
				foreach (var item2 in DrawList2D)
				{
					var pos2 = WorldToMapNoSnap(item2.worldpos);
					windowDrawList.DrawMapTextDot(pos2, item2.name, item2.fgcolor, item2.bgcolor);
				}
				if (Plugin.Configuration.Overlay2D_ShowCenter)
				{
					var vector3 = WorldToMapNoSnap(MeWorldPos);
					windowDrawList.DrawMapTextDot(vector3, (Plugin.Configuration.Overlay2D_DetailLevel > 0) ? "ME" : null, 4294967040u, 4278190080u);
					if (Plugin.Configuration.Overlay2D_ShowAssist)
					{
                        rotation = AdjustRotationToHRotation(Plugin.ClientState.LocalPlayer.Rotation);
                        windowDrawList.PathArcTo(vector3, mapSizeFactor * 25f * UvZoom, 0f - rotation - ((float)Math.PI / 2f) - ((float)Math.PI / 4f), 0f - rotation - ((float)Math.PI / 4f), 24);
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
					if (Plugin.Configuration.ExternalMap_Mode != 0)
					{
						dragPos = (MeWorldPos.ToVector2() + mapOffset) * mapSizeFactor;
						Plugin.Configuration.ExternalMap_Mode = 0;
					}
				}
				UvZoom += UvZoom * ImGui.GetIO().MouseWheel * 0.1f;
			}
			windowDrawList.ChannelsMerge();
			ImGui.End();
		}
		ImGui.PopStyleVar();
		Vector2 WorldToMap(Vector3 worldPos)
		{
			Vector2 vector4 = (worldPos - MeWorldPos).ToVector2() * mapSizeFactor;
			if (Plugin.Configuration.ExternalMap_Mode == 2)
			{
                rotation = AdjustRotationToHRotation(Plugin.ClientState.LocalPlayer.Rotation);
                vector4 = vector4.Rotate(0f - rotation);
			}
			return imGuiWindowCenter + (vector4 * UvZoom);
		}

		Vector2 WorldToMapNoSnap(Vector3 worldPos)
		{
			return imGuiWindowCenter + ((worldPos.ToVector2() + mapOffset) * mapSizeFactor * UvZoom) - (dragPos * (UvZoom - 1f));
		}
	}

	private static Vector2[] Square4(Vector2 ltPos = default, float size = 1f)
	{
		return new Vector2[4]
		{
			ltPos,
			ltPos + new Vector2(size, 0f),
			ltPos + new Vector2(size, size),
			ltPos + new Vector2(0f, size)
		};
	}
}
