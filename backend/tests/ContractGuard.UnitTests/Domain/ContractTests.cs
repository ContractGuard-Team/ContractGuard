using ContractGuard.Domain.Common;
using ContractGuard.Domain.Contracts;

namespace ContractGuard.UnitTests.Domain;

public sealed class ContractTests
{
    private static readonly DateTimeOffset Now = TestData.Now;

    [Fact]
    public void Create_starts_as_new_with_version_1()
    {
        var owner = Guid.CreateVersion7();

        var contract = Contract.Create("  Hợp đồng dịch vụ  ", null, owner, TestData.PdfFile(), Now);

        Assert.Equal(ApprovalStatus.New, contract.ApprovalStatus);
        Assert.Equal("Hợp đồng dịch vụ", contract.ContractName);
        Assert.Null(contract.OrganizationId);
        var version = Assert.Single(contract.Versions);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(owner, version.UploadedByUserAccountId);
    }

    [Fact]
    public void Create_rejects_file_over_20_MB()
    {
        var file = new ContractFile("quarantine/big.pdf", "big.pdf", FileType.Pdf, ContractFile.MaxSizeBytes + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => Contract.Create("Big", null, Guid.CreateVersion7(), file, Now));
    }

    [Fact]
    public void Analysis_cannot_start_before_context_is_confirmed()
    {
        var contract = Contract.Create("No context", null, Guid.CreateVersion7(), TestData.PdfFile(), Now);

        var error = Assert.Throws<DomainException>(() => contract.MarkAnalysisStarted(contract.LatestVersion.ContractVersionId, Now));

        Assert.Equal(ErrorCodes.ContextIncomplete, error.ErrorCode);
    }

    [Fact]
    public void Analysis_cannot_start_twice()
    {
        var contract = TestData.ReadyContract();
        contract.MarkAnalysisStarted(contract.LatestVersion.ContractVersionId, Now);

        var error = Assert.Throws<DomainException>(() => contract.MarkAnalysisStarted(contract.LatestVersion.ContractVersionId, Now));

        Assert.Equal(ErrorCodes.ContractBusy, error.ErrorCode);
    }

    [Fact]
    public void Completed_analysis_moves_to_pending_review_and_is_idempotent()
    {
        var contract = TestData.ReadyContract();
        contract.MarkAnalysisStarted(contract.LatestVersion.ContractVersionId, Now);

        contract.MarkAnalysisCompleted(Now);
        contract.MarkAnalysisCompleted(Now);

        Assert.Equal(ApprovalStatus.PendingReview, contract.ApprovalStatus);
        Assert.Null(contract.StatusBeforeAnalysis);
    }

    [Fact]
    public void Failed_analysis_returns_to_the_status_before_it()
    {
        // TR-CTR-003: an approved contract re-analysed for a new version goes back to approved.
        var contract = TestData.ReadyContract();
        contract.MarkAnalysisStarted(contract.LatestVersion.ContractVersionId, Now);
        contract.MarkAnalysisCompleted(Now);
        contract.ChangeApprovalStatus(ApprovalStatus.Approved, Now);
        var v2 = contract.AddVersion(TestData.PdfFile("quarantine/v2.pdf"), contract.OwnerUserAccountId, Now);
        contract.ConfirmContext(v2.ContractVersionId, TestData.Context(), Now);
        contract.MarkAnalysisStarted(v2.ContractVersionId, Now);

        contract.MarkAnalysisFailed(Now);

        Assert.Equal(ApprovalStatus.Approved, contract.ApprovalStatus);
    }

    // Every user transition of SRS §13.2 is allowed.
    [Theory]
    [InlineData(ApprovalStatus.PendingReview, ApprovalStatus.Negotiating)]
    [InlineData(ApprovalStatus.PendingReview, ApprovalStatus.Approved)]
    [InlineData(ApprovalStatus.PendingReview, ApprovalStatus.Rejected)]
    [InlineData(ApprovalStatus.Negotiating, ApprovalStatus.Approved)]
    [InlineData(ApprovalStatus.Negotiating, ApprovalStatus.Rejected)]
    [InlineData(ApprovalStatus.Approved, ApprovalStatus.Negotiating)]
    [InlineData(ApprovalStatus.Rejected, ApprovalStatus.Negotiating)]
    public void Allowed_user_transitions(ApprovalStatus from, ApprovalStatus to)
    {
        var contract = ContractIn(from);

        contract.ChangeApprovalStatus(to, Now);

        Assert.Equal(to, contract.ApprovalStatus);
    }

    // FR-CTR-013: anything else is rejected with ERR-CTR-001.
    [Theory]
    [InlineData(ApprovalStatus.New, ApprovalStatus.Approved)]
    [InlineData(ApprovalStatus.PendingReview, ApprovalStatus.New)]
    [InlineData(ApprovalStatus.Approved, ApprovalStatus.Rejected)]
    [InlineData(ApprovalStatus.Rejected, ApprovalStatus.Approved)]
    [InlineData(ApprovalStatus.PendingReview, ApprovalStatus.Analyzing)]
    public void Forbidden_user_transitions(ApprovalStatus from, ApprovalStatus to)
    {
        var contract = ContractIn(from);

        var error = Assert.Throws<DomainException>(() => contract.ChangeApprovalStatus(to, Now));

        Assert.Equal(ErrorCodes.InvalidStatusTransition, error.ErrorCode);
        Assert.Equal(from, contract.ApprovalStatus);
    }

    [Fact]
    public void New_version_is_numbered_after_the_latest()
    {
        var contract = TestData.ReadyContract();

        var v2 = contract.AddVersion(TestData.PdfFile("quarantine/v2.pdf"), contract.OwnerUserAccountId, Now);

        Assert.Equal(2, v2.VersionNumber);
        Assert.Same(v2, contract.LatestVersion);
    }

    [Fact]
    public void New_version_is_refused_while_analyzing()
    {
        var contract = TestData.ReadyContract();
        contract.MarkAnalysisStarted(contract.LatestVersion.ContractVersionId, Now);

        var error = Assert.Throws<DomainException>(() =>
            contract.AddVersion(TestData.PdfFile("quarantine/v2.pdf"), contract.OwnerUserAccountId, Now));

        Assert.Equal(ErrorCodes.ContractBusy, error.ErrorCode);
    }

    private static Contract ContractIn(ApprovalStatus status)
    {
        var contract = TestData.ReadyContract();
        if (status == ApprovalStatus.New)
        {
            return contract;
        }

        contract.MarkAnalysisStarted(contract.LatestVersion.ContractVersionId, Now);
        if (status == ApprovalStatus.Analyzing)
        {
            return contract;
        }

        contract.MarkAnalysisCompleted(Now);
        if (status == ApprovalStatus.Negotiating)
        {
            contract.ChangeApprovalStatus(ApprovalStatus.Negotiating, Now);
        }
        else if (status != ApprovalStatus.PendingReview)
        {
            contract.ChangeApprovalStatus(status, Now);
        }

        return contract;
    }
}
