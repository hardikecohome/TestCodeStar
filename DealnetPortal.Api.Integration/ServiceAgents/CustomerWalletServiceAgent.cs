using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Core.ApiClient;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Models.CustomerWallet;
using Unity.Interception.Utilities;

namespace DealnetPortal.Api.Integration.ServiceAgents
{
    public class CustomerWalletServiceAgent : ICustomerWalletServiceAgent
    {
        private IHttpApiClient Client { get; set; }
        private readonly string _fullUri;

        public CustomerWalletServiceAgent(IHttpApiClient client)
        {
            Client = client;            
            _fullUri = Client.Client.BaseAddress.ToString();
        }

        public async Task<IList<Alert>> RegisterCustomer(RegisterCustomerBindingModel registerCustomer)
        {
            var alerts = new List<Alert>();
            try
            {
                var result =
                    await Client.PostAsyncWithHttpResponse($"{_fullUri}/Account/RegisterCustomer", registerCustomer);
                if (!result.IsSuccessStatusCode)
                {
                    var errors = await HttpResponseHelpers.GetModelStateErrorsAsync(result.Content);
                    errors.ModelState?.ForEach(st => st.Value.ForEach(val =>
                        alerts.Add(new Alert()
                        {
                            Type = AlertType.Error,
                            Message = val,
                            Header = st.Key
                        })));
                }
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = $"Register new customer on Customer Wallet portal failed",
                    Message = ex.Message
                });
            }
            return alerts;
        }

        public async Task<IList<Alert>> CreateTransaction(TransactionInfoDTO transactionInfo)
        {
            var alerts = new List<Alert>();
            try
            {
                return
                    await
                        Client.PutAsync<TransactionInfoDTO, IList<Alert>>($"{_fullUri}/Customer/CreateTransaction",
                            transactionInfo);
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = $"Creation of a new transaction on Customer Wallet portal failed",
                    Message = ex.Message
                });
            }
            return alerts;
        }

        public async Task<IList<Alert>> CheckUser(string userName)
        {
            var alerts = new List<Alert>();
            try
            {

                var result =
                    await Client.PostAsync<CheckUserRequest, bool>($"{_fullUri}/Account/CheckUser", new CheckUserRequest {UserLogin = userName});
                if (result)
                {
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = "Cannot create customer",
                        Message = "Customer with this email address is already registered."
                    });
                }
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = $"Creation of a new transaction on Customer Wallet portal failed",
                    Message = ex.Message
                });
            }
            return alerts;
        }
    }
}
