using System.Net;
using System.Net.Mail;
using StockAlarm.Models;

namespace StockAlarm.Services;

public sealed class EmailService
{
    private readonly AppConfig _cfg;

    public EmailService(AppConfig cfg)
    {
        _cfg = cfg;
    }

    public void Send(string subject, string body)
    {
        using var message = new MailMessage();
        message.From = new MailAddress(_cfg.EmailFrom);
        message.To.Add(_cfg.EmailTo);
        message.Subject = subject;
        message.Body = body;

        using var client = new SmtpClient(_cfg.SmtpHost, _cfg.SmtpPort);
        client.EnableSsl = _cfg.SmtpEnableSsl;
        client.Credentials = new NetworkCredential(_cfg.SmtpUser, _cfg.SmtpPassword);

        client.Send(message);
    }
}
