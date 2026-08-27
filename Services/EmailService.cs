using System.Net.Mail;
using System.Net;
using System.Net.Mime; // ضروري لدمج الصور
using System.Text;
using System.Text.RegularExpressions;
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

        private static string StyleContentLinks(string content)
        {
            const string buttonStyle =
                "display:inline-block;background-color:#16A34A;color:#ffffff;text-decoration:none;padding:10px 22px;border-radius:8px;font-size:14px;font-weight:bold;line-height:1.2;";

            return Regex.Replace(
                content,
                @"<a\s+(?![^>]*style=)",
                $"<a style='{buttonStyle}' ",
                RegexOptions.IgnoreCase);
        }

        private string GetHtmlTemplate(string content, string contentId)
        {
            const string primaryColor = "#16A34A";
            var websiteLabel = _locService.GetSystem("Label_Website");
            var addressLabel = _locService.GetSystem("Label_Address");
            var emailLabel = _locService.GetSystem("Label_Email");
            var phoneLabel = _locService.GetSystem("Label_Phone");
            var copyrightText = string.Format(_locService.GetSystem("Email_Footer_Copyright"), DateTime.UtcNow.Year);
            var styledContent = StyleContentLinks(content);

            return $@"
<table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='width:100%;border-collapse:collapse;background-color:#f1f5f9;'>
  <tr>
    <td align='center' style='padding:16px 12px;'>
      <table role='presentation' width='480' cellpadding='0' cellspacing='0' border='0' style='width:100%;max-width:480px;border-collapse:collapse;background-color:#ffffff;border-radius:10px;overflow:hidden;border-top:4px solid {primaryColor};font-family:Arial,Helvetica,sans-serif;'>
        <tr>
          <td align='center' style='padding:20px 24px 10px;'>
            <img src='cid:{contentId}' alt='Physiotech' width='180' style='display:block;width:180px;max-width:70%;height:auto;border:0;' />
          </td>
        </tr>
        <tr>
          <td style='padding:8px 20px 20px;font-size:14px;line-height:1.6;color:#334155;text-align:left;direction:ltr;'>
            {styledContent}
          </td>
        </tr>
        <tr>
          <td style='padding:14px 20px 18px;border-top:1px solid #e5e7eb;background-color:#f8fafc;font-size:12px;line-height:1.45;color:#64748b;'>
            <div style='margin-bottom:8px;'>
              <span style='display:block;font-size:10px;color:#94a3b8;text-transform:uppercase;letter-spacing:0.4px;'>{websiteLabel}</span>
              <a href='https://physiotech-ehrenfeld.de/' style='color:{primaryColor};text-decoration:none;font-weight:600;'>physiotech-ehrenfeld.de</a>
            </div>
            <div style='margin-bottom:8px;'>
              <span style='display:block;font-size:10px;color:#94a3b8;text-transform:uppercase;letter-spacing:0.4px;'>{emailLabel}</span>
              <a href='mailto:dr.kurtoglu@physiotech-ehrenfeld.de' style='color:{primaryColor};text-decoration:none;'>dr.kurtoglu@physiotech-ehrenfeld.de</a>
            </div>
            <div style='margin-bottom:8px;'>
              <span style='display:block;font-size:10px;color:#94a3b8;text-transform:uppercase;letter-spacing:0.4px;'>{phoneLabel}</span>
              <a href='tel:+491728758302' style='color:#334155;text-decoration:none;'>0172 8758302</a>
            </div>
            <div>
              <span style='display:block;font-size:10px;color:#94a3b8;text-transform:uppercase;letter-spacing:0.4px;'>{addressLabel}</span>
              <span style='color:#334155;'>Venloer Straße 305, 50823 Köln-Ehrenfeld</span>
            </div>
            <div style='margin-top:12px;padding-top:10px;border-top:1px solid #e5e7eb;text-align:center;font-size:11px;color:#94a3b8;'>
              {copyrightText}
            </div>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>";
        }
    }
}
