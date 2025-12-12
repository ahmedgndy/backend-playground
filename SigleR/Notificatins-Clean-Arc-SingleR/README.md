# Notificatins-Clean-Arc-SingleR

A clean architecture .NET project demonstrating real-time notifications using SignalR.

## What is SignalR?

SignalR is a library for ASP.NET that enables real-time web functionality, allowing server code to push content to connected clients instantly. It's commonly used for chat apps, live dashboards, notifications, and collaborative tools.

## Project Overview

This project implements a notification system with:

- **Clean Architecture**: Separates concerns into Application, Domain, Infrastructure, and API layers.
- **SignalR**: For real-time communication between server and clients.

## Interview/Concept Questions

### 1. What is SignalR and why use it?

SignalR enables real-time communication between server and clients. Use it when you need instant updates (e.g., notifications, chat, live data).

### 2. What is Clean Architecture?

A design pattern that separates code into layers (Domain, Application, Infrastructure, API) to improve maintainability, testability, and scalability.

### 3. What is a Hub in SignalR?

A Hub is a central class in SignalR that handles client-server communication. Clients connect to the hub to send/receive messages.

## Project Structure

```
Api/
  Hubs/                # SignalR hubs (NotificationHub)
  Contracts/           # API request/response models
Application/
  Interfaces/          # Service and store interfaces
  Services/            # Business logic (NotificationService)
Domain/
  Entities/            # Core domain models (Notification)
Infrastructure/
  Persistence/         # In-memory notification store
  Realtime/            # SignalR dispatcher, online user tracker
Program.cs             # App startup and DI
```

## Key Code Snippets

### 1. SignalR Hub (NotificationHub)

```csharp
public class NotificationHub : Hub { }
```

### 2. Online User Tracker (InMemoryOnlineUserTracker)

```csharp
public class InMemoryOnlineUserTracker : IOnlineUserTracker
{
    private readonly ConcurrentDictionary<string, string> _connectionToUser = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _userToConnections = new(StringComparer.OrdinalIgnoreCase);
    // ...methods to register/unregister connections and get online users...
}
```

### 3. Notification Service

```csharp
public class NotificationService : INotificationService
{
    private readonly INotificationStore _store;
    private readonly INotificationDispatcher _dispatcher;
    public async Task CreateAndDispatchAsync(Notification notification) {
        await _store.SaveAsync(notification);
        await _dispatcher.DispatchAsync(notification);
    }
}
```

### 4. SignalR Notification Dispatcher

```csharp
public class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<NotificationHub> _hubContext;
    public async Task DispatchAsync(Notification notification) {
        await _hubContext.Clients.User(notification.UserId).SendAsync("ReceiveNotification", notification);
    }
}
```

## How Notifications Work

1. **Client connects** to the `NotificationHub`.
2. **User is registered** in `InMemoryOnlineUserTracker`.
3. **Notification is created** via API and saved in `INotificationStore`.
4. **Notification is dispatched** to the user via SignalR.

## Important Concepts

- **Real-time communication** with SignalR
- **User connection management**
- **Separation of concerns** (Clean Architecture)
- **Dependency Injection**

## What to Know for Interviews

- How SignalR works (hubs, connections, groups)
- Clean Architecture principles
- How to manage online users and multiple connections
- How to send targeted notifications
- How to structure a scalable, maintainable .NET project

---

Feel free to explore the code and ask about any specific part!
