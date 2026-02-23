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

        /// <summary>
        /// Thông báo trạng thái mic (bật/tắt) cho các user khác trong room
        /// </summary>
        public async Task ToggleMic(string roomId, bool isMuted)
        {
            RoomUser? user = null;
            lock (_lock)
            {
                if (_rooms.ContainsKey(roomId) && _rooms[roomId].ContainsKey(Context.ConnectionId))
                {
                    user = _rooms[roomId][Context.ConnectionId];
                }
            }

            if (user == null)
            {
                _logger.LogWarning("ToggleMic: User {ConnectionId} not found in room {RoomId}", Context.ConnectionId, roomId);
                return;
            }

            _logger.LogInformation(
                "User {UserId} ({FullName}) toggled mic to {IsMuted} in room {RoomId}",
                user.UserId, user.FullName, isMuted ? "muted" : "unmuted", roomId);

            await Clients.OthersInGroup(roomId)
                         .SendAsync("UserToggleMic", new { user.ConnectionId, user.UserId, user.FullName, IsMuted = isMuted });
        }

        /// <summary>
        /// Thông báo trạng thái camera (bật/tắt) cho các user khác trong room
        /// </summary>
        public async Task ToggleCamera(string roomId, bool isCameraOff)
        {
            RoomUser? user = null;
            lock (_lock)
            {
                if (_rooms.ContainsKey(roomId) && _rooms[roomId].ContainsKey(Context.ConnectionId))
                {
                    user = _rooms[roomId][Context.ConnectionId];
                }
            }

            if (user == null)
            {
                _logger.LogWarning("ToggleCamera: User {ConnectionId} not found in room {RoomId}", Context.ConnectionId, roomId);
                return;
            }

            _logger.LogInformation(
                "User {UserId} ({FullName}) toggled camera to {IsCameraOff} in room {RoomId}",
                user.UserId, user.FullName, isCameraOff ? "off" : "on", roomId);

            await Clients.OthersInGroup(roomId)
                         .SendAsync("UserToggleCamera", new { user.ConnectionId, user.UserId, user.FullName, IsCameraOff = isCameraOff });
        }

        /// <summary>
        /// Thông báo bắt đầu chia sẻ màn hình cho các user khác trong room
        /// </summary>
        public async Task StartScreenShare(string roomId)
        {
            RoomUser? user = null;
            lock (_lock)
            {
                if (_rooms.ContainsKey(roomId) && _rooms[roomId].ContainsKey(Context.ConnectionId))
                {
                    user = _rooms[roomId][Context.ConnectionId];
                }
            }

            if (user == null)
            {
                _logger.LogWarning("StartScreenShare: User {ConnectionId} not found in room {RoomId}", Context.ConnectionId, roomId);
                return;
            }

            _logger.LogInformation(
                "User {UserId} ({FullName}) started screen sharing in room {RoomId}",
                user.UserId, user.FullName, roomId);

            await Clients.OthersInGroup(roomId)
                         .SendAsync("UserStartedScreenShare", new { user.ConnectionId, user.UserId, user.FullName });
        }

        /// <summary>
        /// Thông báo dừng chia sẻ màn hình cho các user khác trong room
        /// </summary>
        public async Task StopScreenShare(string roomId)
        {
            RoomUser? user = null;
            lock (_lock)
            {
                if (_rooms.ContainsKey(roomId) && _rooms[roomId].ContainsKey(Context.ConnectionId))
                {
                    user = _rooms[roomId][Context.ConnectionId];
                }
            }

            if (user == null)
            {
                _logger.LogWarning("StopScreenShare: User {ConnectionId} not found in room {RoomId}", Context.ConnectionId, roomId);
                return;
            }

            _logger.LogInformation(
                "User {UserId} ({FullName}) stopped screen sharing in room {RoomId}",
                user.UserId, user.FullName, roomId);

            await Clients.OthersInGroup(roomId)
                         .SendAsync("UserStoppedScreenShare", new { user.ConnectionId, user.UserId, user.FullName });
        }

        /// <summary>
        /// Gửi tin nhắn chat đến tất cả user trong room
        /// </summary>
        public async Task SendMessage(string roomId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            RoomUser? user = null;
            lock (_lock)
            {
                if (_rooms.ContainsKey(roomId) && _rooms[roomId].ContainsKey(Context.ConnectionId))
                {
                    user = _rooms[roomId][Context.ConnectionId];
                }
            }

            if (user == null)
            {
                _logger.LogWarning("SendMessage: User {ConnectionId} not found in room {RoomId}", Context.ConnectionId, roomId);
                return;
            }

            var chatMessage = new
            {
                MessageId = Guid.NewGuid(),
                user.ConnectionId,
                user.UserId,
                user.FullName,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation(
                "User {UserId} ({FullName}) sent message in room {RoomId}: {Message}",
                user.UserId, user.FullName, roomId, message.Length > 50 ? message[..50] + "..." : message);

            // Gửi tin nhắn cho tất cả user trong room (bao gồm cả người gửi)
            await Clients.Group(roomId)
                         .SendAsync("ReceiveMessage", chatMessage);
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
