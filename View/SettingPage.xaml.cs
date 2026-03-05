namespace TaskManagement.View;

public partial class SettingPage : ContentPage
{
    public SettingPage(SettingViewModel viewModel)
    {
        this.InitializeComponent();
        BindingContext = viewModel;

    }

    public SettingPage()
    {
    }

    protected override async void OnAppearing()

    {
        base.OnAppearing();
        var viewmodel1 = BindingContext as SettingViewModel;

        if (viewmodel1?.LoadUsersCommand.CanExecute(null) == true)
        {
            viewmodel1.LoadUsersCommand.Execute(null);
        }

    }
}