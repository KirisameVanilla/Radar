using System;

namespace Radar;

internal static class Core
{
	public unsafe static GameObject* Me => (GameObject*)((IntPtr)Plugin.cs.LocalPlayer.Address).ToPointer();

	public unsafe static GameObject* Target => (GameObject*)((IntPtr)Plugin.targets.Target.Address).ToPointer();
}
