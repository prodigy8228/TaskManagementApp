namespace TaskManagement.ViewModel;

public partial class MyPopupViewModel : ObservableObject
{
    private readonly IPopupService _popupService;

    public MyPopupViewModel(IPopupService popupService)
    {
        _popupService = popupService;
    }

    [ObservableProperty]
    string message = "Hello from the popup!";

    [RelayCommand]
    private async Task CloseAsync()
    {
        var parent = Application.Current?.MainPage;
        if (parent is not Page page)
            return;

        await _popupService.ClosePopupAsync(page);
    }

}