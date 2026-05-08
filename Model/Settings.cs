
using Plugin.Firebase.Firestore;

namespace TaskManagement.Model;

public class Settings
{
    [FirestoreProperty("id")]
    public int id { get; set; }
    [FirestoreProperty("def_task_type_id")]
    public int def_task_type_id { get; set; }
    [FirestoreProperty("is_quickTaskVisible")]
    public bool is_quickTaskVisible { get; set; } = true;
    [FirestoreProperty("is_completedTaskVisible")]
    public bool is_completedTaskVisible { get; set; } = false;
    [FirestoreProperty("reminderLanguage")]
    public string reminderLanguage { get; set; } = "en-US";
}

public class PragmaInfo
{
    public int cid { get; set; }          // Column ID
    public string name { get; set; }      // Column name
    public string type { get; set; }      // Data type (TEXT, INTEGER, etc.)
    public int notnull { get; set; }      // 1 if NOT NULL constraint
    public string dflt_value { get; set; } // Default value
    public int pk { get; set; }           // 1 if part of primary key
}
