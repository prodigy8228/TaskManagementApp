using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using AndroidX.Core.App;
using TaskManagement.Platforms;

namespace TaskManagement
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    "task_channel",
                    "Task Reminders",
                    NotificationImportance.Default)
                {
                    Description = "Reminders for pending tasks"
                };

                var manager = (NotificationManager)GetSystemService(NotificationService);
                manager.CreateNotificationChannel(channel);
            }

            // 2. Request POST_NOTIFICATIONS permission (Android 13+)
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Permission.Granted)
                {
                    RequestPermissions(new string[] { Android.Manifest.Permission.PostNotifications }, 2001);
                }
            }

            // 3. Check if notifications are enabled
            bool areNotificationsEnabled = NotificationManagerCompat.From(this).AreNotificationsEnabled();
            if (!areNotificationsEnabled)
            {
                // Show a polite prompt (dialog/snackbar) then redirect to settings
                OpenNotificationSettings();
            }



            // Schedule the daily reminder at 9 AM
            var backgroundService = new BackgroundTaskService();
            backgroundService.ScheduleDailyTaskReminder(this);


        }

        private void OpenNotificationSettings()
        {
            Intent intent = new Intent();
            intent.SetAction("android.settings.APP_NOTIFICATION_SETTINGS");
            intent.PutExtra("app_package", PackageName);
            intent.PutExtra("app_uid", ApplicationInfo.Uid);
            intent.PutExtra("android.provider.extra.APP_PACKAGE", PackageName);
            StartActivity(intent);
        }



        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == 1001 && resultCode == Result.Ok && data != null)
            {
                var audioUri = data.Data;
                string audioPath = GetRealPathFromURI(audioUri);

                // Send recorded file path to ViewModel
                MessagingCenter.Send(this, "AudioRecorded", audioPath);
            }
        }

        private string GetRealPathFromURI(Android.Net.Uri contentUri)
        {
            string[] proj = { MediaStore.Audio.Media.InterfaceConsts.Data };
            var cursor = Platform.CurrentActivity.ContentResolver.Query(contentUri, proj, null, null, null);
            if (cursor != null && cursor.MoveToFirst())
            {
                int columnIndex = cursor.GetColumnIndexOrThrow(MediaStore.Audio.Media.InterfaceConsts.Data);
                string filePath = cursor.GetString(columnIndex);
                cursor.Close();
                return filePath;
            }
            return null;
        }
    }


}
