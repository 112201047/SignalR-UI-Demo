using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace SignalR_UI_Demo
{
    public partial class MainWindow : Window
    {
        private bool isConnected = false;
        private HubConnection? hubConnection;

        private static readonly HttpClient http = new HttpClient();

        private const string FunctionBaseUrl = "https://signalr-functionapp-demo.azurewebsites.net/api";

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                string meetingId = MeetingIdTextBox.Text.Trim();
                string userId = UserIdTextBox.Text.Trim();

                if (string.IsNullOrEmpty(meetingId) || string.IsNullOrEmpty(userId))
                {
                    StatusTextBlock.Text = "Please enter both Meeting ID and User Name.";
                    return;
                }

                try
                {
                    StatusTextBlock.Text = "Connecting...";

                    string negotiateUrl = $"{FunctionBaseUrl}/negotiate?userId={userId}";
                    var negotiateResponse = await http.GetStringAsync(negotiateUrl);

                    var negotiateObj = System.Text.Json.JsonSerializer.Deserialize<SignalRNegotiateResponse>(negotiateResponse);

                    hubConnection = new HubConnectionBuilder()
                        .WithUrl(negotiateObj!.Url!, options =>
                        {
                            options.AccessTokenProvider = () => Task.FromResult(negotiateObj.AccessToken);
                            options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
                        })
                        .WithAutomaticReconnect()
                        .Build();



                    string joinUrl = $"{FunctionBaseUrl}/JoinGroup?meetingId={meetingId}&userId={userId}";
                    await http.PostAsync(joinUrl, null);

                    hubConnection.On<string, string>("ReceiveDoubt", (senderId, msg) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ChatDisplayTextBox.Text += $"{senderId}: {msg}\n";
                        });
                    });

                    await hubConnection.StartAsync();

                    isConnected = true;
                    ConnectButton.Content = "Disconnect";

                    MeetingIdTextBox.IsEnabled = false;
                    UserIdTextBox.IsEnabled = false;
                    ChatDisplayTextBox.IsEnabled = true;
                    ChatDisplayTextBox.Text = "";
                    MessageTextBox.IsEnabled = true;
                    SendMessageButton.IsEnabled = true;

                    StatusTextBlock.Text = "Connected!";
                }
                catch (Exception ex)
                {
                    StatusTextBlock.Text = "Connection failed: " + ex.Message;
                }
            }
            else
            {
                string meetingId = MeetingIdTextBox.Text.Trim();
                string userId = UserIdTextBox.Text.Trim();

                string leaveUrl = $"{FunctionBaseUrl}/LeaveGroup?meetingId={meetingId}&userId={userId}";
                await http.PostAsync(leaveUrl, null);

                if (hubConnection is not null)
                    await hubConnection.StopAsync();

                isConnected = false;
                ConnectButton.Content = "Connect";

                MeetingIdTextBox.IsEnabled = true;
                UserIdTextBox.IsEnabled = true;

                ChatDisplayTextBox.IsEnabled = false;
                MessageTextBox.IsEnabled = false;
                SendMessageButton.IsEnabled = false;

                StatusTextBlock.Text = "Disconnected.";
            }
        }

        private async void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            string meetingId = MeetingIdTextBox.Text.Trim();
            string userId = UserIdTextBox.Text.Trim();
            string message = MessageTextBox.Text.Trim();

            if (string.IsNullOrEmpty(message)) return;

            string sendUrl =
                $"{FunctionBaseUrl}/MessageSignalR?meetingId={meetingId}&userId={userId}&message={Uri.EscapeDataString(message)}";

            await http.GetAsync(sendUrl);

            MessageTextBox.Text = "";
        }
    }

    public class SignalRNegotiateResponse
    {
        public string? Url { get; set; }
        public string? AccessToken { get; set; }
    }
}
