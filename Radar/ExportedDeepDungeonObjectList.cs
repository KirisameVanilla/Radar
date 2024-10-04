using System.Collections.Generic;

namespace Radar;

public record ExportedDeepDungeonObjectList
{
	public HashSet<DeepDungeonObject> objectList;

	public byte[] sha1;
}
