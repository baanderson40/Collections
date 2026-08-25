using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace Collections;

public class GlassesCollectible: Collectible<Glasses>, ICreateable<GlassesCollectible, Glasses>
{
    public static string CollectionName => "Glasses";

    public GlassesCollectible(Glasses excelRow) : base(excelRow)
    {
    }

    public static GlassesCollectible Create(Glasses excelRow)
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
        // trying to use the underlying unlock item for glasses, as the unlock description is cooler.
        if(CollectibleKey != null)
            return ExcelCache<Item>.GetSheet().GetRow(CollectibleKey.Id)!.Value.Description.ToString();
        
        return ExcelRow.Description.ToString();
    }

    public override unsafe void UpdateObtainedState()
    {
        isObtained = Services.UnlockState.IsGlassesUnlocked(ExcelRow);
    }

    protected override int GetIconId()
    {
        return ExcelRow.Icon;
    }

    public override unsafe void Interact()
    {
        // Look into using Character->DrawData.SetGlasses(0, (ushort)glassesId);
        // Most likely need to handle this via PreviewExecutor to not clutter everything.
        // if (isObtained)
        //     ActionManager.Instance()->UseAction(ActionType.Unk12, ExcelRow.RowId);
    }

    public override void OpenGamerEscape()
    {
        WikiOpener.OpenGamerEscape(GetDisplayName() + "_(Facewear)");
    }
}
