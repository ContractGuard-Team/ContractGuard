using ContractGuard.Application.Abstractions;
using ContractGuard.Application.Workspaces;
using ContractGuard.Domain.Workspaces;
using ContractGuard.Infrastructure.Email;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace ContractGuard.Api.Endpoints;

// F-ORG-001, F-ORG-002: organizations, members, invitations. The workspace list itself is in GET /me.
internal static class OrganizationEndpoints
{
    // SEC-AUTH-001: opening an invitation link works without a session; accepting needs one.
    public static IEndpointRouteBuilder MapPublicInvitationEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/invitations/{token}", ReadInvitation).WithTags("Organizations");
        return api;
    }

    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder secured)
    {
        var organizations = secured.MapGroup("/organizations").WithTags("Organizations");
        organizations.MapPost("/", Create);
        organizations.MapGet("/{organizationId:guid}", Get);
        organizations.MapPut("/{organizationId:guid}", Rename);
        organizations.MapPost("/{organizationId:guid}/invitations", Invite);
        organizations.MapDelete("/{organizationId:guid}/invitations/{invitationId:guid}", CancelInvitation);
        organizations.MapPut("/{organizationId:guid}/members/{userId:guid}/role", ChangeRole);
        organizations.MapDelete("/{organizationId:guid}/members/{userId:guid}", RemoveMember);

        secured.MapPost("/invitations/{token}/accept", Accept).WithTags("Organizations");
        return secured;
    }

    private static async Task<Ok<CreatedOrganization>> Create(OrganizationNameRequest request, OrganizationService organizations, CancellationToken cancellationToken) =>
        TypedResults.Ok(new CreatedOrganization(await organizations.CreateAsync(request.Name, cancellationToken)));

    private static async Task<Ok<OrganizationDetail>> Get(Guid organizationId, OrganizationService organizations, CancellationToken cancellationToken) =>
        TypedResults.Ok(await organizations.GetAsync(organizationId, cancellationToken));

    private static async Task<NoContent> Rename(Guid organizationId, OrganizationNameRequest request, OrganizationService organizations, CancellationToken cancellationToken)
    {
        await organizations.RenameAsync(organizationId, request.Name, cancellationToken);
        return TypedResults.NoContent();
    }

    // FR-ORG-003: the link goes by email only; the API never returns the token.
    private static async Task<NoContent> Invite(
        Guid organizationId,
        InviteRequest request,
        OrganizationService organizations,
        IEmailSender sender,
        IOptions<EmailOptions> options,
        CancellationToken cancellationToken)
    {
        var invitation = await organizations.InviteAsync(organizationId, request.Email, cancellationToken);
        var link = $"{options.Value.FrontendBaseUrl.TrimEnd('/')}/invitations/{invitation.Token}";
        await sender.SendAsync(
            invitation.Email,
            $"Lời mời tham gia {invitation.OrganizationName} trên ContractGuard",
            $"{invitation.InvitedBy} mời bạn tham gia tổ chức \"{invitation.OrganizationName}\" trên ContractGuard.\n\n"
                + $"Mở liên kết sau để tham gia. Liên kết có hiệu lực trong 7 ngày:\n\n{link}\n\n"
                + "Bạn cần đăng nhập bằng đúng địa chỉ email nhận thư này. Nếu bạn không mong đợi lời mời, hãy bỏ qua email.",
            cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> CancelInvitation(Guid organizationId, Guid invitationId, OrganizationService organizations, CancellationToken cancellationToken)
    {
        await organizations.CancelInvitationAsync(organizationId, invitationId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<InvitationView>> ReadInvitation(string token, OrganizationService organizations, CancellationToken cancellationToken) =>
        TypedResults.Ok(await organizations.ReadInvitationAsync(token, cancellationToken));

    private static async Task<Ok<CreatedOrganization>> Accept(string token, OrganizationService organizations, CancellationToken cancellationToken) =>
        TypedResults.Ok(new CreatedOrganization(await organizations.AcceptAsync(token, cancellationToken)));

    private static async Task<NoContent> ChangeRole(Guid organizationId, Guid userId, RoleRequest request, OrganizationService organizations, CancellationToken cancellationToken)
    {
        await organizations.ChangeRoleAsync(organizationId, userId, request.Role, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> RemoveMember(Guid organizationId, Guid userId, OrganizationService organizations, CancellationToken cancellationToken)
    {
        await organizations.RemoveMemberAsync(organizationId, userId, cancellationToken);
        return TypedResults.NoContent();
    }
}

internal sealed record OrganizationNameRequest(string Name);

internal sealed record CreatedOrganization(Guid OrganizationId);

internal sealed record InviteRequest(string Email);

internal sealed record RoleRequest(OrgRole Role);
