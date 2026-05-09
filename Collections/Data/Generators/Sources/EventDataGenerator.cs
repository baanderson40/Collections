using Lumina.Extensions;

namespace Collections;

public class EventDataGenerator : BaseDataGenerator<string>
{

    // For obtaining localized event names
    private static Dictionary<string, uint> QuestIdStartToCabinetSubCategory = new()
    {
        {"FesNy", 44}, // New Years
        {"FesVl", 55}, // Valentines
        {"FesPd", 12}, // Ladies Day
        {"FesEs", 19}, // Easter
        {"FesSu", 26}, // Summer
        {"FesHl", 36}, // Halloween
        {"FesXm", 62}, // Xmas
        {"FesGs", 25}, // Gold Saucer
        // collabs
        {"FesEvn", 68}, // XI Online
        {"FesLgt", 69}, // XIII
        {"FesBkc", 70}, // XV
        {"FesSxt", 71}, // XVI
        {"FesDxq", 72}, // Dragon Quest X
        {"FesYkw", 73}, // Yokai Watch
    };

    private static Dictionary<string, uint> QuestIdStartToFittingRoomCategory = new()
    {
        {"FesNy", 102}, // New Years
        {"FesVl", 103}, // Valentines
        {"FesPd", 104}, // Ladies Day
        {"FesEs", 105}, // Easter
        {"FesSu", 106}, // Summer
        {"FesAn", 107}, // Rising
        {"FesHl", 109}, // Halloween
        {"FesXm", 110}, // Xmas
    };

    private static readonly string FileName = "ItemIdToEvent.csv";

    // Helper function, gets more localized event name for quests based on Id
    private string GetLocEventName(Quest quest)
    {
        // fallback journal classification
        var fallback = quest.JournalGenre.Value.Name.ToString();
        // try lookup as cash shop category first (easier)
        foreach (var entry in QuestIdStartToFittingRoomCategory)
        {
            if (quest.Id.ToString().StartsWith(entry.Key))
            {
                return ExcelCache<FittingShopCategory>.GetSheet().GetRow(entry.Value).GetValueOrDefault().Name.ToString() ?? fallback;
            }
        }
        // try lookup as armoire sub category (harder)
        foreach(var entry in QuestIdStartToCabinetSubCategory)
        {
            if(quest.Id.ToString().StartsWith(entry.Key))
            {
                return ExcelCache<CabinetSubCategory>.GetSheet().GetRow(entry.Value).GetValueOrDefault().Name.ToString() ?? fallback;
            }
        }
        return fallback;
    }
    
    protected override void InitializeData()
    {
        // Armoire Items
        foreach (var entry in ExcelCache<Cabinet>.GetSheet())
        {
            switch (entry.Category.RowId)
            {
                // seasonal & collab events
                case 2:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                    AddEntry(entry.Item.RowId, entry.SubCategory.Value.Name.ToString());
                    break;
            }
        }
        // Quests marked as collab/event quests
        foreach (var entry in ExcelCache<Quest>.GetSheet())
        {
            if(QuestExecutor.IsEventQuest(entry))
            {
                foreach(var item in QuestsDataGenerator.GetQuestRewards(entry))
                {
                    if(!data.ContainsKey(item.RowId))
                        AddEntry(item.RowId, GetLocEventName(entry));
                }
                // Event shops associated with said events
                foreach(var shop in ExcelCache<SpecialShop>.GetSheet().Where(shop => shop.RequiredFestival.RowId == entry.Festival.RowId))
                {
                    foreach(var shopEntry in shop.Item)
                    {
                        foreach(var item in shopEntry.ReceiveItems)
                        {
                            if(!data.ContainsKey(item.Item.RowId))
                                AddEntry(item.Item.RowId, GetLocEventName(entry));
                        }
                    }
                }
            }
        }
        // Generate event 
        var resourceData = CSVHandler.Load<ItemIdToSource>(FileName);
        foreach(var entry in resourceData)
        {
            if (entry.SourceDescription != "" && !data.ContainsKey(entry.ItemId))
                AddEntry(entry.ItemId, entry.SourceDescription);
        }
    }
}
