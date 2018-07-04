using System.Collections.Generic;
using System.Web.Http;
using DealnetPortal.Api.BackgroundScheduler;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Controllers;
using DealnetPortal.Api.Core.ApiClient;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Integration.ServiceAgents;
using DealnetPortal.Api.Integration.ServiceAgents.ESignature;
using DealnetPortal.Api.Integration.Services;
using DealnetPortal.Api.Integration.Services.Signature;
using DealnetPortal.Aspire.Integration.ServiceAgents;
using DealnetPortal.Aspire.Integration.Storage;
using DealnetPortal.DataAccess;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain.Repositories;
using DealnetPortal.Utilities.Configuration;
using DealnetPortal.Utilities.DataAccess;
using DealnetPortal.Utilities.Logging;
using DealnetPortal.Utilities.Messaging;
using Unity;
using Unity.WebApi;
using Unity.Lifetime;
using Unity.Injection;
using System.Web.Hosting;
using System.Configuration;

namespace DealnetPortal.Api
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
			var container = new UnityContainer();

            // register all your components with the container here
            // it is NOT necessary to register your controllers

            // e.g. container.RegisterType<ITestService, TestService>();
            RegisterTypes(container);

            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);
        }

        public static void RegisterTypes(IUnityContainer container)
        {
            var configReader = new AppConfiguration(WebConfigSections.AdditionalSections);

            container.RegisterType<IDatabaseFactory, DatabaseFactory>(new PerResolveLifetimeManager());
            container.RegisterType<IUnitOfWork, UnitOfWork>(new PerResolveLifetimeManager());

            #region Repositories
            container.RegisterType<IContractRepository, ContractRepository>();
            container.RegisterType<IFileRepository, FileRepository>();
            container.RegisterType<IApplicationRepository, ApplicationRepository>();
            container.RegisterType<ISettingsRepository, SettingsRepository>();
            container.RegisterType<ICustomerFormRepository, CustomerFormRepository>();
            container.RegisterType<IDealerRepository, DealerRepository>();
            container.RegisterType<IRateCardsRepository, RateCardsRepository>();
            container.RegisterType<IDealerOnboardingRepository, DealerOnboardingRepository>();
            container.RegisterType<ILicenseDocumentRepository, LicenseDocumentRepository>();
            #endregion
            #region Services
            container.RegisterType<ILoggingService, LoggingService>();
            container.RegisterType<IContractService, ContractService>();
            container.RegisterType<IMortgageBrokerService, MortgageBrokerService>();
            container.RegisterType<ICreditCheckService, CreditCheckService>();
            container.RegisterType<ICustomerFormService, CustomerFormService>();
            container.RegisterType<IDocumentService, DocumentService>();
            container.RegisterType<IMailService, MailService>();
            container.RegisterType<IEmailService, EmailService>();
            container.RegisterType<IRateCardsService, RateCardsService>();
            container.RegisterType<IPersonalizedMessageService, PersonalizedMessageService>();
            container.RegisterType<IMailChimpService, MailChimpService>();
            container.RegisterType<ISmsSubscriptionService, SmsSubscriptionService>();
            container.RegisterType<IMandrillService, MandrillService>();
            #endregion

            container.RegisterType<IHttpApiClient, HttpApiClient>("AspireClient", new ContainerControlledLifetimeManager(), new InjectionConstructor(configReader.GetSetting(WebConfigKeys.ASPIRE_APIURL_CONFIG_KEY)));
            //container.RegisterType<IHttpApiClient, HttpApiClient>("EcoreClient", new ContainerControlledLifetimeManager(), new InjectionConstructor(configReader.GetSetting("EcoreApiUrl")));
            container.RegisterType<IHttpApiClient, HttpApiClient>("CustomerWalletClient", new ContainerControlledLifetimeManager(), new InjectionConstructor(configReader.GetSetting(WebConfigKeys.CW_APIURL_CONFIG_KEY)));

            container.RegisterType<IAspireServiceAgent, AspireServiceAgent>(new InjectionConstructor(new ResolvedParameter<IHttpApiClient>("AspireClient")));
            container.RegisterType<IAspireService, AspireService>();

            container.RegisterType<ICustomerWalletServiceAgent, CustomerWalletServiceAgent>(new InjectionConstructor(new ResolvedParameter<IHttpApiClient>("CustomerWalletClient")));
            container.RegisterType<ICustomerWalletService, CustomerWalletService>();

            var queryFolderName = configReader.GetSetting(WebConfigKeys.QURIES_FOLDER_CONFIG_KEY);
            var queryFolder = HostingEnvironment.MapPath($"~/{queryFolderName}") ?? queryFolderName;

            container.RegisterType<IQueriesStorage, QueriesFileStorage>(new InjectionConstructor(queryFolder));
            container.RegisterType<IDatabaseService, MsSqlDatabaseService>(
                new InjectionConstructor(ConfigurationManager.ConnectionStrings["AspireConnection"].ConnectionString));

            bool useTestAspire = false;
            bool.TryParse(configReader.GetSetting(WebConfigKeys.USE_TEST_ASPIRE), out useTestAspire);
            if (useTestAspire)
            {
                container.RegisterType<IAspireStorageReader, AspireStorageReader>(
                    new InjectionConstructor(new ResolvedParameter<IDatabaseService>(), new ResolvedParameter<IQueriesStorage>(), new ResolvedParameter<ILoggingService>(),
                    new Dictionary<string,string>()
                    {
                        {"GetDealerDeals", "GetDealerDeals.dev" },
                        {"SearchCustomerAgreements", "SearchCustomerAgreements.dev" }
                    }));
            }
            else
            {
                container.RegisterType<IAspireStorageReader, AspireStorageReader>(
                    new InjectionConstructor(new ResolvedParameter<IDatabaseService>(), new ResolvedParameter<IQueriesStorage>(), new ResolvedParameter<ILoggingService>(),
                    new InjectionParameter<Dictionary<string, string>>(null)));
            }            
            container.RegisterType<IUsersService, UsersService>();

            container.RegisterType<IESignatureServiceAgent, ESignatureServiceAgent>(new InjectionConstructor(new ResolvedParameter<IHttpApiClient>("EcoreClient"), new ResolvedParameter<ILoggingService>()));
            container.RegisterType<ISignatureEngine, DocuSignSignatureEngine>();
            container.RegisterType<IPdfEngine, PdfSharpEngine>();
            container.RegisterType<IDocumentService, DocumentService>();
            container.RegisterType<IMailService, MailService>();
            container.RegisterType<IEmailService, EmailService>();
            container.RegisterType<IDealerService, DealerService>();
            container.RegisterType<IBackgroundSchedulerService, BackgroundSchedulerService>();
            container.RegisterType<IAppConfiguration, AppConfiguration>(new ContainerControlledLifetimeManager(), new InjectionConstructor(WebConfigSections.AdditionalSections as object));
        }
    }
}