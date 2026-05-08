namespace TaskManagement.View;

public partial class DraftTaskPage : ContentPage
{
    public DraftTaskPage(DraftTaskRecordViewModel viewModel)
    {
        InitializeComponent();
        double screenHeight = DeviceDisplay.MainDisplayInfo.Height;
#if ANDROID
        screenHeight /= DeviceDisplay.MainDisplayInfo.Density;
#elif WINDOWS
#endif

        TaskDataGrid.HeightRequest = screenHeight * 0.68;
        BindingContext = viewModel;
    }


    public DraftTaskPage()
    {
    }
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
    }
    public ObservableCollection<TaskRecord> DraftTaskRecords { get; } = new();
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var viewmodel1 = BindingContext as DraftTaskRecordViewModel;

        viewmodel1.IsQuickTask = GlobalVariables.IsQuckTaskVisible;
        viewmodel1.IsCompletedTaskVisible = true;
        if (viewmodel1?.LoadUsersCommand.CanExecute(null) == true)
        {
            viewmodel1.LoadUsersCommand.Execute(null);
        }
        if (viewmodel1?.GetDraftTaskRecordCommand.CanExecute(null) == true)
        {
            viewmodel1.GetDraftTaskRecordCommand.Execute(null);
        }

    }

    private void ContentPage_NavigatedTo(object sender, NavigatedToEventArgs e)
    {
        var vm = BindingContext as DraftTaskRecordViewModel;
        vm?.GetDraftTaskRecordCommand.Execute(null);
    }


    private async void OnItemTapped(object sender, SelectionChangedEventArgs e)
    {
        var selectedTask = e.CurrentSelection.FirstOrDefault() as TaskRecord;
        if (selectedTask == null) return;
        await Task.Delay(350);
        await Shell.Current.GoToAsync(nameof(DetailsPage), true, new Dictionary<string, object>
    {
        { "TaskRecord", selectedTask }
    });

        ((CollectionView)sender).SelectedItem = null; // clear selection
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        var viewModel = BindingContext as DraftTaskRecordViewModel;
        await viewModel.GetDraftTaskRecordCommand.ExecuteAsync(null);
    }

    private async void OnFloatingButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new DetailsPage()); // Navigate to Task Entry Page
    }
}