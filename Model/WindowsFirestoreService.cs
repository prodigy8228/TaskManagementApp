using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;
using System.Net.Http.Headers;
using System.Text;

namespace TaskManagement.Model
{
    public class WindowsFirestoreService : IFirestoreService
    {
        private readonly HttpClient _httpClient;
        private readonly string _projectId = "sprinty-cded8"; // your Firebase project ID
        private readonly string _docId = "global"; // single settings document
        private readonly IFirebaseFirestore _firestore;
        private const string apiKey = "AIzaSyAJkcrWu6U49gLeXUSeN3KAY-Jt2uJq_6E"; // from Firebase project settings
        private const string FirebaseAuthUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";


        public async Task<Dictionary<string, string>> GetCompanyUsersAsync(string targetCompanyId)
        {
            var userMap = new Dictionary<string, string>();

            // 1. Create the query on the "User" collection
            var query = CrossFirebaseFirestore.Current
            .GetCollection("User")
            .WhereEqualsTo("CompanyId", targetCompanyId);

            // 2. Execute the query
            //  var querySnapshot = await query.GetAsync();
            var querySnapshot = await query.GetDocumentsAsync<User>();
            // 3. Iterate through the results
            foreach (var document in querySnapshot.Documents)
            {
                if (document.Data != null)
                {
                    // document.Id is the Uid (the end of the path)
                    string uid = document.Reference.Id;

                    // Access fields safely using the plugin's Data dictionary
                    string name = !string.IsNullOrEmpty(document.Data.Username)
            ? document.Data.Username
            : "Unknown";

                    userMap[uid] = name ?? "Unknown";
                }
            }

            return userMap;
        }


        public async Task<User?> LoginAndGetUserAsync(string email, string password)
        {
            // 1. Authenticate with Firebase Auth
            var authUser = await CrossFirebaseAuth.Current.SignInWithEmailAndPasswordAsync(email, password);
            // authUser.GetIdTokenResultAsync().Wait(); // Ensure token is refreshed and available
            // 2. Fetch the User Document using the Uid

            var snapshot = await CrossFirebaseFirestore.Current.GetCollection("User").GetDocument(authUser.Uid).GetDocumentSnapshotAsync<User>();


            return snapshot.Data; // Returns null if document doesn't exist
        }

        public async Task<string> GetIdTokenAsync(string email, string password)
        {
            var authUser = await CrossFirebaseAuth.Current.SignInWithEmailAndPasswordAsync(email, password);
            string idd = await authUser.GetIdTokenResultAsync().ContinueWith(t => t.Result.Token); // Ensure token is refreshed and available

            return idd;
        }
        public async Task<ObservableCollection<TaskRecord>> GetItemsTypeNotDoneDateAsync()
        {
            var taskRecords = new ObservableCollection<TaskRecord>();
            string companyId = GlobalVariables.companyid;
            // 1. Prepare today's date (The plugin handles the conversion to Firebase Timestamp)
            var today = DateTime.UtcNow.Date;

            // 2. Build the Query using method chaining
            var query = CrossFirebaseFirestore.Current
                .GetCollection("TaskRecord")
                .WhereEqualsTo("CompanyId", companyId)
                .WhereEqualsTo("task_due_date", today)
                .WhereEqualsTo("IsCompleted", false);

            // 3. Execute the query
            var snapshot = await query.GetDocumentsAsync<TaskRecord>();

            // 4. Map the documents to your TaskRecord model
            foreach (var doc in snapshot.Documents)
            {
                if (doc.Data != null)
                {
                    var data = doc.Data;

                    var record = new TaskRecord
                    {
                        // Numeric values usually come back as Long from Firestore
                        task_id = data.task_id,
                        task_type_id = data.task_type_id,
                        task_title = data.task_title,
                        task_description = data.task_description,
                        file_name_image = data.file_name_image,
                        file_name_video = data.file_name_video,

                        IsCompleted = data.IsCompleted,
                        IsSelected = data.IsSelected,

                        // For Enums and Dates, the plugin usually maps these 
                        // automatically if your class properties match the types.
                        Repeat = data.Repeat,
                        task_created_at = data.task_created_at,
                        task_due_date = data.task_due_date,

                        file_data_image = data.file_data_image,
                        file_data_video = data.file_data_video,

                        assignee_id = data.assignee_id,
                        userId = data.userId,
                        pending_description = data.pending_description,
                        CompanyId = data.CompanyId
                    };

                    taskRecords.Add(record);
                }
            }

            return taskRecords;
        }

        public async Task<ObservableCollection<TaskRecord>> GetDraftItemsAsync()
        {
            string role = GlobalVariables.role;
            string companyId = GlobalVariables.companyid;
            string userId = GlobalVariables.userId;

            // 1. Fetch user map for username display
            var userMap = await GetCompanyUsersAsync(companyId);

            // 2. Build the Query
            // Use GetCollection for v4.0.0
            var query = CrossFirebaseFirestore.Current
                .GetCollection("DraftTaskRecords")
                .WhereEqualsTo("CompanyId", companyId);

            // Add additional filter for Members (Implicit AND)
            if (role == "Member")
            {
                query = query.WhereEqualsTo("assignee_id", userId);
            }

            // 3. Execute query with typed mapping
            // This handles all the parsing (int, bool, DateTime) automatically
            var snapshot = await query.GetDocumentsAsync<TaskRecord>();

            var taskRecords = new ObservableCollection<TaskRecord>();

            // 4. Process results
            foreach (var doc in snapshot.Documents)
            {
                if (doc.Data != null)
                {
                    var record = doc.Data;

                    // Map the DisplayUsername from our local map
                    if (!string.IsNullOrEmpty(record.assignee_id) && userMap.ContainsKey(record.assignee_id))
                    {
                        record.DisplayUsername = userMap[record.assignee_id];
                    }

                    taskRecords.Add(record);
                }
            }

            return taskRecords;
        }

        public async Task<ObservableCollection<TaskRecord>> GetItemsAsync()
        {
            // 1. Get current user context
            string role = GlobalVariables.role;
            string companyId = GlobalVariables.companyid;
            string userId = GlobalVariables.userId;

            // Fetch the user map (ID -> Name) using your existing method
            var userMap = await GetCompanyUsersAsync(companyId);

            // 2. Build the Query
            // Start with the base collection and the mandatory CompanyId filter
            var query = CrossFirebaseFirestore.Current
                .GetCollection("TaskRecord")
                .WhereEqualsTo("CompanyId", companyId);

            // If the user is a Member, chain the additional assignee_id filter (Implicit AND)
            if (role == "Member")
            {
                query = query.WhereEqualsTo("assignee_id", userId);
            }

            // 3. Execute the query using the typed mapping
            var snapshot = await query.GetDocumentsAsync<TaskRecord>();

            var taskRecords = new ObservableCollection<TaskRecord>();

            // 4. Process results
            foreach (var doc in snapshot.Documents)
            {
                // doc.Data is automatically a TaskRecord object
                if (doc.Data != null)
                {
                    var record = doc.Data;

                    // Handle the DisplayUsername mapping using the userMap
                    if (!string.IsNullOrEmpty(record.assignee_id) && userMap.ContainsKey(record.assignee_id))
                    {
                        record.DisplayUsername = userMap[record.assignee_id];
                    }

                    taskRecords.Add(record);
                }
            }

            return taskRecords;
        }

        public async Task<ObservableCollection<TaskRecord>> GetItemsAsyncvvv()
        {
            // 1. Fetch Users in the same company to build the UserMap
            var userMap = new Dictionary<string, string>();

            var userSnapshot = await CrossFirebaseFirestore.Current
                .GetCollection("User")
                .WhereEqualsTo("CompanyId", GlobalVariables.companyid)
                .GetDocumentsAsync<User>(); // Automatically maps to your User class

            foreach (var userDoc in userSnapshot.Documents)
            {
                if (userDoc.Data != null)
                {
                    // userDoc.Id is the UID (the document name)
                    userMap[userDoc.Data.Id] = userDoc.Data.Username ?? "Unknown";
                }
            }

            // 2. Fetch all TaskRecords
            // Note: Use GetCollection and GetDocumentsAsync for v4.0.0
            var taskSnapshot = await CrossFirebaseFirestore.Current
                .GetCollection("TaskRecord")
                .GetDocumentsAsync<TaskRecord>();

            var taskRecords = new ObservableCollection<TaskRecord>();

            // 3. Process and link the DisplayUsername
            foreach (var taskDoc in taskSnapshot.Documents)
            {
                if (taskDoc.Data != null)
                {
                    var record = taskDoc.Data;

                    // Link DisplayUsername using the userMap created in Step 1
                    if (!string.IsNullOrEmpty(record.assignee_id) && userMap.ContainsKey(record.assignee_id))
                    {
                        record.DisplayUsername = userMap[record.assignee_id];
                    }

                    taskRecords.Add(record);
                }
            }

            return taskRecords;
        }
        public string GetVal(JsonElement fields, string name, string type)
        {
            return fields.TryGetProperty(name, out var prop) ? prop.GetProperty(type).GetString() : null;
        }
        public async Task<string> GetFirestoreDocumentPath(string collection, string fieldName, int value1)
        {
            string companyId = GlobalVariables.companyid;
            var querySnapshot = await CrossFirebaseFirestore.Current
                .GetCollection(collection)
                .WhereEqualsTo("CompanyId", companyId)
                .WhereEqualsTo(fieldName, value1)
                .LimitedTo(1)
                .GetDocumentsAsync<object>(); // Specify <object> as the type argument

            // Returns the relative path (e.g., "TaskType/abc123xyz")
            return querySnapshot.Documents.FirstOrDefault()?.Reference.Id;
        }


        public async Task UpdateTaskTypeCount(int typeId, int change)
        {

            // 1. Get the document path (using your previously updated method)
            var path = await GetFirestoreDocumentPath("TaskType", "task_type_id", typeId);

            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    CrossFirebaseFirestore.Current
                        .GetDocument($"TaskType/{path}")
                        .UpdateDataAsync(("TaskCount", FieldValue.IntegerIncrement(change)));

                    // await Shell.Current.DisplayAlert("Log Trace me123", $"Path: {path}", "OK");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Log Trace", $"Error: {ex.Message}", "OK");
                }

            }
        }
        public async Task<int> GetCurrentCount(string documentPath)
        {
            if (string.IsNullOrEmpty(documentPath)) return 0;

            // 1. Get the document snapshot
            // Using Dictionary<string, object> is the easiest way to access raw fields
            var document = await CrossFirebaseFirestore.Current
                .GetDocument($"TaskRecord/{documentPath}")
                .GetDocumentSnapshotAsync<Dictionary<string, object>>();

            // 2. Check if document exists and contains the field
            if (document.Data != null && document.Data.TryGetValue("TaskCount", out var countObj))
            {
                // Plugin.Firebase usually returns Firestore numbers as long or double
                return Convert.ToInt32(countObj);
            }

            return 0; // Default if document or field doesn't exist
        }

        public async Task<int> GetOldTypeIdFromPath(string documentId)
        {
            if (string.IsNullOrEmpty(documentId)) return 0;

            try
            {
                // 1. Get the raw snapshot
                var snapshot = await CrossFirebaseFirestore.Current
                    .GetDocument($"TaskRecord/{documentId}")
                    .GetDocumentSnapshotAsync<TaskRecord>(); // No <Dictionary> here

                // 2. Check if the document exists on the server
                if (snapshot.Data != null)
                {
                    // 3. Plugin.Firebase snapshots have a Data dictionary
                    if (snapshot.Data.task_type_id != null)
                    {
                        var val = snapshot.Data.task_type_id;

                        // await Shell.Current.DisplayAlert("Success", $"Found ID: {val}", "OK");
                        return Convert.ToInt32(val);
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Error", "Field 'task_type_id' not found in doc", "OK");
                    }
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "Document does not exist at this path", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Crash", ex.Message, "OK");
            }

            return 0;
        }

        public async Task UpdateTaskTypeCountBySortOrder(int sortOrder, int change)
        {
            string companyId = GlobalVariables.companyid;

            // 1. Query to find the document with the matching sort_order
            var querySnapshot = await CrossFirebaseFirestore.Current
                .GetCollection("TaskType")
                .WhereEqualsTo("sort_order", sortOrder)
                .WhereEqualsTo("CompanyId", companyId)
                .LimitedTo(1)
                .GetDocumentsAsync<TaskType>();



            var document = querySnapshot.Documents.FirstOrDefault();

            // 2. If a document is found, update it atomically
            if (document != null)
            {
                await document.Reference.UpdateDataAsync(
                    ("TaskCount", FieldValue.IntegerIncrement(change))
                );
            }
        }

        public DateTime GetNextDueDate(TaskRecord item)
        {
            // DateTime currentDueDate = item.task_due_date ?? DateTime.Now;
            DateTime currentDueDate = DateTime.Now;
            return item.Repeat switch
            {
                RepeatOption.OnceADay => currentDueDate.AddDays(1),
                RepeatOption.OnceAWeek => currentDueDate.AddDays(7),
                RepeatOption.OnceAMonth => currentDueDate.AddMonths(1),
                RepeatOption.OnceAYear => currentDueDate.AddYears(1),
                RepeatOption.OnceAWeekMonFri => GetNextWeekday(currentDueDate),
                _ => currentDueDate // Covers NoRepeat
            };
        }

        // Helper for your "Mon-Fri" specific option
        public DateTime GetNextWeekday(DateTime date)
        {
            DateTime next = date.AddDays(1);
            while (next.DayOfWeek == DayOfWeek.Saturday || next.DayOfWeek == DayOfWeek.Sunday)
            {
                next = next.AddDays(1);
            }
            return next;
        }

        public async Task InsertNextRepeatTask(TaskRecord item)
        {
            // 1. Update the existing object properties for the "next" iteration
            item.IsCompleted = false;
            item.task_id = await GetNextTaskId();
            item.task_created_at = DateTime.UtcNow;
            item.task_due_date = GetNextDueDate(item).ToUniversalTime();

            // 2. Insert the object directly
            // Plugin.Firebase will ignore null file fields and handle mapping automatically
            await CrossFirebaseFirestore.Current
                .GetCollection("TaskRecord")
                .AddDocumentAsync(item);
        }

        public async Task<int> UpdateDraftItemAsync(TaskRecord item)
        {
            // 1. Get the original task from Firestore to check previous state
            string originalTaskPath = await GetFirestoreDocumentPath("DraftTaskRecords", "task_id", item.task_id);

            // 2. Final Update of the TaskRecord itself
            return await PatchDraftTaskRecord(originalTaskPath, item);
        }

        public async Task<int> AcceptItemAsync(TaskRecord item)
        {
            var saveResponse = await SaveItemAsync(item);


            // 2. If successfully created in main, delete from pending
            var delResponse = await DeleteDraftItemAsync(item);
            return 1;

        }

        public async Task<int> PatchDraftTaskRecord(string documentPath, TaskRecord item)
        {
            try
            {
                // One-liner that updates only the fields defined in your TaskRecord model
                await CrossFirebaseFirestore.Current
                    .GetDocument($"DraftTaskRecords/{documentPath}")
                    .SetDataAsync(item, SetOptions.Merge());

                return 1; // Success
            }
            catch (Exception)
            {
                return 0; // Failed
            }
        }


        public async Task<int> UpdateItemAsync(TaskRecord item)
        {
            // 1. Find the document and get its path
            string originalTaskPath = await GetFirestoreDocumentPath("TaskRecord", "task_id", item.task_id);
            if (string.IsNullOrEmpty(originalTaskPath)) return 0;

            // 2. Get the old type ID for comparison
            int oldTypeId = await GetOldTypeIdFromPath(originalTaskPath);

            if (item.IsCompleted)
            {
                // Decrease count for old type and "All Tasks" (id 1)
                int decrement = -1;
                await UpdateTaskTypeCount(oldTypeId, decrement);
                await UpdateTaskTypeCount(1, decrement);

                // Increase count for 'Completed' (Id 10)
                await UpdateTaskTypeCount(10, 1);

                if (item.Repeat != RepeatOption.NoRepeat)
                {
                    await InsertNextRepeatTask(item);
                }

                item.task_type_id = 10;
            }
            else if (oldTypeId != item.task_type_id)
            {
                // Handle moving between categories
                int decrement = -1;
                await UpdateTaskTypeCount(oldTypeId, decrement);
                // await Shell.Current.DisplayAlert("Check default list taskcount ", $"Path: {oldTypeId}", "OK");
                // await UpdateTaskTypeCount(oldTypeId, -1);
                await UpdateTaskTypeCount(item.task_type_id, 1);
            }

            // 3. Final Update of the TaskRecord itself using the updated Patch method
            return await PatchTaskRecord(originalTaskPath, item);
        }
        public async Task<int> PatchTaskRecord(string documentPath, TaskRecord item)
        {
            try
            {
                // await Shell.Current.DisplayAlert("Log Trace dipti", $"Path: {item.task_description}", "OK");
                // 1. Handle Uploads (Same as before)
                if (item.file_data_image1 != null)
                    item.file_data_image = await UploadToStorage(item.file_data_image1, item.file_name_image);
                if (item.file_data_video1 != null)
                    item.file_data_video = await UploadToStorage(item.file_data_video1, item.file_name_video);

                // 2. Map fields manually to avoid serialization errors with byte[]
                var updates = new List<(string, object)>();

                updates.Add(("task_title", item.task_title));
                updates.Add(("task_description", item.task_description));
                updates.Add(("task_type_id", item.task_type_id));
                updates.Add(("is_completed", item.IsCompleted));
                updates.Add(("pending_description", item.pending_description));
                updates.Add(("file_data_image", item.file_data_image));
                updates.Add(("file_data_video", item.file_data_video));
                updates.Add(("file_name_image", item.file_name_image));
                updates.Add(("file_name_video", item.file_name_video));
                updates.Add(("assignee_id", item.assignee_id));
                // Handle the Repeat enum manually since version 4.0 can be picky with enums
                updates.Add(("Repeat", (int)item.Repeat));
                updates.Add(("task_due_date", item.task_due_date.HasValue ? (object)item.task_due_date.Value : null));

                await CrossFirebaseFirestore.Current
            .GetDocument($"TaskRecord/{documentPath}")
            .UpdateDataAsync(updates.ToArray());
                // await Shell.Current.DisplayAlert("Log Trace123", $"Path: {documentPath}", "OK");
                return 1; // Success
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Log Trace456", $"Path: {ex.Message}", "OK");
                // Log error here if needed
                return 0; // Failed
            }
        }

        public async Task<List<TaskRecord>> SearchTaskRecords(string qry)
        {
            string role = GlobalVariables.role;
            string companyId = GlobalVariables.companyid;
            string userId = GlobalVariables.userId;

            // 1. Fetch user map for username display
            var userMap = await GetCompanyUsersAsync(companyId);
            string queryLower = qry.ToLower();

            // 2. Build the Base Query (Filtering by Company at the server level)
            var query = CrossFirebaseFirestore.Current
                .GetCollection("TaskRecord")
                .WhereEqualsTo("CompanyId", companyId);

            // If Member, only fetch their tasks
            if (role == "Member")
            {
                query = query.WhereEqualsTo("assignee_id", userId);
            }

            // 3. Execute query and map to TaskRecord objects
            var snapshot = await query.GetDocumentsAsync<TaskRecord>();

            var searchResults = new List<TaskRecord>();

            foreach (var doc in snapshot.Documents)
            {
                if (doc.Data != null)
                {
                    var record = doc.Data;

                    // 4. Client-side Search Logic (Substring search)
                    bool matchesTitle = record.task_title?.ToLower().Contains(queryLower) ?? false;
                    bool matchesDesc = record.task_description?.ToLower().Contains(queryLower) ?? false;

                    if (matchesTitle || matchesDesc)
                    {
                        // Map the display name from the map
                        if (!string.IsNullOrEmpty(record.assignee_id) && userMap.ContainsKey(record.assignee_id))
                        {
                            record.DisplayUsername = userMap[record.assignee_id];
                        }

                        searchResults.Add(record);
                    }
                }
            }

            return searchResults;
        }

        public async Task<int> GetNextTaskTypeId()
        {
            try
            {
                string companyId = GlobalVariables.companyid;
                // 1. Query for the highest task_id
                var querySnapshot = await CrossFirebaseFirestore.Current
                    .GetCollection("TaskType")
                    .WhereEqualsTo("CompanyId", companyId)
                    .OrderBy("task_type_id", descending: true)
                    .LimitedTo(1)
                    .GetDocumentsAsync<TaskType>();
                await Shell.Current.DisplayAlert("Log Trace me 546", $"Path: {companyId}", "OK");
                // 2. Access the first document using .Docs
                // querySnapshot is already a List<TaskType>
                var lastDocument = querySnapshot.Documents.FirstOrDefault();

                if (lastDocument != null && lastDocument.Data != null)
                {
                    // Direct property access
                    return lastDocument.Data.task_type_id + 1;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Log Trace me 546", $"Path: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error getting next task_type_id: {ex.Message}");

            }

            // Default to 1 if collection is empty or error occurs
            return 1;
        }

        public async Task<int> GetNextTaskId()
        {
            try
            {
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    // Optional: You can try to query the cache, but it's risky
                    // Better: Use a random large number or a timestamp as a temp ID
                    return (int)DateTime.UtcNow.Ticks;
                }
                string companyId = GlobalVariables.companyid;
                // 1. Query for the highest task_id
                var querySnapshot = await CrossFirebaseFirestore.Current
                    .GetCollection("TaskRecord")
                    .WhereEqualsTo("CompanyId", companyId)
                    .OrderBy("task_id", descending: true)
                    .LimitedTo(1)
                    .GetDocumentsAsync<TaskRecord>();

                // 2. Access the first document using .Docs
                var lastDocument = querySnapshot.Documents.FirstOrDefault();

                if (lastDocument != null && lastDocument.Data != null)
                {
                    // 3. Try to get the task_id from the dictionary
                    // Firestore returns numbers as long, so Convert.ToInt32 is safest
                    return Convert.ToInt32(lastDocument.Data.task_id) + 1;

                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("task not found", $"error: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Error getting next task_id: {ex.Message}");
            }

            // Default to 1 if collection is empty or error occurs
            return new Random().Next(10000, 99999);
        }

        public async Task<int> GetNextDraftTaskId()
        {
            try
            {
                string companyId = GlobalVariables.companyid;
                // 1. Query for the highest task_id in DraftTaskRecords
                var querySnapshot = await CrossFirebaseFirestore.Current
                    .GetCollection("DraftTaskRecords")
                    .WhereEqualsTo("CompanyId", companyId)
                    .OrderBy("task_id", descending: true)
                    .LimitedTo(1)
                    .GetDocumentsAsync<TaskRecord>();

                // 2. Get the first (highest) document
                var lastDocument = querySnapshot.Documents.FirstOrDefault();

                if (lastDocument != null && lastDocument.Data != null)
                {
                    // 3. Extract task_id and increment
                    if (lastDocument.Data != null)
                    {
                        return Convert.ToInt32(lastDocument.Data.task_id) + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting next draft task_id: {ex.Message}");
            }

            // Default to 1 if the collection is empty or the query fails
            return 1;
        }
        public async Task<string> UploadToStorage(byte[] fileBytes, string fileName)
        {
            var imageClient = new HttpClient();
            var pass = "Super1969@";
            var privateKey = "private_8y3QGzNBhVPRshkO4iUIVk7E8M8="; // Get this from ImageKit Dashboard
            var uploadUrl = "https://upload.imagekit.io/api/v1/files/upload";

            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            form.Add(fileContent, "file", fileName);

            // Add required text fields
            form.Add(new StringContent(fileName), "fileName");
            form.Add(new StringContent("/tasks"), "folder");
            form.Add(new StringContent("true"), "useUniqueFileName");
            // _httpClient.DefaultRequestHeaders.Clear();

            // 2. Setup Authentication (Private Key as Username, empty Password)
            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{privateKey}:{pass}"));
            imageClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

            var response = await imageClient.PostAsync(uploadUrl, form);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);
                // The 'url' property contains the direct link to your hosted image
                return doc.RootElement.GetProperty("url").GetString();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"ImageKit Upload Failed: {error}");
                return "null";
            }
        }

        public async Task<int> SaveDraftItemAsync(TaskRecord item)
        {
            try
            {
                // 1. Handle Uploads directly on the object
                if (item.file_data_image1 != null)
                    item.file_data_image = await UploadToStorage(item.file_data_image1, item.file_name_image);
                if (item.file_data_video1 != null)
                    item.file_data_video = await UploadToStorage(item.file_data_video1, item.file_name_video);

                // 2. Set metadata
                item.task_id = await GetNextDraftTaskId();
                item.task_created_at = DateTime.UtcNow; // Match your model property name
                item.task_due_date = item.task_due_date?.ToUniversalTime();

                // 3. Add to specific Draft collection
                await CrossFirebaseFirestore.Current
                    .GetCollection("DraftTaskRecords")
                    .AddDocumentAsync(item);

                return 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving draft: {ex.Message}");
                return 0;
            }
        }



        public async Task<int> SaveItemAsync(TaskRecord item)
        {
            try
            {
                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    // 1. Handle Storage Uploads
                    if (!string.IsNullOrEmpty(item.file_name_image) && item.file_data_image1 != null)
                    {
                        item.file_data_image = await UploadToStorage(item.file_data_image1, item.file_name_image);
                    }
                    if (!string.IsNullOrEmpty(item.file_name_video) && item.file_data_video1 != null)
                    {
                        item.file_data_video = await UploadToStorage(item.file_data_video1, item.file_name_video);
                    }
                }

                // 2. Prepare remaining metadata
                item.task_id = await GetNextTaskId();

                /*  await CrossFirebaseFirestore.Current
             .GetCollection("TaskRecord")
             .AddDocumentAsync(item);*/
                CrossFirebaseFirestore.Current
          .GetCollection("TaskRecord")
          .AddDocumentAsync(item);

                // 4. Update Category Counts using your updated helper
                // await UpdateTaskTypeCount(item.task_type_id, 1);
                await UpdateTaskTypeCount(item.task_type_id, 1);

                // 5. Update Status Counts
                if (item.IsCompleted)
                {
                    // Completed category (ID 999 or 10 based on your previous snippets)
                    // await UpdateTaskTypeCount(999, 1);
                    await UpdateTaskTypeCount(999, 1);
                }
                else
                {
                    // "All Tasks" category (ID 1)
                    // await UpdateTaskTypeCount(1, 1);
                    await UpdateTaskTypeCount(1, 1);
                }

                return 1; // Success
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveItem Error: {ex.Message}");
                return 0; // Failed
            }
        }

        public async Task UpdateTaskTypeCountAsync(int typeId, int delta)
        {
            string companyId = GlobalVariables.companyid;
            // 1. Query for the document(s) with the matching task_type_id
            var querySnapshot = await CrossFirebaseFirestore.Current
                .GetCollection("TaskType")
                .WhereEqualsTo("task_type_id", typeId)
                .WhereEqualsTo("CompanyId", companyId)
                .GetDocumentsAsync<object>();

            // 2. Loop through matching documents (usually just one)
            foreach (var document in querySnapshot.Documents)
            {
                // 3. Use IntegerIncrement to update the count on the server side
                // This replaces the manual fetch + local math logic
                await document.Reference.UpdateDataAsync(
                    ("TaskCount", FieldValue.IntegerIncrement(delta))
                );
            }
        }

        public async Task<int> UpdateFinishItemAsync(TaskRecord item)
        {
            try
            {
                // 1. Find the document path using the task_id field
                string path = await GetFirestoreDocumentPath("TaskRecord", "task_id", item.task_id);

                if (string.IsNullOrEmpty(path)) return 0;

                // 2. Get a reference to that specific document
                var docRef = CrossFirebaseFirestore.Current.GetDocument($"TaskRecord/{path}");

                // 3. Perform the partial update for IsCompleted
                await docRef.UpdateDataAsync(("IsCompleted", item.IsCompleted));

                return 1; // Success
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateFinish Error: {ex.Message}");
                return 0; // Failed
            }
        }

        public async Task<int> DeleteDraftItemAsync(TaskRecord item)
        {
            try
            {
                string companyId = GlobalVariables.companyid;
                // 1. Query for the document in DraftTaskRecords matching the task_id
                var querySnapshot = await CrossFirebaseFirestore.Current
                    .GetCollection("DraftTaskRecords")
                    .WhereEqualsTo("CompanyId", companyId)
                    .WhereEqualsTo("task_id", item.task_id)
                    .LimitedTo(1)
                    .GetDocumentsAsync<TaskRecord>();

                // 2. Get the first matching document
                var document = querySnapshot.Documents.FirstOrDefault();

                if (document != null)
                {
                    // 3. Delete the document using its reference
                    await document.Reference.DeleteDocumentAsync();
                    return 1; // Success
                }

                return 0; // Document not found
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteDraft Error: {ex.Message}");
                return 0; // Failed
            }
        }

        public async Task<int> DeleteItemAsync(TaskRecord item)
        {
            try
            {
                string companyId = GlobalVariables.companyid;
                // 1. Delete the TaskRecord
                // We query by task_id to find the document, then delete it
                var taskQuery = await CrossFirebaseFirestore.Current
                    .GetCollection("TaskRecord")
                    .WhereEqualsTo("task_id", item.task_id)
                    .WhereEqualsTo("CompanyId", companyId)
                    .LimitedTo(1)
                    .GetDocumentsAsync<TaskRecord>();

                var taskDoc = taskQuery.Documents.FirstOrDefault();
                if (taskDoc != null)
                {
                    await taskDoc.Reference.DeleteDocumentAsync();
                }

                // 2. Fetch the TaskType to update the counter
                var typeQuery = await CrossFirebaseFirestore.Current
                    .GetCollection("TaskType")
                    .WhereEqualsTo("task_type_id", item.task_type_id)
                    .WhereEqualsTo("CompanyId", companyId)
                    .LimitedTo(1)
                    .GetDocumentsAsync<TaskType>();

                var typeDoc = typeQuery.Documents.FirstOrDefault();
                if (typeDoc != null)
                {
                    // The plugin can map the document to your TaskType class
                    var taskType = typeDoc.Data;

                    // 3. Logic to decrement TaskCount
                    if (taskType.TaskCount > 0)
                    {
                        taskType.TaskCount -= 1;

                        // 4. Update the TaskCount field only
                        // We use UpdateDataAsync with a dictionary for field-level updates
                        await typeDoc.Reference.UpdateDataAsync(new Dictionary<object, object>
                {
                    { "TaskCount", taskType.TaskCount }
                });
                    }
                }

                return 1;
            }
            catch (Exception ex)
            {
                // Log error: ex.Message
                return 0;
            }
        }


        public async Task<List<TaskRecord>> GetItemsTypeAsync(int type_id)
        {
            try
            {
                string companyId = GlobalVariables.companyid;
                // Fetch user map for display names
                var userMap = await GetCompanyUsersAsync(companyId);

                // 1. Build the Query using GetCollection
                // Note: Firestore numeric values are usually stored as Long
                var query = CrossFirebaseFirestore.Current
                    .GetCollection("TaskRecord")
                    .WhereEqualsTo("task_type_id", type_id)
                    .WhereEqualsTo("CompanyId", companyId); // Usually good practice to filter by company too

                // 2. Execute and map to TaskRecord objects automatically
                var snapshot = await query.GetDocumentsAsync<TaskRecord>();

                var taskRecords = new List<TaskRecord>();

                // 3. Process the results
                foreach (var doc in snapshot.Documents)
                {
                    if (doc.Data != null)
                    {
                        var record = doc.Data;

                        // 4. Map the DisplayUsername from your local map
                        if (!string.IsNullOrEmpty(record.assignee_id) && userMap.ContainsKey(record.assignee_id))
                        {
                            record.DisplayUsername = userMap[record.assignee_id];
                        }

                        taskRecords.Add(record);
                    }
                }

                return taskRecords;
            }
            catch (Exception ex)
            {
                // Log the exception (ex.Message)
                return new List<TaskRecord>();
            }
        }


        public async Task LoadSettingsToGlobalsAsync()
        {
            // Fetch the settings document (singleton, e.g. "settings/global")
            var settings = await GetSettingsAsync();

            if (settings != null)
            {
                GlobalVariables.defTaskType = settings.def_task_type_id;
                GlobalVariables.IsQuckTaskVisible = settings.is_quickTaskVisible;
                GlobalVariables.IsCompletedTaskVisible = settings.is_completedTaskVisible;
                GlobalVariables.ReminderLanguage = settings.reminderLanguage;
            }
        }

        public WindowsFirestoreService()
        {
            _firestore = CrossFirebaseFirestore.Current;

        }

        public async Task<int> SaveTaskTypeAsync(TaskType newTaskType)
        {
            try
            {
                // 1. Default sort order if not set
                if (newTaskType.sort_order == 0)
                    newTaskType.sort_order = 1;
                newTaskType.task_type_id = await GetNextTaskTypeId();
                newTaskType.CompanyId = GlobalVariables.companyid;
                // 2. Prepare the data (Plugin.Firebase handles int/string conversion)
                /*
                var data = new Dictionary<string, object>
        {
            { "TaskCount", newTaskType.TaskCount },
            { "sort_order", newTaskType.sort_order },
            { "task_type", newTaskType.task_type ?? "" },
            { "task_type_id", await GetNextTaskTypeId() }
        };

                // 3. Add to the TaskType collection
                // This generates a random Document ID automatically
                await CrossFirebaseFirestore.Current
                    .GetCollection("TaskType")
                    .AddDocumentAsync(data);
                */

                await CrossFirebaseFirestore.Current
            .GetCollection("TaskType")
            .AddDocumentAsync(newTaskType);
                return 1;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveTaskType Error: {ex.Message}");
                return 0; // failed
            }
        }
        public async Task<int> UpdateDueDateToNextDayAsync(TaskRecord taskRecord)
        {
            try
            {
                string companyId = GlobalVariables.companyid;

                // 1. Query to find the document by task_id
                var querySnapshot = await CrossFirebaseFirestore.Current
                    .GetCollection("TaskRecord")
                    .WhereEqualsTo("task_id", taskRecord.task_id)
                    .WhereEqualsTo("CompanyId", companyId)
                    .LimitedTo(1)
                    .GetDocumentsAsync<TaskRecord>();

                var document = querySnapshot.Documents.FirstOrDefault();

                if (document != null)
                {
                    // 2. Fetch the actual existing data loaded from Firestore
                    var existingData = document.Data;

                    // 🌟 3. Calculate "Next Day of Current Date"
                    // Using DateTimeOffset to maintain consistency with your project structure
                    DateTimeOffset nextDay = DateTimeOffset.Now.AddDays(1);

                    // 4. Check if the due date is actually different from the current one stored
                    bool dueDateChanged = existingData.task_due_date != nextDay;

                    if (dueDateChanged)
                    {
                        // 5. Create the dynamic update list
                        var updates = new List<(string, object)>
                {
                    ("task_due_date", (object)nextDay)
                };

                        // 6. Execute the targeted partial update
                        await document.Reference.UpdateDataAsync(updates.ToArray());

                        System.Diagnostics.Debug.WriteLine("Firestore due date updated to next day successfully.");
                        return 1; // Success
                    }

                    // No changes found. Skip database write entirely!
                    System.Diagnostics.Debug.WriteLine("Due date is already set to next day. Skipping update.");
                    return 1; // Return success since intent is fulfilled
                }

                return 0; // Document not found
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateDueDateToNextDay Error: {ex.Message}");
                return 0; // Failed
            }
        }

        public async Task<int> UpdateItemDescAsync(TaskRecord taskRecord)
        {
            try
            {
                string companyId = GlobalVariables.companyid;

                // 1. Query to find the document by task_id
                var querySnapshot = await CrossFirebaseFirestore.Current
                    .GetCollection("TaskRecord")
                    .WhereEqualsTo("task_id", taskRecord.task_id)
                    .WhereEqualsTo("CompanyId", companyId)
                    .LimitedTo(1)
                    .GetDocumentsAsync<TaskRecord>();

                var document = querySnapshot.Documents.FirstOrDefault();

                if (document != null)
                {
                    // 2. Fetch the actual existing data loaded from Firestore
                    var existingData = document.Data;

                    string newDescription = taskRecord.task_description ?? "";

                    // Handle matching the DateTimeOffset correctly
                    DateTimeOffset? newDueDate = taskRecord.task_due_date;

                    // 3. Check if anything actually changed
                    bool descriptionChanged = existingData.pending_description != newDescription;
                    bool dueDateChanged = existingData.task_due_date != newDueDate;

                    if (descriptionChanged || dueDateChanged)
                    {
                        // 4. Create a dynamic update list depending on what precisely changed
                        var updates = new List<(string, object)>();

                        if (descriptionChanged)
                        {
                            updates.Add(("pending_description", newDescription));
                        }

                        if (dueDateChanged)
                        {
                            updates.Add(("task_due_date", newDueDate.HasValue ? (object)newDueDate.Value : null));
                        }

                        // 5. Execute the targeted partial update
                        await document.Reference.UpdateDataAsync(updates.ToArray());

                        System.Diagnostics.Debug.WriteLine("Firestore document updated successfully.");
                        return 1; // Success
                    }

                    // No changes found. Skip database write entirely!
                    System.Diagnostics.Debug.WriteLine("No changes detected. Skipping Firestore update.");
                    return 1; // Return success since intent is fulfilled
                }

                return 0; // Document not found
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateItemDesc Error: {ex.Message}");
                return 0; // Failed
            }
        }

        public async Task<int> UpdateItemMainAsync(TaskRecord taskRecord)
        {
            try
            {

                // await Shell.Current.DisplayAlert("Log Trace me 456", $"Path: {taskRecord.task_description}", "OK");
                string companyId = GlobalVariables.companyid;

                // 1. Query to find the document by task_id
                var querySnapshot = await CrossFirebaseFirestore.Current
                    .GetCollection("TaskRecord")
                    .WhereEqualsTo("task_id", taskRecord.task_id)
                    .WhereEqualsTo("CompanyId", companyId)
                    .LimitedTo(1)
                    .GetDocumentsAsync<TaskRecord>();

                var document = querySnapshot.Documents.FirstOrDefault();

                if (document != null)
                {
                    // 2. Fetch the actual existing data loaded from Firestore
                    var existingData = document.Data;

                    string newTitle = taskRecord.task_title ?? "";
                    string newDescription = taskRecord.task_description ?? "";
                    // Handle matching the DateTimeOffset correctly
                    DateTimeOffset? newDueDate = taskRecord.task_due_date;

                    // 3. Check if anything actually changed
                    bool titleChanged = existingData.task_title != newTitle;
                    bool descriptionChanged = existingData.task_description != newDescription;
                    bool dueDateChanged = existingData.task_due_date != newDueDate;

                    if (titleChanged || descriptionChanged || dueDateChanged)
                    {
                        // 4. Create a dynamic update list depending on what precisely changed
                        var updates = new List<(string, object)>();

                        if (titleChanged)
                        {
                            updates.Add(("task_title", newTitle));
                        }

                        if (descriptionChanged)
                        {
                            updates.Add(("task_description", newDescription));
                        }

                        if (dueDateChanged)
                        {
                            updates.Add(("task_due_date", newDueDate.HasValue ? (object)newDueDate.Value : null));
                        }

                        // 5. Execute the targeted partial update
                        await document.Reference.UpdateDataAsync(updates.ToArray());
                        //    await Shell.Current.DisplayAlert("Log Trace me123", $"Path: {newDescription}", "OK");
                        System.Diagnostics.Debug.WriteLine("Firestore document updated successfully.");
                        return 1; // Success
                    }

                    // No changes found. Skip database write entirely!
                    System.Diagnostics.Debug.WriteLine("No changes detected. Skipping Firestore update.");
                    return 1; // Return success since intent is fulfilled
                }

                return 0; // Document not found
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateItemDesc Error: {ex.Message}");
                return 0; // Failed
            }
        }

        public async Task<int> UpdateTaskTypeAsync(TaskType updatedTaskType)
        {
            try
            {
                string companyId = GlobalVariables.companyid;
                // 1. Query for the document with the matching task_type_id
                var querySnapshot = await CrossFirebaseFirestore.Current
                    .GetCollection("TaskType")
                    .WhereEqualsTo("task_type_id", updatedTaskType.task_type_id)
                    .WhereEqualsTo("CompanyId", companyId)
                    .LimitedTo(1)
                    .GetDocumentsAsync<TaskType>();

                var document = querySnapshot.Documents.FirstOrDefault();

                if (document != null)
                {
                    // 2. Perform the update using Tuples
                    // This replaces the manual JSON body and updateMask URL
                    await document.Reference.UpdateDataAsync(
                        ("CompanyId", updatedTaskType.CompanyId),
                        ("TaskCount", updatedTaskType.TaskCount),
                        ("sort_order", updatedTaskType.sort_order),
                        ("task_type", updatedTaskType.task_type ?? ""),
                        ("task_type_id", updatedTaskType.task_type_id)
                    );

                    return 1; // Success
                }

                return 0; // Document not found
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateTaskType Error: {ex.Message}");
                return 0; // Failed
            }
        }

        public async Task<int> DeleteTaskTypeAsync(TaskType taskTypeToDelete)
        {
            try
            {
                string companyId = GlobalVariables.companyid;
                //  await Shell.Current.DisplayAlert("Log Trace me 123", $"Path: {taskTypeToDelete.task_type_id}", "OK");
                // 1. Query to find the document with the matching task_type_id
                var querySnapshot = await CrossFirebaseFirestore.Current
                    .GetCollection("TaskType")
                    .WhereEqualsTo("task_type_id", taskTypeToDelete.task_type_id)
                    .WhereEqualsTo("CompanyId", companyId)
                    .LimitedTo(1)
                    .GetDocumentsAsync<TaskType>();

                var document = querySnapshot.Documents.FirstOrDefault();
                //  await Shell.Current.DisplayAlert("Log Trace me 546", $"Path: {document.Reference.Id}", "OK");
                // 2. If the document exists, delete it
                if (document != null)
                {
                    // await document.Reference.DeleteDocumentAsync();
                    string docId = document.Reference.Id;
                    await CrossFirebaseFirestore.Current
                        .GetCollection("TaskType")
                        .GetDocument(docId)
                        .DeleteDocumentAsync();
                    Console.WriteLine($"Deleted task type: {taskTypeToDelete.task_type}");
                    return 1; // Success
                }
                //  await Shell.Current.DisplayAlert("task not found", $"error: {document.Reference.Id}", "OK");
                Console.WriteLine("Task type not found.");
                return 0; // Document doesn't exist
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting task type: {ex.Message}");
                await Shell.Current.DisplayAlert("task not found", $"error: {ex.Message}", "OK");
                return 0; // Failure
            }
        }
        // Save def_task_type_id
        public async Task<int> SaveSettingItemAsync(int item1)
        {
            try
            {
                // 1. Reference the specific document directly by its ID
                var docRef = CrossFirebaseFirestore.Current
                    .GetCollection("Settings")
                    .GetDocument("n0U1sE2k1bGj0tkdkpWg");

                // 2. Perform a partial update on the specific field
                // This is equivalent to the REST 'updateMask'
                await docRef.UpdateDataAsync(("def_task_type_id", item1));

                return 1; // Success
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveSettingItem Error: {ex.Message}");
                return 0; // Failed
            }
        }

        public async Task<int> SaveSettingItemAsync(bool item1)
        {
            try
            {
                // 1. Target the specific Settings document directly by ID
                var docRef = CrossFirebaseFirestore.Current
                    .GetCollection("Settings")
                    .GetDocument("n0U1sE2k1bGj0tkdkpWg");

                // 2. Update only the boolean field (equivalent to updateMask)
                await docRef.UpdateDataAsync(("is_quickTaskVisible", item1));

                return 1; // Success
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveSettingItem Error: {ex.Message}");
                return 0; // Failed
            }
        }
        // Save reminderLanguage
        public async Task<int> SaveSettingLangAsync(string item1)
        {
            try
            {
                // 1. Reference the specific document ID
                var docRef = CrossFirebaseFirestore.Current
                    .GetCollection("Settings")
                    .GetDocument("n0U1sE2k1bGj0tkdkpWg");

                // 2. Perform the partial update for the language string
                await docRef.UpdateDataAsync(("reminderLanguage", item1 ?? "en"));

                return 1; // Success
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveSettingLang Error: {ex.Message}");
                return 0; // Failed
            }
        }

        // Save is_completedTaskVisible
        public async Task<int> SaveSettingOneItemAsync(bool item1)
        {
            try
            {
                // 1. Target the Settings document directly by its ID
                var docRef = CrossFirebaseFirestore.Current
                    .GetCollection("Settings")
                    .GetDocument("n0U1sE2k1bGj0tkdkpWg");

                // 2. Update only the specific boolean field
                // This replaces the REST 'updateMask' and manual JSON payload
                await docRef.UpdateDataAsync(("is_completedTaskVisible", item1));

                return 1; // Success
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveSettingOneItem Error: {ex.Message}");
                return 0; // Failed
            }
        }

        public async Task<ObservableCollection<User>> GetAssigneeAsync()
        {
            try
            {
                string companyId = GlobalVariables.companyid;
                // 1. Fetch all documents from the "User" collection
                // Passing <User> allows the plugin to automatically map fields
                var querySnapshot = await CrossFirebaseFirestore.Current
                    .GetCollection("User")
                    .WhereEqualsTo("CompanyId", companyId)
                    .GetDocumentsAsync<User>();

                // 2. Map the documents to a list of User objects
                var users = querySnapshot.Documents.Select(doc =>
                {
                    var user = doc.Data;
                    // The document ID (the random string) is available via doc.Id
                    user.Id = doc.Data.Id;
                    return user;
                });

                return new ObservableCollection<User>(users);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching assignees: {ex.Message}");
                return new ObservableCollection<User>();
            }
        }



        //fetch task types
        public async Task<List<TaskType>> GetTaskTypesAsync()
        {
            string companyId = GlobalVariables.companyid;
            // 1. Fetch from Firestore
            var querySnapshot = await CrossFirebaseFirestore.Current
                .GetCollection("TaskType")
                .WhereEqualsTo("CompanyId", companyId)
                .GetDocumentsAsync<TaskType>();

            var existingData = querySnapshot.Documents.Select(d => d.Data).ToList();

            // 2. If collection is empty, seed initial data
            if (existingData.Count == 0)
            {
                var taskTypes = new List<TaskType>
        {
            new TaskType { task_type_id = 1, task_type = "All List", sort_order = 0 },
            new TaskType { task_type_id = 2, task_type = "Default", sort_order = 1 },
            new TaskType { task_type_id = 3, task_type = "Health & Wellness", sort_order = 1 },
            new TaskType { task_type_id = 4, task_type = "Household", sort_order = 1 },
            new TaskType { task_type_id = 5, task_type = "Personal", sort_order = 1 },
            new TaskType { task_type_id = 6, task_type = "Shopping", sort_order = 1 },
            new TaskType { task_type_id = 7, task_type = "Social & Relationship", sort_order = 1 },
            new TaskType { task_type_id = 8, task_type = "Travel", sort_order = 1 },
            new TaskType { task_type_id = 9, task_type = "Work", sort_order = 1 },
            new TaskType { task_type_id = 10, task_type = "Completed Tasks List", sort_order = 999 }
        };

                // Use a Batch to save all default types at once
                var batch = CrossFirebaseFirestore.Current.CreateBatch();
                var collection = CrossFirebaseFirestore.Current.GetCollection("TaskType");

                foreach (var taskType in taskTypes)
                {
                    // 1. Generate a new unique ID first
                    // Most v4.0 setups use collection.GetDocument(Guid.NewGuid().ToString()) 
                    // OR a dedicated ID generator.
                    var newId = Guid.NewGuid().ToString();
                    var newDocRef = collection.GetDocument(newId);

                    batch.SetData(newDocRef, taskType);
                }

                await batch.CommitAsync();
                existingData = taskTypes;
            }

            // 3. Return sorted list
            return existingData
                .OrderBy(t => t.sort_order)
                .ThenBy(t => t.task_type)
                .ToList();
        }



        // Fetch Settings
        public async Task<Settings> GetSettingsAsync()
        {
            try
            {
                // 1. Fetch the specific document snapshot
                // We use the ID 'n0U1sE2k1bGj0tkdkpWg' directly
                var snapshot = await CrossFirebaseFirestore.Current
                    .GetCollection("Settings")
                    .GetDocument("n0U1sE2k1bGj0tkdkpWg")
                    .GetDocumentSnapshotAsync<Settings>();

                // 2. Return the mapped data
                if (snapshot.Data != null)
                {
                    return snapshot.Data;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching settings: {ex.Message}");
            }

            // Return a default object if the document doesn't exist or an error occurs
            return new Settings();
        }
    }
}
