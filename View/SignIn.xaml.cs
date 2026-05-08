namespace TaskManagement.View;

public partial class SignInPage : ContentPage
{
    public SignInPage(SignInViewModel viewModel)
    {
        InitializeComponent();
        // Link the View to its logic
        BindingContext = viewModel;
    }
}
