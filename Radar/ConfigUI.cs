using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using ImGuiNET;
using Radar.CustomObject;
using Radar.Utils;
using static Radar.RadarEnum;

namespace Radar;

public class ConfigUI : IDisposable
{
    #region VARIABLE

    public bool ConfigVisible;
    private string newCustomObjectName = string.Empty;
    private Vector4 newCustomObjectColor = Vector4.One;

    #endregion

    #region BASE

    public ConfigUI()
    {
        Plugin.PluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OnOpenConfigUi;
        Plugin.PluginInterface.UiBuilder.Draw += BuildUI;
    }
    
    public void Dispose()
    {
        Plugin.PluginInterface.UiBuilder.OpenConfigUi -= UiBuilder_OnOpenConfigUi;
        Plugin.PluginInterface.UiBuilder.Draw -= BuildUI;
    }

    #endregion

    #region SettingsTabs

    private static void Config2D()
    {
        ImGui.TextWrapped("在游戏平面地图上显示物体信息叠加层。");
        ImGui.Checkbox("启用2D覆盖", ref Plugin.Configuration.Overlay2D_Enabled);
        ImGui.Checkbox("显示自己##Overlay2D_ShowCenter", ref Plugin.Configuration.Overlay2D_ShowCenter);
        ImGui.Checkbox("显示辅助圈(25m|125m)", ref Plugin.Configuration.Overlay2D_ShowAssist);
        DetailLevel overlay2DDetailLevel2 = (DetailLevel)Plugin.Configuration.Overlay2D_DetailLevel;
        ImGui.SliderInt("信息显示级别##2d", ref Plugin.Configuration.Overlay2D_DetailLevel, 0, 2, overlay2DDetailLevel2.ToString());
        ImGui.Separator();
        ImGui.Checkbox("启用外置地图##externalMap", ref Plugin.Configuration.ExternalMap_Enabled);
        ImGui.Checkbox("锁定位置大小##externalMap", ref Plugin.Configuration.ExternalMap_LockSizePos);
        ImGui.Checkbox("点击穿透##externalMap", ref Plugin.Configuration.ExternalMap_ClickThrough);
        ImGui.Checkbox("显示地图信息##externalMap", ref Plugin.Configuration.ExternalMap_ShowMapInfo);
        ImGui.SliderFloat("地图透明度##externalMap", ref Plugin.Configuration.ExternalMap_MapAlpha, 0f, 1f);
        ImGui.SliderFloat("背景透明度##externalMap", ref Plugin.Configuration.ExternalMap_BgAlpha, 0f, 1f);
        MapMode externalMapMode2 = (MapMode)Plugin.Configuration.ExternalMap_Mode;
        ImGui.SliderInt("地图模式##externalMap", ref Plugin.Configuration.ExternalMap_Mode, 0, 2, externalMapMode2.ToString());
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
        DetailLevel overlay3DDetailLevel2 = (DetailLevel)Plugin.Configuration.Overlay3D_DetailLevel;
        ImGui.SliderInt("信息显示级别##3d", ref Plugin.Configuration.Overlay3D_DetailLevel, 0, 2, overlay3DDetailLevel2.ToString());
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
        ImGui.TableSetupColumn("启用");
        ImGui.TableSetupColumn("颜色");
        ImGui.TableSetupColumn("添加/删除");
        ImGui.TableHeadersRow();
        var customHighlightObjList = Plugin.Configuration.customHighlightObjects.ToList();
        foreach (var customHighlightObject in customHighlightObjList)
        {
            var name = customHighlightObject.Key;
            var obj = customHighlightObject.Value;
            var enabled = obj.Enabled;
            var index = customHighlightObjList.IndexOf(customHighlightObject);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.InputText($"##name{index}", ref name, 64);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                customHighlightObjList[index] = new KeyValuePair<string, CustomObjectValue>(name, obj);
                Plugin.Configuration.customHighlightObjects = customHighlightObjList.ToDictionary(x => x.Key, x => x.Value);
                break;
            }

            ImGui.TableNextColumn();
            if (ImGui.Checkbox($"##enabled{index}", ref enabled))
            {
                Plugin.Configuration.customHighlightObjects[customHighlightObject.Key] = customHighlightObject.Value with
                {
                    Enabled = enabled
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
        bool isInput = ImGui.InputTextWithHint("##newName", "要添加的物体名，留空添加当前目标名", ref newCustomObjectName, 64, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.TableNextColumn();
        var newObjectEnabled = true;
        ImGui.Checkbox("##newObjEnabled", ref newObjectEnabled);
        ImGui.TableNextColumn();
        ImguiUtil.ColorPickerWithPalette(99999, string.Empty, ref newCustomObjectColor, ImGuiColorEditFlags.None);
        ImGui.TableNextColumn();
        bool isAddButtonPressed = ImguiUtil.IconButton(FontAwesomeIcon.Plus, "##newCustomObjectEntry");
        ImGui.TableNextColumn();
        if (isInput || isAddButtonPressed)
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
                    Enabled = newObjectEnabled
                };
                newCustomObjectName = string.Empty;
            }
        }
        ImGui.EndTable();
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
        string[] getEnumNames = Enum.GetNames(typeof(MyObjectKind));
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
                if (ImGui.BeginChild("显示类别##childWindow"))
                {
                    ConfigObjectKind();
                    ImGui.EndChild();
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("特殊物体"))
            {
                if (ImGui.BeginChild("狩猎&自定义##childWindow"))
                {
                    MobHuntAndCustomObjects();
                    ImGui.EndChild();
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("2D覆盖"))
            {
                if (ImGui.BeginChild("config2d##child"))
                {
                    Config2D();
                    ImGui.EndChild();
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("3D覆盖"))
            {
                if (ImGui.BeginChild("config3d##child"))
                {
                    Config3D();
                    ImGui.EndChild();
                }
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
        ImGui.End();
    }

    #endregion

    private void UiBuilder_OnOpenConfigUi()
    {
        ConfigVisible = !ConfigVisible;
    }
    
    private void BuildUI()
    {
        if (ConfigVisible)
        {
            DrawConfig();
        }
    }
}
