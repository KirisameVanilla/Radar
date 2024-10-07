using System.Numerics;

namespace Radar.CustomObject;

public record CustomObjectValue
{
	public bool Enabled { get; set; }

	public Vector4 Color { get; set; }
}
