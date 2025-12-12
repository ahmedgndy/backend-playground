# WebSocket Notification System

A real-time notification system built with ASP.NET Core WebSockets, demonstrating bi-directional communication between server and client with a LinkedIn-style notification interface.

## 📋 Overview

This mini project showcases a WebSocket implementation where the server sends random notifications to all connected clients. The client features a modern notification bell interface similar to LinkedIn's notification system.

## 🚀 Features

- **Real-time WebSocket Communication**: Bi-directional messaging between server and clients
- **Automatic Notifications**: Server sends random notifications every 4-9 seconds to simulate real-world scenarios
- **Multiple Client Support**: Handles multiple WebSocket connections simultaneously
- **LinkedIn-style UI**: Modern notification interface with:
  - Notification bell with badge counter
  - Slide-down animations
  - Unread notification indicators
  - Time stamps
  - Avatar placeholders

## 🛠️ Technologies Used

- **Backend**: ASP.NET Core (.NET 10.0)
- **Protocol**: WebSocket
- **Frontend**: Vanilla HTML, CSS, and JavaScript

## 📁 Project Structure

```
WebSockets-SingleR/
├── Program.cs                 # Main server application with WebSocket logic
├── WebSockt.csproj           # Project configuration file
├── appsettings.json          # Application settings
├── WebSocketOpen/
│   └── index.html            # Client UI with WebSocket connection
└── README.md                 # This file
```

## 🔧 How It Works

### Server-Side (`Program.cs`)

1. **WebSocket Endpoint**: Listens on `/ws` for WebSocket connections
2. **Connection Management**: Maintains a list of all active WebSocket connections
3. **Background Task**: Runs a continuous task that:
   - Generates random notifications every 4-9 seconds
   - Sends notifications to all connected clients
   - Simulates button press notifications (buttons 1-20)
4. **Message Handling**: Receives messages from clients and sends acknowledgments

### Client-Side (`index.html`)

1. **WebSocket Connection**: Connects to the server's WebSocket endpoint
2. **Notification Display**: Shows incoming notifications in a bell dropdown
3. **UI Features**:
   - Notification counter badge
   - Unread indicators
   - Smooth animations
   - Timestamp display
   - Empty state when no notifications

## 🚦 Getting Started

### Prerequisites

- .NET 10.0 SDK installed
- A modern web browser with WebSocket support

### Running the Application

1. **Start the Server**:

   ```bash
   dotnet run
   ```

   The server will start on `http://localhost:5000` (or the configured port)

2. **Open the Client**:

   - Navigate to the `WebSocketOpen` folder
   - Open `index.html` in your browser
   - Or update the WebSocket URL in `index.html` to point to your server

3. **Watch the Magic**:
   - The connection status will show "Connected"
   - Notifications will automatically appear every 4-9 seconds
   - Click the bell icon to view all notifications

## 📡 WebSocket Communication Flow

```
Client                          Server
  |                                |
  |--- WebSocket Handshake ------->|
  |<---- Connection Accepted ------|
  |                                |
  |                                |--- Background Task Starts
  |                                |
  |<---- Random Notification ------|
  |<---- Random Notification ------|
  |--- User Message -------------->|
  |<---- Acknowledgment -----------|
  |                                |
  |--- Close Connection ---------->|
  |<---- Connection Closed --------|
```

## 🎯 Key Code Highlights

### Server: Sending Notifications to All Clients

```csharp
foreach (var ws in webSocketConnections.ToList())
{
    if (ws.State == WebSocketState.Open)
    {
        await ws.SendAsync(messageBytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
```

### Client: Handling Incoming Messages

```javascript
socket.onmessage = (event) => {
  const data = JSON.parse(event.data);
  addNotification(data);
};
```

## 🔒 Connection Management

- Connections are added to `webSocketConnections` list when accepted
- Connections are removed when closed
- Error handling ensures failed sends don't crash the server
- State checking prevents sending to closed connections

## 🎨 UI Customization

The notification interface can be customized by modifying the CSS in `index.html`:

- Colors and themes
- Animation timings
- Badge styles
- Avatar styles

## 📝 Notes

- The project name uses "SingleR" in the folder path but the implementation is pure WebSocket (not SignalR)
- Random delays (4-9 seconds) simulate real-world notification patterns
- The server generates button numbers (1-20) as sample notification data

## 🔮 Future Enhancements

- Add user authentication
- Implement persistent notifications (database storage)
- Add notification filtering and categories
- Implement notification preferences
- Add sound alerts
- Support for rich notification content (images, links)

## 📄 License

This is a learning/demonstration project.

---

**Happy Coding! 🚀**
