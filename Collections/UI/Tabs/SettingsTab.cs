namespace Collections;

public class SettingsTab : IDrawable
{
    private List<string> collectionNames = new();
    public SettingsTab()
    {
        separatePreviewAndApply = Services.Configuration.SeparatePreviewAndApply;
        showAdditionalTooltips = Services.Configuration.AdditionalTooltips;
        autoOpenInstanceTab = Services.Configuration.AutoOpenInstanceTab;
        onlyOpenIfUncollected = Services.Configuration.OnlyOpenIfUncollected;
        autoHideObtainedFromInstanceTab = Services.Configuration.AutoHideObtainedFromInstanceTab;
        excludedCollectionsFromInstanceTab = Services.Configuration.ExcludedCollectionsFromInstanceTab;
        highVisibilityObtained = Services.Configuration.HighVisibilityObtained;
        collectionNames = Services.DataProvider.GetCollections().AsParallel().Select(col => col.Key).OrderBy((key) => key != GlamourCollectible.CollectionName).ThenBy(k => k).ToList();
    }

    private bool autoOpenInstanceTab;
    private bool separatePreviewAndApply;
    private bool onlyOpenIfUncollected;
    private bool autoHideObtainedFromInstanceTab;
    private bool highVisibilityObtained;
    private List<string> showAdditionalTooltips;
    private List<string> excludedCollectionsFromInstanceTab;
    private string selectedSection = "General";

    private static readonly string[] sections =
    [
        "General",
        "Display",
        "Instance tab",
        "Hidden tabs",
        "Hidden items",
    ];

    public void Draw()
    {
        ImGui.BeginChild("settings-section-list", new Vector2(170, 0), true);
        foreach (var section in sections)
        {
            if (ImGui.Selectable(section, selectedSection == section, ImGuiSelectableFlags.SpanAllColumns))
            {
                selectedSection = section;
            }
        }
        ImGui.EndChild();

        ImGui.SameLine();
        ImGui.BeginChild("settings-section-content", new Vector2(0, 0), true);
        switch (selectedSection)
        {
            case "General":
                DrawGeneralSettings();
                break;
            case "Instance tab":
                DrawInstanceSettings();
                break;
            case "Display":
                DrawDisplaySettings();
                break;
            case "Hidden tabs":
                DrawHiddenTabs();
                break;
            case "Hidden items":
                DrawHiddenItems();
                break;
        }
        ImGui.EndChild();
    }

    private void DrawGeneralSettings()
    {
        if (ImGui.Checkbox("Separate Preview and Add to Equip Slot", ref separatePreviewAndApply))
        {
            Services.Configuration.SeparatePreviewAndApply = separatePreviewAndApply;
            Services.Configuration.Save();
        }

        if (ImGui.Checkbox("Use green borders to indicate obtained items instead of checkmarks", ref highVisibilityObtained))
        {
            Services.Configuration.HighVisibilityObtained = highVisibilityObtained;
            Services.Configuration.Save();
        }
    }

    private void DrawInstanceSettings()
    {
        if (ImGui.Checkbox("Auto open Instance tab when entering an instance", ref autoOpenInstanceTab))
        {
            Services.Configuration.AutoOpenInstanceTab = autoOpenInstanceTab;
            Services.Configuration.Save();
        }

        ImGui.Indent();
        if (ImGui.Checkbox("Only open Instance tab if there are uncollected items", ref onlyOpenIfUncollected))
        {
            Services.Configuration.OnlyOpenIfUncollected = onlyOpenIfUncollected;
            Services.Configuration.Save();
        }
        ImGui.Unindent();

        if (ImGui.Checkbox("Auto hide obtained items from Instance tab", ref autoHideObtainedFromInstanceTab))
        {
            Services.Configuration.AutoHideObtainedFromInstanceTab = autoHideObtainedFromInstanceTab;
            Services.Configuration.Save();
        }

        ImGui.Text("Exclude these collections from the Instance tab");
        ImGui.BeginChild("excluded-collections-list", new Vector2(-1, 0), true);
        foreach (var collection in collectionNames)
        {
            bool isExcluded = excludedCollectionsFromInstanceTab.Contains(collection);
            if (ImGui.Checkbox($"{collection}", ref isExcluded))
            {
                if (isExcluded)
                    excludedCollectionsFromInstanceTab.Add(collection);
                else
                    excludedCollectionsFromInstanceTab.Remove(collection);
                Services.Configuration.Save();
            }
        }
        ImGui.EndChild();
    }

    private void DrawDisplaySettings()
    {
        ImGui.Text("Show additional item information for these collections");
        ImGui.BeginChild("additional-tooltips-list", new Vector2(-1, 0), true);
        foreach (var collection in collectionNames)
        {
            bool isShown = showAdditionalTooltips.Contains(collection);
            if (ImGui.Checkbox($"{collection}", ref isShown))
            {
                if (isShown)
                    showAdditionalTooltips.Add(collection);
                else
                    showAdditionalTooltips.Remove(collection);
                Services.Configuration.Save();
            }
        }
        ImGui.EndChild();
    }

    private void DrawHiddenTabs()
    {
        ImGui.Text("Hidden tabs");
        ImGui.BeginChild("hidden-tabs-list", new Vector2(-1, 0), true);
        var hiddenTabs = Services.Configuration.HiddenTabs.OrderBy(name => name).ToList();
        if (hiddenTabs.Count == 0)
        {
            ImGui.TextDisabled("No hidden tabs");
        }
        else
        {
            foreach (var tabName in hiddenTabs)
            {
                if (ImGui.Selectable(tabName))
                {
                    Services.Configuration.HiddenTabs.Remove(tabName);
                    Services.Configuration.Save();
                }
            }
        }
        ImGui.EndChild();
    }

    private void DrawHiddenItems()
    {
        ImGui.Text("Hidden items");
        ImGui.BeginChild("hidden-items-list", new Vector2(-1, 0), true);
        var collections = Services.DataProvider.GetCollections();
        var hiddenItems = Services.Configuration.HiddenItems
            .OrderBy(entry => entry.Key)
            .Select(entry => (CollectionName: entry.Key, ItemIds: entry.Value.ToList()))
            .ToList();
        if (hiddenItems.Count == 0)
        {
            ImGui.TextDisabled("No hidden items");
        }
        else
        {
            foreach (var (collectionName, itemIds) in hiddenItems)
            {
                ImGui.TextDisabled(collectionName);
                if (!collections.TryGetValue(collectionName, out var collection))
                {
                    continue;
                }

                foreach (var itemId in itemIds)
                {
                    var item = collection.FirstOrDefault(collectible => collectible.Id == itemId);
                    var itemName = item is null ? $"Unknown item ({itemId})" : item.Name;
                    if (ImGui.Selectable($"{itemName}##hidden-item-{collectionName}-{itemId}"))
                    {
                        Services.Configuration.HiddenItems[collectionName].Remove(itemId);
                        if (Services.Configuration.HiddenItems[collectionName].Count == 0)
                        {
                            Services.Configuration.HiddenItems.Remove(collectionName);
                        }
                        Services.Configuration.Save();
                    }
                }
            }
        }
        ImGui.EndChild();
    }

    public void OnOpen()
    {
        Dev.Log();
    }

    public void OnClose()
    {
        
    }

    public void Dispose()
    {
    }
}
