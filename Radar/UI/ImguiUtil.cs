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
		var overlay3D_RingType = (int)Plugin.config.Overlay3D_RingType;
		var overlay3D_RingSize = Plugin.config.Overlay3D_RingSize;
		var overlay3D_IconStrokeThickness = Plugin.config.Overlay3D_IconStrokeThickness;
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

	/*
	internal static bool ComboEnum<T>(this T eEnum, string label) where T : Enum
	{
		bool result = false;
		Type? typeFromHandle = typeof(T);
		string[] names = Enum.GetNames(typeFromHandle);
		Array values = Enum.GetValues(typeFromHandle);
		ImGui.BeginCombo(label, eEnum.ToString());
		for (int i = 0; i < names.Length; i++)
		{
			if (ImGui.Selectable(names[i] + "##" + label))
			{
				eEnum = (T)values.GetValue(i);
				result = true;
			}
		}
		ImGui.EndCombo();
		return result;
	}
	*/

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

	/*
	public static void DrawTextWithBg(this ImDrawListPtr drawList, Vector2 pos, string text, uint col = uint.MaxValue, uint bgcol = 4278190080u, bool centerAlignX = true)
	{
		Vector2 vector = ImGui.CalcTextSize(text) + new Vector2(ImGui.GetStyle().ItemSpacing.X, 0f);
		if (centerAlignX)
		{
			pos -= new Vector2(vector.X, 0f) / 2f;
		}
		drawList.AddRectFilled(pos, pos + vector, bgcol);
		drawList.AddText(pos + new Vector2(ImGui.GetStyle().ItemSpacing.X / 2f, 0f), col, text);
	}
	*/

	public static void DrawTextWithBorderBg(this ImDrawListPtr drawList, Vector2 pos, string text, uint col = uint.MaxValue, uint bgcol = 4278190080u, bool centerAlignX = true)
	{
		Vector2 vector = ImGui.CalcTextSize(text) + new Vector2(ImGui.GetStyle().ItemSpacing.X, 0f);
		if (centerAlignX)
		{
			pos -= new Vector2(vector.X, 0f) / 2f;
		}
		drawList.AddRectFilled(pos, pos + vector, bgcol, Plugin.config.Overlay3D_NamePlateRound);
		drawList.AddRect(pos, pos + vector, col, Plugin.config.Overlay3D_NamePlateRound);
		drawList.AddText(pos + new Vector2(ImGui.GetStyle().ItemSpacing.X / 2f + 0.5f, -0.5f), col, text);
	}

	/*
	public static void DrawArrow(this ImDrawListPtr drawList, Vector2 pos, float size, uint color, uint bgcolor, float rotation, float thickness, float outlinethickness)
	{
		drawList.AddPolyline(ref (new Vector2[3]
		{
			pos + new Vector2(0f - size - outlinethickness / 2f, -0.5f * size - outlinethickness / 2f).Rotate(rotation),
			pos + new Vector2(0f, 0.5f * size).Rotate(rotation),
			pos + new Vector2(size + outlinethickness / 2f, -0.5f * size - outlinethickness / 2f).Rotate(rotation)
		})[0], 3, bgcolor, ImDrawFlags.RoundCornersAll, thickness + outlinethickness);
		drawList.DrawArrow(pos, size, color, rotation, thickness);
	}

	public static void DrawArrow(this ImDrawListPtr drawList, Vector2 pos, float size, uint color, float rotation, float thickness)
	{
		drawList.AddPolyline(ref (new Vector2[3]
		{
			pos + new Vector2(0f - size, -0.5f * size).Rotate(rotation),
			pos + new Vector2(0f, 0.5f * size).Rotate(rotation),
			pos + new Vector2(size, -0.5f * size).Rotate(rotation)
		})[0], 3, color, ImDrawFlags.RoundCornersAll, thickness);
	}
	*/

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

	/*
	public static void DrawTrangle(this ImDrawListPtr drawList, Vector2 pos, float size, uint color, Vector2 rotation, bool filled = true)
	{
		Vector2[] array = GettriV(pos, size, rotation);
		if (filled)
		{
			drawList.AddTriangleFilled(array[0], array[1], array[2], color);
		}
		else
		{
			drawList.AddTriangle(array[0], array[1], array[2], color);
		}
		static Vector2[] GettriV(Vector2 vin, float s, Vector2 rotation)
		{
			rotation = rotation.Normalize();
			Vector2 vin2 = new Vector2(0f, s * 1.7320508f - s * (2f / 3f));
			Vector2 vin3 = new Vector2((0f - s) * 0.8f, (0f - s) * (2f / 3f));
			Vector2 vin4 = new Vector2(s * 0.8f, (0f - s) * (2f / 3f));
			vin2 = vin + vin2.Rotate(rotation);
			vin3 = vin + vin3.Rotate(rotation);
			vin4 = vin + vin4.Rotate(rotation);
			return new Vector2[3] { vin2, vin3, vin4 };
		}
	}

	public static void DrawMapDot(this ImDrawListPtr drawList, Vector2 pos, uint col)
	{
		uint colorU = ImGui.GetColorU32(Plugin.config.Overlay2D_StrokeColor);
		drawList.AddCircleFilled(pos, Plugin.config.Overlay2D_DotSize, col);
		if (Plugin.config.Overlay2D_DotStroke != 0f)
		{
			drawList.AddCircle(pos, Plugin.config.Overlay2D_DotSize, colorU, 0, Plugin.config.Overlay2D_DotStroke);
		}
	}
	*/

	public static void DrawMapTextDot(this ImDrawListPtr drawList, Vector2 pos, string str, uint fgcolor, uint bgcolor)
	{
		if (!string.IsNullOrWhiteSpace(str))
		{
			drawList.DrawText(pos, str, fgcolor, Plugin.config.Overlay2D_TextStroke, centerAlignX: true, bgcolor);
		}
		drawList.AddCircleFilled(pos, Plugin.config.Overlay2D_DotSize, fgcolor);
		if (Plugin.config.Overlay2D_DotStroke != 0f)
		{
			drawList.AddCircle(pos, Plugin.config.Overlay2D_DotSize, bgcolor, 0, Plugin.config.Overlay2D_DotStroke);
		}
	}

	public static void DrawIcon(this ImDrawListPtr drawlist, Vector2 pos, ISharedImmediateTexture icon, float size = 1f)
	{
		IDalamudTextureWrap textureWrap = icon.GetWrapOrDefault();
		_ = textureWrap.GetSize() * size;
		drawlist.AddImage(textureWrap.ImGuiHandle, pos, pos);
	}
}
