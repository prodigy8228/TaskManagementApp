namespace TaskManagement.View;

public partial class BackupRestorePage : ContentPage
{
    public BackupRestorePage(BackupRestoreViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}