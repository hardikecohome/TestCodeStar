using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Integration.Services
{
    public static class PdfFormFields
    {
        //Application/Transaction ID
        public static string ApplicationId = "ApplicationId";
        //Customer Fields
        public static string FirstName = "FirstName";
        public static string LastName = "LastName";
        public static string DateOfBirth = "DateOfBirth";
        public static string FirstName2 = "FirstName_2";
        public static string LastName2 = "LastName_2";
        public static string DateOfBirth2 = "DateOfBirth_2";
        public static string InstallationAddress = "InstallationAddress";
        //some forms don't have separate Suite field
        public static string InstallationAddressWithSuite = "InstallationAddressWithSuite";
        public static string MailingAddress = "MailingAddress";
        public static string PreviousAddress = "PreviousAddress";
        public static string MailingOrPreviousAddress = "MailingOrPreviousAddress";
        //Co-borrower fields  : some forms don't have separate Suite field
        public static string InstallationAddress2 = "InstallationAddress_2";
        public static string InstallationAddressWithSuite2 = "InstallationAddressWithSuite_2";
        public static string MailingAddress2 = "MailingAddress_2";
        public static string PreviousAddress2 = "PreviousAddress_2";
        public static string MailingOrPreviousAddress2 = "MailingOrPreviousAddress_2";

        public static string CustomerIdTypeDriverLicense = "Tiv1";
        public static string CustomerIdTypeOther = "Tiv1Other";
        public static string CustomerIdTypeOtherValue = "OtherID";

        public static string AllowCommunicate = "AllowCommunicate";
        public static string AllowCommunicate2 = "AllowCommunicate_2";
        public static string RelationshipToCustomer2 = "RelationshipToCustomer_2";

        public static string Sin = "SIN";
        public static string DriverLicense = "DriverLicense";
        public static string Dl = "DL";
        public static string Sin2 = "SIN_2";
        public static string City = "City";
        public static string Province = "Province";
        public static string PostalCode = "PostalCode";
        public static string City2 = "City_2";
        public static string Province2 = "Province_2";
        public static string PostalCode2 = "PostalCode_2";
        public static string HomePhone = "HomePhone";
        public static string HomePhone2 = "HomePhone_2";
        public static string CellPhone = "CellPhone";
        public static string CellPhone2 = "CellPhone_2";
        public static string BusinessPhone = "BusinessPhone";
        public static string BusinessOrCellPhone = "BusinessOrCellPhone";
        public static string CellOrHomePhone = "CellOrHomePhone";
        public static string EmailAddress = "EmailAddress";
        public static string EmailAddress2 = "EmailAddress_2";
        public static string IsMailingDifferent = "IsMailingDifferent";
        public static string IsPreviousAddress = "IsPreviousAddress";

        public static string IsMailingDifferent2 = "IsMailingDifferent_2";
        public static string IsPreviousAddress2 = "IsPreviousAddress_2";

        public static string SuiteNo = "SuiteNo";
        public static string SuiteNo2 = "SuiteNo_2";
        public static string CustomerName = "CustomerName";
        public static string CustomerName2 = "CustomerName_2";
        public static string IsHomeOwner = "IsHomeOwner";
        public static string IsHomeOwner2 = "IsHomeOwner2";

        //Equipment Fields
        public static string IsFurnace = "IsFurnace";
        public static string IsAirConditioner = "IsAirConditioner";
        public static string IsBoiler = "IsBoiler";
        public static string IsWaterFiltration = "IsWaterFiltration";
        public static string IsWaterHeater = "IsWaterHeater";
        public static string IsOther1 = "IsOther1";
        public static string IsOther2 = "IsOther2";
        public static string IsOtherBase = "IsOther";
        public static string FurnaceDetails = "FurnaceDetails";
        public static string AirConditionerDetails = "AirConditionerDetails";
        public static string BoilerDetails = "BoilerDetails";
        public static string WaterFiltrationDetails = "WaterFiltrationDetails";
        public static string WaterHeaterDetails = "WatherHeaterDetails";
        public static string OtherDetails1 = "OtherDetails1";
        public static string OtherDetails2 = "OtherDetails2";
        public static string OtherDetailsBase = "OtherDetails";
        public static string FurnaceMonthlyRental = "FurnaceMonthlyRental";
        public static string AirConditionerMonthlyRental = "AirConditionerMonthlyRental";
        public static string BoilerMonthlyRental = "BoilerMonthlyRental";
        public static string WaterFiltrationMonthlyRental = "WaterFiltrationMonthlyRental";
        public static string WaterHeaterMonthlyRental = "WaterHeaterMonthlyRental";
        public static string OtherMonthlyRental1 = "OtherMonthlyRental1";
        public static string OtherMonthlyRental2 = "OtherMonthlyRental2";
        public static string OtherMonthlyRentalBase = "OtherMonthlyRental";

        public static string TotalRetailPrice = "TotalRetailPrice";
        public static string TotalAmountUsefulLife = "TotalAmountUsefulLife";
        public static string TotalAmountRentalTerm = "TotalAmountRentalTerm";

        public static string MonthlyPayment = "MonthlyPayment";
        public static string CustomerRate = "CustomerRate"; //AnnualInterestRate
        public static string CustomerRate2 = "CustomerRate2";//Annual Percentage Rate
        public static string TotalPayment = "TotalPayment";
        public static string TotalMonthlyPayment = "TotalMonthlyPayment";
        public static string Hst = "HST";
        public static string DownPayment = "DownPayment";
        public static string AdmeenFee = "AdmeenFee";
        public static string LoanTotalCashPrice = "LoanTotalCashPrice";
        public static string LoanAmountFinanced = "LoanAmountFinanced";
        public static string LoanTotalObligation = "LoanTotalObligation";
        public static string LoanBalanceOwing = "LoanBalanceOwing";
        public static string LoanTotalBorowingCost = "LoanTotalBorowingCost";

        public static string RequestedTerm = "RequestedTerm";
        public static string AmortizationTerm = "AmortizationTerm";
        public static string DeferralTerm = "DeferralTerm";
        public static string YesDeferral = "YesDeferral";
        public static string NoDeferral = "NoDeferral";

        public static string EquipmentQuantity = "EquipmentQuantity";
        public static string EquipmentDescription = "EquipmentDescription";
        public static string EquipmentCost = "EquipmentCost";
        //for fst eq only
        public static string IsEquipment = "IsEquipment";
        public static string EquipmentMonthlyRental = "EquipmentMonthlyRental";

        public static string InstallDate = "InstallDate";
        public static string HouseSize = "HouseSize";

        //Payment Fields
        public static string EnbridgeAccountNumber = "EnbridgeAccountNumber";
        public static string Ean = "EAN";
        public static string IsEnbridge = "IsEnbridge";
        public static string IsPAD = "IsPAD";
        public static string IsPAD1 = "IsPAD1";
        public static string IsPAD15 = "IsPAD15";
        public static string BankNumber = "BankNumber";
        public static string TransitNumber = "TransitNumber";
        public static string AccountNumber = "AccountNumber";
        public static string Bn = "BN";
        public static string Tn = "TN";
        public static string An = "BankAccNumber";

        //Dealer and SalesRep fields
        public static string SalesRep = "SalesRep";
        public static string DealerName = "DealerName";
        public static string DealerAddress = "DealerAddress";
        public static string DealerPhone = "DealerPhone";
        public static string DealerFax = "DealerFax";
        public static string DealerEmail = "DealerEmail";
        public static string DealerInitials = "ID1";

        public static string SalesRepInitiatedContact = "SalesRepInitiatedContact";
        public static string SalesRepNegotiatedAgreement = "SalesRepNegotiatedAgreement";
        public static string SalesRepConcludedAgreement = "SalesRepConcludedAgreement";

        //For Signed Installation Certificate
        public static string InstallerName = "InstallerName";
        public static string InstallationDate = "InstallationDate";
        public static string EquipmentModel = "EquipmentModel";
        public static string EquipmentSerialNumber = "EquipmentSerialNumber";

        public static string IsExistingEquipmentRental = "IsExistingEquipmentRental";
        public static string IsExistingEquipmentNoRental = "IsExistingEquipmentNoRental";
        public static string ExistingEquipmentRentalCompany = "ExistingEquipmentRentalCompany";
        public static string ExistingEquipmentMake = "ExistingEquipmentMake";
        public static string ExistingEquipmentModel = "ExistingEquipmentModel";
        public static string ExistingEquipmentSerialNumber = "ExistingEquipmentSerialNumber";
        public static string ExistingEquipmentGeneralCondition = "ExistingEquipmentGeneralCondition";
        public static string ExistingEquipmentAge = "ExistingEquipmentAge";

        // Existing Equipment
        public static string ExistingEquipmentRemovalCustomer = "ExistingEquipmentRemovalCustomer";
        public static string ExistingEquipmentRemovalSupplier = "ExistingEquipmentRemovalSupplier";
        public static string ExistingEquipmentRemovalOther = "ExistingEquipmentRemovalOther";
        public static string ExistingEquipmentRemovalOtherValue = "ExistingEquipmentRemovalOtherValue";
        public static string ExistingEquipmentRemovalNA = "ExistingEquipmentRemovalNA";

        //Bill 59 - Customer Existing agreements information
        public static string ExistingLoansDescription = "ExistingLoansDescription";
        public static string ExistingRentalsDescription = "ExistingRentalsDescription";
        public static string ExistingRentalsDateEntered = "ExistingRentalsDateEntered";
        public static string ExistingRentalsTerminationDate = "ExistingRentalsTerminationDate";

        //Custom envelop fields
        public static string ApplicationID = "ApplicationID";
        public static string DealerID = "DealerID";

        //QC Agreement Date fields
        public static string DateOfAgreement = "DateOfAgreement";
        public static string FirstPaymentDate = "FirstPaymentDate";
        public static string MonthlyPaymentDate = "MonthlyPaymentDate";
        public static string FinalPaymentDate = "FinalPaymentDate";

    }
}
