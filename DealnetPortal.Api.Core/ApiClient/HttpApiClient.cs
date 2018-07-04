using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using DealnetPortal.Api.Core.Helpers;
using Newtonsoft.Json;

namespace DealnetPortal.Api.Core.ApiClient
{
    public class HttpApiClient : IHttpApiClient
    {
        public HttpApiClient(string baseAddress)
        {
            Cookies = new CookieContainer();
            Handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
                UseCookies = true,
                //UseDefaultCredentials = true,
                CookieContainer = Cookies
            };

            Client = new HttpClient(Handler)
            {
                BaseAddress = new Uri(baseAddress),
                Timeout = Timeout.InfiniteTimeSpan
            };
            //Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/bson"));
        }

        public Uri ConstructUrl(string requestUri)
        {
            return new Uri(Client.BaseAddress, requestUri);
        }

        public HttpClient Client { get; private set; }

        public HttpClientHandler Handler { get; private set; }

        public CookieContainer Cookies { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>This operation will retry</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestUri"></param>
        /// <param name="content"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<T> PostAsync<T>(string requestUri, T content,
            CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {                
                var response = await Client.PostAsJsonAsync(requestUri, content, cancellationToken);                
                if (response?.Content == null)
                    return default(T);
                return await response.Content.ReadAsAsync<T>(cancellationToken);
            }
            catch (HttpRequestException)
            {
                throw;
            }
        }

        public async Task<T2> PostAsync<T1, T2>(string requestUri, T1 content,
            CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var response = await Client.PostAsJsonAsync(requestUri, content, cancellationToken);

                if (response?.Content == null)
                    return default(T2);

                return await response.Content.ReadAsAsync<T2>(cancellationToken);
            }
            catch (HttpRequestException)
            {
                throw;
            }
        }

        public async Task<HttpResponseMessage> PostAsyncWithHttpResponse<T>(string requestUri, T content,
            AuthenticationHeaderValue authenticationHeader = null, string culture = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(requestUri)
                };
                //var formatter = new MediaTypeFormatter[] {new JsonMediaTypeFormatter()};
                request.Content = new ObjectContent<T>(content, new JsonMediaTypeFormatter());
                if (authenticationHeader != null)
                {
                    request.Headers.Authorization = authenticationHeader;
                }
                if (!string.IsNullOrEmpty(culture))
                {
                    var filteredCulture = CultureHelper.FilterCulture(culture);
                    request.Headers.AcceptLanguage.Clear();
                    request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(filteredCulture));
                }
                return await Client.SendAsync(request, cancellationToken);               
            }
            catch (HttpRequestException)
            {
                throw;
            }
            //return await Client.PostAsJsonAsync(requestUri, content, cancellationToken);
        }

        public async Task<T2> PostAsyncXmlWithXmlResponce<T1, T2>(string requestUri, T1 content,
            CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var response = await Client.PostAsXmlWithSerializerAsync(requestUri, content, cancellationToken).ConfigureAwait(false);

                if (response?.Content == null)
                    return default(T2);
                return XmlSerializerHelper.DeserializeFromString<T2>(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException)
            {
                throw;
            }
        }

        public async Task<T2> PostAsyncEx<T1, T2>(string requestUri, T1 content,
            AuthenticationHeaderValue authenticationHeader = null, string culture = null,
            MediaTypeFormatter formatter = null, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(requestUri)
                };
                //var formatter = new MediaTypeFormatter[] {new JsonMediaTypeFormatter()};
                request.Content = new ObjectContent<T1>(content, formatter ?? new JsonMediaTypeFormatter());
                if (authenticationHeader != null)
                {
                    request.Headers.Authorization = authenticationHeader;
                }
                if (!string.IsNullOrEmpty(culture))
                {
                    var filteredCulture = CultureHelper.FilterCulture(culture);
                    request.Headers.AcceptLanguage.Clear();
                    request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(filteredCulture));
                }
                var response = await Client.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                if (response?.Content == null)
                    return default(T2);
                if (formatter == null)
                {
                    return await response.Content.ReadAsAsync<T2>(cancellationToken);
                }
                else
                {
                    return await response.Content.ReadAsAsync<T2>(new[] { formatter, }, cancellationToken);
                }
            }
            catch (HttpRequestException)
            {                
                throw;
            }
        }

        /// <summary>
        /// See <see cref="IHttpApiClient"/>
        /// </summary>
        /// <remarks>This operation will retry.</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestUri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<T> GetAsync<T>(string requestUri,
            CancellationToken cancellationToken = new CancellationToken())
        {
            HttpResponseMessage response = await Client.GetAsync(requestUri, cancellationToken);

            if (response == null || response.Content == null)
                return default(T);

            return await response.Content.ReadAsAsync<T>(cancellationToken);
        }

        /// <summary>
        /// Perform a get operation against a uri synchronously.
        /// </summary>
        /// <typeparam name="T">Type of the content or model.</typeparam>
        /// <param name="requestUri">Uri of resource</param>
        /// <returns>Model or resource from the Get operation against the uri.</returns>
        public T Get<T>(string requestUri)
        {
            var response = Client.GetAsync(requestUri).Result;

            if (response.IsSuccessStatusCode)
            {
                var responseContent = response.Content;

                // by calling .Result you are synchronously reading the result
                return responseContent.ReadAsAsync<T>().Result;
            }
            else
                return default(T);
        }

        public async Task<T> GetAsyncEx<T>(string requestUri, AuthenticationHeaderValue authenticationHeader = null, string culture = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(requestUri)
                };
                //var formatter = new MediaTypeFormatter[] { new JsonMediaTypeFormatter()};
                if (authenticationHeader != null)
                {
                    request.Headers.Authorization = authenticationHeader;
                }
                if (!string.IsNullOrEmpty(culture))
                {
                    var filteredCulture = CultureHelper.FilterCulture(culture);
                    request.Headers.AcceptLanguage.Clear();
                    request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(filteredCulture));
                }

                var response = await Client.SendAsync(request, cancellationToken);
                if (response?.Content == null)
                    return default(T);
                return await response.Content.ReadAsAsync<T>(cancellationToken);
            }
            catch (HttpRequestException)
            {
                throw;
            }
        }

        /// <summary>
        /// See <see cref="IHttpApiClient"/>
        /// </summary>
        /// <remarks>This operation will retry.</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestUri"></param>
        /// <param name="content"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<T> PutAsync<T>(string requestUri, T content,
            CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var response = await Client.PutAsJsonAsync(requestUri, content, cancellationToken);
                return await response.Content.ReadAsAsync<T>(cancellationToken);
            }
            catch (HttpRequestException)
            {
                throw;
            }
        }

        public async Task<T> PutAsyncEx<T>(string requestUri, T content, AuthenticationHeaderValue authenticationHeader = null,
            string culture = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri(requestUri)
                };
                //var formatter = new MediaTypeFormatter[] { new JsonMediaTypeFormatter() };
                request.Content = new ObjectContent<T>(content, new JsonMediaTypeFormatter());
                if (authenticationHeader != null)
                {
                    request.Headers.Authorization = authenticationHeader;
                }
                if (!string.IsNullOrEmpty(culture))
                {
                    var filteredCulture = CultureHelper.FilterCulture(culture);
                    request.Headers.AcceptLanguage.Clear();
                    request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(filteredCulture));
                }
                var response = await Client.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                if (response?.Content == null)
                    return default(T);
                return await response.Content.ReadAsAsync<T>(cancellationToken);
            }
            catch (HttpRequestException)
            {
                throw;
            }
        }

        /// <summary>
        /// See <see cref="IHttpApiClient"/>
        /// </summary>
        /// <remarks>This operation will retry</remarks>
        /// <typeparam name="T1">Type of the request</typeparam>
        /// <typeparam name="T2">Type of the response</typeparam>
        /// <param name="requestUri"></param>
        /// <param name="content"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<T2> PutAsync<T1, T2>(string requestUri, T1 content,
            CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var response = await Client.PutAsJsonAsync(requestUri, content, cancellationToken);

                if (response?.Content == null)
                    return default(T2);

                return await response.Content.ReadAsAsync<T2>(cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw;
            }
        }

        public async Task<T2> PutAsyncEx<T1, T2>(string requestUri, T1 content, AuthenticationHeaderValue authenticationHeader = null, string culture = null,
            MediaTypeFormatter formatter = null, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri(requestUri)
                };
                //var formatter = new MediaTypeFormatter[] { new JsonMediaTypeFormatter() };
                request.Content = new ObjectContent<T1>(content, formatter ?? new JsonMediaTypeFormatter());                
                if (authenticationHeader != null)
                {
                    request.Headers.Authorization = authenticationHeader;                    
                }
                if (!string.IsNullOrEmpty(culture))
                {
                    var filteredCulture = CultureHelper.FilterCulture(culture);
                    request.Headers.AcceptLanguage.Clear();
                    request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(filteredCulture));
                }
                var response = await Client.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                if (response?.Content == null)
                    return default(T2);
                if (formatter == null)
                {
                    return await response.Content.ReadAsAsync<T2>(cancellationToken);
                }
                else
                {
                    return await response.Content.ReadAsAsync<T2>(new MediaTypeFormatter[] { formatter, }, cancellationToken);
                }
            }
            catch (HttpRequestException)
            {
                throw;
            }
        }

        public async Task DeleteAsyncEx(string requestUri, AuthenticationHeaderValue authenticationHeader = null,
            string culture = null, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                var request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri(requestUri)
                };
                var formatter = new MediaTypeFormatter[] { new JsonMediaTypeFormatter() };
                if (authenticationHeader != null)
                {
                    request.Headers.Authorization = authenticationHeader;
                }
                if (!string.IsNullOrEmpty(culture))
                {
                    var filteredCulture = CultureHelper.FilterCulture(culture);
                    request.Headers.AcceptLanguage.Clear();
                    request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(filteredCulture));
                }
                var response = await Client.SendAsync(request, cancellationToken);                
                response.EnsureSuccessStatusCode();                
            }
            catch (HttpRequestException)
            {
                throw;
            }
        }
    }
}
