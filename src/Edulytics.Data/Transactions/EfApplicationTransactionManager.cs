using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Edulytics.Data.Transactions;

public sealed class EfApplicationTransactionManager
    : IApplicationTransactionManager
{
    private readonly EdulyticsDbContext _db;

    public EfApplicationTransactionManager(
        EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<IApplicationTransaction> BeginAsync(
        CancellationToken cancellationToken = default)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            return new NonOwningApplicationTransaction();
        }

        var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        return new EfApplicationTransaction(
            transaction);
    }

    private sealed class EfApplicationTransaction
        : IApplicationTransaction
    {
        private readonly IDbContextTransaction _transaction;
        private bool _completed;

        public EfApplicationTransaction(
            IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public async Task CommitAsync(
            CancellationToken cancellationToken = default)
        {
            if (_completed)
                return;

            await _transaction.CommitAsync(
                cancellationToken);

            _completed = true;
        }

        public async Task RollbackAsync(
            CancellationToken cancellationToken = default)
        {
            if (_completed)
                return;

            await _transaction.RollbackAsync(
                cancellationToken);

            _completed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_completed)
            {
                await _transaction.RollbackAsync();
                _completed = true;
            }

            await _transaction.DisposeAsync();
        }
    }

    private sealed class NonOwningApplicationTransaction
        : IApplicationTransaction
    {
        public Task CommitAsync(
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RollbackAsync(
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public ValueTask DisposeAsync() =>
            ValueTask.CompletedTask;
    }
}
