using WasteCollection_RecyclingPlatform.Services.Service;

namespace WasteCollection_RecyclingPlatform.API.Auth;

public sealed class SmtpEmailOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;

    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "EcoSort";


    public string LogoPath { get; set; } = "";
}

public class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly SmtpEmailOptions _opt;

    public SmtpEmailSender(ILogger<SmtpEmailSender> logger, Microsoft.Extensions.Options.IOptions<SmtpEmailOptions> opt)
    {
        _logger = logger;
        _opt = opt.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.Host) ||
            string.IsNullOrWhiteSpace(_opt.Username) ||
            string.IsNullOrWhiteSpace(_opt.Password))
        {
            throw new InvalidOperationException("SMTP is not configured. Please set Smtp:Host, Smtp:Username, Smtp:Password.");
        }

        var normalizedPassword = new string(_opt.Password.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (!string.Equals(normalizedPassword, _opt.Password, StringComparison.Ordinal))
        {
            _logger.LogInformation("Normalized SMTP password by removing whitespace.");
        }
        if (normalizedPassword.Length is > 0 and < 16)
        {
            _logger.LogWarning("SMTP password length looks short ({len}). If using Gmail App Password, it should be 16 characters.", normalizedPassword.Length);
        }

        var fromEmail = string.IsNullOrWhiteSpace(_opt.FromEmail) ? _opt.Username : _opt.FromEmail;

        using var message = new System.Net.Mail.MailMessage();
        message.From = new System.Net.Mail.MailAddress(fromEmail, _opt.FromName);
        message.To.Add(new System.Net.Mail.MailAddress(toEmail));
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true;

        TryAttachInlineLogo(message);

        using var client = new System.Net.Mail.SmtpClient(_opt.Host, _opt.Port)
        {
            EnableSsl = _opt.EnableSsl,
            DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new System.Net.NetworkCredential(_opt.Username, normalizedPassword),
        };
        client.Timeout = 10_000;

        _logger.LogInformation("SMTP EMAIL to={to} subject={subject} host={host}:{port}", toEmail, subject, _opt.Host, _opt.Port);
        var sendTask = client.SendMailAsync(message);
        var done = await Task.WhenAny(sendTask, Task.Delay(10_000, ct));
        if (done != sendTask)
        {
            throw new TimeoutException("SMTP send timed out.");
        }
        await sendTask;
    }

    private void TryAttachInlineLogo(System.Net.Mail.MailMessage message)
    {
        try
        {
            var path = _opt.LogoPath?.Trim();
            if (string.IsNullOrWhiteSpace(path)) return;
            if (!System.IO.File.Exists(path))
            {
                _logger.LogWarning("SMTP LogoPath not found: {path}", path);
                return;
            }

            // Note: Gmail supports inline attachments referenced by Content-Id.
            var attachment = new System.Net.Mail.Attachment(path);
            attachment.ContentId = "ecosort-logo";
            attachment.ContentType.MediaType = "image/png";
            if (attachment.ContentDisposition is not null)
            {
                attachment.ContentDisposition.Inline = true;
                attachment.ContentDisposition.DispositionType = System.Net.Mime.DispositionTypeNames.Inline;
            }
            attachment.Name = System.IO.Path.GetFileName(path);

            message.Attachments.Add(attachment);
            _logger.LogInformation("Attached inline logo from {path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to attach inline logo.");
        }
    }
}

public class DevEmailSender : IEmailSender
{
    private readonly ILogger<DevEmailSender> _logger;

    public DevEmailSender(ILogger<DevEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogWarning("DEV EMAIL to={to} subject={subject}\n{body}", toEmail, subject, htmlBody);
        return Task.CompletedTask;
    }
}

public class SmartEmailSender : IEmailSender
{
    private readonly ILogger<SmartEmailSender> _logger;
    private readonly SmtpEmailOptions _opt;
    private readonly SmtpEmailSender _smtp;
    private readonly DevEmailSender _dev;

    public SmartEmailSender(
        ILogger<SmartEmailSender> logger,
        Microsoft.Extensions.Options.IOptions<SmtpEmailOptions> opt,
        SmtpEmailSender smtp,
        DevEmailSender dev)
    {
        _logger = logger;
        _opt = opt.Value;
        _smtp = smtp;
        _dev = dev;
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var hasSmtp =
            !string.IsNullOrWhiteSpace(_opt.Host) &&
            !string.IsNullOrWhiteSpace(_opt.Username) &&
            !string.IsNullOrWhiteSpace(_opt.Password);

        if (hasSmtp)
        {
            return SendWithFallbackAsync(toEmail, subject, htmlBody, ct);
        }

        _logger.LogWarning("SMTP not configured. Falling back to DEV email logger.");
        return _dev.SendAsync(toEmail, subject, htmlBody, ct);
    }

    private async Task SendWithFallbackAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        try
        {
            await _smtp.SendAsync(toEmail, subject, htmlBody, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed. Falling back to DEV email logger.");
            await _dev.SendAsync(toEmail, subject, htmlBody, ct);
        }
    }
}

