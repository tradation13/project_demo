using System.Net.Mail;
using System.Net;
using System.Net.Mime; // ضروري لدمج الصور
using System.Text;

namespace IPTS.Services
{
    public class EmailService(IConfiguration configuration)
    {
        private readonly IConfiguration _configuration = configuration;

        public async Task SendEmail(string to, string subject, string bodyContent)
        {
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var appPassword = _configuration["EmailSettings:AppPassword"];
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var port = int.Parse(_configuration["EmailSettings:Port"]);

            using var client = new SmtpClient(smtpServer, port);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(fromEmail, appPassword);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "Physiotech"), // اسم المستوصف للعرض
                Subject = subject,
                BodyEncoding = Encoding.UTF8, // لضمان ظهور العربي بشكل صحيح
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            // --- السحر هنا: دمج الصورة كـ Mime Mails ---
            // 1. تعريف مسار الشعار الحقيقي في مشروعك (تأكد أن هذا المسار صح!)
            string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png"); // ضع شعارك في هذا المجلد!
            LinkedResource inlineLogo = new LinkedResource(logoPath, MediaTypeNames.Image.Png);
            inlineLogo.ContentId = "ClinicLogo"; // هذا هو المعرف الذي سنستخدمه في HTML

            // 2. دمج المحتوى مع القالب (نمرر المحتوى و الـ ContentId)
            AlternateView avHtml = AlternateView.CreateAlternateViewFromString(GetHtmlTemplate(bodyContent, inlineLogo.ContentId), null, MediaTypeNames.Text.Html);
            avHtml.LinkedResources.Add(inlineLogo);
            mailMessage.AlternateViews.Add(avHtml);

            await client.SendMailAsync(mailMessage);
        }

        // في دالة GetHtmlTemplate
private string GetHtmlTemplate(string content, string contentId)
{
    var primaryColor = "#27ae60"; // الأخضر الجذاب اللي اخترناه

    return $@"
    <div dir='rtl' style='font-family: Arial, sans-serif; background-color: #f7f7f7; padding: 30px;'>
        <div style='max-width: 600px; margin: auto; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.1); overflow: hidden; border-top: 5px solid {primaryColor};'>
            
            <div style='text-align: center; padding: 25px 20px;'>
                <img src='cid:{contentId}' alt='Physiotech Logo' width='90' />
                <h2 style='color: {primaryColor}; margin-top: 15px; margin-bottom: 0;'>Physiotech</h2>
            </div>

            <div style='padding: 30px 40px; text-align: center; color: #555; line-height: 1.8;'>
                <div style='background-color: #f9f9f9; padding: 20px; border-radius: 8px;'>
                    {content}
                </div>
            </div>

            <div style='text-align: center; padding: 25px; border-top: 1px solid #eee; background-color: #ffffff;'>
               
                
                <div style='display: inline-block; text-align: center; font-size: 14px; color: #666;'>
                    <p style='margin: 5px 0;'>
                        <span style='color: {primaryColor}; font-size: 18px;'>📍</span> 
                        Venloer Straße 305, 50823 Köln-Ehrenfeld
                    </p>
                    
                    <p style='margin: 5px 0;'>
                        <span style='color: {primaryColor}; font-size: 18px;'>📞</span> 
                        0172 8758302
                    </p>
                    
                    <p style='margin: 5px 0;'>
                        <span style='color: {primaryColor}; font-size: 18px;'>🌐</span> 
                        <a href='https://physiotech-ehrenfeld.de/' style='color: #666; text-decoration: none;'>physiotech.it.com</a>
                    </p>
                </div>
            </div>
        </div>
    </div>";
}
    }
}