namespace Collections;

public class InstanceTab : IDrawable
{
    private List<ICollectible> collections = new();
    private uint collectiblesLoadedInstanceId = 0;
    private bool hideObtainedCollectibles = false;
    private bool onlyOpenIfUncollected = false;
    private List<string> excludedCollections = new();

    private EventService EventService { get; init; }
    private CollectionWidget CollectionWidget { get; init; }
    public InstanceTab()
    {
        EventService = new EventService();
        CollectionWidget = new CollectionWidget(EventService, false);
        Services.DutyState.DutyStarted += OnDutyStarted;
    }

    public void OnDutyStarted(IDutyStateEventArgs args)
    {
        Dev.Log("Received DutyStarted event");
        hideObtainedCollectibles = Services.Configuration.AutoHideObtainedFromInstanceTab;
        onlyOpenIfUncollected = Services.Configuration.OnlyOpenIfUncollected;
        excludedCollections = Services.Configuration.ExcludedCollectionsFromInstanceTab;
        collectiblesLoadedInstanceId = 0; // Makes sure we always load at the start of an instance
        if (Services.Configuration.AutoOpenInstanceTab)
        {
            // If this setting is true, don't open the instance tab if the user has already collected everything for the instance
            if (onlyOpenIfUncollected)
            {
                LoadCollectibles();
                if (collections.All(c => c.GetIsObtained())) return;
            }
            Services.WindowsInitializer.MainWindow.OpenTab("Instance");
        }

    }

    public void Draw()
    {
        // Not in valid instance
        if (!InValidInstance())
        {
            ImGui.Text("Can only be viewed in an Instance");
            collectiblesLoadedInstanceId = 0;
            return;
        }
        // Load collectibles if not done already
        if (GetCurrentInstance() != collectiblesLoadedInstanceId)
        {
            LoadCollectibles();
        }
        // Let user hide or show obtained instance collectibles
        if(ImGui.Checkbox("Hide Obtained", ref hideObtainedCollectibles)) {
            LoadCollectibles();
        };

        // Draw collections
        DrawCollections();
    }

    public void DrawCollections()
    {
        CollectionWidget.Draw(collections, enableFilters: false, enableCollectionHeaders: true);
    }

    private void LoadCollectibles()
    {
        collectiblesLoadedInstanceId = GetCurrentInstance();
        var currentDutyItemIds = CurrentDutyItemIds(collectiblesLoadedInstanceId);

        collections =
        Services.DataProvider.GetCollections()
        .Where((col) => !excludedCollections.Contains(col.Key)).ToDictionary()
        .Values.Aggregate(
            (full, col) => [..full, ..col]).Where((c) => 
                {
                    // TODO: blue mage spells don't come from items.
                    if(c.GetCollectionName() == BlueMageCollectible.CollectionName) {
                        return false;
                    }
                    if (((c.CollectibleKey?.Id ?? 0) == 0) || (!currentDutyItemIds?.Contains(c.CollectibleKey?.Id ?? 0) ?? true))
                    {
                        return false;
                    }
                    // Only update items that are part of this instance
                    c.UpdateObtainedState();
                    return !hideObtainedCollectibles || !c.GetIsObtained();
                }
            )
        // put glamour as last 
        .OrderBy(c => c.GetCollectionName() == GlamourCollectible.CollectionName)
        .ToList();
    }

    private static uint GetCurrentInstance()
    {
        var territoryType = ExcelCache<TerritoryType>.GetSheet().GetRow(Services.ClientState.TerritoryType);
        return territoryType.Value.ContentFinderCondition.RowId;
    }

    private static bool InValidInstance()
    {
        var instanceId = GetCurrentInstance();
        var inInstance = instanceId != 0;
        var collectionExists = Services.DataGenerator.SourcesDataGenerator.InstancesDataGenerator.contentFinderConditionToItems.ContainsKey(instanceId);
        return inInstance && collectionExists;
    }

    private List<uint>? CurrentDutyItemIds(uint instanceId)
    {

        if (Services.DataGenerator.SourcesDataGenerator.InstancesDataGenerator.contentFinderConditionToItems.ContainsKey(instanceId))
        {
            return Services.DataGenerator.SourcesDataGenerator.InstancesDataGenerator.contentFinderConditionToItems[instanceId];
        }
        Dev.Log($"Unable to find items obtained by the current duty id: {instanceId}");
        return new List<uint>();
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
        Services.DutyState.DutyStarted -= OnDutyStarted;
    }
}
