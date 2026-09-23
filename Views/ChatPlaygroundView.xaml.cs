using System.Windows.Controls;
using System.Windows.Input;
using OmniKeyStudio.ViewModels;

namespace OmniKeyStudio.Views;

public partial class ChatPlaygroundView : UserControl
{
    public ChatPlaygroundView()
    {
        InitializeComponent();
    }

    private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
        {
            e.Handled = true;
            if (DataContext is ChatPlaygroundViewModel vm && vm.SendMessageCommand.CanExecute(null))
            {
                vm.SendMessageCommand.Execute(null);
                ChatScrollViewer.ScrollToEnd();
            }
        }
    }
}
