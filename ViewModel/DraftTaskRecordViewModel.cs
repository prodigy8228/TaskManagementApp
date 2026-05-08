using CommunityToolkit.Maui.Core;
using Org.BouncyCastle.Asn1.X509;
using System.Globalization;
using System.Windows.Input;
using TaskManagement.Services;
using TaskManagement.View;

#if ANDROID
using TaskManagement.Platforms;
#endif
namespace TaskManagement.ViewModel;

public partial class DraftTaskRecordViewModel : ObservableObject, IQueryAttributable
{
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // This fires whenever you navigate TO this page, including "Back"
        if (query.ContainsKey("refresh"))
        {
            // Clear the parameter so it doesn't refresh again accidentally
            query.Remove("refresh");
            if (LoadUsersCommand.CanExecute(null))
            {
                LoadUsersCommand.Execute(null);
            }
            // Execute your load command
            if (GetDraftTaskRecordCommand.CanExecute(null))
            {
                GetDraftTaskRecordCommand.Execute(null);
            }
        }
    }
#if ANDROID
    private readonly IBackgroundTaskService _backgroundTaskService;
#endif
    //  public Boolean isAdmin { get; set; } = false;
    public bool isAdmin => GlobalVariables.role == "Admin";
    public bool isDraft => GlobalVariables.role == "Member";
    public ObservableCollection<Color> RowColors { get; set; }
    public ObservableCollection<TaskRecord> DraftTaskRecords { get; } = new();
    public ObservableCollection<TaskRecord> allTasks { get; } = [];    // new();
    private Collection<TaskRecord> _originalData;
    public bool IsAndroid => DeviceInfo.Platform == DevicePlatform.Android;
    public bool IsWindows => DeviceInfo.Platform == DevicePlatform.WinUI;
    [ObservableProperty]
    public bool isQuickTask; // Use the static variable from GlobalVariables
    [ObservableProperty]
    public bool isCompletedTaskVisible; // Use the static variable from GlobalVariables

    [ObservableProperty]
    private bool isBusy;

    private readonly IPopupService _popupService;

    public TaskRecord NewTask { get; set; } = new() { };
    [ObservableProperty]
    string taskText;
    public ICommand SearchCommand { get; }
    readonly ISpeechToText speechToText;
    readonly CancellationTokenSource tokenSource;

    private readonly IFirestoreService _fService;
    public DraftTaskRecordViewModel(IPopupService popupService, IFirestoreService fService
#if ANDROID
                       , IBackgroundTaskService backgroundTaskService
#endif
)
    {
        _popupService = popupService;
#if ANDROID
        _backgroundTaskService = backgroundTaskService;
#endif
        _fService = fService;
        _ = GetDraftTaskRecordAsync(); // Explicitly discard the returned Task to suppress CS4014

        RowColors = new ObservableCollection<Color>
           {
               Color.FromArgb("#ffffff"), // Light gray
               Color.FromArgb("#ffffff") // White
           };
        this.speechToText = DependencyService.Get<ISpeechToText>();
#if ANDROID
        speechToText = new SpeechToTextImplementation();
        _backgroundTaskService = backgroundTaskService;

#endif
        this.tokenSource = new CancellationTokenSource();
    }
    public ICommand MultiCommand => new Command(async () =>
    {
        await ListenAsync();
        // await SaveTaskRecordAsync();
    });


    [ObservableProperty]
    bool isRefreshing;
    [RelayCommand]
    async Task ListenAsync()
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
                    RecognitionText = await speechToText.Listen(CultureInfo.GetCultureInfo(GlobalVariables.ReminderLanguage),
                        new Progress<string>(partialText =>
                        {
                            if (DeviceInfo.Platform == DevicePlatform.Android)
                            {
                                RecognitionText = partialText;
                            }
                            else
                            {
                                RecognitionText += partialText + " ";
                            }

                            OnPropertyChanged(nameof(RecognitionText));
                        }), tokenSource.Token);
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
    async Task ListenCancelAsync()
    {
        tokenSource?.Cancel();
    }
    [ObservableProperty]
    private bool showCompleted;

    [RelayCommand]
    async Task GetDraftTaskRecordAsync()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            // if (App.Firestore == null)
            //    await ((App)Application.Current).InitFirestoreAsync();



            var TskRecords = await _fService.GetDraftItemsAsync();

            if (TskRecords!.Count != 0)
                DraftTaskRecords.Clear();

            if (TskRecords != null)
            {
                var today = DateTime.Today;
                var sortedTasks = TskRecords.Where(t => t.task_type_id != 10).OrderByDescending(t => t.task_due_date?.Date == today)
     .ThenBy(t => t.task_due_date)
       .ToList();
                foreach (var task in sortedTasks)
                {
                    Console.WriteLine(task.task_due_date + " ***** " + task.task_title);
                    DraftTaskRecords.Add(task);
                }
            }
            if (DraftTaskRecords != null && SelectedTaskType1 != null && SelectedTaskType1.task_type_id != 1)
            {
                IsBusy = false;
                // Mark the setter as async and use Task.Run to call the async method
                await FilterTaskRecordAsync(SelectedTaskType1.task_type_id);
            }
            _originalData = DraftTaskRecords;

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get taskRecords: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }

    }

    partial void OnShowCompletedChanged(bool value)
    {
        //Task.Run(async () => await FilterTaskAsync());
        _ = FilterTaskAsync();

    }


    private string _searchText;
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged(nameof(SearchText)); // Notify UI of changes
        }
    }


    [RelayCommand]
    async Task SearchTaskRecordAsync(string query)
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            var taskRecords = await _fService.SearchTaskRecords(query);

            if (taskRecords != null && SelectedTaskType1 != null)
            {
                if (SelectedTaskType1.task_type_id != 1)
                {
                    var today = DateTime.Today;
                    taskRecords = taskRecords.Where(t => t.task_type_id == SelectedTaskType1.task_type_id).OrderByDescending(t => t.task_due_date?.Date == today)
     .ThenBy(t => t.task_due_date).ToList();
                }
                else
                {
                    var today = DateTime.Today;
                    taskRecords = taskRecords.Where(t => t.task_type_id != 10).OrderByDescending(t => t.task_due_date?.Date == today)
     .ThenBy(t => t.task_due_date).ToList();

                }
            }



            if (DraftTaskRecords.Count != 0)
                DraftTaskRecords.Clear();

            if (taskRecords != null)
            {
                foreach (var task in taskRecords)
                {
                    DraftTaskRecords.Add(task);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get taskRecords: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    async Task FilterTaskRecordAsync(int query)
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            //var taskRecords = ShowCompleted ? await taskService.GetItemsTypeAsync(query) : await taskService.GetItemsTypeNotDoneAsync(query);
            var taskRecords = await _fService.GetItemsTypeAsync(query);
            if (DraftTaskRecords.Count != 0)
                DraftTaskRecords.Clear();

            if (taskRecords != null)
            {
                var today = DateTime.Today;
                var sortedTasks = taskRecords.OrderByDescending(t => t.task_due_date?.Date == today)
.ThenBy(t => t.task_due_date).ToList();
                foreach (var task in sortedTasks)
                {
                    DraftTaskRecords.Add(task);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get taskRecords: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    async Task FilterTaskAsync()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            var taskRecords = await _fService.GetItemsAsync();
            if (DraftTaskRecords != null && SelectedTaskType1 != null)
            {
                if (SelectedTaskType1.task_type_id != 1)
                {
                    var today = DateTime.Today;
                    taskRecords = new ObservableCollection<TaskRecord>(
    taskRecords
        .Where(t => t.task_type_id == SelectedTaskType1.task_type_id)
        .OrderByDescending(t => t.task_due_date?.Date == today)
        .ThenBy(t => t.task_due_date ?? DateTime.MaxValue)
);
                }
                else
                {
                    var today = DateTime.Today;
                    taskRecords = new ObservableCollection<TaskRecord>(taskRecords.Where(t => t.task_type_id != 10).OrderByDescending(t => t.task_due_date?.Date == today)
     .ThenBy(t => t.task_due_date ?? DateTime.MaxValue));

                }
            }
            await Task.Delay(100);
            if (DraftTaskRecords.Count != 0)
                DraftTaskRecords.Clear();

            if (taskRecords != null)
            {
                foreach (var task in taskRecords)
                {
                    DraftTaskRecords.Add(task);
                }
            }
            OnPropertyChanged(nameof(DraftTaskRecords));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get taskRecords: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }

    }

    [RelayCommand]
    async Task AcceptedTaskRecordAsync(TaskRecord query)
    {
        if (GlobalVariables.role == "Member")
        {
            return;
        }
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            query.userId = GlobalVariables.userId;
            var acceptResponse = await _fService.AcceptItemAsync(query);
            DraftTaskRecords.Remove(query);
            //
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get drafttaskRecords: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
        // await LoadUsersAsync();
    }

    [RelayCommand]
    async Task DeleteTaskRecordAsync(TaskRecord query)
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            var delResponse = await _fService.DeleteDraftItemAsync(query);
            DraftTaskRecords.Remove(query);
            //
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get draftTaskRecords: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
        // await LoadUsersAsync();

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
            if (DraftTaskRecords != null && SelectedTaskType1 != null)
            {
                _ = FilterTaskAsync();
            }
        }
    }

    public ObservableCollection<TaskType> TaskTypes { get; set; } = new ObservableCollection<TaskType>();

    [RelayCommand]
    async Task LoadUsersAsync()
    {
        if (TaskTypes.Count == 0)
        {
            //   if (App.Firestore == null)
            //      await ((App)Application.Current).InitFirestoreAsync();
            var taskTypes = await _fService.GetTaskTypesAsync(); // Fetch users from database/API
            TaskTypes.Clear();
            foreach (var tasktype in taskTypes)
            {
                Console.WriteLine(tasktype.task_type_id);
                Console.WriteLine(tasktype.task_type);
                TaskTypes.Add(tasktype); // Populate the dropdown dynamically
            }

            SelectedTaskType = taskTypes.FirstOrDefault(t => t.task_type_id == GlobalVariables.defTaskType); ;
        }
        else
        {
            var SelectedTaskType1 = SelectedTaskType; // Store the current selection
            var taskTypes = await _fService.GetTaskTypesAsync(); // Fetch users from database/API
            TaskTypes.Clear();
            foreach (var tasktype in taskTypes)
            {
                Console.WriteLine(tasktype.task_type_id);
                Console.WriteLine(tasktype.task_type);
                TaskTypes.Add(tasktype); // Populate the dropdown dynamically
            }
            if (SelectedTaskType1 != null)
            {
                SelectedTaskType = taskTypes
     .FirstOrDefault(t => t.task_type_id == SelectedTaskType1.task_type_id);
            }
        }
        OnPropertyChanged(nameof(DisplayText));
    }
    [RelayCommand]
    async Task NavigateToTaskTypeAsync()
    {
        await Shell.Current.GoToAsync(nameof(TaskTypePage));
    }

    [RelayCommand]
    async Task NavigateToSettingsAsync()
    {
        await Shell.Current.GoToAsync(nameof(SettingPage));
    }

    [RelayCommand]
    async Task NavigateToBackupAsync()
    {
        await Shell.Current.GoToAsync(nameof(BackupRestorePage));
    }


    [RelayCommand]
    async Task GoToDetailsEmpty()
    {
        NewTask = new TaskRecord();
        NewTask.task_due_date = DateTime.Now;
        NewTask.task_type_id = 2;
        NewTask.IsCompleted = false; // Set default values for the new task
        NewTask.Repeat = RepeatOption.NoRepeat;
        NewTask.userId = GlobalVariables.userId;
        NewTask.assignee_id = GlobalVariables.userId;
        NewTask.CompanyId = GlobalVariables.companyid;
        await Shell.Current.GoToAsync(nameof(DetailsPage), true, new Dictionary<string, object>
        {
            {"TaskRecord",  NewTask }
        });
    }

    [RelayCommand]
    async Task GoToDetails(TaskRecord taskRecord)
    {
        if (taskRecord == null)
            return;
        if (taskRecord.task_due_date == null)
        {
            taskRecord.task_due_date = DateTime.Now;
        }
        await Shell.Current.GoToAsync(nameof(DetailsPage), true, new Dictionary<string, object>
        {

            {"TaskRecord", taskRecord }
        });
    }

    [ObservableProperty]
    string? recognitionText;

    [RelayCommand]
    async Task UpdateTaskAsync(TaskRecord query)
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            Console.WriteLine("UpdateTaskAsync called with query: " + query.IsCompleted);
            // taskRecord.IsCompleted = !taskRecord.IsCompleted;
            await _fService.UpdateFinishItemAsync(query);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to update TaskRecord: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
        OnPropertyChanged(nameof(DraftTaskRecords));
    }
}
