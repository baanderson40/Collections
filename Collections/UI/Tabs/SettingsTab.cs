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
    public void Draw()
    {
        if(ImGui.Checkbox("Separate Preview and Add to Equip Slot", ref separatePreviewAndApply))
        {
            Services.Configuration.SeparatePreviewAndApply = separatePreviewAndApply;
            Services.Configuration.Save();
        }
        
        if (ImGui.Checkbox("Auto open Instance tab when entering an instance", ref autoOpenInstanceTab))
        {
            Services.Configuration.AutoOpenInstanceTab = autoOpenInstanceTab;
            Services.Configuration.Save();
        }

        // padding to signify this is a sub-option for auto-open
        ImGui.InvisibleButton("padding", new Vector2(15, 1));
        ImGui.SameLine();
        if (ImGui.Checkbox("Only open Instance tab if there are uncollected items ", ref onlyOpenIfUncollected))
        {
            Services.Configuration.OnlyOpenIfUncollected = onlyOpenIfUncollected;
            Services.Configuration.Save();
        }

        if (ImGui.Checkbox("Auto hide obtained items from Instance tab", ref autoHideObtainedFromInstanceTab))
        {
            Services.Configuration.AutoHideObtainedFromInstanceTab = autoHideObtainedFromInstanceTab;
            Services.Configuration.Save();
        }

        if (ImGui.Checkbox("Use green borders to indicate obtained items instead of checkmarks", ref highVisibilityObtained))
        {
            Services.Configuration.HighVisibilityObtained = highVisibilityObtained;
            Services.Configuration.Save();
        }

        ImGui.Text("Show additional item information for these collections");
        ImGui.BeginListBox("##collection-box-add-tooltips", new Vector2(300, 200));
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
        ImGui.EndListBox();

        ImGui.Text("Exclude these collections from the Instance tab");
        ImGui.BeginListBox("##collection-box", new Vector2(300, 200));
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
        ImGui.EndListBox();

        if (ImGui.BeginChild("hidden-tabs-section", new Vector2(300, 125), false))
        {
            if (ImGui.CollapsingHeader("Hidden tabs"))
            {
                var hiddenTabs = Services.Configuration.HiddenTabs.OrderBy(name => name).ToList();
                if (ImGui.BeginChild("hidden-tabs-list", new Vector2(-1, 100), true))
                {
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
            }

            ImGui.EndChild();
        }

        if (ImGui.BeginChild("hidden-items-section", new Vector2(300, 225), false))
        {
            if (ImGui.CollapsingHeader("Hidden items"))
            {
                var collections = Services.DataProvider.GetCollections();
                var hiddenItems = Services.Configuration.HiddenItems
                    .OrderBy(entry => entry.Key)
                    .Select(entry => (CollectionName: entry.Key, ItemIds: entry.Value.ToList()))
                    .ToList();

                if (ImGui.BeginChild("hidden-items-list", new Vector2(-1, 200), true))
                {
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
            }

            ImGui.EndChild();
        }

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
