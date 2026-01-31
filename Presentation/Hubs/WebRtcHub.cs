using Microsoft.AspNetCore.SignalR;

namespace UNIC.Presentation.Hubs
{
    public class WebRtcHub : Hub
    {
        private static Dictionary<string, HashSet<string>> _rooms = new();
        private static readonly object _lock = new object();
        private readonly ILogger<WebRtcHub> _logger;

        public WebRtcHub(ILogger<WebRtcHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinRoom(string roomId)
        {
            _logger.LogInformation(
                "User {ConnectionId} is joining room {RoomId}",
                Context.ConnectionId, roomId);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            lock (_lock)
            {
                if (!_rooms.ContainsKey(roomId))
                {
                    _rooms[roomId] = new HashSet<string>();
                    _logger.LogInformation("Room {RoomId} created", roomId);
                }

                _rooms[roomId].Add(Context.ConnectionId);
            }

            await Clients.OthersInGroup(roomId)
                         .SendAsync("UserJoined", Context.ConnectionId);

            var existingUsers = _rooms[roomId]
                .Where(id => id != Context.ConnectionId)
                .ToList();

            _logger.LogInformation(
                "Room {RoomId} current users: {Users}",
                roomId, string.Join(", ", existingUsers));

            await Clients.Caller.SendAsync("ExistingUsers", existingUsers);
        }

        public async Task SendSignal(string roomId, string toUserId, object signal)
        {
            _logger.LogInformation(
                "Signal sent from {From} to {To} in room {RoomId}",
                Context.ConnectionId, toUserId, roomId);

            await Clients.Client(toUserId)
                         .SendAsync("ReceiveSignal", Context.ConnectionId, signal);
        }

        public async Task LeaveRoom(string roomId)
        {
            _logger.LogInformation(
                "User {ConnectionId} is leaving room {RoomId}",
                Context.ConnectionId, roomId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

            lock (_lock)
            {
                if (_rooms.ContainsKey(roomId))
                {
                    _rooms[roomId].Remove(Context.ConnectionId);

                    if (_rooms[roomId].Count == 0)
                    {
                        _rooms.Remove(roomId);
                        _logger.LogInformation("Room {RoomId} removed (empty)", roomId);
                    }
                }
            }

            await Clients.OthersInGroup(roomId)
                         .SendAsync("UserLeft", Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogWarning(
                "User {ConnectionId} disconnected. Error: {Error}",
                Context.ConnectionId, exception?.Message);

            lock (_lock)
            {
                var roomsToUpdate = _rooms
                    .Where(r => r.Value.Contains(Context.ConnectionId))
                    .Select(r => r.Key)
                    .ToList();

                foreach (var roomId in roomsToUpdate)
                {
                    _rooms[roomId].Remove(Context.ConnectionId);

                    _logger.LogInformation(
                        "User {ConnectionId} removed from room {RoomId}",
                        Context.ConnectionId, roomId);

                    if (_rooms[roomId].Count == 0)
                    {
                        _rooms.Remove(roomId);
                        _logger.LogInformation("Room {RoomId} removed (empty)", roomId);
                    }
                    else
                    {
                        Clients.OthersInGroup(roomId)
                               .SendAsync("UserLeft", Context.ConnectionId);
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
