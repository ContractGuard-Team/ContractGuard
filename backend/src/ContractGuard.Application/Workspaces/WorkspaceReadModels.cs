using ContractGuard.Domain.Workspaces;

namespace ContractGuard.Application.Workspaces;

// What OrganizationService returns, kept together like ContractReadModels.
public sealed record OrganizationDetail(Guid OrganizationId, string OrganizationName, OrgRole MyRole, List<MemberItem> Members, List<InvitationItem> Invitations);

public sealed record MemberItem(Guid UserId, string DisplayName, string Email, OrgRole Role, DateTimeOffset JoinedAt);

public sealed record InvitationItem(Guid InvitationId, string Email, DateTimeOffset SentAt, DateTimeOffset ExpiresAt);

public sealed record InvitationView(string OrganizationName, string InvitedBy, string Email, DateTimeOffset ExpiresAt);

public sealed record CreatedInvitation(Guid InvitationId, string Email, string OrganizationName, string InvitedBy, string Token);
