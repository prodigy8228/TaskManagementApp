using TaskManagement.View;

namespace TaskManagement;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(TaskTypePage), typeof(TaskTypePage));
        Routing.RegisterRoute(nameof(DraftTaskPage), typeof(DraftTaskPage));
        Routing.RegisterRoute(nameof(BackupRestorePage), typeof(BackupRestorePage));
        Routing.RegisterRoute(nameof(SettingPage), typeof(SettingPage));
        Routing.RegisterRoute(nameof(DetailsPage), typeof(DetailsPage));
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        pageTitleLabel.Text = Shell.Current?.CurrentPage?.Title;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

    }
}
