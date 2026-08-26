using Dalamud.Interface.Windowing;
using System.Collections;
using System.Reflection;

namespace Collections;

public sealed class DebugWindow : Window, IDisposable
{
    private string mountRowInput = "264";
    private string itemRowInput = "36006";
    private List<(string Name, string Value)> columns = new();
    private List<(string Name, string Value)> itemColumns = new();
    private string status = "Enter a Mount sheet row and press Dump.";

    public DebugWindow() : base("Collections Debug")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(700, 400),
            MaximumSize = new Vector2(2000, 1400)
        };
    }

    public override void Draw()
    {
        ImGui.SetNextItemWidth(180);
        ImGui.InputText("Mount sheet row", ref mountRowInput, 16);
        ImGui.SameLine();

        if (ImGui.Button("Dump"))
        {
            DumpMount();
        }

        ImGui.SameLine();
        ImGui.TextUnformatted(status);
        ImGui.Separator();

        ImGui.SetNextItemWidth(180);
        ImGui.InputText("Item row", ref itemRowInput, 16);
        ImGui.SameLine();

        if (ImGui.Button("Dump Item"))
        {
            DumpItem();
        }

        ImGui.Separator();

        if (columns.Count == 0 && itemColumns.Count == 0)
        {
            ImGui.TextUnformatted("No row dumped.");
            return;
        }

        if (ImGui.BeginChild("debug-columns", new Vector2(0, 0), true))
        {
            if (columns.Count > 0)
            {
                ImGui.TextUnformatted("Mount row");
                ImGui.Separator();
                DrawColumns(columns);
            }

            if (itemColumns.Count > 0)
            {
                ImGui.Spacing();
                ImGui.TextUnformatted("Item row");
                ImGui.Separator();
                DrawColumns(itemColumns);
            }

            ImGui.EndChild();
        }
    }

    private static void DrawColumns(List<(string Name, string Value)> values)
    {
        foreach (var (name, value) in values)
        {
            ImGui.TextUnformatted(name);
            ImGui.SameLine(260);
            ImGui.TextUnformatted(value);
        }
    }

    private void DumpMount()
    {
        columns.Clear();

        if (!uint.TryParse(mountRowInput, out var rowId))
        {
            status = "Invalid row ID.";
            return;
        }

        var mount = ExcelCache<Mount>.GetSheet().GetRow(rowId);
        if (mount is null)
        {
            status = $"Mount row {rowId} was not found.";
            return;
        }

        var row = mount.Value;
        foreach (var property in row.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            try
            {
                columns.Add((property.Name, FormatValue(property.GetValue(row))));
            }
            catch (Exception exception)
            {
                columns.Add((property.Name, $"<error: {exception.GetType().Name}>"));
            }
        }

        Services.PluginLog.Information("Mount row {RowId} dump:\n{Columns}", rowId,
            string.Join(Environment.NewLine, columns.Select(column => $"{column.Name}: {column.Value}")));
        status = $"Dumped Mount row {rowId}.";
    }

    private void DumpItem()
    {
        itemColumns.Clear();

        if (!uint.TryParse(itemRowInput, out var rowId))
        {
            status = "Invalid item row ID.";
            return;
        }

        var item = ExcelCache<Item>.GetSheet().GetRow(rowId);
        if (item is null)
        {
            status = $"Item row {rowId} was not found.";
            return;
        }

        var row = item.Value;
        AddColumns(itemColumns, row);

        var action = row.ItemAction.Value;
        itemColumns.Add(("ItemAction.Action", FormatValue(action.Action)));
        itemColumns.Add(("ItemAction.Data", FormatValue(action.Data)));
        for (var index = 0; index < action.Data.Count; index++)
        {
            itemColumns.Add(($"ItemAction.Data[{index}]", action.Data[index].ToString()));
        }

        LogDump("Item", rowId, itemColumns);
        status = $"Dumped Item row {rowId}.";
    }

    private static void AddColumns<T>(List<(string Name, string Value)> target, T row)
    {
        foreach (var property in row!.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            try
            {
                target.Add((property.Name, FormatValue(property.GetValue(row))));
            }
            catch (Exception exception)
            {
                target.Add((property.Name, $"<error: {exception.GetType().Name}>"));
            }
        }
    }

    private static void LogDump(string sheet, uint rowId, List<(string Name, string Value)> values)
    {
        Services.PluginLog.Information("{Sheet} row {RowId} dump:\n{Columns}", sheet, rowId,
            string.Join(Environment.NewLine, values.Select(column => $"{column.Name}: {column.Value}")));
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is string or ReadOnlyMemory<char>)
        {
            return value.ToString() ?? string.Empty;
        }

        if (value is IEnumerable enumerable and not string)
        {
            var values = new List<string>();
            foreach (var entry in enumerable)
            {
                values.Add(FormatValue(entry));
            }

            return $"[{string.Join(", ", values)}]";
        }

        var type = value.GetType();
        var rowId = type.GetProperty("RowId")?.GetValue(value);
        if (rowId is not null)
        {
            var name = type.GetProperty("Name")?.GetValue(value);
            return name is null ? $"RowId={rowId}" : $"RowId={rowId}, Name={name}";
        }

        return value.ToString() ?? string.Empty;
    }

    public void Dispose()
    {
    }
}
