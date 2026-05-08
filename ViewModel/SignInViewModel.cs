namespace TaskManagement.ViewModel
{
    public partial class SignInViewModel(IFirestoreService fService) : ObservableObject
    {
        private readonly IFirestoreService _fService;

        // Fix CS8618: Initialize non-nullable fields to default values
        [ObservableProperty] private string email = string.Empty;
        [ObservableProperty] private string password = string.Empty;
        [ObservableProperty] private bool isBusy;

        // Fix CS0051: Make IFirestoreService public to match constructor accessibility

        [RelayCommand]
        private async Task SignIn()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var user = await fService.LoginAndGetUserAsync(Email, Password);

                if (user != null)
                {
                    GlobalVariables.companyid = user.CompanyId;
                    GlobalVariables.role = user.Role;
                    GlobalVariables.userId = user.Id;
                    GlobalVariables.idToken = await fService.GetIdTokenAsync(Email, Password);

                    // await Shell.Current.GoToAsync("//MainPage");
                    Application.Current.MainPage = new AppShell();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "Login failed or user not found.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
            finally { IsBusy = false; }
        }
    }
}