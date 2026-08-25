using FFXIVClientStructs.FFXIV.Client.Game;

namespace Collections;

public class MinionCollectible : Collectible<Companion>, ICreateable<MinionCollectible, Companion>
{
    public static string CollectionName => "Minions";

    public MinionCollectible(Companion excelRow) : base(excelRow)
    {
    }

    public static MinionCollectible Create(Companion excelRow)
    {
        return new(excelRow);
    }

    protected override string GetCollectionName()
    {
        return CollectionName;
    }

    protected override string GetName()
    {
        return ExcelRow.Singular.ToString();
    }

    protected override uint GetId()
    {
        return ExcelRow.RowId;
    }

    protected override string GetDescription()
    {
        return ExcelCache<CompanionTransient>.GetSheet().GetRow(ExcelRow.RowId)?.Description.ToString() ?? "";
    }

    public override void UpdateObtainedState()
    {
        isObtained = Services.UnlockState.IsCompanionUnlocked(ExcelRow);
    }

    protected override int GetIconId()
    {
        return ExcelRow.Icon;
    }

    private int GetImageId()
    {
        return 64000 + ExcelRow.Icon; 
    }

    public override unsafe void Interact()
    {
        if (isObtained)
        {
            // TODO: Fashion Accessories can prevent the use of actions
            ActionManager.Instance()->UseAction(ActionType.Companion, ExcelRow.RowId);
        }
    }

    public override string GetDisplayName()
    {
        return Name
                .UpperCaseAfterSpaces()
                .LowerCaseWords(new List<string>() { "Of", "Up" });
    }

    public override void DrawAdditionalTooltip()
    {
        var pic = Services.TextureProvider.GetFromGameIcon(new GameIconLookup((uint)GetImageId()));

        ImGui.Image(pic.GetWrapOrEmpty().Handle, pic.GetWrapOrEmpty().Size * UiHelper.ScaleForFontSize(.85f));
        ImGui.SameLine();
        ImGui.BeginGroup();
        if (ImGui.BeginTable($"##minion-{ExcelRow.RowId}-additional-tooltip", 1))
        {
            ImGui.TableSetupColumn("");
            ImGui.TableNextColumn();
            // Acts as a spacer
            ImGui.Text("");
            ImGui.Text($"{ExcelRow.Behavior.Value.Name.ToString()}, {ExcelRow.MinionRace.Value.Name.ToString()}");
            ImGui.Text("");
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35);
            ImGui.TextColored(ColorsPalette.GREY2, $"{ExcelCache<CompanionTransient>.GetSheet().GetRow(ExcelRow.RowId).GetValueOrDefault().DescriptionEnhanced}");
            ImGui.Text("");
            ImGui.Text($"{ExcelCache<CompanionTransient>.GetSheet().GetRow(ExcelRow.RowId).GetValueOrDefault().Tooltip}");
            ImGui.PopTextWrapPos();
            ImGui.EndTable();
        }
        ImGui.EndGroup();       
    }
}
