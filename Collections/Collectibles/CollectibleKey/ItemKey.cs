using System.Linq.Expressions;

namespace Collections;

public class ItemKey : CollectibleKey<(Item, int)>, ICreateable<ItemKey, (Item, int)>
{

    public ItemKey((Item, int) input) : base(input)
    {
    }

    public static ItemKey Create((Item, int) input)
    {
        return new(input);
    }

    protected override string GetName((Item, int) input)
    {
        return input.Item1.Name.ToString();
    }

    protected override uint GetId((Item, int) input)
    {
        return input.Item1.RowId;
    }

    protected override List<ICollectibleSource> GetCollectibleSources((Item, int) input)
    {
        var excelRow = input.Item1;
        var collectibleSources = new List<ICollectibleSource>();
        var dataGenerator = Services.DataGenerator.SourcesDataGenerator;

        // Stop recursion depth at 10 at most
        if (input.Item2 >= 10)
        {
            return collectibleSources;
        }

        // For currencies dont bother looking at another level of shops
        if (input.Item2 == 0)
        {
            if (dataGenerator.ShopsDataGenerator.data.TryGetValue(excelRow.RowId, out var shopEntries))
            {
                collectibleSources.AddRange(shopEntries.Select(shopEntry => new ShopSource(shopEntry, input.Item2 + 1)));
            }
        }

        if (dataGenerator.InstancesDataGenerator.data.TryGetValue(excelRow.RowId, out var duty))
        {
            collectibleSources.AddRange(duty.Select(instance => new InstanceSource(instance)));
        }

        if (dataGenerator.EventDataGenerator.data.TryGetValue(excelRow.RowId, out var events))
        {
            collectibleSources.AddRange(events.Select(eventName => new EventSource(eventName)));
        }

        if (dataGenerator.MogStationDataGenerator.data.ContainsKey(excelRow.RowId))
        {
            collectibleSources.Add(new MogStationSource());
        }

        if (dataGenerator.ContainersDataGenerator.data.TryGetValue(excelRow.RowId, out var containers))
        {
            collectibleSources.AddRange(containers.Select(itemId => new ContainerSource(itemId, input.Item2 + 1)));
        }

        if (dataGenerator.AchievementsDataGenerator.data.TryGetValue(excelRow.RowId, out var achievements))
        {
            collectibleSources.AddRange(achievements.Select(entry => new AchievementSource(entry)));
        }
        
        if (dataGenerator.PvPDataGenerator.data.TryGetValue(excelRow.RowId, out var pvpSeries))
        {
            collectibleSources.AddRange(pvpSeries.Select(entry => new PvPSeriesSource(entry.Item1, entry.Item2)));
        }

        if (dataGenerator.QuestsDataGenerator.data.TryGetValue(excelRow.RowId, out var quests))
        {
            collectibleSources.AddRange(quests.Select(entry => new QuestSource(entry)));
        }

        if (dataGenerator.CraftingDataGenerator.data.TryGetValue(excelRow.RowId, out var recipes))
        {
            collectibleSources.AddRange(recipes.Select(entry => new CraftingSource(entry)));
            // Go one level lower and add instance sources for materials
            foreach(var recipe in recipes)
            {
                foreach(var item in ExcelCache<Recipe>.GetSheet().GetRow(recipe).GetValueOrDefault().Ingredient)
                {
                    if(!item.ValueNullable.HasValue || !dataGenerator.InstancesDataGenerator.data.TryGetValue(item.Value.RowId, out var instance))
                        continue;
                    // Treasure Map Exclusive ingredients
                    // All Treasure map exclusive mat items (and voyage items) have a low sell price of 1
                    if(item.Value.ItemSortCategory.RowId == 16 && item.Value.PriceLow == 1 && item.Value.Lot && (item.Value.PriceMid == 99999 || item.Value.Unknown4 == 2000 || item.Value.Unknown4 == 4000 || item.Value.Unknown4 == 64000))
                        collectibleSources.AddRange(instance.Select(duty => new InstanceSource(duty)));
                    // Raid Crafting Items
                    if(item.Value.ItemSortCategory.RowId == 18 && item.Value.Lot)
                        collectibleSources.AddRange(instance.Select(duty => new InstanceSource(duty)));
                    // Tattered Orchestrion Rolls
                    if(item.Value.ItemSearchCategory.RowId == 80 && item.Value.Unknown4 == 20000)
                        collectibleSources.AddRange(instance.Select(duty => new InstanceSource(duty)));
                }
            }
        }

        if (dataGenerator.SubmarineDataGenerator.data.TryGetValue(excelRow.RowId, out var submarines))
        {
            collectibleSources.AddRange(submarines.Select(entry => new SubmarineSource(entry)));
        }

        if (Services.DataGenerator.KeysDataGenerator.ItemIdToTripleTriadId.TryGetValue(excelRow.RowId, out var tripleTriadId))
        {
            if (dataGenerator.TripleTriadNpcDataGenerator.data.TryGetValue(tripleTriadId, out var npcs))
            {
                collectibleSources.AddRange(npcs.Select(entry => new NpcSource(entry)));
            }
        }

        return collectibleSources;
    }

    protected override HashSet<SourceCategory> GetBaseSourceCategories()
    {
        var sourceCategories = new HashSet<SourceCategory>();

        // Add category if item is a currency
        if (Services.DataGenerator.CurrencyDataGenerator.ItemIdToSourceCategory.TryGetValue(Input.Item1.RowId, out var category))
        {
            sourceCategories.Add(category);
        }

        return sourceCategories;
    }

    public ISharedImmediateTexture GetIcon()
    {
        return IconHandler.GetIcon(Input.Item1.Icon);
    }

    public override Tradeability GetIsTradeable()
    {
        return !Input.Item1.IsUntradable ? Tradeability.Tradeable : Tradeability.Untradeable;
    }

    private World? homeWorld = null;
    private int? marketBoardPrice = null;
    private bool marketBoardPriceScheduled = false;
    public override int? GetMarketBoardPriceLazy()
    {
        if (marketBoardPrice != null)
        {
            return marketBoardPrice;
        }

        if (!marketBoardPriceScheduled)
        {
            marketBoardPriceScheduled = true;
            var world = Services.PlayerState.CurrentWorld.Value;
            homeWorld = world;
            Task.Run(async () =>
            {
                await Services.UniversalisClient.populateMarketBoardData(Input.Item1.RowId, homeWorld);
                Services.UniversalisClient.itemToMarketplaceData.TryGetValue(Input.Item1.RowId, out var marketplaceData);
                var price = marketplaceData?.minPriceWorld;
                if (price != null)
                {
                    marketBoardPrice = (int)price;
                }
            });
        }
        return null;
    }
}

