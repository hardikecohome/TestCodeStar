using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Aspire.Integration.Storage;
using DealnetPortal.Domain;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace DealnetPortal.Api.Integration.Utility
{
    public static class XlsxExporter
    {
        public static void Export(IEnumerable<ContractDTO> contracts, Stream stream, List<ProvinceTaxRate> provincialTaxRates)
        {
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add(Resources.Resources.Reports);
                worksheet.Cells[1, 1].Value = Resources.Resources.Contract + " #";
                worksheet.Cells[1, 2].Value = Resources.Resources.Date;
                worksheet.Cells[1, 3].Value = Resources.Resources.CustomerFirstName;
                worksheet.Cells[1, 4].Value = Resources.Resources.CustomerLastName;
                worksheet.Cells[1, 5].Value = Resources.Resources.Address;
                worksheet.Cells[1, 6].Value = Resources.Resources.City;
                worksheet.Cells[1, 7].Value = Resources.Resources.Province;
                worksheet.Cells[1, 8].Value = Resources.Resources.PostalCode;
                worksheet.Cells[1, 9].Value = Resources.Resources.Phone;
                worksheet.Cells[1, 10].Value = Resources.Resources.Email;
                worksheet.Cells[1, 11].Value = Resources.Resources.PaymentType;
                worksheet.Cells[1, 12].Value = Resources.Resources.Equipment;
                worksheet.Cells[1, 13].Value = Resources.Resources.DealerInvoiceAmount;
                worksheet.Cells[1, 14].Value = Resources.Resources.DownPayment;
                worksheet.Cells[1, 15].Value = Resources.Resources.TotalAmountFinanced;
                worksheet.Cells[1, 16].Value = Resources.Resources.MonthlyPayment;
                worksheet.Cells[1, 17].Value = Resources.Resources.TotalObligation;
                worksheet.Cells[1, 18].Value = Resources.Resources.CostOfBorrowing;
                worksheet.Cells[1, 19].Value = Resources.Resources.Dealer;
                worksheet.Cells[1, 20].Value = Resources.Resources.Status;
                worksheet.Cells[1, 21].Value = Resources.Resources.Type;
                worksheet.Cells[1, 22].Value = Resources.Resources.SalesRep;
                var counter = 1;
                foreach (var contract in contracts)
                {
                    counter++;
                    worksheet.Cells[counter, 1].Value = contract.Details?.TransactionId ?? contract.Id.ToString();
                    worksheet.Cells[counter, 2].Value = contract.CreationTime.ToString(CultureInfo.CurrentCulture);
                    worksheet.Cells[counter, 3].Value = contract.PrimaryCustomer?.FirstName;
                    worksheet.Cells[counter, 4].Value = contract.PrimaryCustomer?.LastName;
                    worksheet.Cells[counter, 5].Value = contract.PrimaryCustomer?.Locations.FirstOrDefault(ad => ad.AddressType == AddressType.MainAddress).Street;
                    worksheet.Cells[counter, 6].Value = contract.PrimaryCustomer?.Locations.FirstOrDefault(ad => ad.AddressType == AddressType.MainAddress).City;
                    worksheet.Cells[counter, 7].Value = contract.PrimaryCustomer?.Locations.FirstOrDefault(ad => ad.AddressType == AddressType.MainAddress).State;
                    worksheet.Cells[counter, 8].Value = contract.PrimaryCustomer?.Locations.FirstOrDefault(ad => ad.AddressType == AddressType.MainAddress).PostalCode;
                    worksheet.Cells[counter, 9].Value = contract.PrimaryCustomer?.Phones?.FirstOrDefault(ph => ph.PhoneType == PhoneType.Cell)?.PhoneNum ?? contract.PrimaryCustomer?.Phones?.FirstOrDefault(ph => ph.PhoneType == PhoneType.Home)?.PhoneNum;
                    worksheet.Cells[counter, 10].Value =
                        contract.PrimaryCustomer?.Emails?.FirstOrDefault(e => e.EmailType == EmailType.Main)?.EmailAddress;
                    worksheet.Cells[counter, 11].Value = contract.PaymentInfo?.PaymentType.GetEnumDescription();
                    worksheet.Cells[counter, 12].Value = contract.Equipment?.NewEquipment?.Select(eq => eq.TypeDescription).ConcatWithComma();
                    if (contract.Equipment != null && contract.Equipment.AmortizationTerm != null)
                    {
                        if (contract.Equipment.AgreementType == AgreementType.LoanApplication)
                        {
                            
                                var priceOfEquipment = 0.0m;
                                var TaxRate = 0.0;
                                if (contract.Equipment?.IsClarityProgram == true)
                                {
                                    //for Clarity program we have different logic
                                    priceOfEquipment =
                                        (contract.Equipment?.NewEquipment?
                                                      .Sum(x => x.MonthlyCost) ?? 0.0m);

                                    TaxRate = provincialTaxRates.FirstOrDefault(p => p.Province == contract.PrimaryCustomer?.Locations?.FirstOrDefault(m => m.AddressType == AddressType.MainAddress).State).Rate;
                                    var packages =
                                        (contract.Equipment?.InstallationPackages?.Sum(x => x.MonthlyCost) ?? 0.0m);
                                    priceOfEquipment += packages;
                                }
                                else
                                {
                                    priceOfEquipment = contract.Equipment?.NewEquipment
                                                           ?.Sum(x => x.Cost) ?? 0;
                                }
                            
                            var loanCalculatorInput = new LoanCalculator.Input
                            {
                                TaxRate = TaxRate,
                                LoanTerm = contract.Equipment.LoanTerm ?? 0,
                                AmortizationTerm = contract.Equipment.AmortizationTerm ?? 0,
                                PriceOfEquipment = (double)priceOfEquipment,
                                AdminFee = contract.Equipment?.IsFeePaidByCutomer == true ? (double)(contract.Equipment?.AdminFee ?? 0) : 0.0,
                                DownPayment = (double)(contract.Equipment.DownPayment ?? 0),
                                CustomerRate = (double)(contract.Equipment.CustomerRate ?? 0),
                                IsClarity = contract.Equipment.IsClarityProgram,

                            };
                            var loanCalculatorOutput = LoanCalculator.Calculate(loanCalculatorInput);
                            worksheet.Cells[counter, 13].Value = loanCalculatorOutput.PriceOfEquipmentWithHst;
                            worksheet.Cells[counter, 14].Value = contract.Equipment?.DownPayment ?? null;
                            worksheet.Cells[counter, 15].Value = loanCalculatorOutput.TotalAmountFinanced;
                            worksheet.Cells[counter, 16].Value = Math.Round(loanCalculatorOutput.TotalMonthlyPayment, 2);
                            worksheet.Cells[counter, 17].Value = loanCalculatorOutput.TotalObligation;
                            worksheet.Cells[counter, 18].Value = loanCalculatorOutput.TotalBorowingCost;
                            worksheet.SelectedRange[counter, 13, counter, 18].Style.Numberformat.Format = "##0.00";
                        }                        
                    }
                    else
                    {
                        if (contract.Equipment?.ValueOfDeal != null)
                        {
                            worksheet.Cells[counter, 15].Value = Math.Round(contract.Equipment.ValueOfDeal.Value, 2);
                            worksheet.Cells[counter, 15].Style.Numberformat.Format = "##0.00";
                        }
                    }

                    worksheet.Cells[counter, 19].Value = contract.DealerName;
                    worksheet.Cells[counter, 20].Value = contract.Details?.LocalizedStatus ?? contract.ContractState.GetEnumDescription();
                    worksheet.Cells[counter, 21].Value = contract.Equipment?.AgreementType.GetEnumDescription();
                    worksheet.Cells[counter, 22].Value = contract.Equipment?.SalesRep;
                    
                }
                worksheet.Cells[1, 1, 1, 22].Style.Font.Bold = true;
                worksheet.Cells[1, 1, 1, 22].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                for (var i = 1; i <= 22; i++)
                {
                    worksheet.Column(i).Width = worksheet.Column(i).Width + 1;
                }
                
                package.Save();
            }
        }
    }
}
