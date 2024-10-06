using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;

namespace Radar.UI;

internal static class AreaMap
{
	public static unsafe AtkUnitBase* AreaMapAddon => (AtkUnitBase*)Plugin.Gui.GetAddonByName("AreaMap");

	public static unsafe bool HasMap => AreaMapAddon != (AtkUnitBase*)IntPtr.Zero;

	public static unsafe bool MapVisible
	{
		get
		{
			if (HasMap)
			{
				return AreaMapAddon->IsVisible;
			}
			return false;
		}
	}

	public static unsafe Vector2 WindowPos => ImGuiHelpers.MainViewport.Pos + new Vector2(AreaMapAddon->X, AreaMapAddon->Y);

	public static unsafe ref float MapScale => ref *(float*)((byte*)AreaMapAddon + 972);

	public static unsafe ref float MapNodeRotation
	{
		get
		{
			AtkComponentNode* ptr = (AtkComponentNode*)AreaMapAddon->UldManager.NodeList[3];
			AtkImageNode* ptr2 = (AtkImageNode*)ptr->Component->UldManager.NodeList[3];
			return ref ptr->AtkResNode.Rotation;
		}
	}

	public static unsafe ref float MapTextureRotation
	{
		get
		{
			if (!HasMap)
			{
				throw null;
			}
			AtkComponentNode* ptr = (AtkComponentNode*)AreaMapAddon->UldManager.NodeList[3];
			AtkImageNode* ptr2 = (AtkImageNode*)ptr->Component->UldManager.NodeList[3];
			return ref ptr2->AtkResNode.Rotation;
		}
	}

	public static unsafe string GetNaviMapTexPath
	{
		get
		{
			string result = null;
			try
			{
				AtkUnitBase* ptr = (AtkUnitBase*)Plugin.Gui.GetAddonByName("_NaviMap");
				AtkComponentNode* ptr2 = (AtkComponentNode*)ptr->UldManager.NodeList[2];
				AtkImageNode* ptr3 = (AtkImageNode*)ptr2->Component->UldManager.NodeList[1];
				if (ptr3->PartsList != null)
				{
					if (ptr3->PartId <= ptr3->PartsList->PartCount)
					{
						AtkUldAsset* uldAsset = ptr3->PartsList->Parts[(int)ptr3->PartId].UldAsset;
						if (uldAsset->AtkTexture.TextureType == TextureType.Resource)
						{
							StdString fileName = uldAsset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName;
							result = fileName.ToString();
							return result;
						}
						return result;
					}
					return result;
				}
				return result;
			}
			catch
			{
				return result;
			}
		}
	}

	public static unsafe ref uint MapNodeFlags
	{
		get
		{
			if (!HasMap)
			{
				throw null;
			}
			AtkComponentNode* ptr = (AtkComponentNode*)AreaMapAddon->UldManager.NodeList[3];
			AtkImageNode* ptr2 = (AtkImageNode*)ptr->Component->UldManager.NodeList[3];
			return ref ptr->AtkResNode.DrawFlags;
		}
	}
}
