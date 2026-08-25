using Collections.Executors;

namespace Collections;

public class InstanceSource : CollectibleSource
{
    private ContentFinderCondition ContentFinderConditionInternal { get; init; }
    public InstanceSource(ContentFinderCondition contentFinderCondition)
    {
        ContentFinderConditionInternal = contentFinderCondition;
    }

    public override string GetName()
    {
        return ContentFinderConditionInternal.Name.ToString();
    }

    private List<SourceCategory> sourceType = null!;
    public override List<SourceCategory> GetSourceCategories()
    {
        if (sourceType != null)
        {
            return sourceType;
        }

        sourceType = new List<SourceCategory>();
        var contentType = ContentFinderConditionInternal.ContentType;
        switch (contentType.Value.RowId)
        {
            case 6:
                sourceType.Add(SourceCategory.PvP);
                break;
            case 9:
                sourceType.Add(SourceCategory.TreasureHunts);
                break;
            case 13:
                sourceType.Add(SourceCategory.BeastTribes);
                break;
            case 21:
                sourceType.Add(SourceCategory.DeepDungeon);
                break;
            default:
                sourceType.Add(SourceCategory.Duty);
                break;
        }
        return sourceType;
    }

    public override bool GetIslocatable()
    {
        return true;
    }

    public override void DisplayLocation()
    {
        DutyFinderOpener.OpenRegularDuty(ContentFinderConditionInternal.RowId);
    }

    public static int defaultIconId = 061801;
    protected override int GetIconId()
    {
        var contentType = ContentFinderConditionInternal.ContentType.ValueNullable;
        var contentIconId = contentType?.Icon ?? 0;
        if (contentIconId == 0)
        {
            return defaultIconId;
        }
        else
        {
            return (int)contentIconId;
        }
    }

    public override InstanceSource Clone()
    {
        return new InstanceSource(ContentFinderConditionInternal);
    }
}
