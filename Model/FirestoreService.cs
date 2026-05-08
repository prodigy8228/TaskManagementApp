using Newtonsoft.Json;
using Plugin.Firebase.Firestore;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TaskManagement.Model;

public class FirestoreService : IFirestoreService
{
    private readonly HttpClient _httpClient;
    private readonly string _projectId = "sprinty-cded8"; // your Firebase project ID
    private readonly string _docId = "global"; // single settings document
    private const string apiKey = "AIzaSyAJkcrWu6U49gLeXUSeN3KAY-Jt2uJq_6E"; // from Firebase project settings
    private const string FirebaseAuthUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";
    // private const string FirestoreBaseUrl = "https://googleapis.com[PROJECT_ID]/databases/(default)/documents/";
    private const string FirestoreBaseUrl = $"https://firestore.googleapis.com/v1/projects/sprinty-cded8/databases/(default)/documents/";
    public static string idToken;
    public async Task<User?> LoginAndGetUserAsync(string email, string password)
    {
        // 1. Auth Call: Get ID Token and LocalId (Uid)
        var authData = new { email, password, returnSecureToken = true };
        var authResponse = await _httpClient.PostAsJsonAsync(FirebaseAuthUrl, authData);

        if (!authResponse.IsSuccessStatusCode) return null;

        var authResult = await authResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // 2. Firestore Call: Fetch User Document using the Token
        var request = new HttpRequestMessage(HttpMethod.Get, $"{FirestoreBaseUrl}User/{authResult.LocalId}");
        // request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.IdToken);
        _httpClient.DefaultRequestHeaders.Authorization =
       new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.IdToken);

        idToken = authResult.IdToken;
        var userResponse = await _httpClient.SendAsync(request);
        if (!userResponse.IsSuccessStatusCode) return null;

        var jsonDoc = await userResponse.Content.ReadFromJsonAsync<JsonElement>();

        // 3. Map the REST JSON to your User object (Using your GetVal method)
        var fields = jsonDoc.GetProperty("fields");
        return new User
        {
            Id = authResult.LocalId,
            Username = GetVal(fields, "Username", "stringValue"),
            Role = GetVal(fields, "Role", "stringValue"),
            CompanyId = GetVal(fields, "CompanyId", "stringValue"),
            Email = GetVal(fields, "Email", "stringValue")
        };
    }

    public async Task<string> GetIdTokenAsync(string email, string password)
    {
        // 1. Auth Call: Get ID Token and LocalId (Uid)
        var authData = new { email, password, returnSecureToken = true };
        var authResponse = await _httpClient.PostAsJsonAsync(FirebaseAuthUrl, authData);

        if (!authResponse.IsSuccessStatusCode) return null;

        var authResult = await authResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // 2. Firestore Call: Fetch User Document using the Token
        var request = new HttpRequestMessage(HttpMethod.Get, $"{FirestoreBaseUrl}User/{authResult.LocalId}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.IdToken);
        idToken = authResult.IdToken;
        return idToken;
    }
    public class AuthResponse { public string LocalId { get; set; } public string IdToken { get; set; } }
    public async Task<Dictionary<string, string>> GetCompanyUsersAsync(string targetCompanyId)
    {
        string url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

        // Build the StructuredQuery payload
        var queryPayload = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "User" } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "CompanyId" },
                        op = "EQUAL",
                        value = new { stringValue = targetCompanyId }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, queryPayload);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var usersDoc = JsonDocument.Parse(json);
        var userMap = new Dictionary<string, string>();

        // runQuery returns an array of results, each containing a "document" property
        foreach (var result in usersDoc.RootElement.EnumerateArray())
        {
            if (result.TryGetProperty("document", out var uDoc))
            {
                var uFields = uDoc.GetProperty("fields");
                string uid = uDoc.GetProperty("name").GetString().Split('/').Last();
                string name = GetVal(uFields, "Username", "stringValue") ?? "Unknown";

                userMap[uid] = name;
            }
        }
        return userMap;
    }

    public async Task<ObservableCollection<TaskRecord>> GetItemsTypeNotDoneDateAsync()
    {
        string url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";
        // 1. Prepare today's date in RFC 3339 format (required by Firestore)
        string todayStr = DateTime.UtcNow.Date.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // 2. Define the Structured Query
        var queryPayload = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "TaskRecord" } },
                where = new
                {
                    compositeFilter = new
                    {
                        op = "AND",
                        filters = new object[]
                        {
                            new {
                                fieldFilter = new {
                                    field = new { fieldPath = "task_due_date" },
                                    op = "EQUAL",
                                    value = new { timestampValue = todayStr }
                                }
                            },
                            new {
                                fieldFilter = new {
                                    field = new { fieldPath = "IsCompleted" },
                                    op = "EQUAL",
                                    value = new { booleanValue = false }
                                }
                            }
                        }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, queryPayload);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        // 3. Parse the results
        var taskRecords = new ObservableCollection<TaskRecord>();
        using var doc = JsonDocument.Parse(json);

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            // runQuery returns an array where each item has a "document" property
            if (element.TryGetProperty("document", out var firestoreDoc))
            {
                var fields = firestoreDoc.GetProperty("fields");
                var record = new TaskRecord
                {
                    task_id = int.Parse(GetVal(fields, "task_id", "integerValue") ?? "0"),
                    task_type_id = int.Parse(GetVal(fields, "task_type_id", "integerValue") ?? "0"),

                    // Strings
                    task_title = GetVal(fields, "task_title", "stringValue"),
                    task_description = GetVal(fields, "task_description", "stringValue"),
                    file_name_image = GetVal(fields, "file_name_image", "stringValue"),
                    file_name_video = GetVal(fields, "file_name_video", "stringValue"),
                    IsCompleted = fields.TryGetProperty("IsCompleted", out var ic) && ic.GetProperty("booleanValue").GetBoolean(),
                    IsSelected = fields.TryGetProperty("IsSelected", out var ise) && ise.GetProperty("booleanValue").GetBoolean(),

                    // Enum (Parsing string back to RepeatOption)
                    Repeat = Enum.TryParse<RepeatOption>(GetVal(fields, "Repeat", "stringValue"), out var res) ? res : RepeatOption.NoRepeat,

                    // Timestamps
                    task_created_at = DateTime.Parse(GetVal(fields, "task_created_date", "timestampValue") ?? DateTime.UtcNow.ToString()),
                    task_due_date = fields.TryGetProperty("task_due_date", out var td) ? DateTime.Parse(td.GetProperty("timestampValue").GetString()) : (DateTime?)null,
                    // Bytes (Base64 strings decoded back to byte arrays)
                    file_data_image = fields.TryGetProperty("file_data_image", out var fdi) ? fdi.GetProperty("stringValue").GetString() : null,
                    file_data_video = fields.TryGetProperty("file_data_video", out var fdv) ? fdv.GetProperty("stringValue").GetString() : null,
                    assignee_id = GetVal(fields, "assignee_id", "stringValue"),
                    userId = GetVal(fields, "userId", "stringValue"),
                    pending_description = GetVal(fields, "pending_description", "stringValue"),
                    CompanyId = GetVal(fields, "CompanyId", "stringValue")
                };


                taskRecords.Add(record);
            }
        }

        return taskRecords;
    }

    public async Task<ObservableCollection<TaskRecord>> GetDraftItemsAsync()
    {
        // 1. Determine the filter based on Role
        // Assuming you store these in a global 'App.CurrentUser' object after login
        string role = GlobalVariables.role;
        string companyId = GlobalVariables.companyid;
        string userId = GlobalVariables.userId;
        var userMap = await GetCompanyUsersAsync(companyId);
        string url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

        // Build the query filters
        var filters = new List<object>
    {
        new { fieldFilter = new { field = new { fieldPath = "CompanyId" }, op = "EQUAL", value = new { stringValue = companyId } } }
    };

        // If Member, add a second filter for their specific ID
        if (role == "Member")
        {
            filters.Add(new { fieldFilter = new { field = new { fieldPath = "assignee_id" }, op = "EQUAL", value = new { stringValue = userId } } });
        }

        var queryPayload = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "DraftTaskRecords" } },
                where = new
                {
                    compositeFilter = new
                    {
                        op = "AND",
                        filters = filters.ToArray()
                    }
                }
            }
        };

        // 2. Execute the Query
        var taskRecords = new ObservableCollection<TaskRecord>();
        var response = await _httpClient.PostAsJsonAsync(url, queryPayload);
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            // 3. Parse the results

            using var doc = JsonDocument.Parse(json);

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                // runQuery returns an array where each item has a "document" property
                if (element.TryGetProperty("document", out var firestoreDoc))
                {
                    var fields = firestoreDoc.GetProperty("fields");
                    var record = new TaskRecord
                    {
                        task_id = int.Parse(GetVal(fields, "task_id", "integerValue") ?? "0"),
                        task_type_id = int.Parse(GetVal(fields, "task_type_id", "integerValue") ?? "0"),

                        // Strings
                        task_title = GetVal(fields, "task_title", "stringValue"),
                        task_description = GetVal(fields, "task_description", "stringValue"),
                        file_name_image = GetVal(fields, "file_name_image", "stringValue"),
                        file_name_video = GetVal(fields, "file_name_video", "stringValue"),
                        IsCompleted = fields.TryGetProperty("IsCompleted", out var ic) && ic.GetProperty("booleanValue").GetBoolean(),
                        IsSelected = fields.TryGetProperty("IsSelected", out var ise) && ise.GetProperty("booleanValue").GetBoolean(),

                        // Enum (Parsing string back to RepeatOption)
                        Repeat = Enum.TryParse<RepeatOption>(GetVal(fields, "Repeat", "stringValue"), out var res) ? res : RepeatOption.NoRepeat,

                        // Timestamps
                        task_created_at = DateTime.Parse(GetVal(fields, "task_created_date", "timestampValue") ?? DateTime.UtcNow.ToString()),
                        task_due_date = fields.TryGetProperty("task_due_date", out var td) ? DateTime.Parse(td.GetProperty("timestampValue").GetString()) : (DateTime?)null,

                        // Bytes (Base64 strings decoded back to byte arrays)
                        file_data_image = fields.TryGetProperty("file_data_image", out var fdi) ? fdi.GetProperty("stringValue").GetString() : null,
                        file_data_video = fields.TryGetProperty("file_data_video", out var fdv) ? fdv.GetProperty("stringValue").GetString() : null,
                        assignee_id = GetVal(fields, "assignee_id", "stringValue"),
                        userId = GetVal(fields, "userId", "stringValue"),
                        CompanyId = GetVal(fields, "CompanyId", "stringValue")
                    };
                    if (!string.IsNullOrEmpty(record.assignee_id) && userMap.ContainsKey(record.assignee_id))
                    {
                        record.DisplayUsername = userMap[record.assignee_id];
                    }
                    taskRecords.Add(record);
                }
            }
        }
        return taskRecords;
    }

    public async Task<ObservableCollection<TaskRecord>> GetItemsAsync()
    {
        // 1. Determine the filter based on Role
        // Assuming you store these in a global 'App.CurrentUser' object after login
        string role = GlobalVariables.role;
        string companyId = GlobalVariables.companyid;
        string userId = GlobalVariables.userId;
        var userMap = await GetCompanyUsersAsync(companyId);
        string url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

        // Build the query filters
        var filters = new List<object>
    {
        new { fieldFilter = new { field = new { fieldPath = "CompanyId" }, op = "EQUAL", value = new { stringValue = companyId } } }
    };

        // If Member, add a second filter for their specific ID
        if (role == "Member")
        {
            filters.Add(new { fieldFilter = new { field = new { fieldPath = "assignee_id" }, op = "EQUAL", value = new { stringValue = userId } } });
        }

        var queryPayload = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "TaskRecord" } },
                where = new
                {
                    compositeFilter = new
                    {
                        op = "AND",
                        filters = filters.ToArray()
                    }
                }
            }
        };

        // 2. Execute the Query
        var response = await _httpClient.PostAsJsonAsync(url, queryPayload);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        // 3. Parse the results
        var taskRecords = new ObservableCollection<TaskRecord>();
        using var doc = JsonDocument.Parse(json);

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            // runQuery returns an array where each item has a "document" property
            if (element.TryGetProperty("document", out var firestoreDoc))
            {
                var fields = firestoreDoc.GetProperty("fields");
                var record = new TaskRecord
                {
                    task_id = int.Parse(GetVal(fields, "task_id", "integerValue") ?? "0"),
                    task_type_id = int.Parse(GetVal(fields, "task_type_id", "integerValue") ?? "0"),

                    // Strings
                    task_title = GetVal(fields, "task_title", "stringValue"),
                    task_description = GetVal(fields, "task_description", "stringValue"),
                    file_name_image = GetVal(fields, "file_name_image", "stringValue"),
                    file_name_video = GetVal(fields, "file_name_video", "stringValue"),
                    IsCompleted = fields.TryGetProperty("IsCompleted", out var ic) && ic.GetProperty("booleanValue").GetBoolean(),
                    IsSelected = fields.TryGetProperty("IsSelected", out var ise) && ise.GetProperty("booleanValue").GetBoolean(),

                    // Enum (Parsing string back to RepeatOption)
                    Repeat = Enum.TryParse<RepeatOption>(GetVal(fields, "Repeat", "stringValue"), out var res) ? res : RepeatOption.NoRepeat,

                    // Timestamps
                    task_created_at = DateTime.Parse(GetVal(fields, "task_created_date", "timestampValue") ?? DateTime.UtcNow.ToString()),
                    task_due_date = fields.TryGetProperty("task_due_date", out var td) ? DateTime.Parse(td.GetProperty("timestampValue").GetString()) : (DateTime?)null,
                    // Bytes (Base64 strings decoded back to byte arrays)
                    file_data_image = fields.TryGetProperty("file_data_image", out var fdi) ? fdi.GetProperty("stringValue").GetString() : null,
                    file_data_video = fields.TryGetProperty("file_data_video", out var fdv) ? fdv.GetProperty("stringValue").GetString() : null,
                    assignee_id = GetVal(fields, "assignee_id", "stringValue"),
                    userId = GetVal(fields, "userId", "stringValue"),
                    pending_description = GetVal(fields, "pending_description", "stringValue"),
                    CompanyId = GetVal(fields, "CompanyId", "stringValue")
                };
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
        string userUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

        // Build the StructuredQuery payload
        var queryPayload = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "User" } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "CompanyId" },
                        op = "EQUAL",
                        value = new { stringValue = GlobalVariables.companyid }
                    }
                }
            }
        };

        var userResponse = await _httpClient.PostAsJsonAsync(userUrl, queryPayload);
        userResponse.EnsureSuccessStatusCode();

        var json = await userResponse.Content.ReadAsStringAsync();
        using var usersDoc = JsonDocument.Parse(json);
        var userMap = new Dictionary<string, string>();

        // runQuery returns an array of results, each containing a "document" property
        foreach (var result in usersDoc.RootElement.EnumerateArray())
        {
            if (result.TryGetProperty("document", out var uDoc))
            {
                var uFields = uDoc.GetProperty("fields");
                string uid = uDoc.GetProperty("name").GetString().Split('/').Last();
                string name = GetVal(uFields, "Username", "stringValue") ?? "Unknown";

                userMap[uid] = name;
            }
        }



        string url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/TaskRecord";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json1 = await response.Content.ReadAsStringAsync();

        var firestoreResponse = JsonDocument.Parse(json1);

        var taskRecords = new ObservableCollection<TaskRecord>();
        foreach (var doc in firestoreResponse.RootElement.GetProperty("documents").EnumerateArray())
        {
            var fields = doc.GetProperty("fields");

            var record = new TaskRecord
            {
                task_id = int.Parse(GetVal(fields, "task_id", "integerValue") ?? "0"),
                task_type_id = int.Parse(GetVal(fields, "task_type_id", "integerValue") ?? "0"),

                // Strings
                task_title = GetVal(fields, "task_title", "stringValue"),
                task_description = GetVal(fields, "task_description", "stringValue"),
                file_name_image = GetVal(fields, "file_name_image", "stringValue"),
                file_name_video = GetVal(fields, "file_name_video", "stringValue"),
                IsCompleted = fields.TryGetProperty("IsCompleted", out var ic) && ic.GetProperty("booleanValue").GetBoolean(),
                IsSelected = fields.TryGetProperty("IsSelected", out var ise) && ise.GetProperty("booleanValue").GetBoolean(),

                // Enum (Parsing string back to RepeatOption)
                Repeat = Enum.TryParse<RepeatOption>(GetVal(fields, "Repeat", "stringValue"), out var res) ? res : RepeatOption.NoRepeat,

                // Timestamps
                task_created_at = DateTime.Parse(GetVal(fields, "task_created_date", "timestampValue") ?? DateTime.UtcNow.ToString()),
                task_due_date = fields.TryGetProperty("task_due_date", out var td) ? DateTime.Parse(td.GetProperty("timestampValue").GetString()) : (DateTime?)null,

                // Bytes (Base64 strings decoded back to byte arrays)
                file_data_image = fields.TryGetProperty("file_data_image", out var fdi) ? fdi.GetProperty("stringValue").GetString() : null,
                file_data_video = fields.TryGetProperty("file_data_video", out var fdv) ? fdv.GetProperty("stringValue").GetString() : null,
                assignee_id = GetVal(fields, "assignee_id", "stringValue")
            };
            if (!string.IsNullOrEmpty(record.assignee_id) && userMap.ContainsKey(record.assignee_id))
            {
                record.DisplayUsername = userMap[record.assignee_id];
            }
            taskRecords.Add(record);
        }

        return taskRecords;
    }
    public string GetVal(JsonElement fields, string name, string type)
    {
        return fields.TryGetProperty(name, out var prop) ? prop.GetProperty(type).GetString() : null;
    }
    public async Task<string> GetFirestoreDocumentPath(string collection, string fieldName, int value1)
    {
        var queryUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";
        var query = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = collection } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = fieldName },
                        op = "EQUAL",
                        value = new { integerValue = value1.ToString() }
                    }
                },
                limit = 1
            }
        };

        var response = await _httpClient.PostAsJsonAsync(queryUrl, query);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        if (doc.RootElement.GetArrayLength() > 0 && doc.RootElement[0].TryGetProperty("document", out var docEl))
        {
            return docEl.GetProperty("name").GetString();
        }
        return null;
    }
    public async Task UpdateTaskTypeCount(int typeId, int change)
    {
        string path = await GetFirestoreDocumentPath("TaskType", "task_type_id", typeId);
        if (string.IsNullOrEmpty(path)) return;

        // In REST, you usually have to fetch current count first OR 
        // use FieldValue.increment (not available in basic REST PATCH without a transform).
        // For simplicity, fetch current -> update:
        int currentCount = await GetCurrentCount(path);

        var updateUrl = $"https://firestore.googleapis.com/v1/{path}?updateMask.fieldPaths=TaskCount";
        var body = new { fields = new { TaskCount = new { integerValue = currentCount + change } } };

        await _httpClient.PatchAsJsonAsync(updateUrl, body);
    }
    public async Task<int> GetCurrentCount(string documentPath)
    {
        // 1. Build the GET URL for the specific TaskType document
        string url = $"https://firestore.googleapis.com/v1/{documentPath}";

        // 2. Fetch the document
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return 0;

        var jsonResponse = await response.Content.ReadAsStringAsync();

        // 3. Parse the 'TaskCount' field
        using var doc = System.Text.Json.JsonDocument.Parse(jsonResponse);

        if (doc.RootElement.TryGetProperty("fields", out var fields) &&
            fields.TryGetProperty("TaskCount", out var countField))
        {
            // Firestore returns integerValue as a string (e.g., "5")
            if (countField.TryGetProperty("integerValue", out var value))
            {
                return int.Parse(value.GetString());
            }
        }

        return 0; // Default if field doesn't exist
    }

    public async Task<int> GetOldTypeIdFromPath(string documentPath)
    {
        // 1. Build the GET URL for the specific document
        string url = $"https://firestore.googleapis.com/v1/{documentPath}";

        // 2. Fetch the document data
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();

        // 3. Parse the Firestore "fields" structure
        using var doc = System.Text.Json.JsonDocument.Parse(jsonResponse);

        // Firestore REST API returns fields in a nested 'fields' property
        if (doc.RootElement.TryGetProperty("fields", out var fields) &&
            fields.TryGetProperty("task_type_id", out var typeIdField) &&
            typeIdField.TryGetProperty("integerValue", out var value))
        {
            // integerValue is returned as a string in the JSON, so parse to int
            return int.Parse(value.GetString());
        }

        return 0; // Default if not found
    }
    public async Task UpdateTaskTypeCountBySortOrder(int sortOrder, int change)
    {
        // 1. Query for the document path using sort_order
        string queryUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

        var queryBody = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "TaskType" } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "sort_order" },
                        op = "EQUAL",
                        value = new { integerValue = sortOrder }
                    }
                },
                limit = 1
            }
        };

        var queryResponse = await _httpClient.PostAsJsonAsync(queryUrl, queryBody);
        var queryResult = await queryResponse.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(queryResult);

        // Check if the query returned a valid document
        if (doc.RootElement.GetArrayLength() > 0 && doc.RootElement[0].TryGetProperty("document", out var docEl))
        {
            string documentPath = docEl.GetProperty("name").GetString();

            // 2. Fetch current count to calculate new count
            int currentCount = await GetCurrentCount(documentPath);

            // 3. Patch the new count
            string updateUrl = $"https://firestore.googleapis.com/v1/{documentPath}?updateMask.fieldPaths=TaskCount";
            var updateBody = new
            {
                fields = new
                {
                    TaskCount = new { integerValue = currentCount + change }
                }
            };

            var response = await _httpClient.PatchAsJsonAsync(updateUrl, updateBody);
            response.EnsureSuccessStatusCode();
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
        // 1. Calculate values for the new recurring task
        var nextDueDate = GetNextDueDate(item);

        // 1. Insert TaskRecord into Firestore
        string insertUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/TaskRecord";
        var insertPayload = new
        {
            fields = new Dictionary<string, object>
    {
        // Booleans
        { "IsCompleted", new { booleanValue = false } },
        { "IsSelected", new { booleanValue = item.IsSelected } },

        // Strings
        { "Repeat", new { stringValue = (item.Repeat.ToString() ) } },
        { "task_title", new { stringValue = item.task_title ?? "" } },
        { "task_description", new { stringValue = item.task_description ?? "" } },

                // Timestamps (Formatted as ISO 8601/RFC 3339)
                {"task_created_at", new{ timestampValue = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") } },
                {"task_due_date" , new { timestampValue = nextDueDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") } },
    

        // Integers (Must be strings in the REST API)
        { "task_id", new { integerValue = (await GetNextTaskId()).ToString() } },
        { "task_type_id", new { integerValue = item.task_type_id.ToString() } },
        { "assignee_id", new { stringValue= item.assignee_id } },
        { "userId", new { stringValue = item.userId } },
        { "CompanyId", new { stringValue = item.CompanyId } }
    }
        };
        if (item.file_name_image != null)
        {
            insertPayload.fields.Add("file_name_image", new { stringValue = item.file_name_image });
        }

        if (item.file_name_video != null)
        {
            insertPayload.fields.Add("file_name_video", new { stringValue = item.file_name_video });
        }

        if (item.file_data_image != null)
        {
            insertPayload.fields.Add("file_data_image", new { stringValue = item.file_data_image });
        }

        if (item.file_data_video != null)
        {
            insertPayload.fields.Add("file_data_video", new { stringValue = item.file_data_video });
        }


        var insertContent = new StringContent(JsonSerializer.Serialize(insertPayload), System.Text.Encoding.UTF8, "application/json");

        var insertResponse = await _httpClient.PostAsync(insertUrl, insertContent);
        insertResponse.EnsureSuccessStatusCode();

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
        // 1. Build the URL with the updateMask for every field you want to change
        // This tells Firestore: "Only touch these specific properties"
        string updateUrl = $"https://firestore.googleapis.com/v1/{documentPath}?" +
                            "updateMask.fieldPaths=task_type_id&" +
                            "updateMask.fieldPaths=IsCompleted&" +
                            "updateMask.fieldPaths=task_title&" +
                            "updateMask.fieldPaths=task_description&" +
                            "updateMask.fieldPaths=Repeat&" +
                            "updateMask.fieldPaths=file_data_image&" +
                            "updateMask.fieldPaths=file_name_image&" +
                            "updateMask.fieldPaths=file_data_video&" +
                            "updateMask.fieldPaths=file_name_video&" +
                            "updateMask.fieldPaths=assignee_id&" +
                            "updateMask.fieldPaths=task_due_date";

        // 2. Map your local TaskRecord to the Firestore JSON structure
        var body = new
        {
            fields = new
            {
                task_type_id = new { integerValue = item.task_type_id },
                IsCompleted = new { booleanValue = item.IsCompleted },
                task_title = new { stringValue = item.task_title },
                task_description = new { stringValue = item.task_description ?? "" },
                Repeat = new { stringValue = item.Repeat.ToString() },
                file_name_image = new { stringValue = item.file_name_image ?? "" },
                file_name_video = new { stringValue = item.file_name_video ?? "" },
                file_data_image = new { stringValue = item.file_data_image ?? "" },
                file_data_video = new { stringValue = item.file_data_video ?? "" },
                assignee_id = new { stringValue = item.assignee_id ?? "" },
                task_due_date = new { timestampValue = item.task_due_date.Value.ToUniversalTime().ToString("o") ?? "" }
            }
        };

        // 3. Send the request
        var json = JsonConvert.SerializeObject(body);
        var response = await _httpClient.PatchAsync(updateUrl,
            new StringContent(json, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            return 1; // Success
        }

        return 0; // Failed
    }

    public async Task<int> UpdateItemAsync(TaskRecord item)
    {
        // 1. Get the original task from Firestore to check previous state
        string originalTaskPath = await GetFirestoreDocumentPath("TaskRecord", "task_id", item.task_id);
        if (string.IsNullOrEmpty(originalTaskPath)) return 0;

        // We need the old task_type_id for the comparison logic
        // (In a real app, you'd parse the full originalTask object here)
        int oldTypeId = await GetOldTypeIdFromPath(originalTaskPath);

        if (item.IsCompleted)
        {
            // Decrease count for old type
            //  await UpdateTaskTypeCount(oldTypeId, -1);
            await UpdateTaskTypeCountAsync(oldTypeId, -1);
            // Decrease count from 'All Tasks' (assuming id 1)
            //    await UpdateTaskTypeCount(1, -1);
            await UpdateTaskTypeCountAsync(1, -1);

            // Increase count for 'Completed' (assuming sort_order 999)
            //  await UpdateTaskTypeCountBySortOrder(999, 1);
            await UpdateTaskTypeCountAsync(10, +1);

            if (item.Repeat != RepeatOption.NoRepeat)
            {
                await InsertNextRepeatTask(item);
            }
            item.task_type_id = 10;
        }
        else if (oldTypeId != item.task_type_id)
        {
            //  await UpdateTaskTypeCount(oldTypeId, -1);
            await UpdateTaskTypeCountAsync(oldTypeId, -1);
            //   await UpdateTaskTypeCount(item.task_type_id, 1);
            await UpdateTaskTypeCountAsync(item.task_type_id, +1);
        }

        // 2. Final Update of the TaskRecord itself
        return await PatchTaskRecord(originalTaskPath, item);
    }
    public async Task<int> PatchTaskRecord(string documentPath, TaskRecord item)
    {
        string imageUrl = "";
        string videoUrl = "";

        if (item.file_name_image != null && item.file_name_image != "" && item.file_data_image1 != null)
        {
            imageUrl = await UploadToStorage(item.file_data_image1, item.file_name_image);
        }
        if (item.file_name_video != null && item.file_name_video != "" && item.file_data_video1 != null)
        {
            videoUrl = await UploadToStorage(item.file_data_video1, item.file_name_video);
        }
        // 1. Build the URL with the updateMask for every field you want to change
        // This tells Firestore: "Only touch these specific properties"
        string updateUrl = $"https://firestore.googleapis.com/v1/{documentPath}?" +
                           "updateMask.fieldPaths=task_type_id&" +
                           "updateMask.fieldPaths=IsCompleted&" +
                           "updateMask.fieldPaths=task_title&" +
                           "updateMask.fieldPaths=task_description&" +
                           "updateMask.fieldPaths=Repeat&" +
                           "updateMask.fieldPaths=file_data_image&" +
                           "updateMask.fieldPaths=file_name_image&" +
                           "updateMask.fieldPaths=file_data_video&" +
                           "updateMask.fieldPaths=file_name_video&" +
                           "updateMask.fieldPaths=assignee_id&" +
                           "updateMask.fieldPaths=pending_description&" +
                           "updateMask.fieldPaths=task_due_date";

        // 2. Map your local TaskRecord to the Firestore JSON structure
        var body = new
        {
            fields = new
            {
                task_type_id = new { integerValue = item.task_type_id },
                IsCompleted = new { booleanValue = item.IsCompleted },
                task_title = new { stringValue = item.task_title },
                task_description = new { stringValue = item.task_description ?? "" },
                Repeat = new { stringValue = item.Repeat.ToString() },
                file_name_image = new { stringValue = item.file_name_image ?? "" },
                file_name_video = new { stringValue = item.file_name_video ?? "" },
                file_data_image = new { stringValue = imageUrl ?? item.file_data_image },
                file_data_video = new { stringValue = videoUrl ?? item.file_data_video },
                assignee_id = new { stringValue = item.assignee_id ?? "" },
                pending_description = new { stringValue = item.pending_description ?? "" },
                task_due_date = new { timestampValue = item.task_due_date.Value.ToUniversalTime().ToString("o") ?? "" }
            }
        };

        // 3. Send the request
        var json = JsonConvert.SerializeObject(body);
        var response = await _httpClient.PatchAsync(updateUrl,
            new StringContent(json, Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            return 1; // Success
        }

        return 0; // Failed
    }

    public async Task<List<TaskRecord>> SearchTaskRecords(string qry)
    {
        string role = GlobalVariables.role;
        string companyId = GlobalVariables.companyid;
        string userId = GlobalVariables.userId;
        var userMap = await GetCompanyUsersAsync(companyId);
        qry = qry.ToLower();
        string url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/TaskRecord";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(json).RootElement;

        var taskRecords = new List<TaskRecord>();
        foreach (var doc in root.GetProperty("documents").EnumerateArray())
        {
            var fields = doc.GetProperty("fields");

            var record = new TaskRecord
            {
                task_id = int.Parse(GetVal(fields, "task_id", "integerValue") ?? "0"),
                task_type_id = int.Parse(GetVal(fields, "task_type_id", "integerValue") ?? "0"),

                // Strings
                task_title = GetVal(fields, "task_title", "stringValue"),
                task_description = GetVal(fields, "task_description", "stringValue"),
                file_name_image = GetVal(fields, "file_name_image", "stringValue"),
                file_name_video = GetVal(fields, "file_name_video", "stringValue"),
                IsCompleted = fields.TryGetProperty("IsCompleted", out var ic) && ic.GetProperty("booleanValue").GetBoolean(),
                IsSelected = fields.TryGetProperty("IsSelected", out var ise) && ise.GetProperty("booleanValue").GetBoolean(),

                // Enum (Parsing string back to RepeatOption)
                Repeat = Enum.TryParse<RepeatOption>(GetVal(fields, "Repeat", "stringValue"), out var res) ? res : RepeatOption.NoRepeat,

                // Timestamps
                task_created_at = DateTime.Parse(GetVal(fields, "task_created_date", "timestampValue") ?? DateTime.UtcNow.ToString()),
                task_due_date = fields.TryGetProperty("task_due_date", out var td) ? DateTime.Parse(td.GetProperty("timestampValue").GetString()) : (DateTime?)null,
                // Bytes (Base64 strings decoded back to byte arrays)
                file_data_image = fields.TryGetProperty("file_data_image", out var fdi) ? fdi.GetProperty("stringValue").GetString() : null,
                file_data_video = fields.TryGetProperty("file_data_video", out var fdv) ? fdv.GetProperty("stringValue").GetString() : null,
                assignee_id = GetVal(fields, "assignee_id", "stringValue"),
                userId = GetVal(fields, "userId", "stringValue"),
                pending_description = GetVal(fields, "pending_description", "stringValue"),
                CompanyId = GetVal(fields, "CompanyId", "stringValue")
            };
            if (!string.IsNullOrEmpty(record.assignee_id) && userMap.ContainsKey(record.assignee_id))
            {
                record.DisplayUsername = userMap[record.assignee_id];
            }

            // Apply client-side substring search
            if ((record.task_title?.ToLower().Contains(qry) ?? false) ||
                (record.task_description?.ToLower().Contains(qry) ?? false))
            {
                taskRecords.Add(record);
            }
        }

        return taskRecords;
    }

    public async Task<int> GetNextTaskId()
    {
        string url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

        var query = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "TaskRecord" } },
                orderBy = new[] { new { field = new { fieldPath = "task_id" }, direction = "DESCENDING" } },
                limit = 1
            }
        };

        try
        {
            // 1. Manually serialize to avoid PostAsJsonAsync internal errors
            var jsonQuery = System.Text.Json.JsonSerializer.Serialize(query);
            var requestContent = new StringContent(jsonQuery, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, requestContent);

            // 2. If collection doesn't exist, Firestore might return 400 or 404.
            // We catch this and return ID 1.
            if (!response.IsSuccessStatusCode)
            {
                return 1;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            // 3. Handle the "Empty Result" format
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            {
                return 1;
            }

            var firstResult = doc.RootElement[0];
            if (firstResult.TryGetProperty("document", out var docEl))
            {
                var fields = docEl.GetProperty("fields");
                if (fields.TryGetProperty("task_id", out var taskIdField))
                {
                    // Firestore returns integers as strings in 'integerValue'
                    string val = taskIdField.GetProperty("integerValue").GetString();
                    return int.Parse(val) + 1;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }

        return 1;
    }

    public async Task<int> GetNextDraftTaskId()
    {
        string url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

        var query = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "DraftTaskRecords" } },
                orderBy = new[] { new { field = new { fieldPath = "task_id" }, direction = "DESCENDING" } },
                limit = 1
            }
        };

        try
        {
            // 1. Manually serialize to avoid PostAsJsonAsync internal errors
            var jsonQuery = System.Text.Json.JsonSerializer.Serialize(query);
            var requestContent = new StringContent(jsonQuery, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, requestContent);

            // 2. If collection doesn't exist, Firestore might return 400 or 404.
            // We catch this and return ID 1.
            if (!response.IsSuccessStatusCode)
            {
                return 1;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            // 3. Handle the "Empty Result" format
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            {
                return 1;
            }

            var firstResult = doc.RootElement[0];
            if (firstResult.TryGetProperty("document", out var docEl))
            {
                var fields = docEl.GetProperty("fields");
                if (fields.TryGetProperty("task_id", out var taskIdField))
                {
                    // Firestore returns integers as strings in 'integerValue'
                    string val = taskIdField.GetProperty("integerValue").GetString();
                    return int.Parse(val) + 1;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }

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
        string imageUrl = "";
        string videoUrl = "";

        if (item.file_name_image != null)
        {
            imageUrl = await UploadToStorage(item.file_data_image1, item.file_name_image);
        }
        if (item.file_name_video != null)
        {
            videoUrl = await UploadToStorage(item.file_data_video1, item.file_name_video);
        }

        // 1. Insert TaskRecord into Firestore
        string insertUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/DraftTaskRecords";

        var insertPayload = new
        {
            fields = new Dictionary<string, object>
    {
        // Booleans
        { "IsCompleted", new { booleanValue = item.IsCompleted } },
        { "IsSelected", new { booleanValue = item.IsSelected } },

        // Strings
        { "Repeat", new { stringValue = (item.Repeat.ToString() ) } },
        { "task_title", new { stringValue = item.task_title ?? "" } },
        { "task_description", new { stringValue = item.task_description ?? "" } },
        { "task_created_date", new { timestampValue = DateTime.UtcNow.ToString("o") } },
        { "task_id", new { integerValue = (await GetNextDraftTaskId()).ToString() } },
        { "task_type_id", new { integerValue = item.task_type_id.ToString() } },
        { "assignee_id", new { stringValue= item.assignee_id } },
        { "userId", new { stringValue = item.userId } },
        { "CompanyId", new { stringValue = item.CompanyId } }
    }
        };

        if (item.file_name_image != null)
        {
            insertPayload.fields.Add("file_name_image", new { stringValue = item.file_name_image });
        }

        if (item.file_name_video != null)
        {
            insertPayload.fields.Add("file_name_video", new { timestampValue = item.file_name_video });
        }

        if (item.file_data_image != null)
        {
            insertPayload.fields.Add("file_data_image", new { stringValue = item.file_data_image });
        }

        if (item.file_data_video != null)
        {
            insertPayload.fields.Add("file_data_video", new { stringValue = item.file_data_video });
        }

        if (item.task_due_date.HasValue)
        {
            insertPayload.fields.Add("task_due_date", new { timestampValue = item.task_due_date.Value.ToUniversalTime().ToString("o") });
        }

        var insertContent = new StringContent(JsonSerializer.Serialize(insertPayload), System.Text.Encoding.UTF8, "application/json");

        var insertResponse = await _httpClient.PostAsync(insertUrl, insertContent);
        insertResponse.EnsureSuccessStatusCode();

        return 1;
    }


    public async Task<int> SaveItemAsync(TaskRecord item)
    {
        string imageUrl = "";
        string videoUrl = "";

        if (item.file_name_image != null && item.file_name_image != "")
        {
            imageUrl = await UploadToStorage(item.file_data_image1, item.file_name_image);
        }
        if (item.file_name_video != null && item.file_name_video != "")
        {
            videoUrl = await UploadToStorage(item.file_data_video1, item.file_name_video);
        }

        // 1. Insert TaskRecord into Firestore
        string insertUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/TaskRecord";
        var insertPayload = new
        {
            fields = new Dictionary<string, object>
    {
        // Booleans
        { "IsCompleted", new { booleanValue = item.IsCompleted } },
        { "IsSelected", new { booleanValue = item.IsSelected } },

        // Strings
        { "Repeat", new { stringValue = (item.Repeat.ToString() ) } },
        { "task_title", new { stringValue = item.task_title ?? "" } },
        { "task_description", new { stringValue = item.task_description ?? "" } },

     // Timestamps (Formatted as ISO 8601/RFC 3339)
        { "task_created_date", new { timestampValue = DateTime.UtcNow.ToString("o") } },
     //   { "task_due_date", new { timestampValue = (item.task_due_date ?? DateTime.UtcNow).ToUniversalTime().ToString("o") } },
    

        // Integers (Must be strings in the REST API)
        { "task_id", new { integerValue = (await GetNextTaskId()).ToString() } },
        { "task_type_id", new { integerValue = item.task_type_id.ToString() } },
        { "assignee_id", new { stringValue= item.assignee_id } },
        { "userId", new { stringValue = item.userId } },
        { "CompanyId", new { stringValue = item.CompanyId } }
    }
        };
        if (item.file_name_image != null)
        {
            insertPayload.fields.Add("file_name_image", new { stringValue = item.file_name_image });
        }

        if (item.file_name_video != null)
        {
            insertPayload.fields.Add("file_name_video", new { stringValue = item.file_name_video });
        }

        if (item.file_data_image != null)
        {
            insertPayload.fields.Add("file_data_image", new { stringValue = item.file_data_image });
        }

        if (item.file_data_video != null)
        {
            insertPayload.fields.Add("file_data_video", new { stringValue = item.file_data_video });
        }

        if (item.task_due_date.HasValue)
        {
            insertPayload.fields.Add("task_due_date", new { timestampValue = item.task_due_date.Value.ToUniversalTime().ToString("o") });
        }
        if (item.pending_description != null)
        {
            insertPayload.fields.Add("pending_description", new { stringValue = item.pending_description });
        }
        var insertContent = new StringContent(JsonSerializer.Serialize(insertPayload), System.Text.Encoding.UTF8, "application/json");

        var insertResponse = await _httpClient.PostAsync(insertUrl, insertContent);
        insertResponse.EnsureSuccessStatusCode();

        // 2. Update TaskType for item.task_type_id
        await UpdateTaskTypeCountAsync(item.task_type_id, +1);

        // 3. Update TaskType depending on IsCompleted
        if (item.IsCompleted)
        {
            await UpdateTaskTypeCountAsync(999, +1);
        }
        else
        {
            await UpdateTaskTypeCountAsync(1, +1);
        }

        return 1;
    }

    // Helper method to update TaskType count
    public async Task UpdateTaskTypeCountAsync(int typeId, int delta)
    {
        string queryUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";
        var query = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "TaskType" } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "task_type_id" },
                        op = "EQUAL",
                        value = new { integerValue = typeId.ToString() }
                    }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(query), System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(queryUrl, content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var docs = JsonDocument.Parse(json).RootElement.EnumerateArray();

        foreach (var docWrapper in docs)
        {
            if (!docWrapper.TryGetProperty("document", out var doc)) continue;
            var fields = doc.GetProperty("fields");
            string docName = doc.GetProperty("name").GetString();

            int currentCount = fields.TryGetProperty("TaskCount", out var countProp) && countProp.TryGetProperty("integerValue", out var cVal)
                ? int.Parse(cVal.GetString())
                : 0;

            int newCount = Math.Max(0, currentCount + delta);

            // PATCH update TaskCount
            var updateUrl = $"https://firestore.googleapis.com/v1/{docName}?updateMask.fieldPaths=TaskCount";
            var updatePayload = new
            {
                fields = new
                {
                    TaskCount = new { integerValue = newCount.ToString() }
                }
            };

            var updateContent = new StringContent(JsonSerializer.Serialize(updatePayload), System.Text.Encoding.UTF8, "application/json");
            var updateResponse = await _httpClient.PatchAsync(updateUrl, updateContent);
            updateResponse.EnsureSuccessStatusCode();
        }
    }

    public async Task<int> UpdateFinishItemAsync(TaskRecord item)
    {
        // Build the Firestore document path
        string docUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/TaskRecord/{item.task_id}?updateMask.fieldPaths=IsCompleted";

        // Prepare payload with updated fields
        var updatePayload = new
        {
            fields = new
            {
                IsCompleted = new { booleanValue = item.IsCompleted }
            }
        };

        var updateContent = new StringContent(JsonSerializer.Serialize(updatePayload), System.Text.Encoding.UTF8, "application/json");

        // Send PATCH request
        var response = await _httpClient.PatchAsync(docUrl, updateContent);
        response.EnsureSuccessStatusCode();

        return 1;
    }

    public async Task<int> DeleteDraftItemAsync(TaskRecord item)
    {
        // 1. Query for the document using task_id
        string queryUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

        var queryContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "DraftTaskRecords" } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "task_id" },
                        op = "EQUAL",
                        value = new { integerValue = item.task_id }
                    }
                },
                limit = 1
            }
        }), System.Text.Encoding.UTF8, "application/json");

        var queryResponse = await _httpClient.PostAsync(queryUrl, queryContent);
        var queryResult = await queryResponse.Content.ReadAsStringAsync();

        // 2. Parse the Document Name (Full Path) from the response
        // The response is an array; the first item contains the document "name"
        using var doc1 = System.Text.Json.JsonDocument.Parse(queryResult);
        string documentPath = doc1.RootElement[0].GetProperty("document").GetProperty("name").GetString();

        // 3. Delete the document using the full path found
        string deleteUrl = $"https://firestore.googleapis.com/v1/{documentPath}";
        var deleteResponse = await _httpClient.DeleteAsync(deleteUrl);
        deleteResponse.EnsureSuccessStatusCode();

        return 1;
    }

    public async Task<int> DeleteItemAsync(TaskRecord item)
    {
        // 1. Delete TaskRecord document by task_id
        //   string deleteUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/TaskRecord/{item.task_id}";
        //   var deleteResponse = await _httpClient.DeleteAsync(deleteUrl);
        //   deleteResponse.EnsureSuccessStatusCode();

        // 1. Query for the document using task_id
        string queryUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

        var queryContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "TaskRecord" } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "task_id" },
                        op = "EQUAL",
                        value = new { integerValue = item.task_id }
                    }
                },
                limit = 1
            }
        }), System.Text.Encoding.UTF8, "application/json");

        var queryResponse = await _httpClient.PostAsync(queryUrl, queryContent);
        var queryResult = await queryResponse.Content.ReadAsStringAsync();

        // 2. Parse the Document Name (Full Path) from the response
        // The response is an array; the first item contains the document "name"
        using var doc1 = System.Text.Json.JsonDocument.Parse(queryResult);
        string documentPath = doc1.RootElement[0].GetProperty("document").GetProperty("name").GetString();

        // 3. Delete the document using the full path found
        string deleteUrl = $"https://firestore.googleapis.com/v1/{documentPath}";
        var deleteResponse = await _httpClient.DeleteAsync(deleteUrl);
        deleteResponse.EnsureSuccessStatusCode();


        // 2. Fetch TaskType by item.task_type_id
        string typeUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";
        var query = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "TaskType" } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "task_type_id" },
                        op = "EQUAL",
                        value = new { integerValue = item.task_type_id.ToString() }
                    }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(query), System.Text.Encoding.UTF8, "application/json");
        var typeResponse = await _httpClient.PostAsync(typeUrl, content);
        typeResponse.EnsureSuccessStatusCode();

        var typeJson = await typeResponse.Content.ReadAsStringAsync();
        var docs = JsonDocument.Parse(typeJson).RootElement.EnumerateArray();

        TaskType taskType = null;
        string taskTypeDocName = null;

        foreach (var docWrapper in docs)
        {
            if (!docWrapper.TryGetProperty("document", out var doc)) continue;
            taskTypeDocName = doc.GetProperty("name").GetString();
            var fields = doc.GetProperty("fields");

            taskType = new TaskType
            {
                TaskCount = fields.TryGetProperty("TaskCount", out var countProp) && countProp.TryGetProperty("integerValue", out var cVal) ? int.Parse(cVal.GetString()) : 0,
                sort_order = fields.TryGetProperty("sort_order", out var sortProp) && sortProp.TryGetProperty("integerValue", out var sVal) ? int.Parse(sVal.GetString()) : 0,
                task_type = fields.TryGetProperty("task_type", out var taskProp) && taskProp.TryGetProperty("stringValue", out var ttVal) ? ttVal.GetString() : null,
                task_type_id = fields.TryGetProperty("task_type_id", out var idProp) && idProp.TryGetProperty("integerValue", out var idVal) ? int.Parse(idVal.GetString()) : 0


            };
        }

        if (taskType != null)
        {
            if (taskType.sort_order == 999)
            {
                if (taskType.TaskCount > 0)
                {
                    taskType.TaskCount -= 1;
                }
            }
            else
            {
                if (taskType.TaskCount > 0)
                {
                    taskType.TaskCount -= 1;
                }

                // Also update taskType with id = 1
                // (fetch and update similarly as above)
            }

            // 3. Update TaskType document back to Firestore
            if (taskTypeDocName != null)
            {
                var updateUrl = $"https://firestore.googleapis.com/v1/{taskTypeDocName}?updateMask.fieldPaths=TaskCount";
                var updatePayload = new
                {
                    fields = new
                    {
                        TaskCount = new { integerValue = taskType.TaskCount.ToString() }
                    }
                };

                var updateContent = new StringContent(JsonSerializer.Serialize(updatePayload), System.Text.Encoding.UTF8, "application/json");
                var updateResponse = await _httpClient.PatchAsync(updateUrl, updateContent);
                updateResponse.EnsureSuccessStatusCode();
            }
        }

        return 1;
    }


    public async Task<List<TaskRecord>> GetItemsTypeAsync(int type_id)
    {
        try
        {
            string role = GlobalVariables.role;
            string companyId = GlobalVariables.companyid;
            string userId = GlobalVariables.userId;
            var userMap = await GetCompanyUsersAsync(companyId);
            string url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

            using var client = new HttpClient();

            // Build Firestore structured query
            var query = new
            {
                structuredQuery = new
                {
                    from = new[] { new { collectionId = "TaskRecord" } },
                    where = new
                    {
                        fieldFilter = new
                        {
                            field = new { fieldPath = "task_type_id" },
                            op = "EQUAL",
                            value = new { integerValue = type_id.ToString() }
                        }
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(query), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var docs = JsonDocument.Parse(json).RootElement.EnumerateArray();

            var taskRecords = new List<TaskRecord>();

            foreach (var docWrapper in docs)
            {
                if (!docWrapper.TryGetProperty("document", out var doc)) continue;
                var fields = doc.GetProperty("fields");

                var record = new TaskRecord
                {
                    task_id = int.Parse(GetVal(fields, "task_id", "integerValue") ?? "0"),
                    task_type_id = int.Parse(GetVal(fields, "task_type_id", "integerValue") ?? "0"),

                    // Strings
                    task_title = GetVal(fields, "task_title", "stringValue"),
                    task_description = GetVal(fields, "task_description", "stringValue"),
                    file_name_image = GetVal(fields, "file_name_image", "stringValue"),
                    file_name_video = GetVal(fields, "file_name_video", "stringValue"),
                    IsCompleted = fields.TryGetProperty("IsCompleted", out var ic) && ic.GetProperty("booleanValue").GetBoolean(),
                    IsSelected = fields.TryGetProperty("IsSelected", out var ise) && ise.GetProperty("booleanValue").GetBoolean(),

                    // Enum (Parsing string back to RepeatOption)
                    Repeat = Enum.TryParse<RepeatOption>(GetVal(fields, "Repeat", "stringValue"), out var res) ? res : RepeatOption.NoRepeat,

                    // Timestamps
                    task_created_at = DateTime.Parse(GetVal(fields, "task_created_date", "timestampValue") ?? DateTime.UtcNow.ToString()),
                    task_due_date = fields.TryGetProperty("task_due_date", out var td) ? DateTime.Parse(td.GetProperty("timestampValue").GetString()) : (DateTime?)null,
                    // Bytes (Base64 strings decoded back to byte arrays)
                    file_data_image = fields.TryGetProperty("file_data_image", out var fdi) ? fdi.GetProperty("stringValue").GetString() : null,
                    file_data_video = fields.TryGetProperty("file_data_video", out var fdv) ? fdv.GetProperty("stringValue").GetString() : null,
                    assignee_id = GetVal(fields, "assignee_id", "stringValue"),
                    userId = GetVal(fields, "userId", "stringValue"),
                    pending_description = GetVal(fields, "pending_description", "stringValue"),
                    CompanyId = GetVal(fields, "CompanyId", "stringValue")
                };
                if (!string.IsNullOrEmpty(record.assignee_id) && userMap.ContainsKey(record.assignee_id))
                {
                    record.DisplayUsername = userMap[record.assignee_id];
                }

                taskRecords.Add(record);
            }

            return taskRecords;
        }
        catch (Exception ex)
        {
            // Log error or handle gracefully
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

    public FirestoreService()
    {
        _httpClient = new HttpClient();
        //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GlobalVariables.idToken);
    }

    public async Task<int> SaveTaskTypeAsync(TaskType newTaskType)
    {
        // Firestore REST endpoint for the TaskType collection
        var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/TaskType";

        // Default sort order if not set
        if (newTaskType.sort_order == 0)
            newTaskType.sort_order = 1;

        // Build Firestore JSON body
        var body = new
        {
            fields = new
            {
                TaskCount = new { integerValue = newTaskType.TaskCount },
                sort_order = new { integerValue = newTaskType.sort_order },
                task_type = new { stringValue = newTaskType.task_type },
                task_type_id = new { integerValue = newTaskType.task_type_id }

            }
        };

        var json = JsonConvert.SerializeObject(body);

        // POST the new document
        var response = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        return 1; // success
    }

    public async Task<int> UpdateItemMainAsync(TaskRecord taskRecord)
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

    public async Task<int> UpdateItemDescAsync(TaskRecord taskRecord)
    {
        // Firestore REST endpoint for the specific TaskType document
        // Use the document ID (string) or a stable ID you assigned when inserting
        string queryUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

        var queryContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "TaskRecord" } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "task_id" },
                        op = "EQUAL",
                        value = new { integerValue = taskRecord.task_id }
                    }
                },
                limit = 1
            }
        }), System.Text.Encoding.UTF8, "application/json");

        var queryResponse = await _httpClient.PostAsync(queryUrl, queryContent);
        var queryResult = await queryResponse.Content.ReadAsStringAsync();

        // 2. Parse the Document Name (Full Path) from the response
        // The response is an array; the first item contains the document "name"
        using var doc = System.Text.Json.JsonDocument.Parse(queryResult);
        if (doc.RootElement.GetArrayLength() == 0 || !doc.RootElement[0].TryGetProperty("document", out var docElement))
        {
            return 0; // Document not found, cannot update
        }
        string absoluteDocumentPath = docElement.GetProperty("name").GetString();




        var url = $"https://firestore.googleapis.com/v1/{absoluteDocumentPath}?" +
             "updateMask.fieldPaths=pending_description";

        // $"updateMask.fieldPaths=task_type&updateMask.fieldPaths=sort_order&updateMask.fieldPaths=TaskCount";

        // Build Firestore JSON body with updated fields
        var body = new
        {
            fields = new
            {
                pending_description = new { stringValue = taskRecord.task_description }
            }
        };

        var json = JsonConvert.SerializeObject(body);

        // PATCH request to update the document
        var response = await _httpClient.PatchAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        return 1; // success
    }

    public async Task<int> UpdateTaskTypeAsync(TaskType updatedTaskType)
    {
        // Firestore REST endpoint for the specific TaskType document
        // Use the document ID (string) or a stable ID you assigned when inserting
        string queryUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

        var queryContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "TaskType" } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "task_type_id" },
                        op = "EQUAL",
                        value = new { integerValue = updatedTaskType.task_type_id }
                    }
                },
                limit = 1
            }
        }), System.Text.Encoding.UTF8, "application/json");

        var queryResponse = await _httpClient.PostAsync(queryUrl, queryContent);
        var queryResult = await queryResponse.Content.ReadAsStringAsync();

        // 2. Parse the Document Name (Full Path) from the response
        // The response is an array; the first item contains the document "name"
        using var doc = System.Text.Json.JsonDocument.Parse(queryResult);
        if (doc.RootElement.GetArrayLength() == 0 || !doc.RootElement[0].TryGetProperty("document", out var docElement))
        {
            return 0; // Document not found, cannot update
        }
        string absoluteDocumentPath = docElement.GetProperty("name").GetString();




        var url = $"https://firestore.googleapis.com/v1/{absoluteDocumentPath}?" +
             "updateMask.fieldPaths=TaskCount&" +
             "updateMask.fieldPaths=sort_order&" +
                  "updateMask.fieldPaths=task_type&" +
                   "updateMask.fieldPaths=task_type_id";


        // $"updateMask.fieldPaths=task_type&updateMask.fieldPaths=sort_order&updateMask.fieldPaths=TaskCount";

        // Build Firestore JSON body with updated fields
        var body = new
        {
            fields = new
            {
                TaskCount = new { integerValue = updatedTaskType.TaskCount },
                sort_order = new { integerValue = updatedTaskType.sort_order },
                task_type = new { stringValue = updatedTaskType.task_type },
                task_type_id = new { integerValue = updatedTaskType.task_type_id }


            }
        };

        var json = JsonConvert.SerializeObject(body);

        // PATCH request to update the document
        var response = await _httpClient.PatchAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        return 1; // success
    }

    public async Task<int> DeleteTaskTypeAsync(TaskType taskTypeToDelete)
    {
        // Make sure you initialized HttpClient etc.

        try
        {
            string queryUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents:runQuery";

            var queryContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
            {
                structuredQuery = new
                {
                    from = new[] { new { collectionId = "TaskType" } },
                    where = new
                    {
                        fieldFilter = new
                        {
                            field = new { fieldPath = "task_type_id" },
                            op = "EQUAL",
                            value = new { integerValue = taskTypeToDelete.task_type_id }
                        }
                    },
                    limit = 1
                }
            }), System.Text.Encoding.UTF8, "application/json");

            var queryResponse = await _httpClient.PostAsync(queryUrl, queryContent);
            var queryResult = await queryResponse.Content.ReadAsStringAsync();

            // 2. Parse the Document Name (Full Path) from the response
            // The response is an array; the first item contains the document "name"
            using var doc = System.Text.Json.JsonDocument.Parse(queryResult);
            if (doc.RootElement.GetArrayLength() == 0 || !doc.RootElement[0].TryGetProperty("document", out var docElement))
            {
                return 0; // Document not found, cannot update
            }
            string absoluteDocumentPath = docElement.GetProperty("name").GetString();

            // Firestore document path — here we assume you used task_type_id as the document ID
            var url = $"https://firestore.googleapis.com/v1/{absoluteDocumentPath}";

            // Send DELETE request
            var response = await _httpClient.DeleteAsync(url);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Deleted task type: {taskTypeToDelete.task_type}");
                return 1; // success
            }
            else
            {
                Console.WriteLine("Task type not found or delete failed.");
                return 0; // failure
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting task type: {ex.Message}");
            return 0;
        }
    }
    // Save def_task_type_id
    public async Task<int> SaveSettingItemAsync(int item1)
    {
        var url = $"https://firestore.googleapis.com/v1/projects/sprinty-cded8/databases/(default)/documents/Settings/n0U1sE2k1bGj0tkdkpWg?updateMask.fieldPaths=def_task_type_id";

        var body = new
        {
            fields = new
            {
                def_task_type_id = new { integerValue = item1 }
            }
        };

        var json = JsonConvert.SerializeObject(body);
        var response = await _httpClient.PatchAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        return 1;
    }

    public async Task<int> SaveSettingItemAsync(bool item1)
    {
        // Firestore REST endpoint for your settings document
        var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/Settings/n0U1sE2k1bGj0tkdkpWg?updateMask.fieldPaths=is_quickTaskVisible";

        // JSON body with the field update
        var body = new
        {
            fields = new
            {
                is_quickTaskVisible = new { booleanValue = item1 }
            }
        };

        var json = JsonConvert.SerializeObject(body);
        var response = await _httpClient.PatchAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        return 1;
    }
    // Save reminderLanguage
    public async Task<int> SaveSettingLangAsync(string item1)
    {
        var url = $"https://firestore.googleapis.com/v1/projects/sprinty-cded8/databases/(default)/documents/Settings/n0U1sE2k1bGj0tkdkpWg?updateMask.fieldPaths=reminderLanguage";

        var body = new
        {
            fields = new
            {
                reminderLanguage = new { stringValue = item1 }
            }
        };

        var json = JsonConvert.SerializeObject(body);
        var response = await _httpClient.PatchAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        return 1;
    }

    // Save is_completedTaskVisible
    public async Task<int> SaveSettingOneItemAsync(bool item1)
    {
        var url = $"https://firestore.googleapis.com/v1/projects/sprinty-cded8/databases/(default)/documents/Settings/n0U1sE2k1bGj0tkdkpWg?updateMask.fieldPaths=is_completedTaskVisible";

        var body = new
        {
            fields = new
            {
                is_completedTaskVisible = new { booleanValue = item1 }
            }
        };

        var json = JsonConvert.SerializeObject(body);
        var response = await _httpClient.PatchAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        return 1;
    }

    public async Task<ObservableCollection<User>> GetAssigneeAsync()
    {
        var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/User";
        var existingData = new ObservableCollection<User>();

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            // 1. Access the "documents" property
            if (doc.RootElement.TryGetProperty("documents", out var documents))
            {
                foreach (var element in documents.EnumerateArray())
                {
                    // 2. The 'fields' property is directly on the element
                    var fields = element.GetProperty("fields");

                    // 3. Extract ID from the "name" property (it's the last part of the path)
                    var fullName = element.GetProperty("name").GetString();
                    var id = fullName.Substring(fullName.LastIndexOf('/') + 1);

                    var record = new User
                    {
                        Id = id,
                        CompanyId = GetVal(fields, "CompanyId", "stringValue"),
                        Username = GetVal(fields, "Username", "stringValue"),
                        Role = GetVal(fields, "Role", "stringValue"),
                        Email = GetVal(fields, "Email", "stringValue")
                    };
                    existingData.Add(record);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {ex.Message}");
            // Return empty collection so the app doesn't exit/crash
            return new ObservableCollection<User>();
        }

        return existingData;
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

    //fetch task types
    public async Task<List<TaskType>> GetTaskTypesAsync()
    {
        var url = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/TaskType";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        dynamic doc = JsonConvert.DeserializeObject(json);

        var existingData = new List<TaskType>();
        if (doc.documents != null)
        {
            foreach (var d in doc.documents)
            {
                var fields = d.fields;
                existingData.Add(new TaskType
                {
                    TaskCount = fields.TaskCount != null ? int.Parse((string)fields.TaskCount.integerValue) : 0,
                    sort_order = int.Parse((string)fields.sort_order.integerValue),
                    task_type = (string)fields.task_type.stringValue,
                    task_type_id = int.Parse((string)fields.task_type_id.integerValue)

                });
            }
        }

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



            foreach (var taskType in taskTypes)
            {
                var body = new
                {
                    fields = new
                    {
                        TaskCount = new { integerValue = taskType.TaskCount },
                        sort_order = new { integerValue = taskType.sort_order },
                        task_type = new { stringValue = taskType.task_type },
                        task_type_id = new { integerValue = taskType.task_type_id }

                    }
                };

                var postJson = JsonConvert.SerializeObject(body);
                var postResponse = await _httpClient.PostAsync(url, new StringContent(postJson, Encoding.UTF8, "application/json"));
                postResponse.EnsureSuccessStatusCode();
            }

            existingData = taskTypes;
        }

        return existingData
            .OrderBy(t => t.sort_order)
            .ThenBy(t => t.task_type)
            .ToList();
    }



    // Fetch Settings
    public async Task<Settings> GetSettingsAsync()
    {
        var url = $"https://firestore.googleapis.com/v1/projects/sprinty-cded8/databases/(default)/documents/Settings/n0U1sE2k1bGj0tkdkpWg";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        dynamic doc = JsonConvert.DeserializeObject(json);
        var fields = doc.fields;

        return new Settings
        {
            id = int.Parse((string)fields.id.integerValue),
            def_task_type_id = int.Parse((string)fields.def_task_type_id.integerValue),
            is_quickTaskVisible = (bool)fields.is_quickTaskVisible.booleanValue,
            is_completedTaskVisible = (bool)fields.is_completedTaskVisible.booleanValue,
            reminderLanguage = (string)fields.reminderLanguage.stringValue
        };
    }
}
