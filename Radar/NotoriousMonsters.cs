using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.GeneratedSheets;

namespace Radar;

internal static class NotoriousMonsters
{
	public static readonly HashSet<uint> ListEMobs = new HashSet<uint> { 7184u, 7567u, 7764u, 8131u };

	public static readonly Lazy<HashSet<uint>> SRankLazy = new Lazy<HashSet<uint>>(() => new HashSet<uint>((from i in Plugin.data.GetExcelSheet<NotoriousMonster>()
		where i.Rank == 3
		select i.BNpcBase.Value.RowId into i
		where i != 0
		select i).Distinct()));

	public static readonly Lazy<HashSet<uint>> ARankLazy = new Lazy<HashSet<uint>>(() => new HashSet<uint>((from i in Plugin.data.GetExcelSheet<NotoriousMonster>()
		where i.Rank == 2
		select i.BNpcBase.Value.RowId into i
		where i != 0
		select i).Distinct()));

	public static readonly Lazy<HashSet<uint>> BRankLazy = new Lazy<HashSet<uint>>(() => new HashSet<uint>((from i in Plugin.data.GetExcelSheet<NotoriousMonster>()
		where i.Rank == 1
		select i.BNpcBase.Value.RowId into i
		where i != 0
		select i).Distinct()));
}
