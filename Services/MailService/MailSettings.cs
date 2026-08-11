namespace RVPark.Services;

/**
 * Holds the mail settings to be used with MailKit, for sending emails.
 * The mail settings themselves are contained in appsettings.json, but this makes them accessible programmatically (i.e. at runtime)
 */
public class MailSettings
{
    public string EmailId { get; set; }
    public string Name { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public bool UseSSL { get; set; }
}