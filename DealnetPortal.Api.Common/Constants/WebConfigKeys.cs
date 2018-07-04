namespace DealnetPortal.Api.Common.Constants
{
    public static class WebConfigKeys
    {
        //this settings for authentification email service-->
        public static readonly string ES_FROMEMAIL_CONFIG_KEY = "EmailService.FromEmailAddress";
        public static readonly string ES_SMTPHOST_CONFIG_KEY = "EmailService.SmtpHost";
        public static readonly string ES_SMTPPORT_CONFIG_KEY = "EmailService.SmtpPort";
        public static readonly string ES_SMTPUSER_CONFIG_KEY = "EmailService.SmtpUser";
        public static readonly string ES_SMTPPASSWORD_CONFIG_KEY = "EmailService.SmtpPassword";
        public static readonly string ES_SMTPPASSLENGTH_CONFIG_KEY = "SecurityHelper.RandomPasswordLength";

        public const string AUTHPROVIDER_CONFIG_KEY = "AuthProvider";

        public const string HTTPS_ON_PRODUCTION_CONFIG_KEY = "HttpsOnProduction";

        //ENTER HERE Mailgun settings
        public static readonly string MG_APIURL_CONFIG_KEY = "MailGun.ApiUrl";
        public static readonly string MG_APIKEY_CONFIG_KEY = "MailGun.ApiKey";
        public static readonly string MG_DOMAIN_CONFIG_KEY = "MailGun.Domain";
        public static readonly string MG_FROM_CONFIG_KEY = "MailGun.From";

        //ENTER HERE Aspire settings
        public static readonly string ASPIRE_APIURL_CONFIG_KEY = "AspireApiUrl";
        public static readonly string ASPIRE_USER_CONFIG_KEY = "AspireUser";
        public static readonly string ASPIRE_PASSWORD_CONFIG_KEY = "AspirePassword";

        public const string MB_ROLE_CONFIG_KEY = "AspireMortgageBrokerRole";

        //ENTER HERE eCore digital signature settings
        /// <summary>
        /// not longer used
        /// </summary>
        //public static readonly string ECORE_APIURL_CONFIG_KEY = "eCoreApiUrl";
        //public static readonly string ECORE_USER_CONFIG_KEY = "eCoreUser";
        //public static readonly string ECORE_PASSWORD_CONFIG_KEY = "eCorePassword";
        //public static readonly string ECORE_ORGANIZATION_CONFIG_KEY = "eCoreOrganization";
        //public static readonly string ECORE_SIGNATUREROLE_CONFIG_KEY = "eCoreSignatureRole";
        //public static readonly string ECORE_AGREEMENTTEMPLATE_CONFIG_KEY = "eCoreAgreementTemplate";
        //public static readonly string ECORE_CUSTOMERSECURITYCODE_CONFIG_KEY = "eCoreCustomerSecurityCode";

        //ENTER HERE DocuSign digital signature settings
        public static readonly string DOCUSIGN_APIURL_CONFIG_KEY = "DocuSignApiUrl";
        public static readonly string DOCUSIGN_USER_CONFIG_KEY = "DocuSignUser";
        public static readonly string DOCUSIGN_PASSWORD_CONFIG_KEY = "DocuSignPassword";
        public static readonly string DOCUSIGN_INTEGRATORKEY_CONFIG_KEY = "DocuSignIntegratorKey";
        public static readonly string DOCUSIGN_BRAND_ID = "DocuSignBrandId";
        public static readonly string QUEBEC_DOCUDIGN_BRAND_ID = "QuebecDocuSignBrandId";
        public static readonly string DOCUSIGN_NOTIFICATIONS_URL = "DocuSignNotificationsUrl";
        public static readonly string DEALER_PORTAL_DRAFTURL_KEY = "DealerPortalDraftUrl";

        //Aspire statuses
        //Document upload status
        public static readonly string DOCUMENT_UPLOAD_STATUS_CONFIG_KEY = "DocumentUploadStatus";
        //All Documents Uploaded status
        public static readonly string ALL_DOCUMENTS_UPLOAD_STATUS_CONFIG_KEY = "AllDocumentsUploadedStatus";
        public static readonly string CREDIT_REVIEW_STATUS_CONFIG_KEY = "CreditReviewStatus";
        public static readonly string ONBOARDING_INIT_STATUS_KEY = "OnboardingInitStatus";
        public static readonly string ONBOARDING_DRAFT_STATUS_KEY = "OnboardingDraftStatus";
        public static readonly string RISK_BASED_STATUS_KEY = "RiskBasedStatus";

        public static readonly string HIDE_PREAPPROVAL_AMOUNT_FOR_LEASEDEALERS_KEY = "HidePreApprovalAmountsForLeaseDealers";        

        //Describing portal constants
        //Ecohome
        public static readonly string PORTAL_DESCRIBER_ECOHOME_CONFIG_KEY = "PortalDescriber.df460bb2-f880-42c9-aae5-9e3c76cdcd0f";
        //ODI
        public static readonly string PORTAL_DESCRIBER_ODI_CONFIG_KEY = "PortalDescriber.606cfa8b-0e2c-47ef-b646-66c5f639aebd";

        public static readonly string DEFAULT_LEAD_SOURCE_KEY = "DefaultLeadSource";
        public static readonly string ONBOARDING_LEAD_SOURCE_KEY = "OnboardingLeadSource";
        public static readonly string ONBOARDING_LEAD_SOURCE_FRENCH_KEY = "OnboardingLeadSourceFrench";

        public static readonly string INITIAL_DATA_SEED_ENABLED_CONFIG_KEY = "InitialDataSeedEnabled";
        public static readonly string AGREEMENT_TEMPLATE_FOLDER_CONFIG_KEY = "AgreementTemplatesFolder";
        public static readonly string QURIES_FOLDER_CONFIG_KEY = "QueriesFolder";

        public static readonly string CUSTOMER_EMAIL_NOFIFICATION_ENABLED_CONFIG_KEY = "CustomerEmailNotificationEnabled";

        //Customer Wallet integration-->
        public static readonly string CW_APIURL_CONFIG_KEY = "CustomerWalletApiUrl";
        public static readonly string CW_CLIENT_CONFIG_KEY = "CustomerWalletClient";
        public static readonly string CW_PHONE_CONFIG_KEY = "CustomerWalletPhone";
        public static readonly string CW_EMAIL_CONFIG_KEY = "CustomerWalletEmail";
        public static readonly string MB_PHONE_CONFIG_KEY = "MortgageBrokerPhone";
        public static readonly string MB_EMAIL_CONFIG_KEY = "MortgageBrokerEmail";
        public static readonly string DN_EMAIL_CONFIG_KEY = "DealNetEmail";

        //MailChimp api-->
        public static readonly string MC_APIKEY_CONFIG_KEY = "MailChimpApiKey";

        //Scheduler configuration-->
        public static readonly string LEAD_EXPIREDMINUTES_CONFIG_KEY = "LeadExpiredMinutes";
        public static readonly string LEAD_CHECKPERIODMINUTES_CONFIG_KEY = "CheckPeriodMinutes";

        //Listener URL
        public static readonly string LISTENER_END_POINT_CONFIG_KEY = "ListenerEndPoint";

        public static readonly string CLARITY_TIER_NAME = "ClarityTierName";

        public static readonly string EMCO_LEASE_TIER_NAME = "EmcoLeaseTierName";

        public static readonly string QUEBEC_POSTAL_CODES = "QuebecPostalCodes";

        //
        public static readonly string USE_TEST_ASPIRE = "UseTestAspire";
    }
}