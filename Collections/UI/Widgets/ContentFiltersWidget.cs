namespace Collections;

public class ContentFiltersWidget
{

    public Dictionary<SourceCategory, bool> Filters = new();

    private Vector2 iconSize = new(25, 25);

    private Dictionary<SourceCategory, ISharedImmediateTexture> icons = new();
    private Dictionary<SourceCategory, int> contentTypesToIconId = new()
    {
        {SourceCategory.Gil, 61758},
        {SourceCategory.Scrips, 65028},
        {SourceCategory.MGP, 61820},
        {SourceCategory.PvP, PvPSeriesSource.iconId}, // 61806
        {SourceCategory.Duty, InstanceSource.defaultIconId},
        {SourceCategory.Quest, QuestSource.iconId},
        {SourceCategory.Fate, 61809},
        {SourceCategory.Event, EventSource.iconId},
        {SourceCategory.Tomestones, 65086},
        {SourceCategory.DeepDungeon, 61824},
        {SourceCategory.BeastTribes, 61814}, 
        {SourceCategory.MogStation, MogStationSource.iconId},
        {SourceCategory.Achievement, AchievementSource.iconId},
        {SourceCategory.CompanySeals, 61812},
        {SourceCategory.IslandSanctuary, 61847},
        {SourceCategory.TheHunt, 61819},
        {SourceCategory.TreasureHunts, 61808},
        {SourceCategory.Crafting, 61816},
        {SourceCategory.RestorationZone, 61815},
        {SourceCategory.FieldOperations, 61837},
        {SourceCategory.Voyages, SubmarineSource.defaultIconId},
    };

    private int columns { get; init; }
    private EventService EventService { get; init; }
    public ContentFiltersWidget(EventService eventService, int columns = 1, List<ICollectible>? collection = null)
    {
        EventService = eventService;
        this.columns = columns;
            FilterAvailableContentTypes(collection);
        InitializeDefaultFiltersState();
        LoadIcons();
    }

    public void Draw()
    {
        if (ImGui.BeginTable($"content-filters", columns, ImGuiTableFlags.ScrollY))
        {
            var i = -1;
            foreach (var (collectibleSourceType, iconId) in contentTypesToIconId)
            {
                // Maintain rows / columns
                i++;
                if (i % columns == 0)
                {
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, 0);
                }
                ImGui.TableNextColumn();
                
                // Background color if selected
                if (Filters[collectibleSourceType])
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(ImGuiCol.ButtonActive));

                // Draw cell
                DrawCell(collectibleSourceType);
            }
            ImGui.EndTable();
        }
    }

    private void DrawCell(SourceCategory collectibleSourceCategory)
    {
        // Draw button as a group
        UiHelper.GroupWithMinWidth(() =>
        {
            ImGui.Image(icons[collectibleSourceCategory].GetWrapOrEmpty().Handle, iconSize * UiHelper.ScaleForFontSize(1));
            ImGui.SameLine();
            ImGui.Text(collectibleSourceCategory.GetEnumName().ToSentence().TrimStart(' '));
            //ImGui.Text(hint1);
        }, ImGui.GetColumnWidth());

        // Left click to set filter
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(ImGuiCol.ButtonActive));
            var currentFilter = Filters[collectibleSourceCategory];
            // ctrl click will not reset filters
            if (!ImGui.IsKeyDown(ImGuiKey.ModCtrl))
            {
                ResetFilters();
            }
            SetFilter(collectibleSourceCategory, !currentFilter);
        }

        // Right click to exclude filter
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(ImGuiCol.CheckMark));
            if(!ImGui.IsKeyDown(ImGuiKey.ModCtrl))
            {
                SetAllFilters();
            }
            var currentFilter = Filters[collectibleSourceCategory];
            SetFilter(collectibleSourceCategory, !currentFilter);
        }

        // Save hover state
        if (ImGui.IsItemHovered())
        {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(ImGuiCol.ButtonHovered));
        }
    }

    private void FilterAvailableContentTypes(List<ICollectible>? collection)
    {
        if (collection != null)
        {
            var sourceTypes = collection.Where(c => c.CollectibleKey != null).SelectMany(c => c.CollectibleKey.SourceCategories).ToHashSet();

            foreach (var sourceType in contentTypesToIconId.Keys.ToList())
            {
                if (!sourceTypes.Contains(sourceType))
                {
                    contentTypesToIconId.Remove(sourceType);
                }
            }
        }
    }

    private void SetFilter(SourceCategory sourceType, bool value)
    {
        Filters[sourceType] = value;
        EventService.Publish<FilterChangeEvent, FilterChangeEventArgs>(new FilterChangeEventArgs());
    }

    private void LoadIcons()
    {
        icons = contentTypesToIconId.AsParallel().ToDictionary(c => c.Key, c => IconHandler.GetIcon(c.Value));
    }

    private void InitializeDefaultFiltersState()
    {
        ResetFilters();
    }

    // Not sending FilterChangeEvent
    private void ResetFilters()
    {
        foreach (var (sourceType, _) in contentTypesToIconId)
        {
            Filters[sourceType] = false;
        }
    }

    private void SetAllFilters()
    {
        foreach (var (sourceType, _) in contentTypesToIconId)
        {
            Filters[sourceType] = true;
        }
    }
}

