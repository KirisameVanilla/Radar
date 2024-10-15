using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using ImGuiNET;
using Newtonsoft.Json;
using Radar.CustomObject;
using Radar.Enums;
using Radar.UI;
using SharpDX;

namespace Radar;

internal static class Util
{
    public static MyObjectKind GetMyObjectKind(IGameObject o)
    {
        MyObjectKind myObjectKind = (MyObjectKind)(o.ObjectKind + 2);
        switch (o.ObjectKind)
        {
            case ObjectKind.None:
            {
                myObjectKind = MyObjectKind.None;
                break;
            }
            case ObjectKind.BattleNpc:
            {
                switch (o.SubKind)
                {
                    case (byte)SubKind.Pet://宝石兽
                        myObjectKind = MyObjectKind.Pet;
                        break;
                    case (byte)SubKind.Chocobo:
                        myObjectKind = MyObjectKind.Chocobo;
                        break;
                    }
                break;
            }
        }

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
		Vector3.Transform(ref vector2, ref BuildUi.MatrixSingetonCache, out SharpDX.Vector4 result);
		Z = result.W;
		screenPos = new System.Numerics.Vector2(result.X / Z, result.Y / Z);
		screenPos.X = (0.5f * BuildUi.ViewPortSizeCache.X * (screenPos.X + 1f)) + vector.X;
		screenPos.Y = (0.5f * BuildUi.ViewPortSizeCache.Y * (1f - screenPos.Y)) + vector.Y;
		if (Z < 0f)
		{
			screenPos = -screenPos + ImGuiHelpers.MainViewport.Pos + ImGuiHelpers.MainViewport.Size;
		}
		if (screenPos.X > vector.X - trolanceX && screenPos.X < vector.X + BuildUi.ViewPortSizeCache.X + trolanceX && screenPos.Y > vector.Y - trolanceY)
		{
			return screenPos.Y < vector.Y + BuildUi.ViewPortSizeCache.Y + trolanceY;
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

	public static float Distance2D(this System.Numerics.Vector3 v, SharpDX.Vector3 v2)
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

	private static unsafe byte[] ReadTerminatedBytes(byte* ptr)
	{
		if (ptr == null)
		{
			return Array.Empty<byte>();
		}
		List<byte> list = new List<byte>();
		while (*ptr != 0)
		{
			list.Add(*ptr);
			ptr++;
		}
		return list.ToArray();
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

	public static string ToCompressedString<T>(this T obj)
	{
		return Compress(obj.ToJsonString());
	}

	public static T DecompressStringToObject<T>(this string compressedString)
	{
		return Decompress(compressedString).JsonStringToObject<T>();
	}

	public static string ToJsonString(this object obj)
	{
		return JsonConvert.SerializeObject(obj);
	}

	public static T JsonStringToObject<T>(this string str)
	{
		return JsonConvert.DeserializeObject<T>(str);
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
}
