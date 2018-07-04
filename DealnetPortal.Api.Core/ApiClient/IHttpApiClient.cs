using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Core.ApiClient
{
    /// <summary>
    /// Client for interacting with REST or Http based services.
    /// </summary>
    public interface IHttpApiClient
    {
        /// <summary>
        /// Constructs Url using client`s BaseAddress and relative path in requestUri parameter
        /// </summary>
        /// <param name="requestUri">relative server path</param>
        /// <returns>constructed Url</returns>
        Uri ConstructUrl(string requestUri);

        /// <summary>
        /// Base HttpClient 
        /// </summary>
        HttpClient Client { get; }

        /// <summary>
        /// Http client handler
        /// </summary>
        HttpClientHandler Handler { get; }

        CookieContainer Cookies { get; }

        /// <summary>
        /// Perform a post operation against a uri.
        /// </summary>
        /// <typeparam name="T">Type of the content or model.</typeparam>
        /// <param name="requestUri">Uri of resource</param>
        /// <param name="content">Individual or list of resources to post.</param>
        /// <param name="cancellationToken">Allows clients to cancel a request.</param>
        /// <returns>Model or resource from the Post operation against the uri.</returns>
        Task<T> PostAsync<T>(string requestUri, T content, CancellationToken cancellationToken = new CancellationToken());

        /// <summary>
        /// Perform a post operation against a uri.
        /// </summary>
        /// <typeparam name="T1">Type of the request</typeparam>
        /// <typeparam name="T2">Type of the response</typeparam>
        /// <param name="requestUri">Uri of resource</param>
        /// <param name="content">Individual or list of resources to post.</param>
        /// <param name="cancellationToken">Allows clients to cancel a request.</param>
        /// <returns>Model or resource from the Post operation against the uri.</returns>
        Task<T2> PostAsync<T1, T2>(string requestUri, T1 content,
            CancellationToken cancellationToken = new CancellationToken());

        /// <summary>
        /// Perform a post operation against a uri.
        /// </summary>
        /// <typeparam name="T">Type of the content or model.</typeparam>
        /// <param name="requestUri">Uri of resource</param>
        /// <param name="content">array of resources to post.</param>
        /// <param name="cancellationToken">Allows clients to cancel a request.</param>
        /// <returns>The response and result from the api.</returns>
        Task<HttpResponseMessage> PostAsyncWithHttpResponse<T>(string requestUri, T content,
            AuthenticationHeaderValue authenticationHeader = null, string culture = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task<T2> PostAsyncXmlWithXmlResponce<T1, T2>(string requestUri, T1 content,
            CancellationToken cancellationToken = new CancellationToken());

        Task<T2> PostAsyncEx<T1, T2>(string requestUri, T1 content, AuthenticationHeaderValue authenticationHeader = null, string culture = null,
            MediaTypeFormatter formatter = null,
            CancellationToken cancellationToken = new CancellationToken());

        /// <summary>
        /// Perform a get operation against a uri.
        /// </summary>
        /// <typeparam name="T">Type of the content or model.</typeparam>
        /// <param name="requestUri">Uri of resource</param>
        /// <param name="cancellationToken">Allows clients to cancel a request.</param>
        /// <returns>Model or resource from the Get operation against the uri.</returns>
        Task<T> GetAsync<T>(string requestUri, CancellationToken cancellationToken = new CancellationToken());

        /// <summary>
        /// Perform a get operation against a uri synchronously.
        /// </summary>
        /// <typeparam name="T">Type of the content or model.</typeparam>
        /// <param name="requestUri">Uri of resource</param>
        /// <returns>Model or resource from the Get operation against the uri.</returns>
        T Get<T>(string requestUri);

        Task<T> GetAsyncEx<T>(string requestUri, AuthenticationHeaderValue authenticationHeader = null, string culture = null, CancellationToken cancellationToken = new CancellationToken());        

        /// <summary>
        /// Perform a put operation against a uri.
        /// </summary>
        /// <typeparam name="T">Type of the content or model.</typeparam>
        /// <param name="requestUri">Uri of resource</param>
        /// <param name="content">Individual or list of resources to post.</param>
        /// <param name="cancellationToken">Allows clients to cancel a request.</param>
        /// <returns>Model or resource from the Put operation against the uri.</returns>
        Task<T> PutAsync<T>(string requestUri, T content, CancellationToken cancellationToken = new CancellationToken());

        Task<T> PutAsyncEx<T>(string requestUri, T content, AuthenticationHeaderValue authenticationHeader = null, string culture = null, 
            CancellationToken cancellationToken = new CancellationToken());

        /// <summary>
        /// Perform a put operation against a uri.
        /// </summary>
        /// <typeparam name="T1">Type of the request</typeparam>
        /// <typeparam name="T2">Type of the response</typeparam>
        /// <param name="requestUri">Uri of resource</param>
        /// <param name="content">Individual or list of resources to post.</param>
        /// <param name="cancellationToken">Allows clients to cancel a request.</param>
        /// <returns>Model or resource from the Post operation against the uri.</returns>
        Task<T2> PutAsync<T1, T2>(string requestUri, T1 content,
            CancellationToken cancellationToken = new CancellationToken());

        Task<T2> PutAsyncEx<T1, T2>(string requestUri, T1 content, AuthenticationHeaderValue authenticationHeader = null, string culture = null,
            MediaTypeFormatter formatter = null,
            CancellationToken cancellationToken = new CancellationToken());

        Task DeleteAsyncEx(string requestUri, AuthenticationHeaderValue authenticationHeader = null, string culture = null,
            CancellationToken cancellationToken = new CancellationToken());
    }
}
