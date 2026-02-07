using Microsoft.AspNetCore.SignalR;

namespace UNIC.Presentation.Hubs
{
    public class RoomUser
    {
        public string ConnectionId { get; set; } = null!;
        public Guid UserId { get; set; }
        public string FullName { get; set; } = null!;
    }

    public class WebRtcHub : Hub
    {
        private static Dictionary<string, Dictionary<string, RoomUser>> _rooms = new();
        private static readonly object _lock = new object();
        private readonly ILogger<WebRtcHub> _logger;

        public WebRtcHub(ILogger<WebRtcHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinRoom(string roomId, Guid userId, string fullName)
        {
            _logger.LogInformation(
                "User {UserId} ({FullName}) with ConnectionId {ConnectionId} is joining room {RoomId}",
                userId, fullName, Context.ConnectionId, roomId);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            var roomUser = new RoomUser
            {
                ConnectionId = Context.ConnectionId,
                UserId = userId,
                FullName = fullName
            };

            lock (_lock)
            {
                if (!_rooms.ContainsKey(roomId))
                {
                    _rooms[roomId] = new Dictionary<string, RoomUser>();
                    _logger.LogInformation("Room {RoomId} created", roomId);
                }

                _rooms[roomId][Context.ConnectionId] = roomUser;
            }

            await Clients.OthersInGroup(roomId)
                         .SendAsync("UserJoined", roomUser);

            List<RoomUser> existingUsers;
            lock (_lock)
            {
                existingUsers = _rooms[roomId]
                    .Where(kvp => kvp.Key != Context.ConnectionId)
                    .Select(kvp => kvp.Value)
                    .ToList();
            }

            _logger.LogInformation(
                "Room {RoomId} current users: {Users}",
                roomId, string.Join(", ", existingUsers.Select(u => $"{u.FullName} ({u.UserId})")));

            await Clients.Caller.SendAsync("ExistingUsers", existingUsers);
        }

        public async Task SendSignal(string roomId, string toConnectionId, object signal)
        {
            RoomUser? fromUser = null;
            lock (_lock)
            {
                if (_rooms.ContainsKey(roomId) && _rooms[roomId].ContainsKey(Context.ConnectionId))
                {
                    fromUser = _rooms[roomId][Context.ConnectionId];
                }
            }

            _logger.LogInformation(
                "Signal sent from {FromUserId} ({FromName}) to {ToConnectionId} in room {RoomId}",
                fromUser?.UserId, fromUser?.FullName, toConnectionId, roomId);

            await Clients.Client(toConnectionId)
                         .SendAsync("ReceiveSignal", fromUser, signal);
        }

        public async Task LeaveRoom(string roomId)
        {
            RoomUser? leavingUser = null;

            lock (_lock)
            {
                if (_rooms.ContainsKey(roomId) && _rooms[roomId].ContainsKey(Context.ConnectionId))
                {
                    leavingUser = _rooms[roomId][Context.ConnectionId];
                }
            }

            _logger.LogInformation(
                "User {UserId} ({FullName}) with ConnectionId {ConnectionId} is leaving room {RoomId}",
                leavingUser?.UserId, leavingUser?.FullName, Context.ConnectionId, roomId);

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
                         .SendAsync("UserLeft", leavingUser);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogWarning(
                "User {ConnectionId} disconnected. Error: {Error}",
                Context.ConnectionId, exception?.Message);

            List<(string roomId, RoomUser user)> usersToNotify = new();

            lock (_lock)
            {
                var roomsToUpdate = _rooms
                    .Where(r => r.Value.ContainsKey(Context.ConnectionId))
                    .Select(r => r.Key)
                    .ToList();

                foreach (var roomId in roomsToUpdate)
                {
                    var user = _rooms[roomId][Context.ConnectionId];
                    usersToNotify.Add((roomId, user));

                    _rooms[roomId].Remove(Context.ConnectionId);

                    _logger.LogInformation(
                        "User {UserId} ({FullName}) removed from room {RoomId}",
                        user.UserId, user.FullName, roomId);

                    if (_rooms[roomId].Count == 0)
                    {
                        _rooms.Remove(roomId);
                        _logger.LogInformation("Room {RoomId} removed (empty)", roomId);
                    }
                }
            }

            foreach (var (roomId, user) in usersToNotify)
            {
                await Clients.OthersInGroup(roomId)
                       .SendAsync("UserLeft", user);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
