using System.Net.Mail;
using System.Net;
using System.Net.Mime; // ضروري لدمج الصور
using System.Text;
using IPTS.Resources;

namespace IPTS.Services
{
    public class EmailService(IConfiguration configuration, LocService locService)
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly LocService _locService = locService;

      public async Task SendEmail(string to, string subject, string bodyContent, string? FromContactSenderEmail = null)
{
    bool isContactInquiry = to == _configuration["EmailSettings:Accounts:main:Email"];
  
    var fromEmail = _configuration["EmailSettings:Accounts:no-reply:Email"];
    var appPassword = _configuration["EmailSettings:Accounts:no-reply:Password"];
    
    var smtpServer = _configuration["EmailSettings:SmtpServer"];
    var port = int.Parse(_configuration["EmailSettings:Port"] ?? "587"); 

    
    if (isContactInquiry)
    {
        fromEmail = _configuration["EmailSettings:Accounts:main:Email"];
        appPassword = _configuration["EmailSettings:Accounts:main:Password"];
    }
            

            using var client = new SmtpClient(smtpServer, port);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(fromEmail, appPassword);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "Physiotech"), // اسم المستوصف للعرض
                Subject = subject,
                BodyEncoding = Encoding.UTF8, // لضمان ظهور العربي بشكل صحيح
                IsBodyHtml = true,
            };
            mailMessage.To.Add(to);

            if (!string.IsNullOrEmpty(FromContactSenderEmail))
{
    mailMessage.ReplyToList.Add(new MailAddress(FromContactSenderEmail));
}

            // --- السحر هنا: دمج الصورة كـ Mime Mails ---
            // 1. تعريف مسار الشعار الحقيقي في مشروعك (تأكد أن هذا المسار صح!)
            string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png"); // ضع شعارك في هذا المجلد!
            LinkedResource inlineLogo = new LinkedResource(logoPath, MediaTypeNames.Image.Png);
            inlineLogo.ContentId = "ClinicLogo"; // هذا هو المعرف الذي سنستخدمه في HTML

           // بعد


if (isContactInquiry)
{
    mailMessage.Body = bodyContent;
}
else
{
    AlternateView avHtml = AlternateView.CreateAlternateViewFromString(GetHtmlTemplate(bodyContent, inlineLogo.ContentId), null, MediaTypeNames.Text.Html);
    avHtml.LinkedResources.Add(inlineLogo);
    mailMessage.AlternateViews.Add(avHtml);
}

            await client.SendMailAsync(mailMessage);
        }

        // في دالة GetHtmlTemplate
private string GetHtmlTemplate(string content, string contentId)
{
    var primaryColor = "#27ae60"; // الأخضر الجذاب اللي اخترناه
    var websiteLabel = _locService.GetSystem("Label_Website");
    var addressLabel = _locService.GetSystem("Label_Address");
    var emailLabel = _locService.GetSystem("Label_Email");
    var phoneLabel = _locService.GetSystem("Label_Phone");
    var copyrightText = string.Format(_locService.GetSystem("Email_Footer_Copyright"), DateTime.UtcNow.Year);

    return $@"
    <div dir='ltr' style='direction: ltr; font-family: Arial, sans-serif; background-color: #f7f7f7; padding: 30px;'>
        <div style='max-width: 600px; margin: auto; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.1); overflow: hidden; border-top: 5px solid {primaryColor};'>
            
            <div style='text-align: center; padding: 25px 20px;'>
                <img src='cid:{contentId}' alt='Physiotech Logo' width='90' />
                <h2 style='color: {primaryColor}; margin-top: 15px; margin-bottom: 0;'>Physiotech</h2>
            </div>

            <div style='padding: 30px 40px; text-align: left; direction: ltr; color: #555; line-height: 1.8;'>
                <div style='background-color: #f9f9f9; padding: 20px; border-radius: 8px; direction: ltr; unicode-bidi: plaintext;'>
                    {content}
                </div>
            </div>

            <div style='padding: 28px 32px 20px; border-top: 1px solid #eee; background-color: #fafafa;'>
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='width: 100%; border-collapse: collapse;'>
                    <tr>
                        <td width='50%' valign='top' align='left' style='padding: 0 12px 0 0; font-size: 13px; line-height: 1.5;'>
                            <div style='margin-bottom: 14px;'>
                                <div style='font-size: 11px; color: #a8a8a8; text-transform: uppercase; letter-spacing: 0.4px; margin-bottom: 3px;'>{websiteLabel}</div>
                                <a href='https://physiotech-ehrenfeld.de/' style='color: {primaryColor}; text-decoration: none; font-weight: 600;'>physiotech-ehrenfeld.de</a>
                            </div>
                            <div>
                                <div style='font-size: 11px; color: #a8a8a8; text-transform: uppercase; letter-spacing: 0.4px; margin-bottom: 3px;'>{addressLabel}</div>
                                <span style='color: #555;'>Venloer Straße 305,<br />50823 Köln-Ehrenfeld</span>
                            </div>
                        </td>
                        <td width='50%' valign='top' align='right' style='padding: 0 0 0 12px; font-size: 13px; line-height: 1.5;'>
                            <div style='margin-bottom: 14px;'>
                                <div style='font-size: 11px; color: #a8a8a8; text-transform: uppercase; letter-spacing: 0.4px; margin-bottom: 3px;'>{emailLabel}</div>
                                <a href='mailto:dr.kurtoglu@physiotech-ehrenfeld.de' style='color: {primaryColor}; text-decoration: none; font-weight: 600;'>dr.kurtoglu@physiotech-ehrenfeld.de</a>
                            </div>
                            <div>
                                <div style='font-size: 11px; color: #a8a8a8; text-transform: uppercase; letter-spacing: 0.4px; margin-bottom: 3px;'>{phoneLabel}</div>
                                <a href='tel:+491728758302' style='color: #555; text-decoration: none;'>0172 8758302</a>
                            </div>
                        </td>
                    </tr>
                </table>
                <div style='margin-top: 22px; padding-top: 16px; border-top: 1px solid #eee; text-align: center; font-size: 11px; color: #999; line-height: 1.5;'>
                    {copyrightText}
                </div>
            </div>
        </div>
    </div>";
}
    }
}
