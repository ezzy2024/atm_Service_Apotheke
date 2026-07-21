using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Services
{
    public class EmailService
    {
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public EmailService(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message, byte[]? attachmentBytes = null, string? attachmentName = null)
        {
            var host = _configuration["SmtpSettings:Server"] ?? _configuration["SmtpSettings__Server"] ?? Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.ionos.de";
            var portStr = _configuration["SmtpSettings:Port"] ?? _configuration["SmtpSettings__Port"] ?? Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587";
            var user = _configuration["SmtpSettings:Username"] ?? _configuration["SmtpSettings__Username"] ?? Environment.GetEnvironmentVariable("SMTP_USER");
            var pass = _configuration["SmtpSettings:Password"] ?? _configuration["SmtpSettings__Password"] ?? Environment.GetEnvironmentVariable("SMTP_PASS");

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                Console.WriteLine($"[EMAIL MOCK - Credentials missing]\nTo: {toEmail}\nSubject: {subject}\nBody: {message}");
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

            Console.WriteLine($"[EmailService Diagnostic] Attempting to send email to {toEmail} via {host}:{port}...");
            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                Console.WriteLine($"[EmailService Diagnostic] Successfully dispatched email to {toEmail} without exceptions.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService Diagnostic] FATAL ERROR during SendMailAsync: {ex.Message}");
                throw;
            }
        }
    }
}