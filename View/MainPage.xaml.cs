namespace TaskManagement.View;

public partial class MainPage : ContentPage
{
    public MainPage(TaskRecordViewModel viewModel)
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


    public MainPage()
    {
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
    }
    public ObservableCollection<TaskRecord> TaskRecords { get; } = new();
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var viewmodel1 = BindingContext as TaskRecordViewModel;

        viewmodel1.IsQuickTask = GlobalVariables.IsQuckTaskVisible;
        viewmodel1.IsCompletedTaskVisible = true;
        if (viewmodel1?.LoadUsersCommand.CanExecute(null) == true)
        {
            viewmodel1.LoadUsersCommand.Execute(null);
        }
        if (viewmodel1?.GetTaskRecordCommand.CanExecute(null) == true)
        {
            viewmodel1.GetTaskRecordCommand.Execute(null);
        }

    }

    private void ContentPage_NavigatedTo(object sender, NavigatedToEventArgs e)
    {
        var vm = BindingContext as TaskRecordViewModel;
        vm?.GetTaskRecordCommand.Execute(null);
    }


    private async void OnItemTapped(object sender, SelectionChangedEventArgs e)
    {
        var selectedTask = e.CurrentSelection.FirstOrDefault() as TaskRecord;

        if (selectedTask == null) return;

        if (!string.IsNullOrEmpty(selectedTask.pending_description))
        {
            // await DisplayAlert("Pending Task", selectedTask.pending_description, "OK");
            //return;
        }
        else
        {
            selectedTask.pending_description = "";
        }
        await Task.Delay(350);
        await Shell.Current.GoToAsync(nameof(DetailsPage), true, new Dictionary<string, object>
    {
        { "TaskRecord", selectedTask }
    });

        ((CollectionView)sender).SelectedItem = null; // clear selection
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        taskSubmitButton.IsEnabled = !string.IsNullOrWhiteSpace(taskTextEntry.Text);
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        var viewModel = BindingContext as TaskRecordViewModel;
        await viewModel.GetTaskRecordCommand.ExecuteAsync(null);
    }
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var viewModel = BindingContext as TaskRecordViewModel;

        /*        if (searchBar.Text.Length == 0)
                {

        #if ANDROID
                    searchBar.WidthRequest = 80;
        #elif WINDOWS
                    searchBar.WidthRequest = 400;
        #endif
                }
                else
                {
        #if ANDROID
                    var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
                    var ScreenWidthDp = displayInfo.Width / displayInfo.Density;
                    var extraWidth = ScreenWidthDp; // Adjust multiplier as needed
                    searchBar.WidthRequest = extraWidth - 30 - 25;
        #elif WINDOWS
                    double screenWidth = DeviceDisplay.MainDisplayInfo.Width;
                    var extraWidth = screenWidth; // Adjust multiplier as needed
                    searchBar.WidthRequest = extraWidth - 60 - 10;
        #endif
                }
        */
        viewModel?.SearchTaskRecordCommand.Execute(e.NewTextValue);
    }

    private async void OnOpenSearchClicked(object sender, EventArgs e)
    {
        // A tiny delay is necessary to allow the SearchBar to become visible 
        // before the focus command is sent.
        await Task.Delay(100);

        if (searchBar != null)
        {
            searchBar.Focus();
        }
    }
    private async void OnFloatingButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new DetailsPage()); // Navigate to Task Entry Page
    }

}