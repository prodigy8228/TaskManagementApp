#if ANDROID
//using AndroidX.Media3.Common;
// No other changes are needed as the error is caused by the missing namespace.
using Android.Content;
using Android.Provider;
using Microsoft.Maui.Controls;
using System.Globalization; // Add this namespace at the top of the file
using System.Threading.Tasks;
using TaskManagement.Platforms;
#endif
namespace TaskManagement.ViewModel;

[QueryProperty(nameof(TaskRecord), "TaskRecord")]
public partial class TaskDetailsViewModel : BaseViewModel
{
    public bool IsAndroid => DeviceInfo.Platform == DevicePlatform.Android;
    public bool IsWindows => DeviceInfo.Platform == DevicePlatform.WinUI;

    public List<RepeatOption> RepeatOptions { get; } =
         Enum.GetValues(typeof(RepeatOption)).Cast<RepeatOption>().ToList();


    readonly ISpeechToText speechToText;
    readonly CancellationTokenSource tokenSource;
    MISDatabase taskService;

    //private DateTime _selectedDate = DateTime.Now;
    private DateTime _selectedDate;
    public ObservableCollection<TaskRecord> TaskRecords { get; set; }

    [ObservableProperty]
    private TaskRecord _taskRecord;
    public Boolean isAdmin { get; set; } = false;
    public ObservableCollection<CommentRecord> Comments { get; set; }
    public TaskDetailsViewModel()
    {
        this.TaskService = new MISDatabase();
        if (GlobalVariables.userid == "Admin")
        {
            isAdmin = true;
        }
        else
        {
            isAdmin = false;
        }
        this.speechToText = DependencyService.Get<ISpeechToText>();
#if ANDROID
        speechToText = new SpeechToTextImplementation();
#endif
        this.tokenSource = new CancellationTokenSource();
        this.Description = TaskRecord?.task_description ?? string.Empty;
        this.TaskTitle = TaskRecord?.task_title ?? string.Empty;
        this.IsCompleted = TaskRecord?.IsCompleted ?? false;
    }
    [ObservableProperty]
    string? description;

    [ObservableProperty]
    string? taskTitle;

    [RelayCommand]
    async Task ListenDescriptionAsync()
    {
#if ANDROID
        try
        {
            var isAuthorized = await speechToText.RequestPermissions();
            //  var isAuthorized=true;
            if (isAuthorized)
            {
                try
                {
                    Description = await speechToText.Listen(CultureInfo.GetCultureInfo("en-us"),
                        new Progress<string>(partialText =>
                        {
                            if (DeviceInfo.Platform == DevicePlatform.Android)
                            {
                                Description = partialText;
                            }
                            else
                            {
                                Description += partialText + " ";
                            }

                            OnPropertyChanged(nameof(Description));
                        }), tokenSource.Token);
                    TaskRecord.task_description = Description;
                }
                catch (Exception ex)
                {
                    // await DisplayAlert("Error", ex.Message, "OK");
                }
            }
            else
            {
                //  await DisplayAlert("Permission Error", "No microphone access", "OK");
            }
        }
        catch (Exception ex)
        {
            //await DisplayAlert("Error", ex.Message, "OK");
        }
#endif
    }

    [RelayCommand]
    async Task ListenTitleAsync()
    {
#if ANDROID
        try
        {
            var isAuthorized = await speechToText.RequestPermissions();
            //  var isAuthorized=true;
            if (isAuthorized)
            {
                try
                {
                    TaskTitle = await speechToText.Listen(CultureInfo.GetCultureInfo("en-us"),
                        new Progress<string>(partialText =>
                        {
                            if (DeviceInfo.Platform == DevicePlatform.Android)
                            {
                                TaskTitle = partialText;
                            }
                            else
                            {
                                TaskTitle += partialText + " ";
                            }

                            OnPropertyChanged(nameof(TaskTitle));
                        }), tokenSource.Token);
                    TaskRecord.task_title = TaskTitle;
                }
                catch (Exception ex)
                {
                    // await DisplayAlert("Error", ex.Message, "OK");
                }
            }
            else
            {
                //  await DisplayAlert("Permission Error", "No microphone access", "OK");
            }
        }
        catch (Exception ex)
        {
            //await DisplayAlert("Error", ex.Message, "OK");
        }
#endif
    }


    [RelayCommand]
    async Task ShareAsync()
    {
#if ANDROID
        try
        {
            string taskDetails = $"Task: {TaskRecord.task_title}\nDescription: {TaskRecord.task_description}\nDue Date: {TaskRecord.task_due_date}";

            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Text = taskDetails,
                Title = "Share Task"
            });

        }
        catch (Exception ex)
        {
            //await DisplayAlert("Error", ex.Message, "OK");
        }
#endif
    }


    [RelayCommand]
    async Task ListenCancelAsync()
    {
        tokenSource?.Cancel();
    }

    public DateTime? SelectedDate
    {
        get => SelectedDate1;
        set
        {
            if (SelectedDate1 != value)
            {
                SelectedDate1 = value ?? DateTime.Today;

                OnPropertyChanged();

                if (TaskRecord != null)
                {
                    TaskRecord.task_due_date = SelectedDate1;
                }
            }

        }
    }
    public ObservableCollection<TaskType> TaskTypes { get; set; } = new ObservableCollection<TaskType>();

    [RelayCommand]
    async Task LoadUsersAsync()
    {
        var taskTypes = await taskService.GetTaskTypesAsync(); // Fetch users from database/API
        taskTypes = taskTypes.Where(o => o.sort_order == 1).ToList();


        TaskTypes.Clear();
        foreach (var tasktype in taskTypes)
        {
            TaskTypes.Add(tasktype); // Populate the dropdown dynamically
        }
        SelectedTaskType = TaskRecord?.task_type_id == null
            ? taskTypes.FirstOrDefault()
            : taskTypes.FirstOrDefault(u => u.task_type_id == TaskRecord.task_type_id);
    }
    public TaskType SelectedTaskType1 { get => _selectedTaskType; set => _selectedTaskType = value; }
    private TaskType _selectedTaskType;
    public TaskType SelectedTaskType
    {
        get => SelectedTaskType1;
        set
        {
            SelectedTaskType1 = value;
            OnPropertyChanged();
            // Update TaskRecord.Assignee when user selection changes
            if (TaskRecord != null && SelectedTaskType1 != null)
            {
                TaskRecord.task_type_id = SelectedTaskType1?.task_type_id ?? 2;
            }
        }
    }
    [ObservableProperty]
    private bool isCompleted;

    [RelayCommand]
    async Task SaveDetailsAsync()
    {
        if (TaskRecord == null)
            return;
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            TaskRecord.task_title = TaskTitle;
            TaskRecord.task_description = Description;
            TaskRecord.task_type_id = SelectedTaskType?.task_type_id ?? 2;
            TaskRecord.IsCompleted = IsCompleted;
            Console.WriteLine($"TaskRecord.task_id: {TaskRecord.task_id}");
            if (TaskRecord.task_id == 0)
            {
                var response = await taskService.SaveItemAsync(TaskRecord);
                if (response == 1)
                {
                    Console.WriteLine("Task added successfully!");
                }
                else
                {
                    Console.WriteLine("Failed to add task.");
                }
            }
            else
            {
                Console.WriteLine(TaskRecord.IsCompleted.ToString() + " ......");
                var response = await taskService.UpdateItemAsync(TaskRecord);
                if (response == 1)
                {
                    Console.WriteLine("Task updated successfully!");
                }
                else
                {
                    Console.WriteLine("Failed to update task.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to update tasks: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
        // TaskRecords = new ObservableCollection<TaskRecord>(await taskService.GetTaskRecords());
        //  IsCompleted = false; // Reset completion status after saving
        await Shell.Current.GoToAsync("..");
    }



    [ObservableProperty]
    String filename_image;
    [ObservableProperty]
    bool imageFileExist;

    [ObservableProperty]
    String filename_video;
    [ObservableProperty]
    bool videoFileExist;

    [RelayCommand]
    async Task AttachFileAsync()
    {
#if ANDROID
        string action = await Shell.Current.DisplayActionSheet(
                    "Select Image Source", "Cancel", null, "Camera", "Gallery");

        FileResult fileResult = null;

        if (action == "Camera")
        {
            if (MediaPicker.Default.IsCaptureSupported)
                fileResult = await MediaPicker.Default.CapturePhotoAsync();
        }
        else if (action == "Gallery")
        {
            fileResult = await MediaPicker.Default.PickPhotoAsync();
        }

        if (fileResult != null)
        {
            var fileInfo = new FileInfo(fileResult.FullPath);
            long sizeInBytes = fileInfo.Length;

            if (sizeInBytes > 2000 * 1024)
            {
                await Shell.Current.DisplayAlert("File Too Large", "Please select a file smaller than 2 MB.", "OK");
                return;
            }


            using var stream = await fileResult.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            TaskRecord.file_data_image = ms.ToArray();

            Filename_image = fileResult.FileName;
            TaskRecord.file_name_image = Filename_image;
            ImageFileExist = true;
            OnPropertyChanged(nameof(Filename_image));
            OnPropertyChanged(nameof(ImageFileExist));
        }

#elif WINDOWS
        var file = await FilePicker.Default.PickAsync();
        Filename_image = "No File Selected";
        if (file != null)
        {
            var fileInfo = new FileInfo(file.FullPath);
            long sizeInBytes = fileInfo.Length;

            if (sizeInBytes > 2000 * 1024)
            {
                await Shell.Current.DisplayAlert("File Too Large", "Please select a file smaller than 2 MB.", "OK");
                return;
            }

            Filename_image = file.FileName;
            TaskRecord.file_name_image = file.FileName;
            using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            TaskRecord.file_data_image = memoryStream.ToArray(); // Store file data
            ImageFileExist = true;
            OnPropertyChanged(nameof(Filename_image));
            OnPropertyChanged(nameof(ImageFileExist));
        }
#endif
    }

    [RelayCommand]
    async Task AttachVideoFileAsync()
    {
#if ANDROID
        string action = await Shell.Current.DisplayActionSheet(
            "Select Video Source", "Cancel", null, "Camera", "Gallery");

        FileResult fileResult = null;

        if (action == "Camera")
        {
            if (MediaPicker.Default.IsCaptureSupported)
                fileResult = await MediaPicker.Default.CaptureVideoAsync();
        }
        else if (action == "Gallery")
        {
            fileResult = await MediaPicker.Default.PickVideoAsync();
        }

        Filename_video = "No File Selected";
        if (fileResult != null)
        {
            var fileInfo = new FileInfo(fileResult.FullPath);
            long sizeInBytes = fileInfo.Length;

            if (sizeInBytes > 2000 * 1024)
            {
                await Shell.Current.DisplayAlert("File Too Large", "Please select a file smaller than 2 MB.", "OK");
                return;
            }
            Filename_video = fileResult.FileName;
            TaskRecord.file_name_video = fileResult.FileName;
            using var stream = await fileResult.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            TaskRecord.file_data_video = memoryStream.ToArray(); // Store file data
            VideoFileExist = true;
            OnPropertyChanged(nameof(Filename_video));
            OnPropertyChanged(nameof(VideoFileExist));
        }
#elif WINDOWS
        var file2 = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select a video file",
            FileTypes = FilePickerFileType.Videos
        });
        Filename_video = "No File Selected";
        if (file2 != null)
        {
            var fileInfo = new FileInfo(file2.FullPath);
            long sizeInBytes = fileInfo.Length;

            if (sizeInBytes > 2000 * 1024)
            {
                await Shell.Current.DisplayAlert("File Too Large", "Please select a file smaller than 2 MB.", "OK");
                return;
            }

            Filename_video = file2.FileName;
            TaskRecord.file_name_video = file2.FileName;
            using var stream = await file2.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            TaskRecord.file_data_video = memoryStream.ToArray(); // Store file data
            VideoFileExist = true;
            OnPropertyChanged(nameof(Filename_video));
            OnPropertyChanged(nameof(VideoFileExist));
        }
#endif
    }

    [RelayCommand]
    async Task DeleteAsync()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;

            var delResponse = await taskService.DeleteItemAsync(TaskRecord);
            //TaskRecords.Remove(TaskRecord);

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get taskRecords: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            //IsRefreshing = false;
        }
        IsCompleted = false; // Reset completion status after saving
        await Shell.Current.GoToAsync("..");
    }

    public MISDatabase TaskService { get => taskService; set => taskService = value; }
    public DateTime SelectedDate1
    {
        get => _selectedDate;
        set => _selectedDate = value;
    }
    public TaskRecord TaskRecord1 { get => TaskRecord; set => TaskRecord = value; }
    public string Filename_image1 { get => Filename_image; set => Filename_image = value; }
    public bool ImageFileExist1 { get => ImageFileExist; set => ImageFileExist = value; }
    public string Filename_video1 { get => Filename_video; set => Filename_video = value; }
    public bool VideoFileExist1 { get => VideoFileExist; set => VideoFileExist = value; }

}