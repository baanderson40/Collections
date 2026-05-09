using Lumina.Extensions;

namespace Collections;

// So, all Adventure Plate and Portrait elements have an associated BannerCondition, and MOST have a BannerBg background associated with them.
// But not ALL of them have that link, and we can't garuntee that in the future either. So BannerCondition it is
public class FramerKitCollectible : Collectible<BannerCondition>, ICreateable<FramerKitCollectible, BannerCondition>
{
    public static string CollectionName => "Framer Kits";

    // Storing row IDs of relevant collections
    private uint PortraitBackground = 0;
    private uint PortraitFrame = 0;
    private uint PortraitAccent = 0;
    private uint PlateBackground = 0;
    private uint PlateBanner = 0;
    private uint PlateFrame = 0;
    private uint PlateBacking = 0;
    private uint PlatePortraitFrame = 0;
    private uint PlateOverlay = 0;
    private uint PlateAccent = 0;

    // Some notes:
    // BannerCondition UnlockType2 links to GameRewardObtainType rowId.
    // In GameRewardObtainType, Unknown0 is the IconId, Unknown1 is the Addon row ID
    // Addon row ID references the fields after UnlockType2 to populate it's strings.
    // This can be used to auto-populate minion, framer kit, and emote unlock data

    public FramerKitCollectible(BannerCondition excelRow) : base(excelRow)
    {
        // Yes, this is still as performant as storing the RowRefs.
        var bg = ExcelCache<BannerBg>.GetSheet().Where(row => row.UnlockCondition.RowId == excelRow.RowId).ToList().FirstOrNull();
        
        PortraitBackground = bg?.RowId ?? 0;
        Name = bg?.Name.ToString() ?? "";
        PortraitFrame = ExcelCache<BannerFrame>.GetSheet().Where(row => row.UnlockCondition.RowId == excelRow.RowId).ToList().FirstOrNull()?.RowId ?? 0;
        PortraitAccent = ExcelCache<BannerDecoration>.GetSheet().Where(row => row.UnlockCondition.RowId == excelRow.RowId).ToList().FirstOrNull()?.RowId ?? 0;
        // There can be multiple backgrounds from BannerCondition, but we'll only care about 1
        PlateBackground = ExcelCache<CharaCardBase>.GetSheet().Where(row => row.UnlockCondition.RowId == excelRow.RowId).ToList().FirstOrNull()?.RowId ?? 0;
        PlateBanner = ExcelCache<CharaCardHeader>.GetSheet().Where(row => row.UnlockCondition.RowId == excelRow.RowId).ToList().FirstOrNull()?.RowId ?? 0;
        // multiple items are stored here, differentiate by Component Field
        var misc = ExcelCache<CharaCardDecoration>.GetSheet().Where(row => row.UnlockCondition.RowId == excelRow.RowId).ToList();
        foreach(var decal in misc)
        {
            switch(decal.Component)
            {
                case 1:
                    PlateBacking = decal.RowId;
                    break;
                case 2:
                    PlateOverlay = decal.RowId;
                    break;
                case 3:
                    PlatePortraitFrame = decal.RowId;
                    break;
                case 4:
                    PlateFrame = decal.RowId;
                    if (Name == "") Name = decal.Name.ToString();
                    break;
                case 5:
                    PlateAccent = decal.RowId;
                    break;
            }
        }
    }

    public static FramerKitCollectible Create(BannerCondition excelRow)
    {
        return new(excelRow);
    }

    protected override string GetCollectionName()
    {
        return CollectionName;
    }

    protected override string GetName()
    {
        return Name; 
    }

    public override string GetDisplayName()
    {
        return GetName();
    }

    protected override uint GetId()
    {
        return ExcelRow.RowId;
    }

    protected override string GetDescription()
    {
        return "";
    }

    public override unsafe void UpdateObtainedState()
    {
        // uncomment if this actually works
        // isObtained = Services.UnlockState.IsBannerConditionUnlocked(ExcelRow);
        
        if (ExcelRow.UnlockType1 == 9)
        {
            isObtained = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState.Instance()->IsFramersKitUnlocked(ExcelRow.UnlockCriteria1.First().RowId);
        }
        // items unlocked from quest
        else if (ExcelRow.UnlockType1 == 1)
        {
            isObtained = Services.UnlockState.IsQuestCompleted(ExcelCache<Quest>.GetSheet().GetRow(ExcelRow.UnlockCriteria1.First().RowId).GetValueOrDefault());
        }
        // unlocked from completing ultimate
        else if (ExcelRow.UnlockType1 == 4)
        {
            Achievement? a = ExcelCache<Achievement>.GetSheet().Where(a => a.Key.RowId == ExcelRow.Prerequisite.RowId).FirstOrNull();
            if (a != null)
                isObtained = Services.UnlockState.IsAchievementComplete(a.Value);
        }
        // PvP
        else if (ExcelRow.UnlockType1 == 11)
        {
            // bleh, but there's not really a better way atm.
            // Find the "starter" CC 
            var unlock = ExcelCache<BannerCondition>.GetSheet().Last(row => row.UnlockType1 == 9 && row.UnlockType2 == 11 && row.RowId < ExcelRow.RowId);
            var unlockId = unlock.UnlockCriteria1.First().RowId + (ExcelRow.RowId - unlock.RowId);
            if (unlockId >= 0) isObtained = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState.Instance()->IsFramersKitUnlocked(unlockId);
        }
    }

    protected override decimal GetPatchAdded()
    {
        decimal guess = base.GetPatchAdded();
        // all framer kits that were added in 6.1 (not in patch data)
        if((guess >= 999 || guess <= 2) && ExcelRow.RowId <= 347) return new decimal(6.1);
        return guess;
    }

    protected override int GetIconId()
    {
        return 026625; // Framer Kit Item Icon
    }

    public override unsafe void Interact()
    {
        // Do nothing
    }

    public override void DrawAdditionalTooltip()
    {
        // Create Portrait Preview
        var background = ExcelCache<BannerBg>.GetSheet().GetRow(PortraitBackground).GetValueOrDefault();
        var tex = GetFrameElementIcon(background.Image);
        var cursor = ImGui.GetCursorPos();
        float portraitScale = UiHelper.ScaleForFontSize(.5f);
        ImGui.Image(tex.GetWrapOrEmpty().Handle, tex.GetWrapOrEmpty().Size * portraitScale);

        if (PortraitFrame != 0)
        {
            ImGui.SameLine();
            ImGui.SetCursorPos(cursor);
            ImGui.SetItemAllowOverlap();
            var frame = ExcelCache<BannerFrame>.GetSheet().GetRow(PortraitFrame).GetValueOrDefault();
            tex = GetFrameElementIcon(frame.Image);
            ImGui.Image(tex.GetWrapOrEmpty().Handle, tex.GetWrapOrEmpty().Size * portraitScale);
        }
        if(PortraitAccent != 0)
        {
            ImGui.SameLine();
            ImGui.SetCursorPos(cursor);
            ImGui.SetItemAllowOverlap();
            var accent = ExcelCache<BannerDecoration>.GetSheet().GetRow(PortraitAccent).GetValueOrDefault();
            tex = GetFrameElementIcon(accent.Image);
            ImGui.Image(tex.GetWrapOrEmpty().Handle, tex.GetWrapOrEmpty().Size * portraitScale);
        }
        // see if we can stop here
        if (PlateBackground + PlateBanner + PlateFrame + PlateBacking + PlatePortraitFrame + PlateAccent == 0) return;
        // Create plate preview, 
        float plateScale = UiHelper.ScaleForFontSize(0.3f);

        // Backing
        ImGui.SameLine();
        cursor = ImGui.GetCursorPos();
        // Using shadow backing helps accent this and also scale everything properly
        var backing = ExcelCache<CharaCardDecoration>.GetSheet().GetRow(PlateBacking != 0 ? PlateBacking : 11).GetValueOrDefault();
        tex = GetFrameElementIcon(backing.Image);
        var fullSize = tex.GetWrapOrEmpty().Size;
        ImGui.Image(tex.GetWrapOrEmpty().Handle, fullSize * plateScale);

        // Background
        ImGui.SameLine();
        ImGui.SetItemAllowOverlap();
        var pBackground = ExcelCache<CharaCardBase>.GetSheet().GetRow(PlateBackground).GetValueOrDefault();
        tex = GetFrameElementIcon(pBackground.Image);
        ImGui.SetCursorPos(cursor + new Vector2((fullSize.X - tex.GetWrapOrEmpty().Size.X) * 0.5f, (fullSize.Y - tex.GetWrapOrEmpty().Size.Y) * 0.5f) * plateScale);
        ImGui.Image(tex.GetWrapOrEmpty().Handle, tex.GetWrapOrEmpty().Size * plateScale);

        // Pattern Overlay
        ImGui.SameLine();
        ImGui.SetItemAllowOverlap();
        var pOverlay = ExcelCache<CharaCardDecoration>.GetSheet().GetRow(PlateOverlay).GetValueOrDefault();
        tex = GetFrameElementIcon(pOverlay.Image);
        ImGui.SetCursorPos(cursor + new Vector2((fullSize.X - tex.GetWrapOrEmpty().Size.X) * 0.5f, (fullSize.Y - tex.GetWrapOrEmpty().Size.Y) * 0.5f) * plateScale);
        ImGui.Image(tex.GetWrapOrEmpty().Handle, tex.GetWrapOrEmpty().Size * plateScale);

        // Plate Frame
        ImGui.SameLine();
        ImGui.SetItemAllowOverlap();
        var pFrame = ExcelCache<CharaCardDecoration>.GetSheet().GetRow(PlateFrame).GetValueOrDefault();
        tex = GetFrameElementIcon(pFrame.Image);
        ImGui.SetCursorPos(cursor + new Vector2((fullSize.X - tex.GetWrapOrEmpty().Size.X) * 0.5f, (fullSize.Y - tex.GetWrapOrEmpty().Size.Y) * 0.5f - 28) * plateScale);
        ImGui.Image(tex.GetWrapOrEmpty().Handle, tex.GetWrapOrEmpty().Size * plateScale);

        // Portrait Frame
        ImGui.SameLine();
        ImGui.SetItemAllowOverlap();
        var pPortrait = ExcelCache<CharaCardDecoration>.GetSheet().GetRow(PlatePortraitFrame).GetValueOrDefault();
        tex = GetFrameElementIcon(pPortrait.Image);
        ImGui.SetCursorPos(cursor + new Vector2((fullSize.X - tex.GetWrapOrEmpty().Size.X) * 0.75f, (fullSize.Y - tex.GetWrapOrEmpty().Size.Y) * 0.5f) * plateScale);
        ImGui.Image(tex.GetWrapOrEmpty().Handle, tex.GetWrapOrEmpty().Size * plateScale);

        // Top Border
        ImGui.SameLine();
        ImGui.SetItemAllowOverlap();
        var pBar = ExcelCache<CharaCardHeader>.GetSheet().GetRow(PlateBanner).GetValueOrDefault();
        tex = GetFrameElementIcon(pBar.TopImage);
        ImGui.SetCursorPos(cursor + new Vector2((fullSize.X - tex.GetWrapOrEmpty().Size.X) * 0.5f, (fullSize.Y - tex.GetWrapOrEmpty().Size.Y) * 0.145f) * plateScale);
        ImGui.Image(tex.GetWrapOrEmpty().Handle, tex.GetWrapOrEmpty().Size * plateScale);

        // Bottom Border
        ImGui.SameLine();
        ImGui.SetItemAllowOverlap();
        tex = GetFrameElementIcon(pBar.BottomImage);
        ImGui.SetCursorPos(cursor + new Vector2((fullSize.X - tex.GetWrapOrEmpty().Size.X) * 0.5f, (fullSize.Y - tex.GetWrapOrEmpty().Size.Y) * 0.81f) * plateScale);
        ImGui.Image(tex.GetWrapOrEmpty().Handle, tex.GetWrapOrEmpty().Size * plateScale);

        // Accent
        ImGui.SameLine();
        ImGui.SetItemAllowOverlap();
        var pAccent = ExcelCache<CharaCardDecoration>.GetSheet().GetRow(PlateAccent).GetValueOrDefault();
        tex = GetFrameElementIcon(pAccent.Image);
        ImGui.SetCursorPos(cursor + new Vector2((fullSize.X - tex.GetWrapOrEmpty().Size.X) * 0.83f, (fullSize.Y - tex.GetWrapOrEmpty().Size.Y) * 0.82f) * plateScale);
        ImGui.Image(tex.GetWrapOrEmpty().Handle, tex.GetWrapOrEmpty().Size * plateScale);
    }

    private ISharedImmediateTexture GetFrameElementIcon(int iconId)
    {
        GameIconLookup lookup = new GameIconLookup(iconId: (uint)iconId, itemHq: false);
        return Services.TextureProvider.GetFromGameIcon(lookup);
    }
}