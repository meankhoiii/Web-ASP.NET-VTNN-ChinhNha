using System.Net;
using System.Net.Mail;
using ChinhNha.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChinhNha.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null)
    {
        var enabled = _configuration.GetValue<bool>("Email:Enabled");
        var host = _configuration["Email:Smtp:Host"];
        var fromAddress = _configuration["Email:From:Address"];
        var fromName = _configuration["Email:From:Name"] ?? "ChinhNha";

        if (!enabled)
        {
            _logger.LogInformation("Email disabled. Skipping send to {Recipient} with subject {Subject}", toEmail, subject);
            return false;
        }

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning("Email is enabled but SMTP configuration is incomplete.");
            return false;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);
            if (!string.IsNullOrWhiteSpace(plainTextBody))
            {
                message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain"));
            }

            using var client = new SmtpClient(host)
            {
                Port = _configuration.GetValue<int?>("Email:Smtp:Port") ?? 587,
                EnableSsl = _configuration.GetValue<bool?>("Email:Smtp:EnableSsl") ?? true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            var username = _configuration["Email:Smtp:Username"];
            var password = _configuration["Email:Smtp:Password"];
            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", toEmail);
            return false;
        }
    }
}