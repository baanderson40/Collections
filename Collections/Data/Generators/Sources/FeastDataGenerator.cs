namespace Collections;

public class FeastDataGenerator : BaseDataGenerator<string>
{
    private const string FileName = "ItemIdToFeast.csv";

    protected override void InitializeData()
    {
        var resourceData = CSVHandler.Load<FeastReward>(FileName);
        foreach (var entry in resourceData)
        {
            AddEntry(entry.ItemId, $"Rewarded from Season {entry.Season} of The Feast");
        }
    }
}

public class FeastReward
{
    public uint ItemId { get; set; }
    public uint Season { get; set; }
}
