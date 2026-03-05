using SQLite;
namespace TaskManagement.Model
{
    [Table("TaskType")]
    public class TaskType : INotifyPropertyChanged

    {
        public event PropertyChangedEventHandler PropertyChanged;

        [PrimaryKey, AutoIncrement]
        public int task_type_id { get; set; }
        public int sort_order { get; set; } = 0;

        private string _taskType;
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

        [Ignore] // Optional: prevents SQLite from trying to map this
        public string DisplayText => $"{task_type} ({TaskCount})";

        protected void OnPropertyChanged(string propertyName) =>
     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}