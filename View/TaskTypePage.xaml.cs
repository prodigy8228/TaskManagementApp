namespace TaskManagement.View;

public partial class TaskTypePage : ContentPage
{
    public TaskTypePage(TaskTypeViewModel viewModel)
    {
        InitializeComponent();
        double screenHeight = DeviceDisplay.MainDisplayInfo.Height;
#if ANDROID
        screenHeight = screenHeight / DeviceDisplay.MainDisplayInfo.Density;

#elif WINDOWS
           // searchBar.WidthRequest = 400;
#endif

        TaskTypeDataGrid.HeightRequest = screenHeight * 0.68;
        BindingContext = viewModel;
    }

    public TaskTypePage()
    {
    }
}