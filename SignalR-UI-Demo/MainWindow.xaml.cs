/******************************************************************************
* Filename    = ManWindow.xaml.cs
* Author      = Nikhil S Thomas
* Product     = SignalR Demo
* Project     = SignalR UI
* Description = WPF Application to demonstrate SignalR client functionality.
*****************************************************************************/

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace SignalR_UI_Demo;

/// <summary>
/// Main WPF window class that manages SignalR connection and messaging
/// </summary>
public partial class MainWindow : Window
{
    // Connection state
    private bool _isConnected = false;
    //SignalR Hub connection
    private HubConnection? _hubConnection;
    // HTTP Client for REST API calls
    private static readonly HttpClient s_http = new HttpClient();
    // Base URL for the Azure Function App
    private const string FunctionBaseUrl = "https://signalr-functionapp-demo.azurewebsites.net/api";

    /// <summary>
    /// Constructor
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Implements Connect/Disconnect button click functionality
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isConnected)
        {
            // Extract Meeting and User Id
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

                // Get Access Token and Hub URL from negotiate endpoint
                string negotiateUrl = $"{FunctionBaseUrl}/negotiate?userId={userId}";
                string negotiateResponse = await s_http.GetStringAsync(negotiateUrl);

                SignalRNegotiateResponse? negotiateObj = System.Text.Json.JsonSerializer.Deserialize<SignalRNegotiateResponse>(negotiateResponse);

                // Build Hub Connection using negotiate response
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(negotiateObj!.Url!, options => {
                        options.AccessTokenProvider = () => Task.FromResult(negotiateObj.AccessToken);
                        options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
                    })
                    .WithAutomaticReconnect()
                    .Build();


                // Join the specified group (meeting) on the hub
                string joinUrl = $"{FunctionBaseUrl}/JoinGroup?meetingId={meetingId}&userId={userId}";
                await s_http.PostAsync(joinUrl, null);

                // Register handler for receiving messages
                _hubConnection.On<string, string>("ReceiveDoubt", (senderId, msg) => {
                    Dispatcher.Invoke(() => {
                        ChatDisplayTextBox.Text += $"{senderId}: {msg}\n";
                    });
                });

                // Start the Hub connection
                await _hubConnection.StartAsync();

                // Set active and inactive elements on successful connection
                _isConnected = true;
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
            // Extract Meeting and User Id
            string meetingId = MeetingIdTextBox.Text.Trim();
            string userId = UserIdTextBox.Text.Trim();

            // Leave the group (meeting) on the hub
            string leaveUrl = $"{FunctionBaseUrl}/LeaveGroup?meetingId={meetingId}&userId={userId}";
            await s_http.PostAsync(leaveUrl, null);

            if (_hubConnection is not null)
            {
                await _hubConnection.StopAsync();
            }

            // Set active and inactive elements on disconnection
            _isConnected = false;
            ConnectButton.Content = "Connect";

            MeetingIdTextBox.IsEnabled = true;
            UserIdTextBox.IsEnabled = true;

            ChatDisplayTextBox.IsEnabled = false;
            MessageTextBox.IsEnabled = false;
            SendMessageButton.IsEnabled = false;

            StatusTextBlock.Text = "Disconnected.";
        }
    }

    /// <summary>
    /// Implements Send Message button click functionality
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void SendMessageButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isConnected)
        {
            return;
        }

        // Extract Meeting Id, User Id and Message
        string meetingId = MeetingIdTextBox.Text.Trim();
        string userId = UserIdTextBox.Text.Trim();
        string message = MessageTextBox.Text.Trim();

        if (!string.IsNullOrEmpty(message))
        {
            // Send message via MessageSignalR function
            string sendUrl =
                $"{FunctionBaseUrl}/MessageSignalR?meetingId={meetingId}&userId={userId}&message={Uri.EscapeDataString(message)}";

            await s_http.GetAsync(sendUrl);

            MessageTextBox.Text = "";
        }
    }
}

/// <summary>
/// Model for SignalR negotiate response
/// </summary>
public class SignalRNegotiateResponse
{
    public string? Url { get; set; }
    public string? AccessToken { get; set; }
}
