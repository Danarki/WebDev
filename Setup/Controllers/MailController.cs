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
            MailAddress from = new MailAddress("mailer@cardigo.com");
            MailAddress to = new MailAddress(email);

            MailMessage mailMessage = new MailMessage(from, to);
            mailMessage.Subject = EncodeString(subject);
            mailMessage.Body = "<!doctype html>" +
                               "<html style='height: 100%; width: 100%'>" +
                               "<div style='" +
                               "border:1px solid black; " +
                               "width: 80%; " +
                               "height: 50%; " +
                               "padding: 25px; " +
                               "border-radius: 5px;" +
                               "margin: 10%'>" +
                               "<h3>Dear Cardigo user,</h3>" +
                               "<br/>" + 
                               "<p>" + EncodeString(body) + "</p>" +
                               "<br/>" +
                               "<p>With kind regards,</p>" +
                               "<br/>" +
                               "<p>Team Cardigo</p>" +
                               "</div>" +
                               "</html>";
            mailMessage.IsBodyHtml = true;

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
