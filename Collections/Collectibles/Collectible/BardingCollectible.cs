using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace Collections;

public class BardingCollectible : Collectible<BuddyEquip>, ICreateable<BardingCollectible, BuddyEquip>
{
    public static string CollectionName => "Bardings";

    public BardingCollectible(BuddyEquip excelRow) : base(excelRow)
    {
    }

    public static BardingCollectible Create(BuddyEquip excelRow)
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

    public override void UpdateObtainedState()
    {
        isObtained = Services.UnlockState.IsBuddyEquipUnlocked(ExcelRow);
    }

    protected override int GetIconId()
    {
        return ExcelRow.IconBody != 0 ? ExcelRow.IconBody : ExcelRow.IconHead;
    }

    public override void Interact()
    {
        // Do nothing
    }
}
