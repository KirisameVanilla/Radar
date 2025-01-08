using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace Radar.Utils;

internal static class AreaMap
{
    public static unsafe AtkUnitBase* AreaMapAddon => (AtkUnitBase*)Plugin.Gui.GetAddonByName("AreaMap");

    public static unsafe bool HasMap => AreaMapAddon != (AtkUnitBase*)IntPtr.Zero;

    public static unsafe bool MapVisible => HasMap && AreaMapAddon->IsVisible;

    public static unsafe ref float MapScale => ref *(float*)((byte*)AreaMapAddon + 980);
}
