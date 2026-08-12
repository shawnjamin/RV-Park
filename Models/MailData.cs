namespace RVPark.Services;

/// <summary>
/// Contains data necessary to send an email, such as the body and subject
/// </summary>
public class MailData
{
    public string EmailToId { get; set; }
    public string EmailToName { get; set; }
    public string EmailSubject { get; set; }
    public string EmailBody { get; set; }
}