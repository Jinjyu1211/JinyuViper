using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using A = ApiHelper.ApiHelper;

namespace Jinyu;

internal static class PotionCatalog
{
    private static List<(uint Id, string Name, uint Level)>? cache;
    public static IReadOnlyList<(uint Id, string Name, uint Level)> Items => cache ??= Svc.Data.GetExcelSheet<Item>()
        .Where(x => x.Name.ToString().Contains("巧力") && x.ItemAction.RowId != 0)
        .Select(x => (x.RowId, x.Name.ToString(), x.LevelItem.RowId)).OrderByDescending(x => x.Item3).ToList();
    public static uint SelectedId => JinyuSettings.Current.PotionId != 0 ? JinyuSettings.Current.PotionId
        : Items.FirstOrDefault(x => A.有物品(x.Id, true) || A.有物品(x.Id)).Id;
    public static string SelectedName => Items.FirstOrDefault(x => x.Id == SelectedId).Name ?? "背包无可用巧力药";
}
