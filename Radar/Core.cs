using System;

namespace Radar;

internal static class Core
{
	public static unsafe GameObject* Me => (GameObject*)((IntPtr)Plugin.ClientState.LocalPlayer.Address).ToPointer();

	public static unsafe GameObject* Target => (GameObject*)((IntPtr)Plugin.TargetManager.Target.Address).ToPointer();
}
