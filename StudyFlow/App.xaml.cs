using StudyFlow.Services;

namespace StudyFlow;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        _ = StudyDataStore.EnsureLoadedAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
