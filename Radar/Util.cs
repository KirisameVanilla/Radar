using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Dalamud.Game;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using ImGuiNET;
using Newtonsoft.Json;
using Radar.UI;
using SharpDX;

namespace Radar;

internal static class Util
{
	/*
	public static System.Numerics.Vector2 Convert(this SharpDX.Vector2 v)
	{
		return new System.Numerics.Vector2(v.X, v.Y);
	}

	public static System.Numerics.Vector3 Convert(this SharpDX.Vector3 v)
	{
		return new System.Numerics.Vector3(v.X, v.Y, v.Z);
	}

	public static SharpDX.Vector2 Convert(this System.Numerics.Vector2 v)
	{
		return new SharpDX.Vector2(v.X, v.Y);
	}
	*/

	public static SharpDX.Vector3 Convert(this System.Numerics.Vector3 v)
	{
		return new SharpDX.Vector3(v.X, v.Y, v.Z);
	}

	public static bool WorldToScreenEx(System.Numerics.Vector3 worldPos, out System.Numerics.Vector2 screenPos, out float Z, System.Numerics.Vector2 pivot, float trolanceX = 0f, float trolanceY = 0f)
	{
		System.Numerics.Vector2 vector = pivot;
		SharpDX.Vector3 vector2 = worldPos.Convert();
		SharpDX.Vector3.Transform(ref vector2, ref BuildUi.MatrixSingetonCache, out SharpDX.Vector4 result);
		Z = result.W;
		screenPos = new System.Numerics.Vector2(result.X / Z, result.Y / Z);
		screenPos.X = 0.5f * BuildUi.ViewPortSizeCache.X * (screenPos.X + 1f) + vector.X;
		screenPos.Y = 0.5f * BuildUi.ViewPortSizeCache.Y * (1f - screenPos.Y) + vector.Y;
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

	/*
	public static bool WorldToScreenEx(System.Numerics.Vector3 worldPos, out System.Numerics.Vector3 screenPos)
	{
		System.Numerics.Vector2 screenPos2;
		float Z;
		bool result = WorldToScreenEx(worldPos, out screenPos2, out Z, ImGui.GetMainViewport().Pos);
		screenPos = new System.Numerics.Vector3(screenPos2.X, screenPos2.Y, Z);
		return result;
	}
	*/

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
	/*
	public static float Distance2D(this SharpDX.Vector3 v, System.Numerics.Vector3 v2)
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

	public static float Distance2D(this SharpDX.Vector3 v, SharpDX.Vector3 v2)
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

	internal static bool TryScanText(this SigScanner scanner, string sig, out nint result)
	{
		result = IntPtr.Zero;
		try
		{
			result = scanner.ScanText(sig);
			return true;
		}
		catch (KeyNotFoundException)
		{
			return false;
		}
	}
	*/

	private unsafe static byte[] ReadTerminatedBytes(byte* ptr)
	{
		if (ptr == null)
		{
			return new byte[0];
		}
		List<byte> list = new List<byte>();
		while (*ptr != 0)
		{
			list.Add(*ptr);
			ptr++;
		}
		return list.ToArray();
	}

	internal unsafe static string ReadTerminatedString(byte* ptr)
	{
		return Encoding.UTF8.GetString(ReadTerminatedBytes(ptr));
	}

	internal static bool ContainsIgnoreCase(this string haystack, string needle)
	{
		return CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase) >= 0;
	}

	public static uint SetAlpha(this uint color32, uint alpha)
	{
		return (color32 << 8 >> 8) + (alpha << 24);
	}

	public static uint Invert(this uint color32)
	{
		return (uint)(-1 - (int)(color32 << 8) >>> 8) + (color32 >> 24 << 24);
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

	public static System.Numerics.Vector2 RotationToNormalizedVector(float rotation)
	{
		return new System.Numerics.Vector2((float)Math.Sin(rotation), (float)Math.Cos(rotation));
	}

	public static System.Numerics.Vector2 Zoom(this System.Numerics.Vector2 vin, float zoom, System.Numerics.Vector2 origin)
	{
		return origin + (vin - origin) * zoom;
	}

	public static System.Numerics.Vector2 Rotate(this System.Numerics.Vector2 vin, float rotation, System.Numerics.Vector2 origin)
	{
		return origin + (vin - origin).Rotate(rotation);
	}

	public static System.Numerics.Vector2 Rotate(this System.Numerics.Vector2 vin, float rotation)
	{
		return vin.Rotate(new System.Numerics.Vector2((float)Math.Sin(rotation), (float)Math.Cos(rotation)));
	}

	public static System.Numerics.Vector2 Rotate(this System.Numerics.Vector2 vin, System.Numerics.Vector2 rotation, System.Numerics.Vector2 origin)
	{
		return origin + (vin - origin).Rotate(rotation);
	}

	public static System.Numerics.Vector2 Rotate(this System.Numerics.Vector2 vin, System.Numerics.Vector2 rotation)
	{
		rotation = rotation.Normalize();
		return new System.Numerics.Vector2(rotation.Y * vin.X + rotation.X * vin.Y, rotation.Y * vin.Y - rotation.X * vin.X);
	}

	public static float ToArc(this System.Numerics.Vector2 vin)
	{
		return (float)Math.Sin(vin.X);
	}

	public static void MassTranspose(System.Numerics.Vector2[] vin, System.Numerics.Vector2 pivot, System.Numerics.Vector2 rotation)
	{
		for (int i = 0; i < vin.Length; i++)
		{
			vin[i] = (vin[i] - pivot).Rotate(rotation) + pivot;
		}
	}

	public static void MassTranspose(System.Numerics.Vector2[] vin, System.Numerics.Vector2 pivot, float rotation)
	{
		for (int i = 0; i < vin.Length; i++)
		{
			vin[i] = (vin[i] - pivot).Rotate(rotation) + pivot;
		}
	}

	public static System.Numerics.Vector2 ToNormalizedVector2(this float rad)
	{
		return new System.Numerics.Vector2((float)Math.Sin(rad), (float)Math.Cos(rad));
	}

	public static System.Numerics.Vector3 ToVector3(this System.Numerics.Vector2 vin)
	{
		return new System.Numerics.Vector3(vin.X, 0f, vin.Y);
	}

	public static T[] ReadArray<T>(nint pointer, int length) where T : struct
	{
		int num = Marshal.SizeOf(typeof(T));
		T[] array = new T[length];
		for (int i = 0; i < length; i++)
		{
			nint ptr = new IntPtr(((IntPtr)pointer).ToInt64() + i * num);
			array[i] = Marshal.PtrToStructure<T>(ptr);
		}
		return array;
	}

	public unsafe static T[] ReadArrayUnmanaged<T>(nint pointer, int length) where T : unmanaged
	{
		T[] array = new T[length];
		for (int i = 0; i < length; i++)
		{
			array[i] = *(T*)(pointer + (nint)i * (nint)sizeof(T));
		}
		return array;
	}

	public static void Log(this object o)
	{
		Plugin.log.Information((o is nint intPtr) ? ((IntPtr)intPtr).ToInt64().ToString("X") : o.ToString());
	}

	public static void Log(this object o, string prefix)
	{
		Plugin.log.Information(prefix + ": " + ((o is nint intPtr) ? ((IntPtr)intPtr).ToInt64().ToString("X") : o.ToString()));
	}

	public static string GetRelative(this nint i)
	{
		return (((IntPtr)i).ToInt64() - ((IntPtr)Plugin.ImageBase).ToInt64()).ToString("X");
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

	public static byte[] GetSHA1(string s)
	{
		byte[] bytes = Encoding.Unicode.GetBytes(s);
		return SHA1.Create().ComputeHash(bytes);
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
