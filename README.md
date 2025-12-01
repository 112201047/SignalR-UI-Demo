# SignalR-UI-Demo

A minimal WPF application demonstrating how to connect to an Azure Function-based SignalR backend, join/leave groups, and exchange messages in real time.  
This UI is built purely for testing and demonstration purposes and requires no additional setup—multiple instances of the application can be launched directly.


## Features

- Connect to Azure SignalR using a **negotiate** endpoint  
- Join and leave SignalR groups (meetings)  
- Send and receive messages in real time  
- Run multiple UI instances to simulate multiple users  
- Fully minimal: no database, no authentication flows, no additional setup  

### Endpoints Used by the UI

| Endpoint               | Purpose |
|------------------------|---------|
| **/negotiate**         | Retrieves hub URL + access token |
| **/JoinGroup**         | Adds user to a SignalR group (MeetingId) |
| **/LeaveGroup**        | Removes user from the group |
| **/MessageSignalR**    | Sends a message to the group |


## Running the Application

### Requirements
- Windows  
- .NET 8 SDK or higher  
- Visual Studio  

### Steps
1. Clone this repository:
   ```bash
   git clone https://github.com/<your-repo>/SignalR-UI-Demo.git
2. Open the solution in Visual Studio.
3. Build and run the project.
4. Launch multiple instances of the app to simulate multiple users.

### How to Use
1. Enter:
    - Meeting ID (any string)
    - User ID (unique per instance)
2. Click Connect
3. Type a message and press Send
4. Messages appear in the chat panel for all users in the same meeting
5. Click Disconnect to leave the meeting

## Function App
The implementation of the function app is done in the following repo:

[SignalR Function App Repository](https://github.com/112201047/SingalR-FunctionApp-Demo)

**Note:** The function app has already been deployed and the API endpoints are up.
