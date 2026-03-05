using SQLite;
namespace TaskManagement.Model
{
    [Table("TaskRecord")]
    public class TaskRecord
    {
        [PrimaryKey, AutoIncrement]
        public int task_id { get; set; }
        [NotNull]
        public string task_title { get; set; }
        public string task_description { get; set; }

        public DateTime? task_due_date { get; set; }

        public DateTime task_created_at { get; set; } = DateTime.UtcNow;


        public string file_name_image { get; set; }
        public string file_name_video { get; set; }

        public bool IsSelected { get; set; } = false;
        public byte[] file_data_image { get; set; }
        public byte[] file_data_video { get; set; }

        public bool IsCompleted { get; set; } = false;

        public int task_type_id { get; set; }

        public RepeatOption Repeat { get; set; } = RepeatOption.NoRepeat;



    }

}