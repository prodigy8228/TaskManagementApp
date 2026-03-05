using CommunityToolkit.Maui.Core;
using Org.BouncyCastle.Asn1.X509;
using System.Globalization;
using System.Windows.Input;
using TaskManagement.Services;
using TaskManagement.View;

#if ANDROID
using TaskManagement.Platforms;
#endif
namespace TaskManagement.ViewModel
{
    public partial class TaskRecordViewModel : BaseViewModel
    {
#if ANDROID
        private readonly IBackgroundTaskService _backgroundTaskService;
#endif

        public ObservableCollection<Color> RowColors { get; set; }
        //[ObservableProperty]
        //Collection<TaskRecord> taskRecords;
        public ObservableCollection<TaskRecord> TaskRecords { get; } = new();
        public ObservableCollection<TaskRecord> allTasks { get; } = [];    // new();
        private Collection<TaskRecord> _originalData;
        public bool IsAndroid => DeviceInfo.Platform == DevicePlatform.Android;
        public bool IsWindows => DeviceInfo.Platform == DevicePlatform.WinUI;
        [ObservableProperty]
        public bool isQuickTask; // Use the static variable from GlobalVariables
        [ObservableProperty]
        public bool isCompletedTaskVisible; // Use the static variable from GlobalVariables


        private readonly IPopupService _popupService;
        MISDatabase taskService;
        public TaskRecord NewTask { get; set; } = new() { };
        [ObservableProperty]
        string taskText;
        public ICommand SearchCommand { get; }
        readonly ISpeechToText speechToText;
        readonly CancellationTokenSource tokenSource;



        public TaskRecordViewModel(IPopupService popupService
#if ANDROID
                           , IBackgroundTaskService backgroundTaskService
#endif
)
        {
            _popupService = popupService;
#if ANDROID
            _backgroundTaskService = backgroundTaskService;
#endif
            SearchCommand = new Command<string>(async (query) => await SearchTaskRecordAsync(query));

            this.taskService = new MISDatabase();
            _ = GetTaskRecordAsync(); // Explicitly discard the returned Task to suppress CS4014

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



            // Task.Run(async () => await LoadUsersAsync());

        }
        public MISDatabase TaskService { get => taskService; set => taskService = value; }
        public ICommand MultiCommand => new Command(async () =>
        {
            await ListenAsync();
            await SaveTaskRecordAsync();
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
                        RecognitionText = await speechToText.Listen(CultureInfo.GetCultureInfo("en-us"),
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
        async Task GetTaskRecordAsync()
        {
            if (IsBusy)
                return;
            try
            {
                IsBusy = true;
                //var TskRecords = ShowCompleted ? await taskService.GetItemsAsync() : await taskService.GetItemsNotDoneAsync();
                var TskRecords = await taskService.GetItemsAsync();

                if (TskRecords!.Count != 0)
                    TaskRecords.Clear();

                if (TskRecords != null)
                {
                    var today = DateTime.Today;
                    var sortedTasks = TskRecords.Where(t => t.task_type_id != 10).OrderByDescending(t => t.task_due_date?.Date == today)
         .ThenBy(t => t.task_due_date)
           .ToList();
                    foreach (var task in sortedTasks)
                    {
                        Console.WriteLine(task.task_due_date + " ***** " + task.task_title);
                        TaskRecords.Add(task);
                    }
                }
                if (TaskRecords != null && SelectedTaskType1 != null && SelectedTaskType1.task_type_id != 1)
                {
                    IsBusy = false;
                    // Mark the setter as async and use Task.Run to call the async method
                    await FilterTaskRecordAsync(SelectedTaskType1.task_type_id);
                }
                _originalData = TaskRecords;

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
                var taskRecords = await taskService.SearchTaskRecords(query);

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



                if (TaskRecords.Count != 0)
                    TaskRecords.Clear();

                if (taskRecords != null)
                {
                    foreach (var task in taskRecords)
                    {
                        TaskRecords.Add(task);
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
                var taskRecords = await taskService.GetItemsTypeAsync(query);
                if (TaskRecords.Count != 0)
                    TaskRecords.Clear();

                if (taskRecords != null)
                {
                    var today = DateTime.Today;
                    var sortedTasks = taskRecords.OrderByDescending(t => t.task_due_date?.Date == today)
.ThenBy(t => t.task_due_date).ToList();
                    foreach (var task in sortedTasks)
                    {
                        TaskRecords.Add(task);
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
                var taskRecords = await taskService.GetItemsAsync();
                if (TaskRecords != null && SelectedTaskType1 != null)
                {
                    if (SelectedTaskType1.task_type_id != 1)
                    {
                        var today = DateTime.Today;
                        taskRecords = taskRecords.Where(t => t.task_type_id == SelectedTaskType1.task_type_id).OrderByDescending(t => t.task_due_date?.Date == today)
         .ThenBy(t => t.task_due_date ?? DateTime.MaxValue).ToList();
                    }
                    else
                    {
                        var today = DateTime.Today;
                        taskRecords = taskRecords.Where(t => t.task_type_id != 10).OrderByDescending(t => t.task_due_date?.Date == today)
         .ThenBy(t => t.task_due_date ?? DateTime.MaxValue).ToList();

                    }
                }
                await Task.Delay(100);
                if (TaskRecords.Count != 0)
                    TaskRecords.Clear();

                if (taskRecords != null)
                {
                    foreach (var task in taskRecords)
                    {
                        TaskRecords.Add(task);
                    }
                }
                OnPropertyChanged(nameof(taskRecords));
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
        async Task DeleteTaskRecordAsync(TaskRecord query)
        {
            if (IsBusy)
                return;
            try
            {
                IsBusy = true;
                var delResponse = await taskService.DeleteItemAsync(query);
                TaskRecords.Remove(query);
                //
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
            await LoadUsersAsync();

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
                if (TaskRecords != null && SelectedTaskType1 != null)
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
                var taskTypes = await taskService.GetTaskTypesAsync(); // Fetch users from database/API
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
                var taskTypes = await taskService.GetTaskTypesAsync(); // Fetch users from database/API
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
        async Task SaveTaskRecordAsync()
        {
            if (IsBusy)
                return;
            try
            {
#if ANDROID
                IsBusy = true;
                NewTask.task_title = RecognitionText; // Use the recognized text as the task title
                if (NewTask.task_title != "")
                {

                    //  NewTask.task_due_date = DateTime.Now;
                    NewTask.task_type_id = 2; // Set the task type ID based on the selected task type
                    NewTask.task_description = "";
                    NewTask.IsCompleted = false; // Set default values for the new task
                    var response = await taskService.SaveItemAsync(NewTask);
                    if (response == 1)
                    {
                        Console.WriteLine("Task inserted successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Failed to insert task.");
                    }

                    TaskRecords.Add(NewTask);
                    RecognitionText = "";
                }
#elif WINDOWS
                if (RecognitionText != "" && RecognitionText != null)
                {
                    NewTask.task_title = RecognitionText;
                    /*  if (NewTask.task_due_date != default(DateTime))
                    {
                        NewTask.task_due_date = DateTime.Now;
                    }*/
                    //  NewTask.task_due_date = DateTime.Now;
                    NewTask.task_description = "";
                    NewTask.task_type_id = 2; // Set the task type ID based on the selected task type
                    var response = await taskService.SaveItemAsync(NewTask);
                    if (response == 1)
                    {
                        Console.WriteLine("Task inserted successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Failed to insert task.");
                    }


                    // TaskRecords.Add(NewTask);
                    TaskRecords.Add(NewTask);
                    RecognitionText = "";
                }
                else
                {
                    Debug.WriteLine($"Unable to get TaskRecord");
                    await Shell.Current.DisplayAlert("Error", "Task Title is empty!", "OK");
                }
#endif
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get TaskRecord: {ex.Message}");
                await Shell.Current.DisplayAlert("Error!", "Task Title is empty", "OK");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
            NewTask = new TaskRecord() { };
            OnPropertyChanged(nameof(NewTask));
#if ANDROID
           // _backgroundTaskService.EnqueueOneTimeReminder("Task Reminder", "Finish compliance report today");
#endif
        }


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
                await taskService.UpdateFinishItemAsync(query);
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
            OnPropertyChanged(nameof(TaskRecords));
        }
    }

}
