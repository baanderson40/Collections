using Lumina.Extensions;

namespace Collections;

public class KeysDataGenerator
{
    public readonly Dictionary<Type, Dictionary<uint, Item>> collectibleIdToItem = new();
    public readonly Dictionary<Type, Dictionary<uint, Quest>> collectibleIdToQuest = new();
    public readonly Dictionary<Type, Dictionary<uint, ContentFinderCondition>> collectibleIdToInstance = new();
    public readonly Dictionary<Type, Dictionary<uint, Achievement>> collectibleIdToAchievement = new();
    public readonly Dictionary<Type, Dictionary<uint, string>> collectibleIdToMisc = new();
    public Dictionary<uint, Monster> ActionIdToBlueSpell = new();
    public Dictionary<uint, uint> ItemIdToTripleTriadId = new();

    private static readonly int MountItemActionType = 1322;
    private static readonly int MinionItemActionType = 853;
    private static readonly int EmoteHairstyleItemActionType = 2633;
    private static readonly int TripleTriadItemActionType = 3357;
    private static readonly int BardingItemActionType = 1013;
    private static readonly int OrchestrionItemActionType = 25183;
    private static readonly int FashionAccessoryItemActionType = 20086;
    private static readonly int GlassesItemActionType = 37312;
    private static readonly int FramerKitItemActionType = 29459;

    public KeysDataGenerator()
    {
        PopulateItemData();
        PopulateQuestData();
        PopulateInstanceData();
        PopulateAchievementData();
        PopulateMiscData();
        PopulateBannerUnlockData();
        PopulateBlueSpellData();
    }

    private void PopulateItemData()
    {
        foreach (var item in ExcelCache<Item>.GetSheet())
        {
            var type = item.ItemAction.Value.Action.RowId;
            var collectibleData = item.ItemAction.Value.Data;
            var additionalData = item.AdditionalData.RowId;
            if (type == MountItemActionType)
            {
                AddCollectibleKeyEntry(collectibleIdToItem, typeof(Mount), collectibleData[0], item);
            }
            else if (type == MinionItemActionType)
            {
                AddCollectibleKeyEntry(collectibleIdToItem, typeof(Companion), collectibleData[0], item);
            }
            else if (type == EmoteHairstyleItemActionType)
            {
                AddCollectibleKeyEntry(collectibleIdToItem, typeof(Emote), collectibleData[0], item);
                AddCollectibleKeyEntry(collectibleIdToItem, typeof(CharaMakeCustomize), collectibleData[0], item);
            }
            else if (type == TripleTriadItemActionType)
            {
                AddCollectibleKeyEntry(collectibleIdToItem, typeof(TripleTriadCard), collectibleData[0], item);
                ItemIdToTripleTriadId[item.RowId] = collectibleData[0]; // Maintain reverse look up for triple triad cards
            }
            else if (type == BardingItemActionType)
            {
                AddCollectibleKeyEntry(collectibleIdToItem, typeof(BuddyEquip), collectibleData[0], item);
            }
            else if (type == OrchestrionItemActionType)
            {
                AddCollectibleKeyEntry(collectibleIdToItem, typeof(Orchestrion), item.AdditionalData.RowId, item);
            }
            else if (type == FashionAccessoryItemActionType)
            {
                AddCollectibleKeyEntry(collectibleIdToItem, typeof(Ornament), collectibleData[0], item);
            }
            else if (type == GlassesItemActionType)
            {
                AddCollectibleKeyEntry(collectibleIdToItem, typeof(Glasses), item.AdditionalData.RowId, item);
            }
            else if (type == FramerKitItemActionType)
            {
                // can't use RowId here
                BannerCondition? found = ExcelCache<BannerCondition>.GetSheet().Where(cond => cond.UnlockType1 == 9 && cond.UnlockCriteria1.First().RowId == item.AdditionalData.RowId).FirstOrNull();
                if(found != null)
                {
                    AddCollectibleKeyEntry(collectibleIdToItem, typeof(BannerCondition), found.Value.RowId, item);
                }
            }
        }
    }

    private void PopulateQuestData()
    {
        foreach (var quest in ExcelCache<Quest>.GetSheet())
        {
            var emote = quest.EmoteReward.Value;
            if (emote.RowId != 0)
            {
                AddCollectibleKeyEntry(collectibleIdToQuest, typeof(Emote), emote.UnlockLink, quest);
            }
        }

        // Emote Unlock Data
        foreach (var emote in ExcelCache<Emote>.GetSheet())
        {
            if (emote.UnlockLink > ExcelCache<Quest>.GetSheet().First().RowId && emote.UnlockLink < ExcelCache<Quest>.GetSheet().Last().RowId)
            {
                var quest = (Quest)ExcelCache<Quest>.GetSheet().GetRow(emote.UnlockLink)!;
                AddCollectibleKeyEntry(collectibleIdToQuest, typeof(Emote), emote.UnlockLink, quest);
            }
        }

        foreach (var (type, dict) in DataOverrides.collectibleIdToUnlockQuestId)
        {
            foreach (var (collectibleId, questId) in dict)
            {
                var quest = (Quest)ExcelCache<Quest>.GetSheet().GetRow(questId)!;
                AddCollectibleKeyEntry(collectibleIdToQuest, type, collectibleId, quest);
            }
        }
    }

    private void PopulateInstanceData()
    {
        foreach (var (type, dict) in DataOverrides.collectibleIdToUnlockInstanceId)
        {
            foreach (var (collectibleId, instanceId) in dict)
            {
                var instance = (ContentFinderCondition)ExcelCache<ContentFinderCondition>.GetSheet().GetRow(instanceId)!;
                AddCollectibleKeyEntry(collectibleIdToInstance, type, collectibleId, instance);
            }
        }
    }

    private void PopulateAchievementData()
    {
        foreach (var (type, dict) in DataOverrides.collectibleIdToUnlockAchievementId)
        {
            foreach (var (collectibleId, achievementId) in dict)
            {
                var achievement = (Achievement)ExcelCache<Achievement>.GetSheet().GetRow(achievementId)!;
                AddCollectibleKeyEntry(collectibleIdToAchievement, type, collectibleId, achievement);
            }
        }
    }

    private void PopulateMiscData()
    {
        foreach (var (type, dict) in DataOverrides.collectibleIdToUnlockMisc)
        {
            foreach (var (collectibleId, misc) in dict)
            {
                AddCollectibleKeyEntry(collectibleIdToMisc, type, collectibleId, misc);
            }
        }
    }

    private void PopulateBannerUnlockData()
    {
        foreach (var cond in ExcelCache<BannerCondition>.GetSheet())
        {
            if (cond.UnlockType1 == 1 && cond.UnlockType2 == 2)
            {
                    var quest = cond.UnlockCriteria1.First().GetValueOrDefault<Quest>();
                    if (quest is not null)
                        AddCollectibleKeyEntry(collectibleIdToQuest, typeof(BannerCondition), cond.RowId, quest.Value);
            }
            else if (cond.UnlockType1 == 4)
            {
                var instance = cond.UnlockCriteria1.First().GetValueOrDefault<InstanceContent>();
                if (instance is not null)
                    AddCollectibleKeyEntry(collectibleIdToInstance, typeof(BannerCondition), cond.RowId, instance.Value.ContentFinderCondition.Value);
            }
            else if (cond.UnlockType1 == 11)
            {
                AddCollectibleKeyEntry(collectibleIdToMisc, typeof(BannerCondition), cond.RowId, "Crystalline Conflict Season Reward");
            }
        }
    }

    private static readonly string BlueSpellsFileName = "BlueSpells.csv";
    private void PopulateBlueSpellData()
    {
        var data = CSVHandler.Load<BlueSpell>(BlueSpellsFileName);
        ActionIdToBlueSpell = data
            .GroupBy(entry => entry.ActionId)
            .ToDictionary(kv => kv.Key, kv =>
            {
                var blueSpell = kv.First();
                return new Monster()
                {
                    name = blueSpell.MobDescription,
                    LocationDescription = blueSpell.LocationDescription,
                    dutyId = blueSpell.DutyId,
                    territoryId = blueSpell.TerritoryId,
                    X = blueSpell.X,
                    Y = blueSpell.Y,
                };
            });
    }

    private void AddCollectibleKeyEntry<T>(Dictionary<Type, Dictionary<uint, T>> dict, Type type, uint id, T entry)
    {
        if (!dict.ContainsKey(type))
        {
            dict[type] = new Dictionary<uint, T>();
        }
        dict[type][id] = entry;
    }
}

public class BlueSpell
{
    public uint ActionId { get; set; }
    public string MobDescription { get; set; } = null!;
    public string LocationDescription { get; set; } = null!;
    public uint? DutyId { get; set; }
    public uint? TerritoryId { get; set; }
    public float? X { get; set; }
    public float? Y { get; set; }
}
