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
        var enabled = GetBoolSetting("Email:Enabled", "CHINHNHA_EMAIL_ENABLED", false);
        var host = GetSetting("Email:Smtp:Host", "CHINHNHA_SMTP_HOST");
        var fromAddress = GetSetting("Email:From:Address", "CHINHNHA_EMAIL_FROM_ADDRESS");
        var fromName = GetSetting("Email:From:Name", "CHINHNHA_EMAIL_FROM_NAME") ?? "ChinhNha";

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
                Port = GetIntSetting("Email:Smtp:Port", "CHINHNHA_SMTP_PORT", 587),
                EnableSsl = GetBoolSetting("Email:Smtp:EnableSsl", "CHINHNHA_SMTP_ENABLE_SSL", true),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            var username = GetSetting("Email:Smtp:Username", "CHINHNHA_SMTP_USERNAME");
            var password = GetSetting("Email:Smtp:Password", "CHINHNHA_SMTP_PASSWORD");
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Email is enabled but SMTP credentials are missing.");
                return false;
            }

            client.Credentials = new NetworkCredential(username, password);

            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", toEmail);
            return false;
        }
    }

    private string? GetSetting(string key, string envKey)
    {
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }

        return _configuration[key];
    }

    private int GetIntSetting(string key, string envKey, int defaultValue)
    {
        var value = GetSetting(key, envKey);
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private bool GetBoolSetting(string key, string envKey, bool defaultValue)
    {
        var value = GetSetting(key, envKey);
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}