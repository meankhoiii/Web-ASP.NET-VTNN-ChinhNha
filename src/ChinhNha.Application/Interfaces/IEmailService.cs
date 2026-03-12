namespace ChinhNha.Application.Interfaces;

public interface IEmailService
{
    Task<bool> SendAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null);
}