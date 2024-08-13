using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.WebHost.UseUrls("http://0.0.0.0:6969");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Logger.LogInformation("*** BACKEND IS NOW ONLINE ***");
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseHttpsRedirection();
app.UseWebSockets();
var rooms = new ConcurrentDictionary<string, Room>();
app.Map("/controller", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        var clientIp = context.Connection.RemoteIpAddress;

        if (clientIp == null)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Unable to determine client IP.");
            return;
        }

        // Determine the subnet based on the IP address (assuming IPv4)
        var subnet = GetSubnet(clientIp);
        var isController = context.Request.Query["isController"] == "1";

        // Ensure a room exists for this subnet
        var room = rooms.GetOrAdd(subnet, new Room());

        if (isController)
        {
            room.Controller = ws;
        }
        else
        {
            room.Clients.TryAdd(context.Connection.Id, ws);
        }

        while (ws.State == WebSocketState.Open)
        {
            var buffer = new byte[1024 * 4];
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Client ID {context.Connection.Id} has sent: {message}");
                if (isController)
                {
                    // Broadcast message to all clients in the room
                    foreach (var client in room.Clients.Values)
                    {
                        if (client.State == WebSocketState.Open)
                        {
                            await client.SendAsync(
                                new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None
                            );
                        }
                    }
                }
                else
                {
                    // Handle non-controller messages (if necessary)
                }
            }

            if (ws.State == WebSocketState.CloseReceived && result.CloseStatus.HasValue)
            {
                if (isController)
                {
                    room.Controller = null;
                }
                else
                {
                    room.Clients.TryRemove(context.Connection.Id, out _);
                }
                await ws.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
        }
    }
    else
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("WebSocket requests only.");
    }
});
await app.RunAsync();


string GetSubnet(IPAddress ipAddress)
{
    // Assuming IPv4 and subnet mask of 255.255.255.0 (Class C network)
    var ipBytes = ipAddress.GetAddressBytes();
    return $"{ipBytes[0]}.{ipBytes[1]}.{ipBytes[2]}.0";
}
public class Room
{
    public WebSocket? Controller { get; set; }
    public ConcurrentDictionary<string, WebSocket> Clients { get; } = new();
}


public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log the request details to the console
        Console.WriteLine($"Request Path: {context.Request.Path}");
        Console.WriteLine($"Request Method: {context.Request.Method}");
        Console.WriteLine($"Request Headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
        Console.WriteLine($"Request Query String: {context.Request.QueryString}");
        Console.WriteLine($"Request ID: {context.Connection.Id}");

        // Call the next middleware in the pipeline
        await _next(context);
    }
}

