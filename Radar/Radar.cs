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
using Radar.Utils;
using SharpDX;
using static Radar.RadarEnum;
using Map = Lumina.Excel.Sheets.Map;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
using TerritoryType = Lumina.Excel.Sheets.TerritoryType;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Radar;

public class Radar : IDisposable
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

    #region VARIABLE

    private Vector2? mapOrigin = Vector2.Zero;
	private float globalUiScale = 1f;
	private Vector2[] mapPosSize = new Vector2[2];
	private static Vector2 MeScreenPos = ImGuiHelpers.MainViewport.GetCenter();
	private static Vector3 MeWorldPos = Vector3.Zero;
	internal static Matrix MatrixSingetonCache;
	internal static Vector2 ViewPortSizeCache;
    private ImDrawListPtr foregroundDrawList;
	private ImDrawListPtr backgroundDrawList;
	private Dictionary<ushort, bool> isPvpZoneDict;
	private Dictionary<ushort, string> territoryIdToBg;
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
    private List<(IGameObject obj, uint fgcolor, string title)> SpecialObjectDrawList { get; } = new();
    private static int FontsSize => ImGui.GetIO().Fonts.Fonts.Size;
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
    private Dictionary<ushort, bool> IsPvpZone => isPvpZoneDict 
                                                      ??= TerritoryTypeSheet.ToDictionary(
                                                          i => (ushort)i.RowId, 
                                                          j => j.IsPvpZone
                                                      );
    private static readonly ExcelSheet<TerritoryType> TerritoryTypeSheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();
    private static readonly ExcelSheet<Map>           MapSheet           = Plugin.DataManager.GetExcelSheet<Map>();

    #endregion

	public static DeepDungeonTerritoryEqualityComparer DeepDungeonTerritoryEqual { get; set; }

	public static DeepDungeonObjectLocationEqualityComparer DeepDungeonObjectLocationEqual { get; set; }

	public Radar()
	{
		sizeFactorDict = TerritoryTypeSheet.ToDictionary(k => k.RowId, v => v.Map.Value.SizeFactor);
		DeepDungeonTerritoryEqual = new DeepDungeonTerritoryEqualityComparer();
		DeepDungeonObjectLocationEqual = new DeepDungeonObjectLocationEqualityComparer();
		Plugin.ClientState.TerritoryChanged += TerritoryChanged;
		Plugin.PluginInterface.UiBuilder.Draw += UiBuilder_OnBuildUi;
	}

	private void TerritoryChanged(ushort territoryId)
	{
		Plugin.PluginLog.Information($"territory changed to: {territoryId}");
		trapBlacklist.Clear();
		hoardBlackList.Clear();
	}

	public void Dispose()
	{
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
        string dictionaryName = obj.Name.TextValue;
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
        string dictionaryName = a.Name.TextValue;
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
        string dictionaryName = obj.Name.TextValue;
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
        if (Plugin.ClientState.LocalPlayer == null)
        {
            return;
        }
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, Plugin.Configuration.OverlayHint_BorderSize);
        var windowPos = Plugin.Configuration.WindowPos;
        foreach (var specialObjectTuple in SpecialObjectDrawList.OrderBy(i => i.obj.Position.Distance(MeWorldPos)))
        {
            var thisGameObject = specialObjectTuple.obj;
            var fgcolor = specialObjectTuple.fgcolor;
            var nameString = specialObjectTuple.title;
            // 不能用thisGameObject.Address，会在后面获取NameId的时候炸游戏
            if (thisGameObject is not ICharacter objCharacter) continue;
            ImGui.PushStyleColor(ImGuiCol.Border, fgcolor);
            ImGui.SetNextWindowBgAlpha(Plugin.Configuration.OverlayHint_BgAlpha);
            ImGui.SetNextWindowPos(windowPos);
            if (!ImGui.Begin( $"{thisGameObject.Name.TextValue} {thisGameObject.EntityId}", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus))
            {
                continue;
            }
            windowPos.Y += 15f;
            var pos2 = ImGui.GetWindowPos() + ImGui.GetCursorPos() + new Vector2(ImGui.GetTextLineHeight(), ImGui.GetTextLineHeight());
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
            ImGui.TextUnformatted($"{text}{thisGameObject.Position.Distance2D(MeWorldPos):F2}m\t{direction}{Math.Abs(distanceY):F1}m");
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

		IDalamudTextureWrap textureWrap = null;

        Map map = MapSheet.GetRow(Plugin.ClientState.MapId);

        var rawString = map.Id.ExtractText();
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
					Plugin.Configuration.ExternalMap_Mode %= 3;
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
                if (textureWrap is null) return;
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
