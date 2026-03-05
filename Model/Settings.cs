using SQLite;

namespace TaskManagement.Model
{
    [Table("Settings")]
    public class Settings
    {
        [PrimaryKey, AutoIncrement]
        public int id { get; set; }
        public int def_task_type_id { get; set; }
        public bool is_quickTaskVisible { get; set; } = true;
        public bool is_completedTaskVisible { get; set; } = false;
    }
}
