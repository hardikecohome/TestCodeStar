using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DealnetPortal.Api.Tests.TrdPartySDKs
{
    [TestClass]
    public class MailGunTest
    {
        //[Ignore]
        [TestMethod]
        public void SendSimpleMessageTest()
        {
            HttpClient client = new HttpClient();
            string baseUri = "https://api.mailgun.net/v3/";
            client.BaseAddress = new Uri(baseUri);
            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("api", "key-7b6273c4442da6aaa9496eb3eed25036");
            var credentials = Encoding.ASCII.GetBytes("api:key-7b6273c4442da6aaa9496eb3eed25036");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));


            var domain = "sandbox36ed7e337cd34757869b6c132e07e7b0.mailgun.org";

            var data = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("domain", domain),
                new KeyValuePair<string, string>("from", "Mailgun Sandbox <postmaster@sandbox36ed7e337cd34757869b6c132e07e7b0.mailgun.org>"),
                new KeyValuePair<string, string>("to", "mkhar@yandex.ru"),
                new KeyValuePair<string, string>("to", "mykharlamov@gmail.com"),
                //new KeyValuePair<string, string>("to", "user@user.com"),
                new KeyValuePair<string, string>("subject", "Hello Maksim"),
                new KeyValuePair<string, string>("text", "Congratulations Maksim, you just sent an email with Mailgun!  You are truly awesome!  You can see a record of this email in your logs: https://mailgun.com/cp/log .  You can send up to 300 emails/day from this sandbox server.  Next, you should add your own domain so you can send 10,000 emails/month for free."),
            });

            var requestUri = new Uri(new Uri(baseUri), $"{domain}/messages");

            var response = client.PostAsync(requestUri, data).GetAwaiter().GetResult();

            var strContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            //var results = JsonConvert.DeserializeObject<ErrorResponseModel>(strContent);                                                
        }

        //public class ErrorResponseModel
        //{
        //    public string Message { get; set; }

        //    public Dictionary<string, string[]> ModelState { get; set; }

        //    public ErrorResponseModel()
        //    {
        //        Message = string.Empty;
        //        ModelState = new Dictionary<string, string[]>();
        //    }
        //}
    }
}
