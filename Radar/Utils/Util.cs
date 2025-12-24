using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using static Radar.RadarEnum;

namespace Radar.Utils;

internal static class Util
{
    public static MyObjectKind GetMyObjectKind(this IGameObject o)
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

    public static bool WorldToScreenEx(Vector3 worldPos, out Vector2 screenPos, out float Z, Vector2? pivot = null, float toleranceX = 0f, float toleranceY = 0f)
    {
        /* 
         * Transform(vector3 vector, Matrix4x4 transform) =>
         * X vector.X * transform.M11 + vector.Y * transform.M21 + vector.Z * transform.M31 + transform.M41,
         * Y vector.X * transform.M12 + vector.Y * transform.M22 + vector.Z * transform.M32 + transform.M42,
         * Z vector.X * transform.M13 + vector.Y * transform.M23 + vector.Z * transform.M33 + transform.M43,
         * W vector.X * transform.M14 + vector.Y * transform.M24 + vector.Z * transform.M34 + transform.M44);
        */
        pivot ??= ImGui.GetMainViewport().Pos;
        Z = (worldPos.X * Radar.MatrixSingetonCache.M14) + 
            (worldPos.Y * Radar.MatrixSingetonCache.M24) + 
            (worldPos.Z * Radar.MatrixSingetonCache.M34) + 
            Radar.MatrixSingetonCache.M44;
        
        Plugin.Gui.WorldToScreen(worldPos, out screenPos);
        return screenPos.X > pivot.Value.X - toleranceX 
            && screenPos.X < pivot.Value.X + Radar.ViewPortSizeCache.X + toleranceX 
            && screenPos.Y > pivot.Value.Y - toleranceY 
            && screenPos.Y < pivot.Value.Y + Radar.ViewPortSizeCache.Y + toleranceY;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ToVector2(this Vector3 v) =>
        new(v.X, v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(this Vector3 v, Vector3 v2)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance2D(this Vector3 v, Vector3 v2)
    {
        try
        {
            return new Vector2(v.X - v2.X, v.Z - v2.Z).Length();
        }
        catch (Exception)
        {
            return 0f;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsZero(this float v) => Math.Abs(v) < 1E-06f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Normalize(this Vector2 v)
    {
        var num = v.Length();
        if (!IsZero(num))
        {
            var num2 = 1f / num;
            v.X *= num2;
            v.Y *= num2;
            return v;
        }
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Zoom(
        this Vector2 vin, float zoom, Vector2 origin) =>
        origin + ((vin - origin) * zoom);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Rotate(
        this Vector2 vin, float rotation, Vector2 origin) =>
        origin + (vin - origin).Rotate(rotation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Rotate(this Vector2 vin, float rotation) =>
        vin.Rotate(new Vector2((float)Math.Sin(rotation), (float)Math.Cos(rotation)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Rotate(this Vector2 vin, Vector2 rotation)
    {
        rotation = rotation.Normalize();
        return new Vector2((rotation.Y * vin.X) + (rotation.X * vin.Y), (rotation.Y * vin.Y) - (rotation.X * vin.X));
    }
    public static bool GetBorderClampedVector2(Vector2 screenPos, Vector2 clampSize, out Vector2 clampedPos)
    {
        var mainViewport = ImGuiHelpers.MainViewport;
        var center = mainViewport.GetCenter();
        var vector = mainViewport.Pos + clampSize;
        var vector2 = mainViewport.Pos + new Vector2(mainViewport.Size.X - clampSize.X, clampSize.Y);
        var vector3 = mainViewport.Pos + new Vector2(clampSize.X, mainViewport.Size.Y - clampSize.Y);
        var vector4 = mainViewport.Pos + mainViewport.Size - clampSize;
        if (FindIntersection(vector, vector2, center, screenPos, out var intersection))
        {
            clampedPos = intersection;
        }
        else if (FindIntersection(vector2, vector4, center, screenPos, out var intersection2))
        {
            clampedPos = intersection2;
        }
        else if (FindIntersection(vector4, vector3, center, screenPos, out var intersection3))
        {
            clampedPos = intersection3;
        }
        else
        {
            if (!FindIntersection(vector3, vector, center, screenPos, out var intersection4))
            {
                clampedPos = Vector2.Zero;
                return false;
            }
            clampedPos = intersection4;
        }
        return true;
    }

    private static bool FindIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        float num = p2.X - p1.X;
        float num2 = p2.Y - p1.Y;
        float num3 = p4.X - p3.X;
        float num4 = p4.Y - p3.Y;
        float num5 = (num2 * num3) - (num * num4);
        float num6 = (((p1.X - p3.X) * num4) + ((p3.Y - p1.Y) * num3)) / num5;
        if (float.IsInfinity(num6))
        {
            intersection = new Vector2(float.NaN, float.NaN);
            return false;
        }
        float num7 = (((p3.X - p1.X) * num2) + ((p1.Y - p3.Y) * num)) / (0f - num5);
        intersection = new Vector2(p1.X + (num * num6), p1.Y + (num2 * num6));
        return num6 >= 0f && num6 <= 1f && num7 >= 0f && num7 <= 1f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToCompressedString<T>(this T obj) => obj.ToJsonString().Compress();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToJsonString(this object obj) => JsonConvert.SerializeObject(obj);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T JsonStringToObject<T>(this string str) => JsonConvert.DeserializeObject<T>(str);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T DecompressStringToObject<T>(this string compressedString) =>
        compressedString.Decompress().JsonStringToObject<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Decompress(this string s)
    {
        using MemoryStream stream = new MemoryStream(System.Convert.FromBase64String(s));
        using MemoryStream memoryStream = new MemoryStream();
        using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress))
        {
            gZipStream.CopyTo(memoryStream);
        }
        return Encoding.Unicode.GetString(memoryStream.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Compress(this string s)
    {
        using MemoryStream memoryStream2 = new MemoryStream(Encoding.Unicode.GetBytes(s));
        using MemoryStream memoryStream = new MemoryStream();
        using (GZipStream destination = new GZipStream(memoryStream, CompressionLevel.Optimal))
        {
            memoryStream2.CopyTo(destination);
        }
        return System.Convert.ToBase64String(memoryStream.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTrap(this IGameObject obj)
    {
        return obj switch
        {
            { BaseId: 6388, Position: var p } when p != Vector3.Zero => true,
            { BaseId: >= 2007182 and <= 2007186 } => true,
            { BaseId: 2009504 } => true,
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAccursedHoard(this IGameObject obj) => obj.BaseId == 2007542 || obj.BaseId == 2007543;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSilverCoffer(this IGameObject obj) => obj.BaseId == 2007357;
}
