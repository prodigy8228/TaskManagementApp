using Plugin.Firebase.Firestore;


namespace TaskManagement.Model;

public partial class TaskRecord : ObservableObject
{
    public TaskRecord()
    {
    }

    [FirestoreProperty("task_id")]
    public int task_id { get; set; }   // Firestore doc ID (string)
    [FirestoreProperty("task_title")]
    public string task_title { get; set; }
    [FirestoreProperty("task_description")]
    public string task_description { get; set; }

    [FirestoreProperty("task_due_date")]
    public DateTimeOffset? task_due_date { get; set; } = null;

    [FirestoreProperty("task_created_at")]
    public DateTimeOffset task_created_at { get; set; } = DateTimeOffset.UtcNow;

    public DateTime? task_updated_at { get; set; }
    [FirestoreProperty("file_name_image")]
    public string file_name_image { get; set; } = "";
    [FirestoreProperty("file_name_video")]
    public string file_name_video { get; set; } = "";
    [FirestoreProperty("is_selected")]
    public bool IsSelected { get; set; } = false;
    public byte[] file_data_image1 { get; set; }
    public byte[] file_data_video1 { get; set; }
    [FirestoreProperty("file_data_image")]
    public string file_data_image { get; set; } = "";
    [FirestoreProperty("file_data_video")]
    public string file_data_video { get; set; } = "";
    [FirestoreProperty("is_completed")]
    public bool IsCompleted { get; set; } = false;
    [FirestoreProperty("task_type_id")]
    public int task_type_id { get; set; }
    [FirestoreProperty("Repeat")]
    public object RepeatRaw
    {
        get => (int)Repeat; // Saves as Integer
        set
        {
            if (value is long l) Repeat = (RepeatOption)(int)l;
            else if (value is string s && Enum.TryParse(s, out RepeatOption result)) Repeat = result;
            else Repeat = RepeatOption.NoRepeat;
        }
    }

    public RepeatOption Repeat { get; set; } = RepeatOption.NoRepeat;

    [FirestoreProperty("assignee_id")]
    public string assignee_id { get; set; }

    [FirestoreProperty("CompanyId")]
    public string CompanyId { get; set; }
    // Use the correct attribute name and namespace

    public string DisplayUsername { get; set; }

    [FirestoreProperty("userId")]
    public string userId { get; set; }
    [FirestoreProperty("pending_description")]
    public string pending_description { get; set; } = "";
    [ObservableProperty]
    private bool isEditing = false;
}