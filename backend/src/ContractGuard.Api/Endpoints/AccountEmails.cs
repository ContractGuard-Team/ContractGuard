using ContractGuard.Application.Abstractions;
using ContractGuard.Infrastructure.Email;
using ContractGuard.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ContractGuard.Api.Endpoints;

// The two emails of the account flows. Links open a page of the SPA, which calls the API.
// They carry the user id rather than the email, so no address ends up in URLs or access logs.
internal sealed class AccountEmails(UserManager<AppUser> users, IEmailSender sender, IOptions<EmailOptions> options)
{
    public async Task SendVerificationAsync(AppUser user, CancellationToken cancellationToken)
    {
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var link = BuildLink("verify-email", user, token);
        await sender.SendAsync(
            user.Email!,
            "Xác thực email ContractGuard",
            $"Mở liên kết sau để xác thực email của bạn. Liên kết có hiệu lực trong 10 phút:\n\n{link}\n\nNếu bạn không đăng ký ContractGuard, hãy bỏ qua email này.",
            cancellationToken);
    }

    public async Task SendPasswordResetAsync(AppUser user, CancellationToken cancellationToken)
    {
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var link = BuildLink("reset-password", user, token);
        await sender.SendAsync(
            user.Email!,
            "Đặt lại mật khẩu ContractGuard",
            $"Mở liên kết sau để đặt mật khẩu mới. Liên kết có hiệu lực trong 10 phút:\n\n{link}\n\nNếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.",
            cancellationToken);
    }

    private string BuildLink(string page, AppUser user, string token) =>
        $"{options.Value.FrontendBaseUrl.TrimEnd('/')}/{page}?userId={user.Id}&token={Uri.EscapeDataString(token)}";
}
