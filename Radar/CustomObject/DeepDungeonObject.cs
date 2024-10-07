using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using Radar.Enums;
using Radar.UI;

namespace Radar.CustomObject;

public record DeepDungeonObject
{
	public Vector3 Location { get; init; }

	public ushort Territory { get; init; }

	public uint Base { get; init; }

	public uint InstanceId { get; init; }

	public DeepDungeonType Type { get; init; }

	[JsonIgnore]
	internal Vector2 Location2D => new Vector2(Location.X, Location.Z);

	[JsonIgnore]
	internal BuildUi.DeepDungeonBg GetBg => BuildUi.GetDeepDungeonBg(Territory);

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
		builder.Append(Territory.ToString());
		builder.Append(", ");
		builder.Append("Base");
		builder.Append(" = ");
		builder.Append(Base.ToString());
		builder.Append(", ");
		builder.Append("InstanceId");
		builder.Append(" = ");
		builder.Append(InstanceId.ToString());
		builder.Append(", ");
		builder.Append("Type");
		builder.Append(" = ");
		builder.Append(Type.ToString());
		return true;
	}
}