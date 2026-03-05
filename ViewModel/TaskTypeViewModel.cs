namespace TaskManagement.ViewModel
{

    public partial class TaskTypeViewModel : BaseViewModel
    {
        MISDatabase taskService;
        public ObservableCollection<TaskType> TaskTypes { get; } = new();
        [ObservableProperty]
        bool isRefreshing;
        [ObservableProperty]
        string taskText;
        [ObservableProperty]
        string taskTypeLabel;
        public TaskTypeViewModel()
        {
            this.taskService = new MISDatabase();
            GetTaskTypesAsync();
        }

        [RelayCommand]
        async Task GetTaskTypesAsync()
        {
            if (IsBusy)
                return;
            try
            {
                IsBusy = true;
                var taskTypes = await taskService.GetTaskTypesAsync(); // Fetch users from database/API
                taskTypes = taskTypes.Where(o => o.sort_order == 1).ToList();
                if (taskTypes != null)
                    TaskTypes.Clear();
                foreach (var tasktype in taskTypes)
                {
                    Console.WriteLine("*****************" + tasktype.task_type_id);
                    Console.WriteLine(tasktype.task_type);
                    TaskTypes.Add(tasktype); // Populate the dropdown dynamically
                    TaskTypeLabel = "Add Task Type :";
                }
                OnPropertyChanged(nameof(TaskTypes));
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
        public TaskType NewTaskType { get; set; } = new() { };

        [RelayCommand]
        async Task SaveTaskTypeAsync()
        {
            if (IsBusy)
                return;
            try
            {
#if ANDROID
                IsBusy = true;
                NewTaskType.task_type = TaskText; // Use the recognized text as the task title
                if (NewTaskType.task_type != "")
                {

                    NewTaskType.TaskCount = 0;

                    var response = await taskService.SaveTaskTypeAsync(NewTaskType);
                    if (response == 1)
                    {
                        Console.WriteLine("Task inserted successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Failed to insert task.");
                    }

                    TaskTypes.Add(NewTaskType);
                    TaskText = "";
                }
#elif WINDOWS
                if (TaskText != "")
                {
                    NewTaskType.task_type = TaskText;

                    NewTaskType.TaskCount = 0;
                    var response = await taskService.SaveTaskTypeAsync(NewTaskType);
                    if (response == 1)
                    {
                        Console.WriteLine("Task Type inserted successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Failed to insert Task Type.");
                    }


                    TaskTypes.Add(NewTaskType);
                    TaskText = "";
                }
#endif
                await GetTaskTypesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get TaskType: {ex.Message}");
                await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
            NewTaskType = new TaskType() { };
            OnPropertyChanged(nameof(NewTaskType));
        }

        [RelayCommand]
        async Task EditTaskTypeAsync(TaskType taskType)
        {
            NewTaskType = taskType; // Fill the entry with selected task text
            TaskText = taskType.task_type; // Fill the entry with selected task text
            TaskTypeLabel = "Edit Task Type :";

        }

        [RelayCommand]
        async Task DeleteTaskTypeAsync(TaskType taskType)
        {
            TaskText = taskType.task_type; // Fill the entry with selected task text
            TaskTypeLabel = "Delete Task Type :";

        }

        [RelayCommand]
        async Task UpdateTaskTypeAsync()
        {
            if (IsBusy)
                return;
            try
            {
#if ANDROID
                IsBusy = true;
                NewTaskType.task_type = TaskText; // Use the recognized text as the task title
                if (NewTaskType.task_type != "")
                {

                    var response = await taskService.UpdateTaskTypeAsync(NewTaskType);
                    if (response == 1)
                    {
                        Console.WriteLine("Task type updated successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Failed to update task type.");
                    }

                    TaskText = "";
                }
#elif WINDOWS
                if (TaskText != "")
                {
                    NewTaskType.task_type = TaskText;
                    var response = await taskService.UpdateTaskTypeAsync(NewTaskType);
                    if (response == 1)
                    {
                        Console.WriteLine("Task Type updated successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Failed to update Task Type.");
                    }
                    TaskText = "";
                }
#endif
                await GetTaskTypesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get TaskType: {ex.Message}");
                await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
            NewTaskType = new TaskType() { };
            OnPropertyChanged(nameof(NewTaskType));
            TaskTypeLabel = "Add Task Type :";
            OnPropertyChanged(nameof(TaskTypeLabel));
        }

        [RelayCommand]
        async Task CloseTaskTypeAsync()
        {
            if (IsBusy)
                return;

            if (TaskText != "")
            {
                TaskText = "";
            }
            NewTaskType = new TaskType() { };
            OnPropertyChanged(nameof(NewTaskType));
            TaskTypeLabel = "Add Task Type :";
            OnPropertyChanged(nameof(TaskTypeLabel));
        }

        [RelayCommand]
        async Task DelTaskTypeAsync()
        {
            if (IsBusy)
                return;
            try
            {
#if ANDROID
                IsBusy = true;
                NewTaskType.task_type = TaskText; // Use the recognized text as the task title
                if (NewTaskType.task_type != "")
                {
                    var response = await taskService.DeleteTaskTypeAsync(NewTaskType);
                    if (response == 1)
                    {
                        Console.WriteLine("Task type deleted successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Failed to delete task type.");
                    }

                    TaskText = "";
                }
#elif WINDOWS
                if (TaskText != "")
                {
                    NewTaskType.task_type = TaskText;
                    var response = await taskService.DeleteTaskTypeAsync(NewTaskType);
                    if (response == 1)
                    {
                        Console.WriteLine("Task Type deleted successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Failed to delete Task Type.");
                    }
                    TaskText = "";
                }
#endif

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get TaskType: {ex.Message}");
                await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
            await GetTaskTypesAsync();
            NewTaskType = new TaskType() { };
            OnPropertyChanged(nameof(NewTaskType));
            TaskTypeLabel = "Add Task Type :";
            OnPropertyChanged(nameof(TaskTypeLabel));
        }

    }
}






