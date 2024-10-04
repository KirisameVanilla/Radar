using System.Numerics;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace Radar;

internal static class Vector2Intersect
{
	public static bool GetBorderClampedVector2(Vector2 screenpos, Vector2 clampSize, out Vector2 clampedPos)
	{
		ImGuiViewportPtr mainViewport = ImGuiHelpers.MainViewport;
		Vector2 center = mainViewport.GetCenter();
		Vector2 vector = mainViewport.Pos + clampSize;
		Vector2 vector2 = mainViewport.Pos + new Vector2(mainViewport.Size.X - clampSize.X, clampSize.Y);
		Vector2 vector3 = mainViewport.Pos + new Vector2(clampSize.X, mainViewport.Size.Y - clampSize.Y);
		Vector2 vector4 = mainViewport.Pos + mainViewport.Size - clampSize;
		FindIntersection(vector, vector2, center, screenpos, out var lines_intersect, out var segmentsIntersect, out var intersection, out var closeP, out var closeP2);
		FindIntersection(vector2, vector4, center, screenpos, out lines_intersect, out var segmentsIntersect2, out var intersection2, out closeP2, out closeP);
		FindIntersection(vector4, vector3, center, screenpos, out lines_intersect, out var segmentsIntersect3, out var intersection3, out closeP, out closeP2);
		FindIntersection(vector3, vector, center, screenpos, out lines_intersect, out var segmentsIntersect4, out var intersection4, out closeP2, out closeP);
		if (segmentsIntersect)
		{
			clampedPos = intersection;
		}
		else if (segmentsIntersect2)
		{
			clampedPos = intersection2;
		}
		else if (segmentsIntersect3)
		{
			clampedPos = intersection3;
		}
		else
		{
			if (!segmentsIntersect4)
			{
				clampedPos = Vector2.Zero;
				return false;
			}
			clampedPos = intersection4;
		}
		return true;
	}

	private static void FindIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out bool lines_intersect, out bool segmentsIntersect, out Vector2 intersection, out Vector2 closeP1, out Vector2 closeP2)
	{
		float num = p2.X - p1.X;
		float num2 = p2.Y - p1.Y;
		float num3 = p4.X - p3.X;
		float num4 = p4.Y - p3.Y;
		float num5 = num2 * num3 - num * num4;
		float num6 = ((p1.X - p3.X) * num4 + (p3.Y - p1.Y) * num3) / num5;
		if (float.IsInfinity(num6))
		{
			lines_intersect = false;
			segmentsIntersect = false;
			intersection = new Vector2(float.NaN, float.NaN);
			closeP1 = new Vector2(float.NaN, float.NaN);
			closeP2 = new Vector2(float.NaN, float.NaN);
			return;
		}
		lines_intersect = true;
		float num7 = ((p3.X - p1.X) * num2 + (p1.Y - p3.Y) * num) / (0f - num5);
		intersection = new Vector2(p1.X + num * num6, p1.Y + num2 * num6);
		segmentsIntersect = num6 >= 0f && num6 <= 1f && num7 >= 0f && num7 <= 1f;
		if (num6 < 0f)
		{
			num6 = 0f;
		}
		else if (num6 > 1f)
		{
			num6 = 1f;
		}
		if (num7 < 0f)
		{
			num7 = 0f;
		}
		else if (num7 > 1f)
		{
			num7 = 1f;
		}
		closeP1 = new Vector2(p1.X + num * num6, p1.Y + num2 * num6);
		closeP2 = new Vector2(p3.X + num3 * num7, p3.Y + num4 * num7);
	}
}
