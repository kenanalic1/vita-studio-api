using System.Net;
using System.Net.Mail;

namespace VitaStudio.Api.Services;

public interface IEmailService
{
    Task PosaljiAsync(string primaocEmail, string primaocIme, string naslov, string poruka);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _cfg;
    public EmailService(IConfiguration cfg) => _cfg = cfg;

    public async Task PosaljiAsync(string primaocEmail, string primaocIme, string naslov, string poruka)
    {
        var host     = _cfg["Email:SmtpHost"] ?? "smtp.gmail.com";
        var port     = int.Parse(_cfg["Email:SmtpPort"] ?? "587");
        var sender   = _cfg["Email:SenderEmail"] ?? "";
        var name     = _cfg["Email:SenderName"] ?? "Vita Studio";
        var password = _cfg["Email:Password"] ?? "";

        if (string.IsNullOrWhiteSpace(password)) return;

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(sender, password),
            EnableSsl   = true,
        };

        var mail = new MailMessage
        {
            From       = new MailAddress(sender, name),
            Subject    = naslov,
            Body       = $"Zdravo {primaocIme},\n\n{poruka}\n\nPozdrav,\nVita Studio tim",
            IsBodyHtml = false,
        };
        mail.To.Add(new MailAddress(primaocEmail, primaocIme));

        await client.SendMailAsync(mail);
    }
}
