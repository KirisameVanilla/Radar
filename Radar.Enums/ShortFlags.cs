using System;

namespace Radar.Enums;

[Flags]
public enum ShortFlags : ushort
{
	f0 = 0,
	f1 = 2,
	f2 = 4,
	f3 = 8,
	f4 = 0x10,
	f5 = 0x20,
	f6 = 0x40,
	f7 = 0x80,
	f8 = 0x100,
	f9 = 0x200,
	f10 = 0x400,
	f11 = 0x800,
	f12 = 0x1000,
	f13 = 0x2000,
	f14 = 0x4000,
	f15 = 0x8000
}
