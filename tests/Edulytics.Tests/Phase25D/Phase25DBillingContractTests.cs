namespace Edulytics.Tests.Phase25D;

public sealed class Phase25DBillingContractTests
{
    [Fact]
    public void BillingDomain_HasRequiredPersistentAggregates()
    {
        Assert.Contains("class SchoolBillingProfile", Read("src/Edulytics.Core/Entities/SchoolBillingProfile.cs"));
        Assert.Contains("class BillingInvoice", Read("src/Edulytics.Core/Entities/BillingInvoice.cs"));
        Assert.Contains("class BillingInvoiceLine", Read("src/Edulytics.Core/Entities/BillingInvoiceLine.cs"));
        Assert.Contains("class BankTransferPayment", Read("src/Edulytics.Core/Entities/BankTransferPayment.cs"));
        Assert.Contains("class BillingRefund", Read("src/Edulytics.Core/Entities/BillingRefund.cs"));
    }

    [Fact]
    public void BillingDbContext_RegistersAllPhase25DTables()
    {
        var source = Read("src/Edulytics.Data/Contexts/EdulyticsDbContext.cs");
        Assert.Contains("DbSet<SchoolBillingProfile>", source);
        Assert.Contains("DbSet<BillingInvoice>", source);
        Assert.Contains("DbSet<BillingInvoiceLine>", source);
        Assert.Contains("DbSet<BankTransferPayment>", source);
        Assert.Contains("DbSet<BillingRefund>", source);
    }

    [Fact]
    public void FinancialRows_UseConcurrencyTokensAndUniqueRaceGuards()
    {
        var invoice = Read("src/Edulytics.Data/Configurations/BillingInvoiceConfiguration.cs");
        var profile = Read("src/Edulytics.Data/Configurations/SchoolBillingProfileConfiguration.cs");
        var payment = Read("src/Edulytics.Data/Configurations/BankTransferPaymentConfiguration.cs");
        var line = Read("src/Edulytics.Data/Configurations/BillingInvoiceLineConfiguration.cs");

        Assert.Contains(".IsConcurrencyToken()", invoice);
        Assert.Contains(".IsConcurrencyToken()", profile);
        Assert.Contains(".IsConcurrencyToken()", payment);
        Assert.Contains("InvoiceNumber).IsUnique()", invoice);
        Assert.Contains("SubscriptionSeatChangeId).IsUnique()", line);
    }

    [Fact]
    public void Repository_UsesPostgresRowLocksForFinancialWrites()
    {
        var source = Read("src/Edulytics.Data/Repositories/BillingRepository.cs");
        Assert.Contains("FOR UPDATE", source);
        Assert.Contains("GetInvoiceForUpdateAsync", source);
        Assert.Contains("GetPaymentForUpdateAsync", source);
        Assert.Contains("GetProfileForUpdateAsync", source);
    }

    [Fact]
    public void BillingController_IsPlatformOnlyAntiForgeryAndNoDbContext()
    {
        var source = Read("src/Edulytics.Web/Controllers/BillingController.cs");
        Assert.Contains("[Authorize(Policy = \"PlatformAdministration\")]", source);
        Assert.True(source.Split("[ValidateAntiForgeryToken]").Length - 1 >= 10);
        Assert.DoesNotContain("EdulyticsDbContext", source);
        Assert.DoesNotContain("FromSql", source);
        Assert.DoesNotContain("SELECT ", source);
    }

    [Fact]
    public void LegacyGenericActivationAndRenewal_AreNonMutatingGuards()
    {
        var controller = Read("src/Edulytics.Web/Controllers/SubscriptionsController.cs");
        var view = Read("src/Edulytics.Web/Views/Subscriptions/Index.cshtml");
        Assert.Contains("Phase25DBillingRequired", controller);
        Assert.DoesNotContain("asp-action=\"Activate\"", view);
        Assert.DoesNotContain("asp-action=\"Renew\"", view);
    }

    [Fact]
    public void BillingService_HasNoPaymentGatewayOrAutomaticSuspension()
    {
        var source = Read("src/Edulytics.Services/Billing/BillingService.cs");
        Assert.DoesNotContain("Stripe", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Adyen", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mollie", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SuspendAsync(", source);
        Assert.Contains("Billing.InitialActivationCompleted", source);
        Assert.Contains("Billing.SeatProrationInvoiced", source);
    }

    [Fact]
    public void PaymentConfirmation_IsIdempotentAndRechecksOutstanding()
    {
        var source = Read("src/Edulytics.Services/Billing/BillingService.cs");
        Assert.Contains("VerificationStatus == BankTransferVerificationStatus.Confirmed", source);
        Assert.Contains("outstandingBeforeConfirmation", source);
        Assert.Contains("AmountExceedsOutstanding", source);
    }

    [Fact]
    public void BillingUi_ExposesLiveWorkflowMarkers()
    {
        var source = Read("src/Edulytics.Web/Views/Billing/Index.cshtml");
        Assert.Contains("data-billing-school-id", source);
        Assert.Contains("data-invoice-id", source);
        Assert.Contains("data-payment-id", source);
        Assert.Contains("RecordPayment", source);
        Assert.Contains("ConfirmPayment", source);
    }

    [Fact]
    public void CommercialSourceContract_RemainsBankTransferOnlyAndManualSuspension()
    {
        var source = Read("docs/PHASE_25A_COMMERCIAL_MODEL.md");
        Assert.Contains("**Bank transfer only.**", source);
        Assert.Contains("**14 calendar days**", source);
        Assert.Contains("**7 additional days**", source);
        Assert.Contains("not automatic", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string Read(string relative) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relative));

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Edulytics.sln")))
                return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
