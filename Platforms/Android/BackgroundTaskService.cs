// File: Platforms/Android/BackgroundTaskService.cs
using Android.App;
using Android.Content;
using AndroidX.Work;
using TaskManagement.Services;

namespace TaskManagement.Platforms
{
    public class BackgroundTaskService : IBackgroundTaskService
    {
        public void SchedulePeriodicReminders()
        {
            var workRequest = PeriodicWorkRequest.Builder
                .From<ReminderWorker>(TimeSpan.FromMinutes(15))
                .Build();

            WorkManager.GetInstance(Android.App.Application.Context).Enqueue(workRequest);
        }

        public void EnqueueOneTimeReminder(string title, string message)
        {
            var data = new Data.Builder()
                .PutString("title", title)
                .PutString("message", message)
                .Build();

            var work = OneTimeWorkRequest.Builder.From<ReminderWorker>()
                .SetInputData(data)
                .Build();

            WorkManager.GetInstance(Android.App.Application.Context).Enqueue(work);
        }

        public void ScheduleDailyTaskReminder(Context context)
        {
            var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService);

            var intent = new Intent(context, typeof(TaskReminderReceiver));
            var pendingIntent = PendingIntent.GetBroadcast(
                context,
                0,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var calendar = Java.Util.Calendar.Instance;
            calendar.Set(Java.Util.CalendarField.HourOfDay, 9);
            calendar.Set(Java.Util.CalendarField.Minute, 0);
            calendar.Set(Java.Util.CalendarField.Second, 0);

            // If time already passed today, schedule for tomorrow
            if (calendar.TimeInMillis < Java.Lang.JavaSystem.CurrentTimeMillis())
            {
                calendar.Add(Java.Util.CalendarField.DayOfYear, 1);
            }

            alarmManager.SetRepeating(
                AlarmType.RtcWakeup,
                calendar.TimeInMillis,
                AlarmManager.IntervalDay,
                pendingIntent);
        }
    }
}