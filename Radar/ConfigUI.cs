using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Lumina.Excel;
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
    private HashSet<DeepDungeonObject> deepDungeonObjectsImportCache;
    private bool importingError;
    private static Vector4 RedVector4 = new(1f, 0f, 0f, 1f);
    private int treeLevel;
    private string errorMessage = string.Empty;
    private Dictionary<ushort, string> territoryIdToBg;
    private static readonly ExcelSheet<TerritoryType> TerritoryTypeSheet = Plugin.DataManager.GetExcelSheet<TerritoryType>();

    private Dictionary<ushort, string> TerritoryIdToBg
    {
        get
        {
            if (territoryIdToBg == null)
            {
                territoryIdToBg = TerritoryTypeSheet.ToDictionary((i) => (ushort)i.RowId, (j) => j.Bg.ExtractText());
                territoryIdToBg[0] = "未记录区域（数据不可用）";
            }
            return territoryIdToBg;
        }
    }

    #endregion

    #region BASE

    public ConfigUI()
    {
        Plugin.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
        Plugin.PluginInterface.UiBuilder.OpenMainUi += OnOpenConfigUi;
        Plugin.PluginInterface.UiBuilder.Draw += DrawConfig;
    }
    
    public void Dispose()
    {
        Plugin.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        Plugin.PluginInterface.UiBuilder.OpenMainUi -= OnOpenConfigUi;
        Plugin.PluginInterface.UiBuilder.Draw -= DrawConfig;
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
        ImGui.TextWrapped($"将要导入 {array.Length} 个区域的 {deepDungeonObjectsImportCache.Count} 条记录。({arg})\n包含 {array.Select(i => (from j in i
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

    private void DrawConfig()
    {
        if (!ConfigVisible) return;
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

            if (ImGui.BeginTabItem("Deep Dungeon"))
            {
                if (ImGui.BeginChild("##DeepDungeonSettings"))
                {
                    ConfigDeepDungeonRecord();
                    ImGui.EndChild();
                }
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
        ImGui.End();
    }

    #endregion

    private void OnOpenConfigUi() => ConfigVisible ^= true;
}
