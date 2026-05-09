using FFXIVClientStructs.FFXIV.Component.Excel;

namespace Collections;

public class GlamourCollectible : Collectible<Item>, ICreateable<GlamourCollectible, Item>
{
    public static string CollectionName => "Glamour";

    public GlamourCollectible(Item excelRow) : base(excelRow)
    {
        SortOptions.Add(new CollectibleSortOption("Dye Channels", (c) => c is GlamourCollectible ? ((GlamourCollectible)c).ExcelRow.DyeCount : -1, true));
        SortOptions.Add(new CollectibleSortOption("Level", (c) => c is GlamourCollectible ? ((GlamourCollectible)c).ExcelRow.LevelEquip : -1, true, (FontAwesomeIcon.SortNumericDownAlt, FontAwesomeIcon.SortNumericUpAlt)));
        SortOptions.Add(new CollectibleSortOption("Model", (c) => c is GlamourCollectible ? ((GlamourCollectible)c).ExcelRow.ModelMain : 0, false, null));
        FilterOptions.Insert(1, new CollectibleListFilterOption<int>(
            "Gender",
            (c) => {
                if(c.Item1 is GlamourCollectible)
                {
                    bool exclude = (c.Item1 as GlamourCollectible).ExcelRow.EquipRestriction.RowId > 1 &&
                    // Modulus of the equip restriction gives if it's exclusive to male or female
                    (c.Item1 as GlamourCollectible).ExcelRow.EquipRestriction.RowId % 2 == (c.Item2 < 100 ? c.Item2 % 2 : c.Item2 - 102);
                    // Flip result if going male/female only
                    if(c.Item2 > 100) exclude = !exclude;
                    return exclude;
                }
                return false;
            },
            // So that we don't have to deal with gender stuff
            [2,3,102,103],
            // TODO: localization
            ["Show All Genders", "Hide Male-Exclusive", "Hide Female-Exclusive", "Show Only Male-Exclusive", "Show Only Female-Exclusive"]
        ));
    }

    public static GlamourCollectible Create(Item excelRow)
    {
        return new(excelRow);
    }

    protected override string GetCollectionName()
    {
        return CollectionName;
    }

    protected override string GetName()
    {
        return ExcelRow.Name.ToString();
    }

    protected override uint GetId()
    {
        return ExcelRow.RowId;
    }

    protected override string GetDescription()
    {
        return "";
    }

    protected override HintModule GetSecondaryHint()
    {
        return new HintModule($"{ExcelRow.ClassJobCategory.Value.Name}, Lv. {ExcelRow.LevelEquip}", null);
    }

    public override void UpdateObtainedState()
    {
        isObtained = Services.ItemFinder.IsItemInInventory(ExcelRow.RowId)
                    || Services.ItemFinder.IsItemInArmoireCache(ExcelRow.RowId)
                    || Services.ItemFinder.IsItemInDresser(ExcelRow.RowId, true);
    }

    public override void DrawAdditionalTooltip()
    {
        base.DrawAdditionalTooltip();
        List<Item> sharedModels = ExcelCache<Item>.GetSheet().Where(c => c.ModelMain == ExcelRow.ModelMain && c.GetEquipSlot() == ExcelRow.GetEquipSlot() && c.RowId != ExcelRow.RowId).ToList();
        if(sharedModels.Count > 0)
        {
            ImGui.Text("Identical to: ");
            foreach(Item sharedModel in sharedModels)
            {
                ImGui.Image(IconHandler.GetIcon(sharedModel.Icon).GetWrapOrEmpty().Handle, new Vector2(1, 1) * UiHelper.ScaleForFontSize(40));
                ImGui.SameLine();
                ImGui.Text($"{sharedModel.Name} ({sharedModel.DyeCount} dye slots)");
            }
        }
    }    

    protected override int GetIconId()
    {
        return ExcelRow.Icon;
    }
    public int GetNumberOfDyeSlots()
    {
        return ExcelRow.DyeCount;
    }

    public override void Interact()
    {
        // Do nothing - taken care of by event service (should probably unify this in some way)
    }
}
