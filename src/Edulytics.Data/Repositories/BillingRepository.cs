using Edulytics.Core.Billing;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class BillingRepository : IBillingRepository
{
    private readonly EdulyticsDbContext _db;

    public BillingRepository(EdulyticsDbContext db)
    {
        _db = db;
    }

    public Task<SchoolBillingProfile?> GetProfileAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        _db.SchoolBillingProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.SchoolId == schoolId, cancellationToken);

    public Task<SchoolBillingProfile?> GetProfileForUpdateAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        if (!_db.Database.IsRelational())
        {
            return _db.SchoolBillingProfiles.SingleOrDefaultAsync(
                x => x.SchoolId == schoolId,
                cancellationToken);
        }

        return _db.SchoolBillingProfiles
            .FromSqlInterpolated(
                $@"SELECT * FROM ""SchoolBillingProfiles"" WHERE ""SchoolId"" = {schoolId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BillingInvoice>> ListInvoicesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        await _db.BillingInvoices
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

    public Task<BillingInvoice?> GetInvoiceForUpdateAsync(
        Guid schoolId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        if (!_db.Database.IsRelational())
        {
            return _db.BillingInvoices.SingleOrDefaultAsync(
                x => x.SchoolId == schoolId && x.Id == invoiceId,
                cancellationToken);
        }

        return _db.BillingInvoices
            .FromSqlInterpolated(
                $@"SELECT * FROM ""BillingInvoices"" WHERE ""SchoolId"" = {schoolId} AND ""Id"" = {invoiceId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BillingInvoiceLine>> ListInvoiceLinesAsync(
        Guid schoolId,
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        await _db.BillingInvoiceLines
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.InvoiceId == invoiceId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<BankTransferPayment>> ListPaymentsAsync(
        Guid schoolId,
        Guid invoiceId,
        CancellationToken cancellationToken = default) =>
        await _db.BankTransferPayments
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.InvoiceId == invoiceId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

    public Task<BankTransferPayment?> GetPaymentForUpdateAsync(
        Guid schoolId,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        if (!_db.Database.IsRelational())
        {
            return _db.BankTransferPayments.SingleOrDefaultAsync(
                x => x.SchoolId == schoolId && x.Id == paymentId,
                cancellationToken);
        }

        return _db.BankTransferPayments
            .FromSqlInterpolated(
                $@"SELECT * FROM ""BankTransferPayments"" WHERE ""SchoolId"" = {schoolId} AND ""Id"" = {paymentId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriptionSeatChange>> ListUnbilledSeatIncreasesAsync(
        Guid schoolId,
        Guid subscriptionId,
        CancellationToken cancellationToken = default) =>
        await _db.SubscriptionSeatChanges
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == schoolId &&
                x.SubscriptionId == subscriptionId &&
                x.ChangeType == SeatCommitmentChangeType.Increase &&
                !_db.BillingInvoiceLines.Any(line => line.SubscriptionSeatChangeId == x.Id))
            .OrderBy(x => x.EffectiveAtUtc)
            .ToArrayAsync(cancellationToken);

    public Task<bool> HasInvoiceAsync(
        Guid subscriptionId,
        BillingInvoiceKind kind,
        int? installmentNumber,
        CancellationToken cancellationToken = default) =>
        _db.BillingInvoices
            .AsNoTracking()
            .AnyAsync(
                x => x.SubscriptionId == subscriptionId &&
                     x.Kind == kind &&
                     x.InstallmentNumber == installmentNumber &&
                     x.Status != BillingInvoiceStatus.Cancelled,
                cancellationToken);

    public async Task<BillingPersistenceResult> SaveProfileAsync(
        SchoolBillingProfile profile,
        byte[]? expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (expectedRowVersion is null)
                await _db.SchoolBillingProfiles.AddAsync(profile, cancellationToken);
            else
                _db.Entry(profile).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;

            await _db.SaveChangesAsync(cancellationToken);
            return BillingPersistenceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            _db.ChangeTracker.Clear();
            return BillingPersistenceResult.Failure(BillingPersistenceError.Concurrency);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return BillingPersistenceResult.Failure(BillingPersistenceError.Constraint);
        }
        catch
        {
            _db.ChangeTracker.Clear();
            return BillingPersistenceResult.Failure(BillingPersistenceError.Unknown);
        }
    }

    public async Task<BillingPersistenceResult> AddInvoiceAsync(
        BillingInvoice invoice,
        IReadOnlyList<BillingInvoiceLine> lines,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.BillingInvoices.AddAsync(invoice, cancellationToken);
            await _db.BillingInvoiceLines.AddRangeAsync(lines, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return BillingPersistenceResult.Success();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return BillingPersistenceResult.Failure(BillingPersistenceError.Constraint);
        }
        catch
        {
            _db.ChangeTracker.Clear();
            return BillingPersistenceResult.Failure(BillingPersistenceError.Unknown);
        }
    }

    public async Task<BillingPersistenceResult> AddPaymentAsync(
        BankTransferPayment payment,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.BankTransferPayments.AddAsync(payment, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return BillingPersistenceResult.Success();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return BillingPersistenceResult.Failure(BillingPersistenceError.Constraint);
        }
        catch
        {
            _db.ChangeTracker.Clear();
            return BillingPersistenceResult.Failure(BillingPersistenceError.Unknown);
        }
    }

    public async Task<BillingPersistenceResult> SavePaymentAndInvoiceAsync(
        BankTransferPayment payment,
        byte[] expectedPaymentRowVersion,
        BillingInvoice invoice,
        byte[] expectedInvoiceRowVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _db.Entry(payment).Property(x => x.RowVersion).OriginalValue = expectedPaymentRowVersion;
            _db.Entry(invoice).Property(x => x.RowVersion).OriginalValue = expectedInvoiceRowVersion;
            await _db.SaveChangesAsync(cancellationToken);
            return BillingPersistenceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            _db.ChangeTracker.Clear();
            return BillingPersistenceResult.Failure(BillingPersistenceError.Concurrency);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return BillingPersistenceResult.Failure(BillingPersistenceError.Constraint);
        }
        catch
        {
            _db.ChangeTracker.Clear();
            return BillingPersistenceResult.Failure(BillingPersistenceError.Unknown);
        }
    }

    public async Task<BillingPersistenceResult> SaveInvoiceAndRefundAsync(
        BillingInvoice invoice,
        byte[] expectedInvoiceRowVersion,
        BillingRefund refund,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _db.Entry(invoice).Property(x => x.RowVersion).OriginalValue = expectedInvoiceRowVersion;
            await _db.BillingRefunds.AddAsync(refund, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return BillingPersistenceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            _db.ChangeTracker.Clear();
            return BillingPersistenceResult.Failure(BillingPersistenceError.Concurrency);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return BillingPersistenceResult.Failure(BillingPersistenceError.Constraint);
        }
        catch
        {
            _db.ChangeTracker.Clear();
            return BillingPersistenceResult.Failure(BillingPersistenceError.Unknown);
        }
    }
}
