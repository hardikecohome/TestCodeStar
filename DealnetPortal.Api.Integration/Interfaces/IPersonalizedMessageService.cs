using System.Net.Http;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Integration.Interfaces
{
    public interface IPersonalizedMessageService
    {
        Task<HttpResponseMessage> SendMessage(string phonenumber, string messagebody);
    }
}
