namespace Collections;

public class Monster
{
    public string name = string.Empty;
    public string LocationDescription = string.Empty;
    public uint? territoryId;
    public uint? dutyId;
    public float? X;
    public float? Y;

    public Location? GetLocationEntry()
    {
        if (territoryId is null || X is null || Y is null)
        {
            return null;
        }

        return new Location((TerritoryType)ExcelCache<TerritoryType>.GetSheet().GetRow((uint)territoryId)!, (float)X, (float)Y);
    }
}

