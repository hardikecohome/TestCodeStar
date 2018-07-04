using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DealnetPortal.Api.Common.Helpers
{
    public static class HttpResponseHelpers
    {
        public static async Task<ErrorResponseModel> GetModelStateErrorsAsync(HttpContent content)
        {
            var strContent = await content.ReadAsStringAsync();
            var errors = new List<string>();
            var results =
                        JsonConvert.DeserializeObject<ErrorResponseModel>(strContent);            
            return results;
        }
    }

    public class ErrorResponseModel
    {
        public string Message { get; set; }

        public Dictionary<string, string[]> ModelState { get; set; }

        public ErrorResponseModel()
        {
            Message = string.Empty;
            ModelState = new Dictionary<string, string[]>();
        }
    }
}
