using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace DealnetPortal.Api.Common.Helpers
{
    public static class LoanCalculator
    {
        public class Input
        {
            public double TaxRate { get; set; } 
            public int LoanTerm { get; set; }
            public int AmortizationTerm { get; set; }
            public double CustomerRate { get; set; }
            //Price of equipment(s) excl tax 
            public double PriceOfEquipment { get; set; }
            public double AdminFee { get; set; }
            public double DownPayment { get; set; }
            //can be as input data (for clarity program)
            public bool? IsClarity { get; set; }
            public bool IsOldClarityDeal { get; set; }
        }

        public class Output
        {
            public double Hst { get; set; }
            //Price of equipment (incl tax)
            public double PriceOfEquipmentWithHst { get; set; }
            public double TotalAmountFinanced { get; set; }
            //TODO: why double, not decimal?
            //TODO: because methods from Financial class outputs value in double
            public double TotalMonthlyPayment { get; set; }
            //Monthly cost of ownership - for clarity = MOC reduced by Down payment
            public double TotalMCO { get; set; }
            public double AnnualPercentageRate { get; set; }
            public double TotalAllMonthlyPayments { get; set; }
            public double ResidualBalance { get; set; }
            public double TotalObligation { get; set; }
            public double TotalBorowingCost { get; set; }

            public double LoanTotalCashPrice { get; set; }
        }

        public static Output Calculate(Input input)
        {            
            var output = new Output();
            output.Hst = input.TaxRate/100*input.PriceOfEquipment;
            output.PriceOfEquipmentWithHst = output.Hst + input.PriceOfEquipment;
            var customerRate = input.CustomerRate / 100 / 12;
            var mco = 0.0;
            var admeenFee = input.AdminFee;
            if (input.IsClarity == true)
            {
                if (input.IsOldClarityDeal)
                {
                    output.TotalAmountFinanced = input.PriceOfEquipment + admeenFee - input.DownPayment;
                    //output.PriceOfEquipmentWithHst /*+ input.AdminFee*/ - input.DownPayment;
                    output.TotalMonthlyPayment = customerRate == 0 && input.AmortizationTerm == 0 ? 0 :
                        customerRate > 0 ? Math.Round(output.TotalAmountFinanced * Financial.Pmt(customerRate, input.AmortizationTerm, -1), 2)
                            : output.TotalAmountFinanced * Financial.Pmt(customerRate, input.AmortizationTerm, -1);
                    output.TotalAllMonthlyPayments = Math.Round(output.TotalMonthlyPayment, 2) * input.LoanTerm;
                    mco = output.TotalMonthlyPayment;                    
                }
                else
                {
                    admeenFee = 0.0;
                    const double clarityPaymentFactor = 0.010257;
                    output.TotalMonthlyPayment = output.PriceOfEquipmentWithHst;                             
                    output.PriceOfEquipmentWithHst = output.TotalMonthlyPayment / clarityPaymentFactor;
                    output.TotalAmountFinanced = output.PriceOfEquipmentWithHst + admeenFee - input.DownPayment;
                    mco = Math.Round(output.TotalMonthlyPayment, 2) - input.DownPayment * clarityPaymentFactor;
                    output.TotalAllMonthlyPayments = Math.Round(mco, 2) * input.LoanTerm;                    
                }
                output.TotalMCO = mco;
            }
            else
            {
                output.TotalAmountFinanced = input.PriceOfEquipment + admeenFee - input.DownPayment;
                output.TotalMonthlyPayment = customerRate == 0 && input.AmortizationTerm == 0 ? 0 :
                    customerRate > 0 ? Math.Round(output.TotalAmountFinanced * Financial.Pmt(customerRate, input.AmortizationTerm, -1), 2)
                        : output.TotalAmountFinanced * Financial.Pmt(customerRate, input.AmortizationTerm, -1);                
                output.TotalAllMonthlyPayments = Math.Round(output.TotalMonthlyPayment, 2) * input.LoanTerm;
                mco = output.TotalMonthlyPayment;
                output.TotalMCO = mco;

                if (customerRate == 0.0 && input.AmortizationTerm == input.LoanTerm)
                {
                    //logic for Equal payments program (0%)
                    output.TotalAllMonthlyPayments = output.TotalAmountFinanced;
                    output.AnnualPercentageRate = 0.0;
                }
                else
                {                    
                    try
                    {
                        output.AnnualPercentageRate = Financial.Rate(input.AmortizationTerm, -mco, output.TotalAmountFinanced - admeenFee) * 1200;
                    }
                    catch (ArgumentException)
                    {
                        //sometimes we get exception here
                        output.AnnualPercentageRate = 0.0;
                    }                    
                }                
            }
            output.LoanTotalCashPrice = output.TotalAmountFinanced - admeenFee + input.DownPayment;
            if (input.LoanTerm != input.AmortizationTerm)
            {
                output.ResidualBalance = Math.Round(Financial.FV(customerRate, input.LoanTerm, Math.Round(mco, 2), -output.TotalAmountFinanced), 2);
            }
            output.TotalObligation = output.ResidualBalance + output.TotalAllMonthlyPayments; // + admeenFee;
            output.TotalBorowingCost = Math.Round(output.TotalObligation - output.TotalAmountFinanced + admeenFee, 2);
            return output;
        }
    }
}
