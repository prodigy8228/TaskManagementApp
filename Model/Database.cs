using SQLite;
namespace TaskManagement.Model
{
    public class MISDatabase
    {
        SQLiteAsyncConnection Database;

        public MISDatabase()
        {
        }


        async Task Init()
        {
            //if (Database is not null)
            //{
            //    if (File.Exists(Constants.DatabasePath))
            //    {
            //        File.Delete(Constants.DatabasePath);
            //    }
            //}

            if (Database is null)
            {
                Database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);

                //  Preferences.Set("DatabaseVersion", 2);
            }
            await Database.CreateTableAsync<Settings>();

            var settings = new Settings
            {
                def_task_type_id = 1,
                is_quickTaskVisible = true,
                is_completedTaskVisible = false
            };

            await Database.InsertAsync(settings);

            // Ensure the rest are always created
            await Database.CreateTableAsync<TaskType>();
            await Database.CreateTableAsync<TaskRecord>();

        }
        public async Task ExecuteAsync(string query)
        {
            // await Init();
            await Database.ExecuteAsync(query);
        }
        public async Task<List<TaskRecord>> GetItemsAsync()
        {
            await Init();
            await Task.Delay(200);
            var TaskRecords = await Database.Table<TaskRecord>().ToListAsync();

            return TaskRecords;
        }
        // Add the SaveTaskTypeAsync method to resolve the error  
        public async Task<int> SaveTaskTypeAsync(TaskType newTaskType)
        {
            await Init();
            newTaskType.sort_order = 1; // Default sort order
            await Database.InsertAsync(newTaskType);
            await Task.Delay(100);
            return 1; // Return 1 to indicate success  
        }

        public async Task<int> UpdateTaskTypeAsync(TaskType newTaskType)
        {
            await Init();
            await Database.UpdateAsync(newTaskType);
            await Task.Delay(100);
            return 1; // Return 1 to indicate success  
        }

        public async Task<int> DeleteTaskTypeAsync(TaskType newTaskType)
        {
            await Init();
            Console.WriteLine(newTaskType.task_type);
            var existing = await Database.Table<TaskType>()
    .Where(t => t.task_type == newTaskType.task_type)
    .FirstOrDefaultAsync();

            if (existing != null)
            {
                await Database.DeleteAsync(existing);
                return 1;
            }
            else
            {
                Console.WriteLine("Task type not found.");
                return 0;
            }
        }
        public async Task<List<TaskRecord>> GetItemsTypeAsync(int type_id)
        {
            try
            {
                await Init();
                return await Database.Table<TaskRecord>()
                    .Where(t => t.task_type_id == type_id)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log error or handle gracefully
                return new List<TaskRecord>();
            }


        }

        public async Task<List<TaskRecord>> GetItemsTypeNotDoneDateAsync()
        {
            try
            {
                await Init();
                var today = DateTime.Today;
                return await Database.Table<TaskRecord>()
                    .Where(t => t.task_due_date == today && !t.IsCompleted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log error or handle gracefully
                return new List<TaskRecord>();
            }


        }

        public async Task<List<TaskRecord>> GetItemsTypeNotDoneAsync(int type_id)
        {
            try
            {
                await Init();
                return await Database.Table<TaskRecord>()
                    .Where(t => t.task_type_id == type_id && !t.IsCompleted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log error or handle gracefully
                return new List<TaskRecord>();
            }


        }

        public async Task<List<TaskType>> GetTaskTypesAsync()
        {
            await Init();
            Console.WriteLine($"SQLite DB Path: {Constants.DatabasePath}");
            var existingData = await Database.Table<TaskType>().ToListAsync();
            if (existingData.Count == 0) // Ensures data is inserted only once
            {
                var taskTypes = new List<TaskType>
        {
            new TaskType { task_type = "All List", sort_order=0 }, //id 1
            new TaskType { task_type = "Default" , sort_order=1}, //id 2
            new TaskType { task_type = "Health & Wellness", sort_order=1 },
            new TaskType { task_type = "Household", sort_order=1 },
            new TaskType { task_type = "Personal" , sort_order=1},
            new TaskType { task_type = "Shopping", sort_order=1 },
            new TaskType { task_type = "Social & Relationship", sort_order=1 },
            new TaskType { task_type = "Travel", sort_order=1 },
            new TaskType { task_type = "Work" , sort_order=1},
            new TaskType { task_type = "Completed Tasks List", sort_order=999 }
        };

                await Database.InsertAllAsync(taskTypes);
            }
            return await Database.Table<TaskType>().OrderBy(t => t.sort_order).ThenBy(t => t.task_type).ToListAsync();
        }

        public async Task<List<TaskRecord>> SearchTaskRecords(string qry)
        {
            await Init();


            qry = qry.ToLower();
            var TaskRecords = await Database.Table<TaskRecord>()
                .Where(t => t.task_title.ToLower().Contains(qry) || t.task_description.ToLower().Contains(qry))
                .ToListAsync();
            return TaskRecords;
        }
        public async Task<List<TaskRecord>> GetItemsNotDoneAsync()
        {
            await Init();
            return await Database.Table<TaskRecord>().Where(t => !t.IsCompleted).ToListAsync();
        }
        public async Task<TaskRecord> GetItemAsync(int id)
        {
            await Init();
            return await Database.Table<TaskRecord>().Where(i => i.task_id == id).FirstOrDefaultAsync();
        }

        public async Task<int> SaveSettingItemAsync(int item1)
        {
            await Init();
            var setting = await Database.Table<Settings>().FirstOrDefaultAsync();
            if (setting != null)
            {
                setting.def_task_type_id = item1;
                try
                {
                    await Database.UpdateAsync(setting);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Setting saved: exception : " + e.ToString());
                }
                Console.WriteLine($"Setting saved: {setting.def_task_type_id}");
            }
            return 1;


        }

        public async Task<int> SaveSettingOneItemAsync(Boolean item1)
        {
            await Init();
            var setting = await Database.Table<Settings>().FirstOrDefaultAsync();
            setting.is_completedTaskVisible = item1;
            await Database.UpdateAsync(setting);
            return 1;
        }
        public async Task<int> SaveSettingItemAsync(Boolean item1)
        {
            await Init();
            var setting = await Database.Table<Settings>().FirstOrDefaultAsync();
            setting.is_quickTaskVisible = item1;
            await Database.UpdateAsync(setting);
            return 1;
        }
        public async Task LoadSettingsToGlobalsAsync()
        {
            await Init();
            var setting = await Database.Table<Settings>().FirstOrDefaultAsync();
            if (setting != null)
            {
                GlobalVariables.defTaskType = setting.def_task_type_id;
                GlobalVariables.IsQuckTaskVisible = setting.is_quickTaskVisible;
                GlobalVariables.IsCompletedTaskVisible = setting.is_completedTaskVisible;
            }
        }

        public async Task<int> SaveItemAsync(TaskRecord item)
        {
            await Init();
            await Database.InsertAsync(item);

            var taskType = await Database.Table<TaskType>()
                          .Where(t => t.task_type_id == item.task_type_id)
                          .FirstOrDefaultAsync();


            if (taskType != null)
            {
                taskType.TaskCount += 1;
                await Database.UpdateAsync(taskType);
            }
            if (item.IsCompleted == true)
            {
                var taskType1 = await Database.Table<TaskType>()
                                        .Where(t => t.task_type_id == 999)
                                        .FirstOrDefaultAsync();
                if (taskType1 != null)
                {
                    taskType1.TaskCount += 1;
                    await Database.UpdateAsync(taskType1);
                }
            }
            else
            {
                var taskType1 = await Database.Table<TaskType>()
                                                        .Where(t => t.task_type_id == 1)
                                                        .FirstOrDefaultAsync();
                if (taskType1 != null)
                {
                    taskType1.TaskCount += 1;
                    await Database.UpdateAsync(taskType1);
                }
            }

            return 1;
        }
        public async Task<int> UpdateItemAsync(TaskRecord item)
        {
            await Init();
            var originalTask = await Database.Table<TaskRecord>()
                               .Where(t => t.task_id == item.task_id)
                               .FirstOrDefaultAsync();
            if (originalTask != null && item.IsCompleted == true)
            {

                // Decrease count for old type
                var oldType = await Database.Table<TaskType>()
                                      .Where(t => t.task_type_id == originalTask.task_type_id)
                                      .FirstOrDefaultAsync();
                if (oldType != null && oldType.TaskCount > 0 && item.Repeat == RepeatOption.NoRepeat)
                {
                    oldType.TaskCount -= 1;
                    await Database.UpdateAsync(oldType);
                }

                // Decrease count from all task list
                var newType = await Database.Table<TaskType>()
                                      .Where(t => t.task_type_id == 1)
                                      .FirstOrDefaultAsync();
                if (newType != null && item.Repeat == RepeatOption.NoRepeat)
                {
                    newType.TaskCount -= 1;
                    await Database.UpdateAsync(newType);
                }
                // Increase count from Completed task list
                var newType1 = await Database.Table<TaskType>()
                                      .Where(t => t.sort_order == 999)
                                      .FirstOrDefaultAsync();
                if (newType1 != null)
                {
                    newType1.TaskCount += 1;
                    await Database.UpdateAsync(newType1);
                }

                if (item.Repeat != RepeatOption.NoRepeat)
                {
                    var nextRecord = new TaskRecord
                    {
                        task_title = item.task_title,
                        task_description = item.task_description,
                        task_created_at = DateTime.Now,
                        Repeat = item.Repeat,
                        task_due_date = GetNextDueDate(item),
                        IsCompleted = false,
                        task_type_id = item.task_type_id

                    };

                    await Database.InsertAsync(nextRecord);
                }
                item.task_type_id = 10;
            }
            else if (originalTask != null && item.IsCompleted == false)
            {
                // Check if TaskTypeId has changed
                if (originalTask.task_type_id != item.task_type_id)
                {
                    // Decrease count for old type
                    var oldType = await Database.Table<TaskType>()
                                          .Where(t => t.task_type_id == originalTask.task_type_id)
                                          .FirstOrDefaultAsync();
                    if (oldType != null && oldType.TaskCount > 0)
                    {
                        oldType.TaskCount -= 1;
                        await Database.UpdateAsync(oldType);
                    }

                    // Increase count for new type
                    var newType = await Database.Table<TaskType>()
                                          .Where(t => t.task_type_id == item.task_type_id)
                                          .FirstOrDefaultAsync();
                    if (newType != null)
                    {
                        newType.TaskCount += 1;
                        await Database.UpdateAsync(newType);
                    }
                }
            }



            return await Database.UpdateAsync(item);
        }

        private DateTime GetNextMonFri(DateTime baseDate)
        {
            // Move forward one day at a time until we hit Monday or Friday
            DateTime nextDate = baseDate.AddDays(1);
            while (nextDate.DayOfWeek != DayOfWeek.Monday &&
                   nextDate.DayOfWeek != DayOfWeek.Friday)
            {
                nextDate = nextDate.AddDays(1);
            }
            return nextDate;
        }




        private DateTime GetNextDueDate(TaskRecord record)
        {
            var baseDate = record.task_due_date ?? DateTime.Now;

            return record.Repeat switch
            {
                RepeatOption.OnceADay => baseDate.AddDays(1),
                RepeatOption.OnceAWeek => baseDate.AddDays(7),
                RepeatOption.OnceAMonth => baseDate.AddMonths(1),
                RepeatOption.OnceAYear => baseDate.AddYears(1),
                RepeatOption.OnceAWeekMonFri => GetNextMonFri(baseDate),
                _ => baseDate
            };
        }


        public async Task<int> UpdateFinishItemAsync(TaskRecord id)
        {
            await Init();
            return await Database.UpdateAsync(id);
        }

        public async Task<int> DeleteItemAsync(TaskRecord item)
        {
            await Init();
            await Database.DeleteAsync(item);
            var taskType = await Database.Table<TaskType>()
                           .Where(t => t.task_type_id == item.task_type_id)
                           .FirstOrDefaultAsync();

            if (taskType != null && taskType.sort_order == 999)
            {
                var taskType1 = await Database.Table<TaskType>()
                  .Where(t => t.sort_order == 999)
                  .FirstOrDefaultAsync();

                if (taskType1 != null && taskType1.TaskCount > 0)
                {
                    taskType1.TaskCount -= 1;
                    await Database.UpdateAsync(taskType1);
                }
            }
            else
            {
                if (taskType != null && taskType.TaskCount > 0)
                {
                    taskType.TaskCount -= 1;
                    await Database.UpdateAsync(taskType);
                }
                var taskType1 = await Database.Table<TaskType>()
                   .Where(t => t.task_type_id == 1)
                   .FirstOrDefaultAsync();

                if (taskType1 != null && taskType1.TaskCount > 0)
                {
                    taskType1.TaskCount -= 1;
                    await Database.UpdateAsync(taskType1);
                }
            }

            return 1;

        }
    }
}
