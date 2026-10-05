using System.Net;
using System.Net.Mail;
using BookStore.Core.Interfaces;
using BookStore.Core.Options;
using Microsoft.Extensions.Options;

namespace BookStore.Data.Services;

// Real sender over SMTP. For production you would normally use MailKit or a provider API; the
// IEmailSender interface means only this class changes.
public class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        var o = options.Value;

        using var client = new SmtpClient(o.Host, o.Port)
        {
            EnableSsl = o.EnableSsl,
            Credentials = new NetworkCredential(o.Username, o.Password)
        };
        using var message = new MailMessage(new MailAddress(o.FromAddress, o.FromName), new MailAddress(to))
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        await client.SendMailAsync(message, ct);
    }
}
