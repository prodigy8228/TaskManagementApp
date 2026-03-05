// File: Platforms/Android/ReminderWorker.cs
using Android.App;
using Android.Content;
using AndroidX.Core.App;
using AndroidX.Core.Graphics.Drawable;
using AndroidX.Work;

namespace TaskManagement.Platforms
{
    public class ReminderWorker : Worker
    {
        public ReminderWorker(Context context, WorkerParameters workerParams)
            : base(context, workerParams) { }

        public override Result DoWork()
        {
            MISDatabase taskService = new MISDatabase();
            // Example: fetch tasks from a repository
            var pendingTasks = Task.Run(async () =>
               await taskService.GetItemsTypeNotDoneDateAsync()
           ).Result;

            if (pendingTasks.Any())
            {
                var title = "Today's Tasks";
                var message = $"You have {pendingTasks.Count} pending task(s) for today.";
                ShowNotification(title, message);
            }

            return Result.InvokeSuccess();
        }

        private void ShowNotification(string title, string message)
        {
            // Intent to open the app (AppShell or specific page)
            var intent = new Intent(ApplicationContext, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            var pendingIntent = PendingIntent.GetActivity(
                ApplicationContext,
                0,
                intent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
            );
            var icon = IconCompat.CreateWithResource(Android.App.Application.Context, Resource.Drawable.ic_notification);


            var builder = new NotificationCompat.Builder(ApplicationContext, "task_channel")
    .SetSmallIcon(icon)
    .SetContentTitle(title)
    .SetContentText(message)
    .SetPriority((int)NotificationPriority.Default)
    .SetAutoCancel(true)
    .SetContentIntent(pendingIntent);
            //.SetSmallIcon(Resource.Drawable.ic_notification)
            var notificationManager = NotificationManagerCompat.From(ApplicationContext);
            notificationManager.Notify(1001, builder.Build());
        }
    }
}