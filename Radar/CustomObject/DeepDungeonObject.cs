using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using static Radar.RadarEnum;

namespace Radar.CustomObject;

public record DeepDungeonObject
{
	public Vector3 Location { get; init; }

	public ushort Territory { get; init; }

	public uint Base { get; init; }

	public uint InstanceId { get; init; }

	public DeepDungeonType Type { get; init; }

	[JsonIgnore]
	internal Vector2 Location2D => new(Location.X, Location.Z);

	[JsonIgnore]
	internal DeepDungeonBg GetBg => Radar.GetDeepDungeonBg(Territory);

	public override string ToString()
	{
		return $"{Type}, {Territory}, {Base}, {InstanceId:X}, {Location}";
	}

	protected virtual bool PrintMembers(StringBuilder builder)
	{
		builder.Append("Location");
		builder.Append(" = ");
		builder.Append(Location.ToString());
		builder.Append(", ");
		builder.Append("Territory");
		builder.Append(" = ");
		builder.Append(Territory);
		builder.Append(", ");
		builder.Append("Base");
		builder.Append(" = ");
		builder.Append(Base);
		builder.Append(", ");
		builder.Append("InstanceId");
		builder.Append(" = ");
		builder.Append(InstanceId);
		builder.Append(", ");
		builder.Append("Type");
		builder.Append(" = ");
		builder.Append(Type.ToString());
		return true;
	}
}
