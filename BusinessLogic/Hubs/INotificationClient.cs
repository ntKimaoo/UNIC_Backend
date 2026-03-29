namespace BusinessLogic.Hubs
{
    public interface INotificationClient
    {
        Task ReceiveNotification(object notification);
    }
}
