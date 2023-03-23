using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;

namespace WebDev.Controllers
{
    public class MailController : Controller
    {
        public static string EncodeString(string str)
        {
            return System.Web.HttpUtility.HtmlEncode(str);
        }

        public static void SendEmail(string email, string subject, string body)
        {
            MailAddress from = new MailAddress("webdev@webdev.com");
            MailAddress to = new MailAddress(email);

            MailMessage mailMessage = new MailMessage(from, to);
            mailMessage.Subject = EncodeString(subject);
            mailMessage.Body = EncodeString(body);

            SmtpClient smtp = new SmtpClient();
            smtp.Host = "sandbox.smtp.mailtrap.io";
            smtp.Port = 2525;
            smtp.Credentials = new NetworkCredential("7f5acecb4c8ab0", "761a0e2ebeff63");
            smtp.EnableSsl = true;
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

            try
            {
                smtp.Send(mailMessage);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
