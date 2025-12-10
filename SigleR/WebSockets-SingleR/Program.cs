using System.Net.WebSockets;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var webSocketConnections = new List<WebSocket>();// Keep track of all connected WebSockets

app.UseWebSockets();

// Background task to send fake notifications
var cts = new CancellationTokenSource();
_ =
Task.Run(async () =>
{
    var random = new Random();
    while (!cts.Token.IsCancellationRequested)
    {
        await Task.Delay(random.Next(4000, 9000)); // Random delay between 3-8 seconds

        if (webSocketConnections.Count > 0)
        {
            var buttonNumber = random.Next(1, 21); // Random button 1-20
            var notification = JsonSerializer.Serialize(new { buttonNumber = buttonNumber.ToString() });
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(notification);

            Console.WriteLine($"Sending fake notification to button {buttonNumber}");

            // Send to all connected clients
            foreach (var ws in webSocketConnections.ToList())
            {
                if (ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending notification: {ex.Message}");
                    }
                }
            }
        }
    }
}, cts.Token);

app.MapGet("/ws", async (HttpContext context) =>
{
    //check if the request is a WebSocket request
    if (context.WebSockets.IsWebSocketRequest)
    {
        // Accept the WebSocket connection
        using WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
        webSocketConnections.Add(webSocket);
        // Handle the WebSocket connection
        var buffer = new byte[1024 * 4];
        // Receive messages in a loop from the client
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        //WHILE the connection is open
        while (!result.CloseStatus.HasValue)
        {
            // Process the received message
            var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Received: {message}");

            // Send acknowledgment back to the client
            var response = System.Text.Encoding.UTF8.GetBytes($"Server received your message");
            await webSocket.SendAsync(new ArraySegment<byte>(response), result.MessageType, result.EndOfMessage, CancellationToken.None);

            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        webSocketConnections.Remove(webSocket);
        Console.WriteLine("WebSocket connection closed");
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();
