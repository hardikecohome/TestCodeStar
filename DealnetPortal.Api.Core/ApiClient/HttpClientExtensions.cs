using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Core.ApiClient
{
    public static class HttpExtensions
    {
        public static async Task<HttpResponseMessage> PostAsXmlWithSerializerAsync<T>(this HttpClient client, string requestUri, T value, CancellationToken cancellationToken)
        {
            return await client.PostAsync(requestUri, value,
                          new XmlMediaTypeFormatter { UseXmlSerializer = true },
                          cancellationToken).ConfigureAwait(false);
        }
    }
}
