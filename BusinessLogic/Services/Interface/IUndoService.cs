namespace BusinessLogic.Services.Interface
{
    public interface IUndoService
    {
        Task<(bool Success, string Message)> UndoAsync(int recordOfChangeId);
    }
}
