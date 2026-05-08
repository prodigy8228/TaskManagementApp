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
public partial class TaskDetailsViewModel : ObservableObject
{
    public bool IsAndroid => DeviceInfo.Platform == DevicePlatform.Android;
    public bool IsWindows => DeviceInfo.Platform == DevicePlatform.WinUI;

    public List<RepeatOption> RepeatOptions { get; } =
         Enum.GetValues(typeof(RepeatOption)).Cast<RepeatOption>().ToList();

    [ObservableProperty]
    private bool isBusy;
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;
    readonly ISpeechToText speechToText;
    readonly CancellationTokenSource tokenSource;
    //private DateTime _selectedDate = DateTime.Now;
    // private DateTime _selectedDate;
    public ObservableCollection<TaskRecord> TaskRecords { get; set; }

    // In TaskDetailsViewModel.cs
    private TaskRecord _taskRecord;


    public TaskRecord TaskRecord
    {
        get => _taskRecord;
        set
        {
            // Use the Toolkit's SetProperty to avoid ambiguity
            if (SetProperty(ref _taskRecord, value))
            {
                if (_taskRecord != null)
                {
                    // 🔥 Safely handle null dates from Firebase
                    if (_taskRecord.task_due_date.HasValue)
                    {
                        SelectedDate = _taskRecord.task_due_date.Value.ToLocalTime().Date;
                    }
                    else
                    {
                        // If null in DB, default the DatePicker UI to show today
                        SelectedDate = DateTime.Today;
                    }
                }
                // This tells the UI to re-check the delete button visibility
                OnPropertyChanged(nameof(isDraft));
                OnPropertyChanged(nameof(isAdmin));
            }
        }
    }

    // Use a safe comparison (handles nulls and type differences)
    public bool isDraft => TaskRecord?.userId?.ToString() == GlobalVariables.userId?.ToString();


    // public Boolean isAdmin { get; set; } = false;
    public bool isAdmin => GlobalVariables.role == "Admin";
    private readonly IFirestoreService _fService;
    public TaskDetailsViewModel(IFirestoreService fService)
    {
        _fService = fService;
        this.speechToText = DependencyService.Get<ISpeechToText>();
#if ANDROID
        speechToText = new SpeechToTextImplementation();
#endif
        this.tokenSource = new CancellationTokenSource();
        this.Description = TaskRecord?.task_description ?? string.Empty;
        this.PendingDescription = TaskRecord?.pending_description ?? string.Empty;

        // 3. Notify the UI properties

        this.TaskTitle = TaskRecord?.task_title ?? string.Empty;
        this.IsCompleted = TaskRecord?.IsCompleted ?? false;
    }

    [ObservableProperty]
    string? description;
    [ObservableProperty]
    string? pendingDescription;

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


    partial void OnSelectedDateChanged(DateTime value)
    {
        if (TaskRecord != null)
        {
            // Option A: If you want to allow clearing the date in Firebase
            if (value == DateTime.MinValue)
            {
                TaskRecord.task_due_date = null;
            }
            else
            {
                // Pushes the 12:00 PM buffer to Firebase
                DateTime localNoon = value.Date.AddHours(12);
                TaskRecord.task_due_date = new DateTimeOffset(localNoon);
            }
        }
    }

    /*
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
    */

    public ObservableCollection<TaskType> TaskTypes { get; set; } = new ObservableCollection<TaskType>();

    [RelayCommand]
    async Task LoadUsersAsync()
    {
        var taskTypes = await _fService.GetTaskTypesAsync(); // Fetch users from database/API
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
    public ObservableCollection<User> Assignees { get; set; } = new ObservableCollection<User>();
    public User SelectedAssignee1 { get => _selectedAssignee; set => _selectedAssignee = value; }
    private User _selectedAssignee;
    public User SelectedAssignee
    {
        get => SelectedAssignee1;
        set
        {
            SelectedAssignee1 = value;
            OnPropertyChanged();
            // Update TaskRecord.Assignee when user selection changes
            if (TaskRecord != null && SelectedAssignee1 != null)
            {
                TaskRecord.assignee_id = SelectedAssignee1?.Id ?? "";
            }
        }
    }
    [RelayCommand]
    async Task LoadAssigneeAsync()
    {
        var assignees = await _fService.GetAssigneeAsync(); // Fetch users from database/API
                                                            //assignees = assignees.ToList();

        Assignees.Clear();
        foreach (var assignee in assignees)
        {
            Assignees.Add(assignee); // Populate the dropdown dynamically
        }
        SelectedAssignee = TaskRecord?.assignee_id == null
            ? assignees.FirstOrDefault()
            : assignees.FirstOrDefault(u => u.Id == TaskRecord.assignee_id);
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
            if (TaskRecord.task_title.Trim() == "")
            {
                await Shell.Current.DisplayAlert("Error!", "Task title cannot be empty.", "OK");
                return;
            }

            if (TaskRecord.task_id == 0)
            {
                var response = 0;
                if (GlobalVariables.role == "Admin")
                {
                    response = await _fService.SaveItemAsync(TaskRecord);
                }
                else
                {
                    response = await _fService.SaveDraftItemAsync(TaskRecord);
                }

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

                var response = 0;
                if (GlobalVariables.role == "Admin")
                {
                    response = await _fService.UpdateItemAsync(TaskRecord);
                }
                else
                {
                    if (GlobalVariables.userId != TaskRecord.userId)
                    {

                        response = await _fService.UpdateItemDescAsync(TaskRecord);
                    }
                    else
                    {
                        response = await _fService.UpdateDraftItemAsync(TaskRecord);
                    }
                }
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
        //TaskRecords = new ObservableCollection<TaskRecord>(await App.Firestore.GetItemsAsync());
        //  IsCompleted = false; // Reset completion status after saving
        // await Task.Delay(3500);
        await Shell.Current.GoToAsync("..?refresh=true");
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
            TaskRecord.file_data_image1 = ms.ToArray();

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
            TaskRecord.file_data_image1 = memoryStream.ToArray(); // Store file data
            TaskRecord.file_data_image = "";
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
            TaskRecord.file_data_video1 = memoryStream.ToArray(); // Store file data
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
            TaskRecord.file_data_video1 = memoryStream.ToArray(); // Store file data
            VideoFileExist = true;
            OnPropertyChanged(nameof(Filename_video));
            OnPropertyChanged(nameof(VideoFileExist));
        }
#endif
    }


    [RelayCommand]
    async Task AcceptedTaskRecordAsync()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            if (TaskRecord != null)
            {
                TaskRecord.task_title = TaskTitle;
                TaskRecord.task_description = TaskRecord.pending_description;
                TaskRecord.pending_description = "";
                TaskRecord.task_type_id = SelectedTaskType?.task_type_id ?? 2;
                TaskRecord.IsCompleted = IsCompleted;
            }
            var acceptResponse = await _fService.UpdateItemAsync(TaskRecord);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to mearge taskRecords: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }

        await Shell.Current.GoToAsync("..?refresh=true");
    }

    [RelayCommand]
    async Task DeleteAsync()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;

            var delResponse = await _fService.DeleteItemAsync(TaskRecord);
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


    public TaskRecord TaskRecord1 { get => TaskRecord; set => TaskRecord = value; }
    public string Filename_image1 { get => Filename_image; set => Filename_image = value; }
    public bool ImageFileExist1 { get => ImageFileExist; set => ImageFileExist = value; }
    public string Filename_video1 { get => Filename_video; set => Filename_video = value; }
    public bool VideoFileExist1 { get => VideoFileExist; set => VideoFileExist = value; }

}