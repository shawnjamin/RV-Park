using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace RVPark.Services;

/**
 * <summary>Used to send emails.</summary>
 * <para>EmailSent contains a boolean value pertaining to whether the email was successfully sent or not. <br/><br/>
 * ErrorMessage contains a user-friendly error message that is set if the email fails to send. <br/><br/>
 * DebugMessage contains a developer-friendly error message that is set and printed to the console if the email fails to send.</para>
 */
public class MailService
{
    // Class members
    private MailSettings _mailSettings = null;
    private MailData _mailData = null;
    public bool EmailSent = false;
    public string ErrorMessage = "none";
    public string DebugMessage = "none";

    // Constructor(s)
    
    /// <summary>
    /// Creates a new MailService instance
    /// </summary>
    /// <param name="options">An IOptions object contianing MailSettings options</param>
    public MailService(IOptions<MailSettings> options)
    {
        _mailSettings = options.Value;
    }

    // Functions
    
    /// <summary>
    /// Sends an email
    /// </summary>
    /// <param name="mailData">An instance of a MailData object containing email data</param>
    /// <returns>True if the email was sent. False if the sending failed for any reason</returns>
    public bool SendMail(MailData mailData)
    {
        try
        {
            // Set up message and addresses
            MimeMessage message = new MimeMessage();
            MailboxAddress address = new MailboxAddress(_mailSettings.Name, _mailSettings.EmailId);
            message.From.Add(address);
            MailboxAddress to = new MailboxAddress(mailData.EmailToName, mailData.EmailToId);
            message.To.Add(to);
            // Set up subject and body
            message.Subject = mailData.EmailSubject;
            BodyBuilder bodyBuilder = new BodyBuilder();
            bodyBuilder.TextBody = mailData.EmailBody;
            message.Body = bodyBuilder.ToMessageBody();
            // Set up SMTP Client
            SmtpClient client = new SmtpClient();
            client.Connect(_mailSettings.Host, _mailSettings.Port, _mailSettings.UseSSL);
            client.Authenticate(_mailSettings.EmailId, _mailSettings.Password);
            // Send email
            client.Send(message);
            client.Disconnect(true);
            client.Dispose();
            // Success
            EmailSent = true;
            ErrorMessage = "none";
            DebugMessage = "none";
            return true;
        }
        catch (Exception e)
        {
            // Fail
            EmailSent = false;
            ErrorMessage = "Failed to send email";
            DebugMessage = $"Failed to send email\n Exception: {e}\n Message: {e.Data}";
            Console.WriteLine(DebugMessage);
            return false;
        }
    }
}