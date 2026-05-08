using CommunityToolkit.Maui.Views;

namespace TaskManagement.View;

public partial class DetailsPage : ContentPage
{
    public DetailsPage(TaskDetailsViewModel viewModel)
    {
        this.InitializeComponent(); // Explicitly use 'this' to resolve ambiguity.
        BindingContext = viewModel;
        double screenHeight = DeviceDisplay.MainDisplayInfo.Height;
#if ANDROID
        screenHeight = screenHeight / DeviceDisplay.MainDisplayInfo.Density;
#elif WINDOWS
#endif

        detailFrame.HeightRequest = screenHeight * 0.79;
    }

    public DetailsPage()
    {

    }

    public static bool IsReturning = false;

    public ObservableCollection<TaskRecord> TaskRecords { get; } = new();
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var viewmodel1 = BindingContext as TaskDetailsViewModel;

        if (viewmodel1 != null && viewmodel1.TaskRecord != null)
        {
            viewmodel1.IsCompleted = viewmodel1.TaskRecord?.IsCompleted ?? false;
            //  viewmodel1.SelectedDate = viewmodel1.TaskRecord?.task_due_date?.DateTime ?? DateTime.Today;
            viewmodel1.Filename_image = viewmodel1.TaskRecord?.file_name_image ?? "";
            viewmodel1.Filename_video = viewmodel1.TaskRecord?.file_name_video ?? "";
            viewmodel1.Description = viewmodel1.TaskRecord?.task_description ?? "";
            viewmodel1.PendingDescription = viewmodel1.TaskRecord?.pending_description ?? string.Empty;
            viewmodel1.TaskTitle = viewmodel1.TaskRecord?.task_title ?? "";
            viewmodel1.ImageFileExist = viewmodel1.TaskRecord != null && !string.IsNullOrEmpty(viewmodel1.Filename_image) && !viewmodel1.Filename_image.Equals("No File Selected");
            //  viewmodel1.VideoFileExist = viewmodel1.TaskRecord != null && !string.IsNullOrEmpty(viewmodel1.Filename_video) && !viewmodel1.Filename_video.Equals("No File Selected");
        }
        if (viewmodel1?.LoadUsersCommand.CanExecute(null) == true)
        {
            viewmodel1.LoadUsersCommand.Execute(null);
        }
        if (viewmodel1?.LoadAssigneeCommand.CanExecute(null) == true)
        {
            viewmodel1.LoadAssigneeCommand.Execute(null);
        }
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        IsReturning = true; // Set flag when navigating back
        StackLayout targetContainer = null;
        targetContainer = ImageContainer1;
        var childToRemove = targetContainer.Children.FirstOrDefault(c => c is Image);
        if (childToRemove != null)
        {
            targetContainer.Children.Remove(childToRemove);
        }
    }

    private void OnViewIconClicked(object sender, EventArgs e)
    {
        StackLayout targetContainer = null;
        targetContainer = ImageContainer1;

        if (targetContainer != null)
        {
            var viewmodel1 = BindingContext as TaskDetailsViewModel;
            var image = new Image
            {
                WidthRequest = 200,
                HeightRequest = 200
            };
            if (viewmodel1.TaskRecord.file_data_image == "" && viewmodel1.TaskRecord.file_data_image1 != null)
            {
                image.Source = ImageSource.FromStream(() => new MemoryStream(viewmodel1.TaskRecord.file_data_image1));
            }
            else
            {
                image.Source = viewmodel1.TaskRecord.file_data_image;
            }

            if (targetContainer.Children.Count <= 0)
            {
                targetContainer.Children.Add(image); // Insert image below the correct button
            }
            else
            {
                var childToRemove = targetContainer.Children.FirstOrDefault(c => c is Image);
                if (childToRemove != null)
                {
                    targetContainer.Children.Remove(childToRemove);
                }
            }
        }
    }

    private void OnViewVideoIconClicked(object sender, EventArgs e)
    {
        StackLayout targetContainer = null;
        targetContainer = VideoContainer1;

        if (targetContainer != null)
        {
            var viewmodel1 = BindingContext as TaskDetailsViewModel;
            //  string tempFilePath = Path.Combine(FileSystem.CacheDirectory, "tempVideo.mp3");
            //  File.WriteAllBytes(tempFilePath, viewmodel1.TaskRecord.file_data_video); // Save byte array to file

            var mediaElement = new MediaElement
            {
                Source = MediaSource.FromFile(viewmodel1.TaskRecord.file_data_video),

                HeightRequest = 200
            };

            if (targetContainer.Children.Count <= 0)
            {
                targetContainer.Children.Add(mediaElement); // Insert image below the correct button
            }
            else
            {
                var childToRemove = targetContainer.Children.FirstOrDefault(c => c is MediaElement);
                if (childToRemove != null)
                {
                    targetContainer.Children.Remove(childToRemove);
                }

            }

        }
    }
}

