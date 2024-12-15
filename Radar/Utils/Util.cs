using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using ImGuiNET;
using Newtonsoft.Json;
using SharpDX;
using static Radar.RadarEnum;

namespace Radar.Utils;

internal static class Util
{
    public static MyObjectKind GetMyObjectKind(IGameObject o)
    {
        var myObjectKind = (MyObjectKind)(o.ObjectKind + 2);
        myObjectKind = o.ObjectKind switch
        {
            ObjectKind.None => MyObjectKind.None,
            ObjectKind.BattleNpc => o.SubKind switch
            {
                (byte)SubKind.Pet => MyObjectKind.Pet, // 宝石兽
                (byte)SubKind.Chocobo => MyObjectKind.Chocobo,
                _ => myObjectKind,
            },
            _ => myObjectKind
        };


        return myObjectKind;
    }

	public static Vector3 Convert(this System.Numerics.Vector3 v)
	{
		return new Vector3(v.X, v.Y, v.Z);
	}

	public static bool WorldToScreenEx(System.Numerics.Vector3 worldPos, out System.Numerics.Vector2 screenPos, out float Z, System.Numerics.Vector2 pivot, float trolanceX = 0f, float trolanceY = 0f)
	{
		System.Numerics.Vector2 vector = pivot;
		Vector3 vector2 = worldPos.Convert();
		Vector3.Transform(ref vector2, ref Radar.MatrixSingetonCache, out SharpDX.Vector4 result);
		Z = result.W;
		screenPos = new System.Numerics.Vector2(result.X / Z, result.Y / Z);
		screenPos.X = (0.5f * Radar.ViewPortSizeCache.X * (screenPos.X + 1f)) + vector.X;
		screenPos.Y = (0.5f * Radar.ViewPortSizeCache.Y * (1f - screenPos.Y)) + vector.Y;
		if (Z < 0f)
		{
			screenPos = -screenPos + ImGuiHelpers.MainViewport.Pos + ImGuiHelpers.MainViewport.Size;
		}
		if (screenPos.X > vector.X - trolanceX && screenPos.X < vector.X + Radar.ViewPortSizeCache.X + trolanceX && screenPos.Y > vector.Y - trolanceY)
		{
			return screenPos.Y < vector.Y + Radar.ViewPortSizeCache.Y + trolanceY;
		}
		return false;
	}

	public static bool WorldToScreenEx(System.Numerics.Vector3 worldPos, out System.Numerics.Vector2 screenPos, out float Z)
	{
		return WorldToScreenEx(worldPos, out screenPos, out Z, ImGui.GetMainViewport().Pos);
	}

	public static System.Numerics.Vector2 GetSize(this IDalamudTextureWrap textureWrap)
	{
		return new System.Numerics.Vector2(textureWrap.Width, textureWrap.Height);
	}

	public static System.Numerics.Vector2 ToVector2(this System.Numerics.Vector3 v)
	{
		return new System.Numerics.Vector2(v.X, v.Z);
	}

	public static float Distance(this System.Numerics.Vector3 v, System.Numerics.Vector3 v2)
	{
		try
		{
			return (v - v2).Length();
		}
		catch (Exception)
		{
			return 0f;
		}
	}

	public static float Distance2D(this System.Numerics.Vector3 v, System.Numerics.Vector3 v2)
	{
		try
		{
			return new System.Numerics.Vector2(v.X - v2.X, v.Z - v2.Z).Length();
		}
		catch (Exception)
		{
			return 0f;
		}
	}

	public static float Distance2D(this System.Numerics.Vector3 v, Vector3 v2)
	{
		try
		{
			return new System.Numerics.Vector2(v.X - v2.X, v.Z - v2.Z).Length();
		}
		catch (Exception)
		{
			return 0f;
		}
	}

	public static System.Numerics.Vector2 Normalize(this System.Numerics.Vector2 v)
	{
		float num = v.Length();
		if (!MathUtil.IsZero(num))
		{
			float num2 = 1f / num;
			v.X *= num2;
			v.Y *= num2;
			return v;
		}
		return v;
	}

	public static System.Numerics.Vector2 Zoom(this System.Numerics.Vector2 vin, float zoom, System.Numerics.Vector2 origin)
	{
		return origin + ((vin - origin) * zoom);
	}

	public static System.Numerics.Vector2 Rotate(this System.Numerics.Vector2 vin, float rotation, System.Numerics.Vector2 origin)
	{
		return origin + (vin - origin).Rotate(rotation);
	}

	public static System.Numerics.Vector2 Rotate(this System.Numerics.Vector2 vin, float rotation)
	{
		return vin.Rotate(new System.Numerics.Vector2((float)Math.Sin(rotation), (float)Math.Cos(rotation)));
	}

	public static System.Numerics.Vector2 Rotate(this System.Numerics.Vector2 vin, System.Numerics.Vector2 rotation)
	{
		rotation = rotation.Normalize();
		return new System.Numerics.Vector2((rotation.Y * vin.X) + (rotation.X * vin.Y), (rotation.Y * vin.Y) - (rotation.X * vin.X));
	}
    public static bool GetBorderClampedVector2(System.Numerics.Vector2 screenpos, System.Numerics.Vector2 clampSize, out System.Numerics.Vector2 clampedPos)
    {
        ImGuiViewportPtr mainViewport = ImGuiHelpers.MainViewport;
        System.Numerics.Vector2 center = mainViewport.GetCenter();
        System.Numerics.Vector2 vector = mainViewport.Pos + clampSize;
        System.Numerics.Vector2 vector2 = mainViewport.Pos + new System.Numerics.Vector2(mainViewport.Size.X - clampSize.X, clampSize.Y);
        System.Numerics.Vector2 vector3 = mainViewport.Pos + new System.Numerics.Vector2(clampSize.X, mainViewport.Size.Y - clampSize.Y);
        System.Numerics.Vector2 vector4 = mainViewport.Pos + mainViewport.Size - clampSize;
        FindIntersection(vector, vector2, center, screenpos, out var lines_intersect, out var segmentsIntersect, out var intersection, out var _closeP, out var _closeP2);
        FindIntersection(vector2, vector4, center, screenpos, out lines_intersect, out var segmentsIntersect2, out var intersection2, out _closeP2, out _closeP);
        FindIntersection(vector4, vector3, center, screenpos, out lines_intersect, out var segmentsIntersect3, out var intersection3, out _closeP, out _closeP2);
        FindIntersection(vector3, vector, center, screenpos, out lines_intersect, out var segmentsIntersect4, out var intersection4, out _closeP2, out _closeP);
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
                clampedPos = System.Numerics.Vector2.Zero;
                return false;
            }
            clampedPos = intersection4;
        }
        return true;
    }

    private static void FindIntersection(System.Numerics.Vector2 p1, System.Numerics.Vector2 p2, System.Numerics.Vector2 p3, System.Numerics.Vector2 p4, out bool lines_intersect, out bool segmentsIntersect, out System.Numerics.Vector2 intersection, out System.Numerics.Vector2 closeP1, out System.Numerics.Vector2 closeP2)
    {
        float num = p2.X - p1.X;
        float num2 = p2.Y - p1.Y;
        float num3 = p4.X - p3.X;
        float num4 = p4.Y - p3.Y;
        float num5 = (num2 * num3) - (num * num4);
        float num6 = (((p1.X - p3.X) * num4) + ((p3.Y - p1.Y) * num3)) / num5;
        if (float.IsInfinity(num6))
        {
            lines_intersect = false;
            segmentsIntersect = false;
            intersection = new System.Numerics.Vector2(float.NaN, float.NaN);
            closeP1 = new System.Numerics.Vector2(float.NaN, float.NaN);
            closeP2 = new System.Numerics.Vector2(float.NaN, float.NaN);
            return;
        }
        lines_intersect = true;
        float num7 = (((p3.X - p1.X) * num2) + ((p1.Y - p3.Y) * num)) / (0f - num5);
        intersection = new System.Numerics.Vector2(p1.X + (num * num6), p1.Y + (num2 * num6));
        segmentsIntersect = num6 >= 0f && num6 <= 1f && num7 >= 0f && num7 <= 1f;
        num6 = float.Clamp(num6, 0f, 1f);
        num7 = float.Clamp(num7, 0f, 1f);
        closeP1 = new System.Numerics.Vector2(p1.X + (num * num6), p1.Y + (num2 * num6));
        closeP2 = new System.Numerics.Vector2(p3.X + (num3 * num7), p3.Y + (num4 * num7));
    }

    public static string ToCompressedString<T>(this T obj)
    {
        return Compress(obj.ToJsonString());
    }

    public static string ToJsonString(this object obj)
    {
        return JsonConvert.SerializeObject(obj);
    }

    public static T JsonStringToObject<T>(this string str)
    {
        return JsonConvert.DeserializeObject<T>(str);
    }

    public static T DecompressStringToObject<T>(this string compressedString)
    {
        return Decompress(compressedString).JsonStringToObject<T>();
    }

    public static string Decompress(string s)
    {
        using MemoryStream stream = new MemoryStream(System.Convert.FromBase64String(s));
        using MemoryStream memoryStream = new MemoryStream();
        using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress))
        {
            gZipStream.CopyTo(memoryStream);
        }
        return Encoding.Unicode.GetString(memoryStream.ToArray());
    }

    public static string Compress(string s)
    {
        using MemoryStream memoryStream2 = new MemoryStream(Encoding.Unicode.GetBytes(s));
        using MemoryStream memoryStream = new MemoryStream();
        using (GZipStream destination = new GZipStream(memoryStream, CompressionLevel.Optimal))
        {
            memoryStream2.CopyTo(destination);
        }
        return System.Convert.ToBase64String(memoryStream.ToArray());
    }
}
