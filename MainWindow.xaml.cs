using System.Windows;
using System.Windows.Input;

namespace HomeWork10
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TelegramMessageClient client;

        public MainWindow()
        {
            InitializeComponent();

            client = new TelegramMessageClient(this);

            logList.ItemsSource = client.BotMessageLog;
        }

        private void btnMsgSendClick(object sender, RoutedEventArgs e)
        {
            client.SendMessage(txtMsgSend.Text, TargetSend.Text);
            txtMsgSend.Text = string.Empty;
        }
        private void btnMsgSendEnter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                client.SendMessage(txtMsgSend.Text, TargetSend.Text);
                txtMsgSend.Text = string.Empty;
            }
        }
    }
}
