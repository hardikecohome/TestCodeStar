using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Utilities.Configuration;
using Microsoft.AspNet.Identity;

namespace DealnetPortal.Utilities.Messaging
{
    public class EmailService: IEmailService, IIdentityMessageService
    {
        private readonly IAppConfiguration _сonfiguration;
        public EmailService(IAppConfiguration appConfiguration)
        {
            _сonfiguration = appConfiguration;
        }

        public async Task SendAsync(IList<string> recipients, string from, string subject, string body)
        {
            var message = new IdentityMessage()
            {
                Body = body,
                Subject = subject,
                Destination = recipients?.FirstOrDefault() ?? string.Empty
            };
            try
            {
                await SendAsync(message);
            }
            catch (Exception ex)
            {                
                throw ex;
            }            
        }

        public async Task SendAsync(MailMessage message)
        {
            var smtpClient = InitSmtpClient();
            using (smtpClient)
            {
                smtpClient.Credentials = InitCredentials();
                try
                {
                    await smtpClient.SendMailAsync(message);
                }
                catch (Exception ex)
                {                    
                    throw ex;
                }                
            }
        }

        #region IIdentityMessageService implementation
        public async Task SendAsync(IdentityMessage message)
        {
            var text = message.Body;
            var html = message.Body;
            var msg = new MailMessage
            {
                Subject = message.Subject,
                From = new MailAddress(_сonfiguration.GetSetting(WebConfigKeys.ES_FROMEMAIL_CONFIG_KEY))
            };
            msg.To.Add(new MailAddress(message.Destination));
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, MediaTypeNames.Text.Plain));
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));

            await SendAsync(msg);
        }
        #endregion

        private SmtpClient InitSmtpClient()
        {
            return new SmtpClient(_сonfiguration.GetSetting(WebConfigKeys.ES_SMTPHOST_CONFIG_KEY), Convert.ToInt32(_сonfiguration.GetSetting(WebConfigKeys.ES_SMTPPORT_CONFIG_KEY)));
        }

        private NetworkCredential InitCredentials()
        {
            return new NetworkCredential(_сonfiguration.GetSetting(WebConfigKeys.ES_SMTPUSER_CONFIG_KEY), _сonfiguration.GetSetting(WebConfigKeys.ES_SMTPPASSWORD_CONFIG_KEY));
        }
    }
}