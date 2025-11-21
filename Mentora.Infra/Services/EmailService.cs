using Mentora.Core.Data;
using Mentora.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Mentora.Infra.Services;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Mentora Platform";
    public bool EnableSsl { get; set; } = true;
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string message)
    {
        try
        {
            using var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                EnableSsl = _emailSettings.EnableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent successfully to {Email} with subject {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} with subject {Subject}", to, subject);
            return false;
        }
    }

    public async Task<bool> SendSessionReminderEmailAsync(string to, string userName, Session session)
    {
        var subject = $"Reminder: {session.Type} Session - {session.StartAt:MMM dd, HH:mm}";
        var message = BuildSessionReminderEmail(userName, session);
        return await SendEmailAsync(to, subject, message);
    }

    public async Task<bool> SendSessionConfirmationEmailAsync(string to, string userName, Session session)
    {
        var subject = $"Confirmed: {session.Type} Session - {session.StartAt:MMM dd, HH:mm}";
        var message = BuildSessionConfirmationEmail(userName, session);
        return await SendEmailAsync(to, subject, message);
    }

    public async Task<bool> SendFollowUpEmailAsync(string to, string userName, Session session)
    {
        var subject = $"Follow-up: {session.Type} Session";
        var message = BuildFollowUpEmail(userName, session);
        return await SendEmailAsync(to, subject, message);
    }

    public async Task<bool> SendFeedbackRequestEmailAsync(string to, string userName, Session session)
    {
        var subject = $"Feedback Request: {session.Type} Session";
        var message = BuildFeedbackRequestEmail(userName, session);
        return await SendEmailAsync(to, subject, message);
    }

    private string BuildSessionReminderEmail(string userName, Session session)
    {
        var template = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .session-details {{ background-color: white; padding: 15px; margin: 10px 0; border-left: 4px solid #4CAF50; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
        .btn {{ display: inline-block; padding: 10px 20px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Session Reminder</h1>
        </div>
        <div class='content'>
            <p>Hi {userName},</p>
            <p>This is a friendly reminder about your upcoming session:</p>

            <div class='session-details'>
                <h3>{session.Type} Session</h3>
                <p><strong>Date:</strong> {session.StartAt:dddd, MMMM dd, yyyy}</p>
                <p><strong>Time:</strong> {session.StartAt:HH:mm} - {session.EndAt:HH:mm}</p>
                <p><strong>Price:</strong> ${session.Price}</p>
                {(!string.IsNullOrEmpty(session.Notes) ? $"<p><strong>Notes:</strong> {session.Notes}</p>" : "")}
            </div>

            <p>Please make sure you're prepared and available for the session. If you need to make any changes, please do so as soon as possible.</p>

            <p>Best regards,<br>The Mentora Team</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

        return template;
    }

    private string BuildSessionConfirmationEmail(string userName, Session session)
    {
        var template = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2196F3; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .session-details {{ background-color: white; padding: 15px; margin: 10px 0; border-left: 4px solid #2196F3; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
        .success {{ background-color: #d4edda; color: #155724; padding: 10px; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Session Confirmed</h1>
        </div>
        <div class='content'>
            <div class='success'>
                <strong>✓ Your session has been confirmed!</strong>
            </div>

            <p>Hi {userName},</p>
            <p>Your session has been successfully confirmed. Here are the details:</p>

            <div class='session-details'>
                <h3>{session.Type} Session</h3>
                <p><strong>Date:</strong> {session.StartAt:dddd, MMMM dd, yyyy}</p>
                <p><strong>Time:</strong> {session.StartAt:HH:mm} - {session.EndAt:HH:mm}</p>
                <p><strong>Price:</strong> ${session.Price}</p>
                <p><strong>Status:</strong> {session.Status}</p>
                {(!string.IsNullOrEmpty(session.Notes) ? $"<p><strong>Notes:</strong> {session.Notes}</p>" : "")}
            </div>

            <p>We'll send you reminders closer to the session time. If you need to make any changes, you can do so through your dashboard.</p>

            <p>Best regards,<br>The Mentora Team</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

        return template;
    }

    private string BuildFollowUpEmail(string userName, Session session)
    {
        var template = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FF9800; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .session-details {{ background-color: white; padding: 15px; margin: 10px 0; border-left: 4px solid #FF9800; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
        .feedback-section {{ background-color: #e3f2fd; padding: 15px; border-radius: 5px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Session Follow-up</h1>
        </div>
        <div class='content'>
            <p>Hi {userName},</p>
            <p>We hope your recent session went well! Here's a quick recap:</p>

            <div class='session-details'>
                <h3>{session.Type} Session</h3>
                <p><strong>Date:</strong> {session.StartAt:dddd, MMMM dd, yyyy}</p>
                <p><strong>Time:</strong> {session.StartAt:HH:mm} - {session.EndAt:HH:mm}</p>
                <p><strong>Status:</strong> {session.Status}</p>
            </div>

            <div class='feedback-section'>
                <h3>How did it go?</h3>
                <p>We'd love to hear about your experience. Your feedback helps us improve our platform and provide better services.</p>
                <p>Consider:</p>
                <ul>
                    <li>What went well during the session?</li>
                    <li>What could be improved?</li>
                    <li>Would you recommend this mentor to others?</li>
                </ul>
            </div>

            <p>Thank you for using Mentora. We look forward to supporting your continued growth!</p>

            <p>Best regards,<br>The Mentora Team</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

        return template;
    }

    private string BuildFeedbackRequestEmail(string userName, Session session)
    {
        var template = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #9C27B0; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .session-details {{ background-color: white; padding: 15px; margin: 10px 0; border-left: 4px solid #9C27B0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; }}
        .rating-section {{ background-color: #f3e5f5; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .btn {{ display: inline-block; padding: 10px 20px; background-color: #9C27B0; color: white; text-decoration: none; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Share Your Feedback</h1>
        </div>
        <div class='content'>
            <p>Hi {userName},</p>
            <p>Your recent session has just concluded. We'd appreciate it if you could take a moment to share your feedback:</p>

            <div class='session-details'>
                <h3>{session.Type} Session</h3>
                <p><strong>Date:</strong> {session.StartAt:dddd, MMMM dd, yyyy}</p>
                <p><strong>Time:</strong> {session.StartAt:HH:mm} - {session.EndAt:HH:mm}</p>
            </div>

            <div class='rating-section'>
                <h3>Rate Your Experience</h3>
                <p>Your feedback helps us maintain quality and improve our services.</p>
                <p>Please consider rating:</p>
                <ul>
                    <li>Session content quality</li>
                    <li>Mentor's expertise</li>
                    <li>Overall experience</li>
                    <li>Value for money</li>
                </ul>
            </div>

            <p>You can share your feedback through your Mentora dashboard or by replying to this email with your thoughts.</p>

            <p>Thank you for helping us improve!<br>The Mentora Team</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

        return template;
    }
}
