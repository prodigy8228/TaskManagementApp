namespace TaskManagement.Model
{
    public interface IFirestoreService
    {
        Task<int> UpdateItemMainAsync(TaskRecord taskRecord);
        Task<int> UpdateDueDateToNextDayAsync(TaskRecord taskRecord);
        Task<Dictionary<string, string>> GetCompanyUsersAsync(string targetCompanyId);
        Task<ObservableCollection<TaskRecord>> GetItemsTypeNotDoneDateAsync();
        Task<ObservableCollection<TaskRecord>> GetDraftItemsAsync();
        Task<ObservableCollection<TaskRecord>> GetItemsAsync();
        Task<ObservableCollection<TaskRecord>> GetItemsAsyncvvv();
        string GetVal(JsonElement fields, string name, string type);
        Task<string> GetFirestoreDocumentPath(string collection, string fieldName, int value1);
        Task UpdateTaskTypeCount(int typeId, int change);
        Task<int> GetCurrentCount(string documentPath);
        Task<int> GetOldTypeIdFromPath(string documentPath);
        Task UpdateTaskTypeCountBySortOrder(int sortOrder, int change);
        DateTime GetNextDueDate(TaskRecord item);
        DateTime GetNextWeekday(DateTime date);
        Task InsertNextRepeatTask(TaskRecord item);
        Task<int> UpdateDraftItemAsync(TaskRecord item);
        Task<int> AcceptItemAsync(TaskRecord item);
        Task<int> PatchDraftTaskRecord(string documentPath, TaskRecord item);
        Task<int> UpdateItemAsync(TaskRecord item);
        Task<int> PatchTaskRecord(string documentPath, TaskRecord item);
        Task<List<TaskRecord>> SearchTaskRecords(string qry);
        Task<int> GetNextTaskId();
        Task<int> GetNextDraftTaskId();
        Task<string> UploadToStorage(byte[] fileBytes, string fileName);
        Task<int> SaveDraftItemAsync(TaskRecord item);
        Task<int> SaveItemAsync(TaskRecord item);
        Task UpdateTaskTypeCountAsync(int typeId, int delta);
        Task<int> UpdateFinishItemAsync(TaskRecord item);
        Task<int> DeleteDraftItemAsync(TaskRecord item);
        Task<int> DeleteItemAsync(TaskRecord item);
        Task<List<TaskRecord>> GetItemsTypeAsync(int type_id);
        Task LoadSettingsToGlobalsAsync();
        Task<int> SaveTaskTypeAsync(TaskType newTaskType);
        Task<int> UpdateItemDescAsync(TaskRecord taskRecord);
        Task<int> UpdateTaskTypeAsync(TaskType updatedTaskType);
        Task<int> DeleteTaskTypeAsync(TaskType taskTypeToDelete);
        Task<int> SaveSettingItemAsync(int item1);
        Task<int> SaveSettingItemAsync(bool item1);
        Task<int> SaveSettingLangAsync(string item1);
        Task<int> SaveSettingOneItemAsync(bool item1);
        Task<ObservableCollection<User>> GetAssigneeAsync();
        Task<List<TaskType>> GetTaskTypesAsync();
        Task<Settings> GetSettingsAsync();
        Task<User?> LoginAndGetUserAsync(string email, string password);
        Task<string> GetIdTokenAsync(string email, string password);
    }
}
