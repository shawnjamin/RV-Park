using Microsoft.AspNetCore.Mvc;
using RVPark.Services;

namespace RVPark.Controllers;

/// <summary>
/// Controller for mail operations. Primarily uses the MailService class
/// </summary>
public class MailController : Controller
{
    private MailService _mailService = null;

    [HttpPost]
    public bool SendMail(MailData mailData)
    {
        return _mailService.SendMail(mailData);
    }
}