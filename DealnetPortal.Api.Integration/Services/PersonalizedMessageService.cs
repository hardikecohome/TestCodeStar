using DealnetPortal.Api.Models.Notification;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Integration.Interfaces;

namespace DealnetPortal.Api.Integration.Services
{
    public class PersonalizedMessageService : IPersonalizedMessageService
    {
        private readonly string _endPoint = ConfigurationManager.AppSettings["SMSENDPOINT"];

        private readonly string _apiKey = ConfigurationManager.AppSettings["SMSAPIKEY"];

        public async Task<HttpResponseMessage> SendMessage(string phonenumber, string messagebody)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_endPoint);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("IMWS1", "key = " + _apiKey);
                client.DefaultRequestHeaders.Add("X-Impact-Fail-Fast", "false");
                client.DefaultRequestHeaders.Add("X-Impact-Response-Detail", "standard");
                RequestMessage request = new RequestMessage();
                request.messages.Add(new Message()
                {
                    content = new Content() { body = "MyhomeWallet by Ecohome Financial: " + messagebody + " StdMsg&DataRtsAply Txt STOP to stop INFO for info" },
                    sendDate = DateTime.Now,
                    validUntil = DateTime.Now.AddMinutes(5),
                    to = new To() { subscriber = new Subscriber() { phone = phonenumber } },
                    tracking = new Tracking() { code = "try123" }
                });

                return await client.PostAsJsonAsync("media/ws/rest/mbox/v1/reference/"+ System.Configuration.ConfigurationManager.AppSettings["SubscriptionRef"] +"/message", request);

            }
        }
    }
}
