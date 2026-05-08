using Plugin.Firebase.Firestore;

namespace TaskManagement.Model;

public partial class TaskType : INotifyPropertyChanged
{
    public TaskType()
    {
        // Leave this empty
    }
    public event PropertyChangedEventHandler PropertyChanged;
    [FirestoreProperty("task_type_id")]
    public int task_type_id { get; set; }
    [FirestoreProperty("sort_order")]
    public int sort_order { get; set; } = 0;

    private string _taskType;
    [FirestoreProperty("task_type")]
    public string task_type
    {
        get => _taskType;
        set
        {
            if (_taskType != value)
            {
                _taskType = value;
                OnPropertyChanged(nameof(task_type));
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    private int _taskCount;
    [FirestoreProperty("TaskCount")]
    public int TaskCount
    {
        get => _taskCount;
        set
        {
            if (_taskCount != value)
            {
                _taskCount = value;
                OnPropertyChanged(nameof(TaskCount));
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    [FirestoreProperty("CompanyId")]
    public string CompanyId { get; set; }

    public string DisplayText => $"{task_type} ({TaskCount})";

    protected void OnPropertyChanged(string propertyName) =>
 PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

