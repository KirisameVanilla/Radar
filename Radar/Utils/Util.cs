using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using ImGuiNET;
using Newtonsoft.Json;
using SharpDX;
using System;
using System.IO;
using System.IO.Compression;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Convert(this System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool WorldToScreenEx(
        System.Numerics.Vector3 worldPos, out System.Numerics.Vector2 screenPos, out float Z) =>
        WorldToScreenEx(worldPos, out screenPos, out Z, ImGui.GetMainViewport().Pos);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Numerics.Vector2 GetSize(this IDalamudTextureWrap textureWrap) =>
        new(textureWrap.Width, textureWrap.Height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Numerics.Vector2 ToVector2(this System.Numerics.Vector3 v) =>
        new(v.X, v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Numerics.Vector2 Zoom(
        this System.Numerics.Vector2 vin, float zoom, System.Numerics.Vector2 origin) =>
        origin + ((vin - origin) * zoom);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Numerics.Vector2 Rotate(
        this System.Numerics.Vector2 vin, float rotation, System.Numerics.Vector2 origin) =>
        origin + (vin - origin).Rotate(rotation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Numerics.Vector2 Rotate(this System.Numerics.Vector2 vin, float rotation) =>
        vin.Rotate(new System.Numerics.Vector2((float)Math.Sin(rotation), (float)Math.Cos(rotation)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Numerics.Vector2 Rotate(this System.Numerics.Vector2 vin, System.Numerics.Vector2 rotation)
    {
        rotation = rotation.Normalize();
        return new System.Numerics.Vector2((rotation.Y * vin.X) + (rotation.X * vin.Y), (rotation.Y * vin.Y) - (rotation.X * vin.X));
    }
    public static bool GetBorderClampedVector2(System.Numerics.Vector2 screenPos, System.Numerics.Vector2 clampSize, out System.Numerics.Vector2 clampedPos)
    {
        var mainViewport = ImGuiHelpers.MainViewport;
        var center = mainViewport.GetCenter();
        var vector = mainViewport.Pos + clampSize;
        var vector2 = mainViewport.Pos + new System.Numerics.Vector2(mainViewport.Size.X - clampSize.X, clampSize.Y);
        var vector3 = mainViewport.Pos + new System.Numerics.Vector2(clampSize.X, mainViewport.Size.Y - clampSize.Y);
        var vector4 = mainViewport.Pos + mainViewport.Size - clampSize;
        FindIntersection(vector, vector2, center, screenPos, out var lines_intersect, out var segmentsIntersect, out var intersection, out var _closeP, out var _closeP2);
        FindIntersection(vector2, vector4, center, screenPos, out lines_intersect, out var segmentsIntersect2, out var intersection2, out _closeP2, out _closeP);
        FindIntersection(vector4, vector3, center, screenPos, out lines_intersect, out var segmentsIntersect3, out var intersection3, out _closeP, out _closeP2);
        FindIntersection(vector3, vector, center, screenPos, out lines_intersect, out var segmentsIntersect4, out var intersection4, out _closeP2, out _closeP);
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

    private static void FindIntersection(
    System.Numerics.Vector2 line1Start, System.Numerics.Vector2 line1End,
    System.Numerics.Vector2 line2Start, System.Numerics.Vector2 line2End,
    out bool linesIntersect, out bool segmentsIntersect,
    out System.Numerics.Vector2 intersectionPoint,
    out System.Numerics.Vector2 closestLine1, out System.Numerics.Vector2 closestLine2)
    {
        // Calculate direction vectors for both line segments
        System.Numerics.Vector2 direction1 = line1End - line1Start;
        System.Numerics.Vector2 direction2 = line2End - line2Start;

        // Calculate the cross product of the direction vectors
        float crossProduct = direction1.Y * direction2.X - direction1.X * direction2.Y;

        // Handle parallel lines case
        const float epsilon = 1e-8f;
        if (MathF.Abs(crossProduct) < epsilon)
        {
            HandleParallelLines(out linesIntersect, out segmentsIntersect,
                out intersectionPoint, out closestLine1, out closestLine2);
            return;
        }

        // Calculate vectors between segment starts
        System.Numerics.Vector2 startDifference = line2Start - line1Start;

        // Calculate line intersection parameters
        float t = (startDifference.X * direction2.Y - startDifference.Y * direction2.X) / crossProduct;
        float u = (startDifference.X * direction1.Y - startDifference.Y * direction1.X) / crossProduct;

        // Determine intersection results
        linesIntersect = true;
        intersectionPoint = line1Start + t * direction1;
        segmentsIntersect = IsWithinSegmentRange(t) && IsWithinSegmentRange(u);

        // Calculate closest points on each segment
        t = Math.Clamp(t, 0f, 1f);
        u = Math.Clamp(u, 0f, 1f);

        closestLine1 = line1Start + t * direction1;
        closestLine2 = line2Start + u * direction2;
    }

    private static void HandleParallelLines(out bool linesIntersect, out bool segmentsIntersect,
        out System.Numerics.Vector2 intersectionPoint, out System.Numerics.Vector2 closestLine1, out System.Numerics.Vector2 closestLine2)
    {
        linesIntersect = false;
        segmentsIntersect = false;
        intersectionPoint = new System.Numerics.Vector2(float.NaN, float.NaN);
        closestLine1 = intersectionPoint;
        closestLine2 = intersectionPoint;
    }

    private static bool IsWithinSegmentRange(float value) => value >= 0f && value <= 1f;

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
            { DataId: 6388, Position: var p } when p != System.Numerics.Vector3.Zero => true,
            { DataId: >= 2007182 and <= 2007186 } => true,
            { DataId: 2009504 } => true,
            _ => false
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAccursedHoard(this IGameObject obj) => obj.DataId == 2007542 || obj.DataId == 2007543;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSilverCoffer(this IGameObject obj) => obj.DataId == 2007357;
}
