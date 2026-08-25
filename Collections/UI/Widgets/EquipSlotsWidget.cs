using System.IO;

namespace Collections;

public class EquipSlotsWidget
{
    public EquipSlot activeEquipSlot { get; set; } = EquipSlot.Body;
    public Dictionary<EquipSlot, PaletteWidget> paletteWidgets = new();

    private Vector2 activeEquipSlotRectSize = new(50.2f, 50.2f);
    private Vector2 equipSlotBackgroundRectSize = new(46, 46);
    private Vector2 paletteWidgetButtonOffset = new(-25, 34);
    private Vector4 paletteWidgetButtonDefaultColor = ColorsPalette.WHITE;
    private Vector2 brushIconRectSize = new(4, 4);

    public GlamourSet currentGlamourSet { get; set; } = null!;
    private Dictionary<EquipSlot, bool> hoveredPaletteButton = new();

    private EventService EventService { get; init; }
    private TooltipWidget CollectibleTooltipWidget { get; init; }
    public EquipSlotsWidget(EventService eventService)
    {
        EventService = eventService;
        CollectibleTooltipWidget = new TooltipWidget(EventService);
        LoadEquipSlotIcons();
        InitializePaletteWidgets();
        eventService.Subscribe<GlamourSetChangeEvent, GlamourSetChangeEventArgs>(OnPublish);
        eventService.Subscribe<GlamourItemChangeEvent, GlamourItemChangeEventArgs>(OnPublish);
        eventService.Subscribe<OutfitItemChangeEvent, OutfitItemChangeEventArgs>(OnPublish);
        eventService.Subscribe<DyeChangeEvent, DyeChangeEventArgs>(OnPublish);
    }

    private void ApplyGlamourSetToPlate()
    {
        // TODO indication which items exist in Dresser
        foreach (var (equipSlot, glamourItem) in currentGlamourSet.Items)
        {
            PlatesExecutor.SetPlateItem(glamourItem.GetCollectible().ExcelRow, (byte)glamourItem.Stain0Id, (byte)glamourItem.Stain1Id, equipSlot);
        }
    }

    public unsafe void Draw()
    {
        // max size of a slot if active. 
        float slotSize = UiHelper.ScaleForFontSize(50.2f);
        // ratio to scale slot icons by
        const float iconScale = .916f;
        var bgColor = *ImGui.GetStyleColorVec4(ImGuiCol.WindowBg);
        for (int i = 0; i < Services.DataProvider.SupportedEquipSlots.Count; i++)
        {
            EquipSlot equipSlot = Services.DataProvider.SupportedEquipSlots[i];

            // Load collectible if set
            ISharedImmediateTexture? icon = null;
            GlamourCollectible? collectible = null;

            var glamourItem = currentGlamourSet.GetItem(equipSlot);
            if (glamourItem is not null)
            {
                collectible = glamourItem.GetCollectible();
                icon = collectible.GetIcon();
            }

            // Draw icon
            if (icon is null)
                icon = equipSlotIcons[equipSlot];

            // i+1 here so that we only do this every odd item
            if ((i + 1) % 2 == 0) ImGui.SameLine();
            // Draw blue rect border over active equip slot
            var origPos = ImGui.GetCursorPos();
            if (activeEquipSlot == equipSlot)
            {
                ImGui.SetCursorPos(new Vector2(origPos.X - ImGui.GetFontSize() * .11f, origPos.Y - ImGui.GetFontSize() * .11f));
                ImGui.ColorButton(equipSlot.ToString(), ColorsPalette.BLUE, ImGuiColorEditFlags.NoTooltip, new Vector2(slotSize, slotSize));
                ImGui.SetCursorPos(origPos);
            }

            // adjust icon to scale correctly based on 
            ImGui.ColorButton(equipSlot.ToString(), bgColor, ImGuiColorEditFlags.NoTooltip, new Vector2(slotSize, slotSize) * iconScale);
            var finalCursorPos = ImGui.GetCursorPos();
            ImGui.SetCursorPos(origPos);

            ImGui.SetItemAllowOverlap();

            // Draw equip slot buttons
            // overwrite custom ImGui frame padding so it's actually a square (default is (4,3))
            const int equipSlotFramePadding = 2;
            if (ImGui.ImageButton(icon.GetWrapOrEmpty().Handle, new Vector2(slotSize - equipSlotFramePadding * 2, slotSize - equipSlotFramePadding * 2) * iconScale, equipSlotFramePadding))
            {
                SetEquipSlot(equipSlot);
            }

            // Interaction with buttons (details / reset), Item must be set in this slot
            if (ImGui.IsItemHovered() && collectible is not null && !hoveredPaletteButton[equipSlot]) // hoveredPaletteButton is here to take presedence of it over this
            {
                // Details on hover
                ImGui.BeginTooltip();
                ImGui.Text("Right Click to interact...");
                CollectibleTooltipWidget.DrawItemTooltip(collectible);
                ImGui.EndTooltip();
            }
            if (collectible is not null && !hoveredPaletteButton[equipSlot] && ImGui.BeginPopupContextItem($"click-glam-item##{collectible.GetHashCode()}", ImGuiPopupFlags.MouseButtonRight))
            {
                if(ImGui.Button("Remove from Slot"))
                {
                    currentGlamourSet.ClearEquipSlot(equipSlot);
                    Services.PreviewExecutor.ResetSlotPreview(equipSlot);
                }
                CollectibleTooltipWidget.DrawItemTooltip(collectible);
                ImGui.EndPopup();
            }
             // Draw dye circles if able
            if(collectible is not null)
            {
                int dyeSlots = collectible.GetNumberOfDyeSlots();
                if (dyeSlots > 0)
                {
                    // First dye slot
                    var primaryDyeColor = paletteWidgets[equipSlot].ActiveStainPrimary.RowId == 0 ?
                        ColorsPalette.BLACK : paletteWidgets[equipSlot].ActiveStainPrimary.VecColor();
                    ImGui.SameLine();
                    ImGui.SetItemAllowOverlap();
                    ImGui.SetCursorPos(origPos + new Vector2(slotSize - ImGui.GetFontSize(), 0) * .9f);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0, 0, 0, 0));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0, 0, 0, 0));
                    ImGui.PushStyleColor(ImGuiCol.Text, primaryDyeColor);
                    ImGui.SetWindowFontScale(.65f);
                    ImGuiComponents.IconButton(paletteWidgets[equipSlot].ActiveStainPrimary.RowId == 0 ? FontAwesomeIcon.CircleNotch : FontAwesomeIcon.Circle);
                    ImGui.SetWindowFontScale(1f);
                    ImGui.PopStyleColor();
                    if (dyeSlots == 2)
                    {
                        // second dye slot
                        var secondaryDyeColor = paletteWidgets[equipSlot].ActiveStainSecondary.RowId == 0 ?
                            ColorsPalette.BLACK : paletteWidgets[equipSlot].ActiveStainSecondary.VecColor();
                        ImGui.SameLine();
                        ImGui.SetItemAllowOverlap();
                        ImGui.SetCursorPos(origPos + new Vector2(slotSize - ImGui.GetFontSize(), ImGui.GetFontSize()) * .9f);
                        ImGui.PushStyleColor(ImGuiCol.Text, secondaryDyeColor);
                        ImGui.SetWindowFontScale(.65f);
                        ImGuiComponents.IconButton(paletteWidgets[equipSlot].ActiveStainSecondary.RowId == 0 ? FontAwesomeIcon.CircleNotch : FontAwesomeIcon.Circle);
                        ImGui.SetWindowFontScale(1f);
                        ImGui.PopStyleColor();
                    }
                    // pops the text button hover/active colors
                    ImGui.PopStyleColor();
                    ImGui.PopStyleColor();
                }
            }
            // Set cursor on bottom right to draw Palette Widget button
            ImGui.SameLine();
            ImGui.SetItemAllowOverlap(); // Makes this button take precedence
            ImGui.SetCursorPos(origPos + new Vector2(slotSize - ImGui.GetFontSize(), slotSize - ImGui.GetFontSize()) * 0.9f);
            
            ImGui.PushStyleColor(ImGuiCol.Text, paletteWidgetButtonDefaultColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0, 0, 0, 0));
            ImGui.SetWindowFontScale(.7f);
            ImGuiComponents.IconButton(FontAwesomeIcon.PaintBrush);
            ImGui.SetWindowFontScale(1f);
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();

            // Open Palette Widget popup
            if (ImGui.BeginPopupContextItem($"palette-widget##{equipSlot}", ImGuiPopupFlags.MouseButtonLeft))
            {
                paletteWidgets[equipSlot].Draw();
                ImGui.EndPopup();
            }

            // Interaction with palette button (details / reset)
            if (ImGui.IsItemHovered())
            {
                hoveredPaletteButton[equipSlot] = true;

                // Details on hover
                ImGui.BeginTooltip();
                var stain0Name = paletteWidgets[equipSlot].ActiveStainPrimary.RowId == 0 ? "No Dye Selected" : paletteWidgets[equipSlot].ActiveStainPrimary.Name;
                var stain1Name = paletteWidgets[equipSlot].ActiveStainSecondary.RowId == 0 ? "No Dye Selected" : paletteWidgets[equipSlot].ActiveStainSecondary.Name;
                ImGui.Text(stain0Name.ToString());
                ImGui.Text(stain1Name.ToString());
                ImGui.EndTooltip();

                // Reset on right click
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    paletteWidgets[equipSlot].ResetStain();
                    EventService.Publish<DyeChangeEvent, DyeChangeEventArgs>(new DyeChangeEventArgs(equipSlot));
                }
            }
            else
            {
                hoveredPaletteButton[equipSlot] = false;
            }
            ImGui.SetCursorPos(finalCursorPos);
        }
    }

    private void SetEquipSlot(EquipSlot equipSlot)
    {
        activeEquipSlot = equipSlot;
        EventService.Publish<FilterChangeEvent, FilterChangeEventArgs>(new FilterChangeEventArgs());
    }

    private Dictionary<EquipSlot, ISharedImmediateTexture> equipSlotIcons = new();
    private void LoadEquipSlotIcons()
    {
        foreach (var equipSlot in Services.DataProvider.SupportedEquipSlots)
        {
            var equipSlotName = Enum.GetName(typeof(EquipSlot), equipSlot);
            var iconPath = Path.Combine(Services.PluginInterface.AssemblyLocation.Directory?.FullName!, $"Data\\Resources\\{equipSlotName}.png");
            var icon = Services.TextureProvider.GetFromFile(iconPath);

            equipSlotIcons[equipSlot] = icon;
        }
    }

    private void InitializePaletteWidgets()
    {
        foreach (var equipSlot in Services.DataProvider.SupportedEquipSlots)
        {
            paletteWidgets[equipSlot] = new PaletteWidget(equipSlot, EventService);
            hoveredPaletteButton[equipSlot] = false;
        }
    }

    public void ResetPaletteWidgets()
    {
        foreach (var equipSlot in Services.DataProvider.SupportedEquipSlots)
        {
            paletteWidgets[equipSlot].ResetStain();
        }
    }

    public void OnPublish(GlamourSetChangeEventArgs args)
    {
        // Update currentGlamourSet reference
        currentGlamourSet = args.GlamourSet;

        // Reset palette state
        ResetPaletteWidgets();

        // Set pelette stain state
        foreach (var (equipSlot, glamourItem) in args.GlamourSet.Items)
        {
            paletteWidgets[equipSlot].ActiveStainPrimary = glamourItem.GetStainPrimary();
            paletteWidgets[equipSlot].ActiveStainSecondary = glamourItem.GetStainSecondary();
        }
    }
    public void OnPublish(OutfitItemChangeEventArgs args)
    {
        // reset items
        currentGlamourSet.Items.Clear();

        // add items in outfit
        List<Item> items = Services.ItemFinder.ItemsInOutfit(args.Collectible.ExcelRow.RowId);
        foreach (var item in items)
        {
            currentGlamourSet.SetItem(item, paletteWidgets[item.GetEquipSlot()].ActiveStainPrimary.RowId, paletteWidgets[item.GetEquipSlot()].ActiveStainSecondary.RowId);
        }
    }

    public void OnPublish(GlamourItemChangeEventArgs args)
    {
        if (Services.Configuration.SeparatePreviewAndApply && !args.ApplyToSlot) return;
        // Update current glamour set
        var item = args.Collectible.ExcelRow;
        currentGlamourSet.SetItem(item, paletteWidgets[item.GetEquipSlot()].ActiveStainPrimary.RowId, paletteWidgets[item.GetEquipSlot()].ActiveStainSecondary.RowId, equipSlot: activeEquipSlot);
    }

    public void OnPublish(DyeChangeEventArgs args)
    {
        var glamourItem = currentGlamourSet.GetItem(args.EquipSlot);
        var equipSlot = args.EquipSlot;
        // Update currentGlamourSet
        currentGlamourSet.GetItem(equipSlot)?.Stain0Id = paletteWidgets[equipSlot].ActiveStainPrimary.RowId;
        currentGlamourSet.GetItem(equipSlot)?.Stain1Id = paletteWidgets[equipSlot].ActiveStainSecondary.RowId;
        // If Dye changed for empty equip slot - use the characters equipped item
        if (glamourItem is null || Services.Configuration.SeparatePreviewAndApply)
        {
            Services.PreviewExecutor.PreviewWithTryOnRestrictions(
                equipSlot,
                paletteWidgets[equipSlot].ActiveStainPrimary.RowId,
                paletteWidgets[equipSlot].ActiveStainSecondary.RowId,
                Services.Configuration.ForceTryOn
                );
        }
        else
        {
            // Refresh Preview
            Services.PreviewExecutor.PreviewWithTryOnRestrictions(
                glamourItem.GetCollectible(),
                paletteWidgets[equipSlot].ActiveStainPrimary.RowId,
                paletteWidgets[equipSlot].ActiveStainSecondary.RowId,
                Services.Configuration.ForceTryOn
            );
        }
    }
}

