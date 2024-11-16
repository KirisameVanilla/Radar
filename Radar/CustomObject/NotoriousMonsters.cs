using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Radar.CustomObject;

internal static class NotoriousMonsters
{
	public static readonly HashSet<uint> ListEurekaMobs = new() { 7184u, 7567u, 7764u, 8131u };
    private static readonly ExcelSheet<NotoriousMonster> NotoriousMonsterSheet = Plugin.DataManager.GetExcelSheet<NotoriousMonster>();

    private static Lazy<HashSet<uint>> GetRankLazyHashSet(int rank)
    {
        Lazy<HashSet<uint>> rankLazyHashSet = new(() =>
            new(
                NotoriousMonsterSheet
                    .Where(i => i.Rank == rank && i.BNpcBase.Value.RowId != 0)
                    .Select(i => i.BNpcBase.Value.RowId)
                    .Distinct()
            )
        );
        return rankLazyHashSet;
    }

    public static readonly Lazy<HashSet<uint>> SRankLazy = GetRankLazyHashSet(3);
    public static readonly Lazy<HashSet<uint>> ARankLazy = GetRankLazyHashSet(2);
    public static readonly Lazy<HashSet<uint>> BRankLazy = GetRankLazyHashSet(1);

}
