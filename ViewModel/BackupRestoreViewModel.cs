#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
#endif

namespace TaskManagement.ViewModel;

public partial class BackupRestoreViewModel : BaseViewModel
{
    public enum BackupOption
    {
        Backup,
        Restore
    }

    public BackupRestoreViewModel()
    {

    }
    //  [ObservableProperty]
    //  private Boolean isConformationMessageVisible = false;

    //   [ObservableProperty]
    //   private string confirmationMessage;
    private bool isConformationMessageVisible = false;
    public bool IsConformationMessageVisible
    {
        get => isConformationMessageVisible;
        set => SetProperty(ref isConformationMessageVisible, value);
    }

    private string confirmationMessage;
    public string ConfirmationMessage
    {
        get => confirmationMessage;
        set => SetProperty(ref confirmationMessage, value);
    }



    //[ObservableProperty]
    //  private BackupOption selectedOption = BackupOption.Backup;
    private BackupOption selectedOption = BackupOption.Backup;
    public BackupOption SelectedOption
    {
        get => selectedOption;
        set => SetProperty(ref selectedOption, value);
    }



    [RelayCommand]
    async Task ConfirmSelectionAsync()
    {
        var sourcePath = "";    //Constants.DatabasePath;
        if (SelectedOption == BackupOption.Backup)
        {

            // ConfirmationMessage = "Database Backup succesfully taken at";
#if ANDROID
            string downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath; // Fully qualify Android.OS.Environment to resolve ambiguity
          //  string targetPath = Path.Combine(downloadsPath, Constants.DatabaseFilename);
             string targetPath = Path.Combine(downloadsPath, "");
            var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Permission Denied", "Storage permission is required.", "OK");
                return;
            }
            try
            {
                File.Copy(targetPath, sourcePath, overwrite: true);
                await Shell.Current.DisplayAlert("Success", "Database Backup completed to Downloads folder.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Database Backup failed: {ex.Message}", "OK");
            }
#endif
        }

        else
        {
#if ANDROID
            string downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath; // Fully qualify Android.OS.Environment to resolve ambiguity
           //string backupPath = Path.Combine(downloadsPath, Constants.DatabaseFilename);
            string backupPath = Path.Combine(downloadsPath, "");
            var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Permission Denied", "Storage permission is required.", "OK");
                return;
            }
            try
            {
                File.Copy(backupPath, sourcePath, overwrite: true);
                await Shell.Current.DisplayAlert("Success", "Database Restored....", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Database Restore failed: {ex.Message}", "OK");
            }
#endif           
        }
    }
}