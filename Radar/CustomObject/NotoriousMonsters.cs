using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Radar.CustomObject;

internal static class NotoriousMonsters
{
    public static readonly HashSet<uint> ListEurekaMobs = new() { 7184u, 7567u, 7764u, 8131u };
    public static readonly HashSet<uint> ListFateMobs = new HashSet<uint> { 882U, 733U, 7422U, 7415U, 10573U, 10157U, 13677U, 13515U, 16863U, 17387U };
    private static readonly ExcelSheet<NotoriousMonster> NotoriousMonsterSheet = Plugin.DataManager.GetExcelSheet<NotoriousMonster>();

    private static Lazy<HashSet<uint>> GetRankLazyHashSet(int rank) 
        => new(() 
            =>new(NotoriousMonsterSheet
                .Where(i => i.Rank == rank && i.BNpcBase.Value.RowId != 0)
                .Select(i => i.BNpcBase.Value.RowId)
                .Distinct()));

    public static readonly Lazy<HashSet<uint>> SRankLazy = GetRankLazyHashSet(3);
    public static readonly Lazy<HashSet<uint>> ARankLazy = GetRankLazyHashSet(2);
    public static readonly Lazy<HashSet<uint>> BRankLazy = GetRankLazyHashSet(1);
}
