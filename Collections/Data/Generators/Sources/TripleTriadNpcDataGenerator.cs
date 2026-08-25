namespace Collections;

public class TripleTriadNpcDataGenerator : BaseDataGenerator<ENpcResident>
{
    protected override void InitializeData()
    {
        foreach (var tripleTriadCardResident in ExcelCache<TripleTriadCardResident>.GetSheet())
        {
            if (tripleTriadCardResident.AcquisitionType.RowId == 6 || tripleTriadCardResident.AcquisitionType.RowId == 10)
            {
                if (tripleTriadCardResident.Acquisition.RowId != 0)
                {
                    var npc = ExcelCache<ENpcResident>.GetSheet().GetRow(tripleTriadCardResident.Acquisition.RowId);
                    if (npc is not null)
                        AddEntry(tripleTriadCardResident.RowId, npc.Value);
                }
            }
        }
    }
}

