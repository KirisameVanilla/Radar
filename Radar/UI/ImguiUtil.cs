using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace Radar.UI;

internal static class ImguiUtil
{
	public static void ColorPickerWithPalette(int id, string description, ref Vector4 originalColor, ImGuiColorEditFlags flags)
	{
		Vector4 col = originalColor;
		List<Vector4> list = ImGuiHelpers.DefaultColorPalette(36);
		if (ImGui.ColorButton($"{description}###ColorPickerButton{id}", originalColor, flags))
		{
			ImGui.OpenPopup($"###ColorPickerPopup{id}");
		}
		if (!ImGui.BeginPopup($"###ColorPickerPopup{id}"))
		{
			return;
		}
		if (ImGui.ColorPicker4($"###ColorPicker{id}", ref col, flags))
		{
			originalColor = col;
		}
		for (int i = 0; i < 4; i++)
		{
			ImGui.Spacing();
			for (int j = i * 9; j < i * 9 + 9; j++)
			{
				if (ImGui.ColorButton($"###ColorPickerSwatch{id}{i}{j}", list[j]))
				{
					originalColor = list[j];
					ImGui.CloseCurrentPopup();
					ImGui.EndPopup();
					return;
				}
				ImGui.SameLine();
			}
		}
		ImGui.EndPopup();
	}

	public static bool DrawRingWorld(this ImDrawListPtr drawList, Vector3 vector3, float radius, int numSegments, float thicc, uint colour, out Vector2 ringCenter)
	{
		if (!Util.WorldToScreenEx(vector3, out ringCenter, out var Z) || Z < 0f)
		{
			return false;
		}
		int num = numSegments / 2;
		for (int i = 0; i <= numSegments; i++)
		{
			Util.WorldToScreenEx(new Vector3(vector3.X + radius * (float)Math.Sin(Math.PI / (double)num * (double)i), vector3.Y, vector3.Z + radius * (float)Math.Cos(Math.PI / (double)num * (double)i)), out var screenPos, out var _);
			drawList.PathLineTo(new Vector2(screenPos.X, screenPos.Y));
		}
		drawList.PathStroke(colour, ImDrawFlags.Closed, thicc);
		return true;
	}

	public static bool DrawRingWorldWithText(this ImDrawListPtr drawList, Vector3 vector3, float radius, int numSegments, float thicc, uint colour, string text, Vector2 offset = default(Vector2))
	{
		if (!drawList.DrawRingWorld(vector3, radius, numSegments, thicc, colour, out var ringCenter))
		{
			return false;
		}
		drawList.DrawTextWithBorderBg(ringCenter + offset, text, colour, 2147483648u);
		return true;
	}

	public static void DrawCircleOutlined(this ImDrawListPtr drawList, Vector2 screenPos, uint fgcol, uint bgcol)
	{
		var overlay3D_RingType = (int)Plugin.Configuration.Overlay3D_RingType;
		var overlay3D_RingSize = Plugin.Configuration.Overlay3D_RingSize;
		var overlay3D_IconStrokeThickness = Plugin.Configuration.Overlay3D_IconStrokeThickness;
		drawList.AddCircleFilled(screenPos, overlay3D_RingSize, fgcol, overlay3D_RingType);
		drawList.AddCircle(screenPos, overlay3D_RingSize, bgcol, overlay3D_RingType, overlay3D_IconStrokeThickness / 2f);
	}

	internal static bool IconButton(FontAwesomeIcon icon, string id, Vector2 size)
	{
		ImGui.PushFont(UiBuilder.IconFont);
		bool result = ImGui.Button(icon.ToIconString() + "##" + id, size);
		ImGui.PopFont();
		return result;
	}

	internal static bool IconButton(FontAwesomeIcon icon, string id)
	{
		ImGui.PushFont(UiBuilder.IconFont);
		bool result = ImGui.Button(icon.ToIconString() + "##" + id);
		ImGui.PopFont();
		return result;
	}

	public static void DrawText(this ImDrawListPtr drawList, Vector2 pos, string text, uint col, bool stroke, bool centerAlignX = true, uint strokecol = 4278190080u)
	{
		if (centerAlignX)
		{
			pos -= new Vector2(ImGui.CalcTextSize(text).X, 0f) / 2f;
		}
		if (stroke)
		{
			drawList.AddText(pos + new Vector2(-1f, -1f), strokecol, text);
			drawList.AddText(pos + new Vector2(-1f, 1f), strokecol, text);
			drawList.AddText(pos + new Vector2(1f, -1f), strokecol, text);
			drawList.AddText(pos + new Vector2(1f, 1f), strokecol, text);
		}
		drawList.AddText(pos, col, text);
	}

	public static void DrawTextWithBorderBg(this ImDrawListPtr drawList, Vector2 pos, string text, uint col = uint.MaxValue, uint bgcol = 4278190080u, bool centerAlignX = true)
	{
		Vector2 vector = ImGui.CalcTextSize(text) + new Vector2(ImGui.GetStyle().ItemSpacing.X, 0f);
		if (centerAlignX)
		{
			pos -= new Vector2(vector.X, 0f) / 2f;
		}
		drawList.AddRectFilled(pos, pos + vector, bgcol, Plugin.Configuration.Overlay3D_NamePlateRound);
		drawList.AddRect(pos, pos + vector, col, Plugin.Configuration.Overlay3D_NamePlateRound);
		drawList.AddText(pos + new Vector2(ImGui.GetStyle().ItemSpacing.X / 2f + 0.5f, -0.5f), col, text);
	}

	public static void DrawArrow(this ImDrawListPtr drawList, Vector2 pos, float size, uint color, uint bgcolor, Vector2 rotation, float thickness, float outlinethickness)
	{
		drawList.AddPolyline(ref (new Vector2[3]
		{
			pos + new Vector2(0f - size - outlinethickness / 2f, -0.4f * size - outlinethickness / 2f).Rotate(rotation),
			pos + new Vector2(0f, 0.6f * size).Rotate(rotation),
			pos + new Vector2(size + outlinethickness / 2f, -0.4f * size - outlinethickness / 2f).Rotate(rotation)
		})[0], 3, bgcolor, ImDrawFlags.RoundCornersAll, thickness + outlinethickness);
		drawList.DrawArrow(pos, size, color, rotation, thickness);
	}

	public static void DrawArrow(this ImDrawListPtr drawList, Vector2 pos, float size, uint color, Vector2 rotation, float thickness)
	{
		drawList.AddPolyline(ref (new Vector2[3]
		{
			pos + new Vector2(0f - size, -0.4f * size).Rotate(rotation),
			pos + new Vector2(0f, 0.6f * size).Rotate(rotation),
			pos + new Vector2(size, -0.4f * size).Rotate(rotation)
		})[0], 3, color, ImDrawFlags.RoundCornersAll, thickness);
	}

	public static void DrawMapTextDot(this ImDrawListPtr drawList, Vector2 pos, string str, uint fgcolor, uint bgcolor)
	{
		if (!string.IsNullOrWhiteSpace(str))
		{
			drawList.DrawText(pos, str, fgcolor, Plugin.Configuration.Overlay2D_TextStroke, centerAlignX: true, bgcolor);
		}
		drawList.AddCircleFilled(pos, Plugin.Configuration.Overlay2D_DotSize, fgcolor);
		if (Plugin.Configuration.Overlay2D_DotStroke != 0f)
		{
			drawList.AddCircle(pos, Plugin.Configuration.Overlay2D_DotSize, bgcolor, 0, Plugin.Configuration.Overlay2D_DotStroke);
		}
	}

	public static void DrawIcon(this ImDrawListPtr drawList, Vector2 pos, ISharedImmediateTexture icon, float size = 1f)
	{
		IDalamudTextureWrap textureWrap = icon.GetWrapOrDefault();
        if (textureWrap is null) return;
		_ = textureWrap.GetSize() * size;
		drawList.AddImage(textureWrap.ImGuiHandle, pos, pos);
	}
}
