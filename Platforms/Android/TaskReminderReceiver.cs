using Android.Content;
using AndroidX.Work;

namespace TaskManagement.Platforms
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    public class TaskReminderReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            var workRequest = OneTimeWorkRequest.Builder
                .From<ReminderWorker>()
                .Build();

            WorkManager.GetInstance(context).Enqueue(workRequest);
        }
    }
}
