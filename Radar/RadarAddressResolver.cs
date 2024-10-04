using System.Runtime.InteropServices;
using Dalamud.Game;

namespace Radar;

internal class RadarAddressResolver : BaseAddressResolver
{
	public nint CamPtr { get; private set; }

	public nint MapIdDungeon { get; private set; }

	public nint MapIdWorld { get; private set; }

	protected override void Setup64Bit(ISigScanner scanner)
	{
		CamPtr = Marshal.ReadIntPtr(scanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 45 33 C9 45 33 C0 33 D2 C6 40 09 01"));
		MapIdDungeon = scanner.GetStaticAddressFromSig("44 8B 35 ?? ?? ?? ?? 45 85 F6");
		MapIdWorld = scanner.GetStaticAddressFromSig("44 0F 44 3D ?? ?? ?? ??");
	}
}
