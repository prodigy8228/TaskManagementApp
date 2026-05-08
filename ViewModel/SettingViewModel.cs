#if ANDROID
using Android.Content;
using Android.Provider;
using Microsoft.Maui.Controls;
using System.Globalization; // Add this namespace at the top of the file
using System.Threading.Tasks;
using TaskManagement.Platforms;
#endif
namespace TaskManagement.ViewModel;

public partial class SettingViewModel : BaseViewModel
{
    public bool IsAndroid => DeviceInfo.Platform == DevicePlatform.Android;
    public bool IsWindows => DeviceInfo.Platform == DevicePlatform.WinUI;

    private readonly IFirestoreService _fService;
    public SettingViewModel(IFirestoreService fService)
    {
        _fService = fService;
        showQuickTask = GlobalVariables.IsQuckTaskVisible;
        showCompletedTask = GlobalVariables.IsCompletedTaskVisible;
        SelectedReminderLanguage = Preferences.Get("ReminderLanguage", "English");
    }
    public ObservableCollection<TaskType> TaskTypes { get; set; } = new ObservableCollection<TaskType>();

    [RelayCommand]
    async Task LoadUsersAsync()
    {
        var taskTypes = await _fService.GetTaskTypesAsync(); // Fetch tasktype from database/API
        TaskTypes.Clear();
        foreach (var tasktype in taskTypes)
        {
            Console.WriteLine("task type id --> " + tasktype.task_type_id);
            TaskTypes.Add(tasktype); // Populate the dropdown dynamically
        }
        SelectedTaskType = taskTypes.FirstOrDefault(u => u.task_type_id == GlobalVariables.defTaskType);
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
            if (SelectedTaskType1 != null && GlobalVariables.defTaskType != SelectedTaskType1.task_type_id)
            {
                GlobalVariables.defTaskType = SelectedTaskType1.task_type_id;
                _ = UpdateSettingsAsync(); // fire and forget
            }
        }
    }
    private async Task UpdateSettingsAsync()
    {
        await _fService.SaveSettingItemAsync(GlobalVariables.defTaskType);
    }

    public ObservableCollection<string> ReminderLanguages { get; } =
    new ObservableCollection<string> { "English", "Gujarati" };

    private string _selectedReminderLanguage;
    public string SelectedReminderLanguage
    {
        get => _selectedReminderLanguage;
        set
        {
            if (_selectedReminderLanguage != value)
            {
                _selectedReminderLanguage = value;
                Preferences.Set("ReminderLanguage", value);
                OnPropertyChanged();
                if (_selectedReminderLanguage != null && GlobalVariables.ReminderLanguage != _selectedReminderLanguage)
                {
                    string cultureCode = value == "Gujarati" ? "gu-IN" : "en-US";

                    GlobalVariables.ReminderLanguage = cultureCode;
                    //Task.Run(async () => taskService.SaveSettingLangAsync(GlobalVariables.ReminderLanguage));
                    Task.Run(async () => _fService.SaveSettingLangAsync(GlobalVariables.ReminderLanguage));

                }
            }
        }
    }

    [ObservableProperty]
    bool showQuickTask;

    [ObservableProperty]
    bool showCompletedTask;

    partial void OnShowQuickTaskChanged(bool value)
    {

        _ = _fService.SaveSettingItemAsync(value);

    }



    partial void OnShowCompletedTaskChanged(bool value)
    {

        _ = _fService.SaveSettingOneItemAsync(value);

    }

}