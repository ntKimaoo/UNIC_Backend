namespace BusinessLogic.Hubs
{
    public interface INotificationHubContext
    {
        Task SendToUserAsync(Guid userId, object notification);
    }
}
