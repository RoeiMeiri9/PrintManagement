using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PrintManagement.Dialogs;

public sealed partial class CustomLoginDialog : ContentDialog
{
    public string Username => UsernameBox.Text;
    public string Password => PasswordBox.Password;
    public bool RememberMe => RememberMeCheckBox.IsChecked ?? false;

    public CustomLoginDialog(string serverName, XamlRoot xamlRoot)
    {
        InitializeComponent();
        XamlRoot = xamlRoot;
        ServerNameText.Text = $"Enter your credentials to connect to: {serverName}";

        // Cursor starts in the user name box, like the system dialog.
        Opened += (_, _) => UsernameBox.Focus(FocusState.Programmatic);
    }

    public void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ShowError("Enter a user name and password.");
            args.Cancel = true;   // keep the dialog open
        }
    }
}