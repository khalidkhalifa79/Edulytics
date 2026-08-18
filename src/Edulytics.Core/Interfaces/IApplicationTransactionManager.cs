namespace Edulytics.Core.Interfaces;

public interface IApplicationTransaction : IAsyncDisposable
{
    Task CommitAsync(
        CancellationToken cancellationToken = default);

    Task RollbackAsync(
        CancellationToken cancellationToken = default);
}

public interface IApplicationTransactionManager
{
    Task<IApplicationTransaction> BeginAsync(
        CancellationToken cancellationToken = default);
}
