using Dalamud.Interface.Windowing;

namespace Collections;

public class WindowsInitializer
{

    public WindowSystem WindowSystem { get; init; }
    public MainWindow MainWindow { get; init; }
    public DebugWindow DebugWindow { get; init; }

    public WindowsInitializer()
    {
        // Attach windows
        MainWindow = new MainWindow();
        DebugWindow = new DebugWindow();
        WindowSystem = new WindowSystem(Services.Plugin.NameSpace);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(DebugWindow);

        // Attach draw functions
        Services.PluginInterface.UiBuilder.Draw += DrawUI;
        Services.PluginInterface.UiBuilder.OpenMainUi += DrawConfigUI;
        Services.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    }

    public void DrawUI()
    {
        WindowSystem.Draw();
    }

    public void DrawConfigUI()
    {
        MainWindow.IsOpen = true;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();
        DebugWindow.Dispose();
    }
}
