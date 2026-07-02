using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Services
{
    public class EmailService
    {
        public async Task SendEmailAsync(string toEmail, string subject, string message, byte[]? attachmentBytes = null, string? attachmentName = null)
        {
            var host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.ionos.de";
            var portStr = Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587";
            var user = Environment.GetEnvironmentVariable("SMTP_USER");
            var pass = Environment.GetEnvironmentVariable("SMTP_PASS");

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                Console.WriteLine("[EMAIL ERROR] SMTP Credentials missing.");
                return;
            }

            int.TryParse(portStr, out int port);

            using var smtpClient = new SmtpClient(host)
            {
                Port = port,
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = true,
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(user, "ServiceApotheke"),
                Subject = subject,
                Body = message,
                IsBodyHtml = false,
            };

            mailMessage.To.Add(toEmail);

            if (attachmentBytes != null && !string.IsNullOrEmpty(attachmentName))
            {
                var stream = new System.IO.MemoryStream(attachmentBytes);
                var attachment = new Attachment(stream, attachmentName, "application/pdf");
                mailMessage.Attachments.Add(attachment);
            }

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}