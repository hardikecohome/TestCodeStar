using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using DealnetPortal.Aspire.Integration.Models.AspireDb;
using DealnetPortal.Utilities.DataAccess;
using DealnetPortal.Utilities.Logging;

namespace DealnetPortal.Aspire.Integration.Storage
{
    public class AspireStorageReader : IAspireStorageReader
    {
        private readonly IDatabaseService _databaseService;
        private readonly IQueriesStorage _queriesStorage;
        private readonly ILoggingService _loggingService;
        private Dictionary<string, string> _queryNames;

        public AspireStorageReader(IDatabaseService databaseService, IQueriesStorage queriesStorage, ILoggingService loggingService, Dictionary<string, string> queryNames = null)
        {
            _databaseService = databaseService;
            _queriesStorage = queriesStorage;
            _loggingService = loggingService;
            SetupDefaulQueryNames();
            if (queryNames?.Any() == true)
            {
                foreach (var qn in queryNames)
                {
                    _queryNames[qn.Key] = qn.Value;
                }
            }
        }

        public IList<DropDownItem> GetGenericFieldValues()
        {
            string sqlStatement =
                @"SELECT a.ref_type,a.ShowInAllRoles,a.field_size,a.UseInWorkQueues,a.OID,A.SEQ_NUM,A.DESCR AS UDF_COLUMN_NAME,
                                    B.inactive_date,
                                    B.descr as Field_Description,
                                    B.active,
                                    B.OID as Field_Oid,
                                    B.valid_value as SubmissionValue
                                      FROM GenericField (NOLOCK)A
                                      LEFT JOIN GenericFieldValidValue(NOLOCK) B ON A.oid = B.genf_oid
                                      and b.active = 1 and a.active = 1                                      
                                      ORDER BY 3, 5";

            var list = GetListFromQuery(sqlStatement, _databaseService, ReadDropDownItem);
            return list;
        }

        public IList<GenericSubDealer> GetSubDealersList(string dealerUserName)
        {           
            string sqlStatement = _queriesStorage.GetQuery(_queryNames[nameof(GetSubDealersList)]);
            if (!string.IsNullOrEmpty(sqlStatement))
            {
                sqlStatement = string.Format(sqlStatement, dealerUserName);
                var list = GetListFromQuery(sqlStatement, _databaseService, ReadSubDealerItem);
                return list;
            }
            else
            {
                _loggingService.LogWarning("Cannot get GetSubDealers query for request");
            }
            return new List<GenericSubDealer>();
        }

        public IList<Contract> GetDealerDeals(string dealerUserName)
        {
            string sqlStatement = _queriesStorage.GetQuery(_queryNames[nameof(GetDealerDeals)]);
            if (!string.IsNullOrEmpty(sqlStatement))
            {
                sqlStatement = string.Format(sqlStatement, dealerUserName);
                var list = GetListFromQuery(sqlStatement, _databaseService, ReadSampleDealItem);
                return list;
            }
            else
            {
                _loggingService.LogWarning("Cannot get GetDealerDeals query for request");
            }
            return new List<Contract>();
        }

        public IList<CustomerAgreementShortInfo> SearchCustomerAgreements(string firstName, string lastName,
            DateTime dateOfBirth)
        {
            string sqlStatement = _queriesStorage.GetQuery(_queryNames[nameof(SearchCustomerAgreements)]);
            if (!string.IsNullOrEmpty(sqlStatement))
            {
                var dob = dateOfBirth.ToString("MM/dd/yyyy");
                sqlStatement = string.Format(sqlStatement, firstName, lastName, dob);
                var list = GetListFromQuery(sqlStatement, _databaseService, ReadCustomerAgreementShortInfo);
                return list;
            }
            else
            {
                _loggingService.LogWarning("Cannot get GetDealerDeals query for request");
            }
            return new List<CustomerAgreementShortInfo>();
        }

        public Entity GetDealerInfo(string dealerUserName)
        {
            string sqlStatement = _queriesStorage.GetQuery(_queryNames[nameof(GetDealerInfo)]);

            if (!string.IsNullOrEmpty(sqlStatement))
            {
                sqlStatement = string.Format(sqlStatement, dealerUserName);
                var list = GetListFromQuery(sqlStatement, _databaseService, ReadDealerInfoItem);
                if (list?.Any() ?? false)
                {
                    return list[0];
                }
            }
            else
            {
                _loggingService.LogWarning("Cannot get GetDealerDeals query for request");
            }
            return null;            
        }
        
        public Entity GetCustomerById(string customerId)
        {           
            string sqlStatement = _queriesStorage.GetQuery(_queryNames[nameof(GetCustomerById)]);
            if (!string.IsNullOrEmpty(sqlStatement))
            {

                sqlStatement = string.Format(sqlStatement, customerId);
                var list = GetListFromQuery(sqlStatement, _databaseService, ReadCustomerItem);
                if (list?.Any() ?? false)
                {
                    return list[0];
                }
            }
            else
            {
                _loggingService.LogWarning("Cannot get GetCustomerById query for request");
            }
            return null;
        }        

        public Entity FindCustomer(string firstName, string lastName, DateTime dateOfBirth, string postalCode)
        {            
            string sqlStatement = _queriesStorage.GetQuery(_queryNames[nameof(FindCustomer)]);
            if (!string.IsNullOrEmpty(sqlStatement))
            {
                var dob = dateOfBirth.ToString(CultureInfo.InvariantCulture);
                sqlStatement = string.Format(sqlStatement, firstName, lastName, dob, postalCode.Replace(" ", ""));
                var list = GetListFromQuery(sqlStatement, _databaseService, ReadCustomerItem);
                if (list?.Any() ?? false)
                {
                    return list[0];
                }                
            }
            else
            {
                _loggingService.LogWarning("Cannot get FindCustomer query for request");
            }
            return null;
        }

        public DealerRoleEntity GetDealerRoleInfo(string dealerUserName)
        {
            string sqlStatement = _queriesStorage.GetQuery(_queryNames[nameof(GetDealerRoleInfo)]);

            if (!string.IsNullOrEmpty(sqlStatement))
            {
                sqlStatement = string.Format(sqlStatement, dealerUserName);
                var list = GetListFromQuery(sqlStatement, _databaseService, ReadDealerRoleInfoItem);
                if (list?.Any() ?? false)
                {
                    return list[0];
                }
            }
            else
            {
                _loggingService.LogWarning("Cannot get DealerRoleInformation query for request");
            }
            return null;
        }

        public string GetDealStatus(string transactionId)
        {
            string sqlStatement = _queriesStorage.GetQuery(_queryNames[nameof(GetDealStatus)]);

            if (!string.IsNullOrEmpty(sqlStatement))
            {
                try
                {
                    sqlStatement = string.Format(sqlStatement, transactionId);
                    var list = GetListFromQuery(sqlStatement, _databaseService, ReadDealStatusItem);
                    if (list?.Any() ?? false)
                    {
                        return list[0];
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError("Cannot get GetDealStatus query",ex);
                }
            }
            else
            {
                _loggingService.LogWarning("Cannot get GetDealStatus query for request");
            }
            return null;
        }

        public CreditReport GetCustomerCreditReport(string customerId)
        {
            string sqlStatement = _queriesStorage.GetQuery(_queryNames[nameof(GetCustomerCreditReport)]);
            if (!string.IsNullOrEmpty(sqlStatement))
            {
                try
                {
                    sqlStatement = string.Format(sqlStatement, customerId);
                    var list = GetListFromQuery(sqlStatement, _databaseService, ReadCreditReportItem);
                    if (list?.Any() ?? false)
                    {
                        return list[0];
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError("Cannot get GetCustomerBeaconById query", ex);
                }
            }
            else
            {
                _loggingService.LogWarning("Cannot get GetCustomerBeaconById query for request");
            }
            return null;
        }

        public CreditReport GetCustomerCreditReport(string firstName, string lastName, DateTime dateOfBirth,
            string postalCode)
        {
            string sqlStatement = _queriesStorage.GetQuery(_queryNames["GetCustomerCreditReport.dev"]);
            if (!string.IsNullOrEmpty(sqlStatement))
            {
                try
                {
                    var dob = dateOfBirth.ToString(CultureInfo.InvariantCulture);
                    sqlStatement = string.Format(sqlStatement, firstName, lastName, dob, postalCode?.Replace(" ", ""));
                    var list = GetListFromQuery(sqlStatement, _databaseService, ReadCreditReportItem);
                    if (list?.Any() ?? false)
                    {
                        return list[0];
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError("Cannot get GetCustomerBeacon query", ex);
                }
            }
            else
            {
                _loggingService.LogWarning("Cannot get GetCustomerBeacon query for request");
            }
            return null;
        }

        private List<T> GetListFromQuery<T>(string query, IDatabaseService ds, Func<IDataReader, T> func)
        {
            //Define list
            var resultList = new List<T>();

            //Execute query and populate data 
            using (IDataReader reader = ds.ExecuteReader(query))
            {                
                while (reader.Read())
                {
                    var row = func(reader);
                    if (row != null)
                    {
                        resultList.Add(row);
                    }
                }
            }

            return resultList;
        }

        private string ReadDealStatusItem(IDataReader dr)
        {
            try
            {
                var status = ConvertFromDbVal<string>(dr["deal status"]);
                return status;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private DropDownItem ReadDropDownItem(IDataReader dr)
        {
            try
            {            
                var row = new DropDownItem();

                row.Oid = (int)(long) dr["OID"];
                row.Name = (string) dr["UDF_COLUMN_NAME"];
                row.Description = ConvertFromDbVal<string>(dr["Field_Description"]);
                row.FieldNum = ConvertFromDbVal<long>(dr["Field_Oid"]);
                row.RefType = ConvertFromDbVal<string>(dr["ref_type"]);
                row.SeqNum = ConvertFromDbVal<int>(dr["SEQ_NUM"]);
                row.SubmissionValue = ConvertFromDbVal<string>(dr["SubmissionValue"]);

                return row;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private GenericSubDealer ReadSubDealerItem(IDataReader dr)
        {
            try
            {
                var row = new GenericSubDealer();

                row.DealerId = ((long)dr["OID"]).ToString();
                row.SeqNum = ConvertFromDbVal<int>(dr["SEQ_NUM"]).ToString();
                row.DealerName = (string)dr["UDF_COLUMN_NAME"];
                row.SubDealerName = ConvertFromDbVal<string>(dr["Field_Description"]);
                row.SubDealerId = ConvertFromDbVal<long>(dr["Field_Oid"]).ToString();                
                row.SubmissionValue = ConvertFromDbVal<string>(dr["SubmissionValue"]);                
                return row;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private Contract ReadSampleDealItem(IDataReader dr)
        {
            try
            {
                var item = new Contract();

                var date = ConvertFromDbVal<string>(dr["Last_Update_Date"]);
                var time = ConvertFromDbVal<string>(dr["Last_Update_Time"]);
                DateTime updateTime;
                DateTime.TryParse($"{date} {time}",CultureInfo.InvariantCulture,DateTimeStyles.None, out updateTime);

                item.LastUpdateTime = time;
                item.LastUpdateDate = date;
                item.LastUpdateDateTime = updateTime;

                item.TransactionId = ConvertFromDbVal<long>(dr["transaction#"]);
                item.DealStatus = ConvertFromDbVal<string>(dr["Deal_Status"]);
                item.AgreementType = ConvertFromDbVal<string>(dr["Contract_Type_Code"]);

                item.Term = ConvertFromDbVal<int>(dr["Term"]);
                item.AmountFinanced = ConvertFromDbVal<decimal>(dr["Amount Financed"]);
                item.AgreementType = ConvertFromDbVal<string>(dr["Contract_Type_Code"]);

                item.EquipmentDescription = ConvertFromDbVal<string>(dr["Equipment_Description"]);
                item.EquipmentType = ConvertFromDbVal<string>(dr["Equipment_Type"]);
                
                var names = ConvertFromDbVal<string>(dr["Customer_name"])?.Split(new string[] {","}, StringSplitOptions.RemoveEmptyEntries);
                var fstName = names?.Count() > 1 ? names[0] : string.Empty;
                var lstName = names?.Count() > 1 ? names[1] : string.Empty;

                item.CustomerAccountId = ConvertFromDbVal<string>(dr["Customer ID"]);
                item.CustomerFirstName = fstName;
                item.CustomerLastName = lstName;
                item.OverrideCreditAmountLimit = ConvertFromDbVal<decimal?>(dr["OverrideCreditAmountLimit"]);
                item.OverrideCustomerRiskGroup = ConvertFromDbVal<string>(dr["OverrideCustomerRiskGroup"]);

                return item;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private Entity ReadCustomerItem(IDataReader dr)
        {
            try
            {
                Entity userEntity = new Entity()
                {
                    FirstName = ConvertFromDbVal<string>(dr["fname"]),
                    LastName = ConvertFromDbVal<string>(dr["lname"]),
                    EntityId = ConvertFromDbVal<string>(dr["entt_id"]),
                };

                userEntity.DateOfBirth = ConvertFromDbVal<DateTime>(dr["date_of_birth"]);
                userEntity.EmailAddress = ConvertFromDbVal<string>(dr["email_addr"]);

                var postalCode = ConvertFromDbVal<string>(dr["postal_code"]);
                if (!string.IsNullOrEmpty(postalCode))
                {
                    userEntity.PostalCode = postalCode;
                    userEntity.City = ConvertFromDbVal<string>(dr["city"]);
                    userEntity.State = ConvertFromDbVal<string>(dr["state"]);
                    userEntity.Street = ConvertFromDbVal<string>(dr["addr_line1"]);                    
                }

                var phoneNum = ConvertFromDbVal<string>(dr["phone_num"]);
                if (!string.IsNullOrEmpty(phoneNum))
                {
                    userEntity.PhoneNum = phoneNum;                    
                }

                try
                {
                    userEntity.LeaseSource = ConvertFromDbVal<string>(dr["leaseSource"]);
                }
                catch (Exception)
                {
                    userEntity.LeaseSource = null;
                }
               
                return userEntity;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot read entity info record from Aspire DB", ex);
                return null;
            }
        }

        private Entity ReadDealerInfoItem(IDataReader dr)
        {            
            var dealerCustomerInfo = ReadCustomerItem(dr);

            if (dealerCustomerInfo != null)
            {              
                try
                {
                    var name = ConvertFromDbVal<string>(dr["name"]);
                    if (!string.IsNullOrEmpty(name))
                    {
                        dealerCustomerInfo.Name = name;
                    }                    
                }
                catch (Exception ex)
                {       
                    _loggingService.LogError("Cannot read dealer info record from Aspire DB", ex);
                }
            }

            return dealerCustomerInfo;
        }

        private DealerRoleEntity ReadDealerRoleInfoItem(IDataReader dr)
        {
            var dealerEntity = ReadDealerInfoItem(dr);
            if (dealerEntity != null)
            {
                try
                {                    
                    var dealerInfo = new DealerRoleEntity()
                    {
                        FirstName = dealerEntity.FirstName,
                        LastName = dealerEntity.LastName,
                        Name = dealerEntity.Name,
                        ParentUserName = dealerEntity.ParentUserName,
                        EntityId = dealerEntity.EntityId,
                        DateOfBirth = dealerEntity.DateOfBirth,
                        EmailAddress = dealerEntity.EmailAddress,
                        PostalCode = dealerEntity.PostalCode,
                        City = dealerEntity.City,
                        State = dealerEntity.State,
                        Street = dealerEntity.Street,
                        PhoneNum = dealerEntity.PhoneNum
                    };

                    dealerInfo.ProductType = ConvertFromDbVal<string>(dr["Product_Type"]);
                    dealerInfo.ChannelType = ConvertFromDbVal<string>(dr["Channel_Type"]);
                    dealerInfo.Ratecard = ConvertFromDbVal<string>(dr["ratecard"]);
                    dealerInfo.LeaseRatecard = ConvertFromDbVal<string>(dr["lease_ratecard"]);
                    dealerInfo.Role = ConvertFromDbVal<string>(dr["Role"]);
                    dealerInfo.UserId = ConvertFromDbVal<string>(dr["user_id"]);
                    if (dr["parent_uname"] != null)
                    {
                        var pname = ConvertFromDbVal<string>(dr["parent_uname"]);
                        if (!string.IsNullOrEmpty(pname))
                        {
                            dealerInfo.ParentUserName = pname;
                        }
                    }

                    return dealerInfo;
                }
                catch (Exception ex)
                {
                    _loggingService.LogError("Cannot read dealer-role info from Aspire DB", ex);
                    return null;
                }
            }
            return null;
        }

        private CreditReport ReadCreditReportItem(IDataReader dr)
        {
            try
            {
                var creditReport = new CreditReport()
                {
                    FirstName = ConvertFromDbVal<string>(dr["fname"]),
                    LastName = ConvertFromDbVal<string>(dr["lname"]),
                    CustomerId = ConvertFromDbVal<string>(dr["customer_id"]),
                    ContractId = ConvertFromDbVal<string>(dr["contractoid"])
                };

                creditReport.DateOfBirth = ConvertFromDbVal<DateTime?>(dr["date_of_birth"]);
                creditReport.CreatedTime = ConvertFromDbVal<DateTime?>(dr["crtd_datetime"]);

                creditReport.LastUpdatedTime = ConvertFromDbVal<DateTime?>(dr["lupd_datetime"]);
                creditReport.LastUpdateUser = ConvertFromDbVal<string>(dr["lupd_user"]);

                creditReport.CreditId = ConvertFromDbVal<string>(dr["creditoid"]);
                var strBeacon = ConvertFromDbVal<string>(dr["Beacon"]);
                if (string.IsNullOrEmpty(strBeacon))
                {
                    return null;
                }
                int beacon = 0;
                int.TryParse(strBeacon, out beacon);
                creditReport.Beacon = beacon;
                return creditReport;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot read credit report record from Aspire DB", ex);
                return null;
            }
        }

        private CustomerAgreementShortInfo ReadCustomerAgreementShortInfo(IDataReader dr)
        {
            try
            {
                var leaseInfo = new CustomerAgreementShortInfo()
                {
                    LeaseNum = ConvertFromDbVal<string>(dr["lease_num"]),
                    TransactionId = ConvertFromDbVal<string>(dr["lease_app_num"]),
                    BookType = ConvertFromDbVal<string>(dr["lease_book_type"]),
                    BookedPostDate = ConvertFromDbVal<DateTime>(dr["lease_booked_post_date"]),
                    SignDate = ConvertFromDbVal<DateTime>(dr["lease_sign_date"]),
                    StartDate = ConvertFromDbVal<DateTime>(dr["lease_start_date"]),
                    MaturityDate = ConvertFromDbVal<DateTime>(dr["lease_maturity_date"]),
                    Term = ConvertFromDbVal<string>(dr["lease_term"]),
                    TypeDesc = ConvertFromDbVal<string>(dr["lease_type_desc"]),
                    Type = ConvertFromDbVal<string>(dr["lease_type"]),
                    CustomerId = ConvertFromDbVal<string>(dr["lease_cust_id_num"]),
                    EquipType = ConvertFromDbVal<string>(dr["equip_type"]),
                    EquipTypeDesc = ConvertFromDbVal<string>(dr["equip_type_desc"]),
                    EquipActiveFlag = ConvertFromDbVal<string>(dr["equip_active_flag"])
                };
                return leaseInfo;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot read credit report record from Aspire DB", ex);
                return null;
            }
        }

        private void SetupDefaulQueryNames()
        {
            _queryNames = new Dictionary<string, string>();
            _queryNames.Add(nameof(GetSubDealersList), "GetSubDealers");
            _queryNames.Add(nameof(GetDealerDeals), "GetDealerDeals");
            _queryNames.Add(nameof(GetDealerInfo), "GetDealerInfoByUserId");
            _queryNames.Add(nameof(GetCustomerById), "GetCustomerById");
            _queryNames.Add(nameof(FindCustomer), "FindCustomer");
            _queryNames.Add(nameof(GetDealerRoleInfo), "DealerRoleInformation");
            _queryNames.Add(nameof(GetDealStatus), "GetDealStatus");
            _queryNames.Add(nameof(GetCustomerCreditReport), "GetCustomerBeaconById");
            _queryNames.Add("GetCustomerCreditReport.dev", "GetCustomerBeacon.dev");
            _queryNames.Add(nameof(SearchCustomerAgreements), "SearchCustomerAgreements");
        }

        public static T ConvertFromDbVal<T>(object obj)
        {
            if (obj == null || obj == DBNull.Value)
            {
                return default(T); // returns the default value for the type
            }
            else
            {
                return (T)obj;
            }
        }
    }
}
