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

public partial class SettingViewModel : BaseViewModel
{
    public bool IsAndroid => DeviceInfo.Platform == DevicePlatform.Android;
    public bool IsWindows => DeviceInfo.Platform == DevicePlatform.WinUI;

    MISDatabase taskService;
    public SettingViewModel()
    {
        this.TaskService = new MISDatabase();
        showQuickTask = GlobalVariables.IsQuckTaskVisible;
        showCompletedTask = GlobalVariables.IsCompletedTaskVisible;
    }
    public MISDatabase TaskService { get => taskService; set => taskService = value; }
    public ObservableCollection<TaskType> TaskTypes { get; set; } = new ObservableCollection<TaskType>();

    [RelayCommand]
    async Task LoadUsersAsync()
    {
        var taskTypes = await taskService.GetTaskTypesAsync(); // Fetch users from database/API
        TaskTypes.Clear();
        foreach (var tasktype in taskTypes)
        {
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
                // var response = await taskService.SaveItemAsync(NewTask);
                Task.Run(async () => taskService.SaveSettingItemAsync(GlobalVariables.defTaskType));
            }
        }
    }

    [ObservableProperty]
    bool showQuickTask;

    [ObservableProperty]
    bool showCompletedTask;

    partial void OnShowQuickTaskChanged(bool value)
    {
        taskService.SaveSettingItemAsync(value);
    }

    partial void OnShowCompletedTaskChanged(bool value)
    {
        taskService.SaveSettingOneItemAsync(value);
    }

}