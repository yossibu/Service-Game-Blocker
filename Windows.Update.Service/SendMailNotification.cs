using System;
using System.Net.Mail;

namespace Windows.Update.Service
{
    internal class SendMailNotification
    {
        internal static void SendMail(string subject,string body)
        {
            try
            {
                var sendMail = Common.AppSettings.Settings["SendMail"].Value.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase);
                if (!sendMail)
                    return;

                var mail = new MailMessage();
                var smtpServer = new SmtpClient("smtp.gmail.com");
                var from = Common.AppSettings.Settings["Username"].Value;
                mail.From = new MailAddress(from);
                var emailAddress = Common.AppSettings.Settings["SendTo"].Value.Split(';');
                foreach (var addr in emailAddress)
                {
                    mail.To.Add(addr);
                }                
                mail.Subject = subject;
                mail.Body = body;

                //System.Net.Mail.Attachment attachment;
                //attachment = new System.Net.Mail.Attachment("c:/textfile.txt");
                //mail.Attachments.Add(attachment);

                smtpServer.Port = 587;
                var pwd = Common.ConvertFromBase64(Common.AppSettings.Settings["Password"].Value);
                smtpServer.Credentials = new System.Net.NetworkCredential(from, pwd);
                smtpServer.EnableSsl = true;

                smtpServer.Send(mail);
            }
            catch (System.Exception ex)
            {
                Common.WriteErrorToLog(ex);
            }
        }
        
    }
}
