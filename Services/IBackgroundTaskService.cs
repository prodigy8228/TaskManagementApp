namespace TaskManagement.Services
{
    public interface IBackgroundTaskService
    {
        void SchedulePeriodicReminders();
        void EnqueueOneTimeReminder(string title, string message);
    }
}


