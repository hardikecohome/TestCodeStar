using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Utility;
using DealnetPortal.Api.Models.Scanning;
using Inlite.ClearImageNet;

namespace DealnetPortal.Api.Integration.Services
{
    /// <summary>
    /// Read, recognize and extract data from images manager
    /// </summary>
    public class ImageScanService
    {
        /// <summary>
        /// Recognize Driver License image
        /// </summary>
        /// <param name="scanningRequest">Scanned image request</param>
        /// <returns></returns>
        public Tuple<DriverLicenseData, IList<Alert>>  ReadDriverLicense(ScanningRequest scanningRequest)
        {
            List<Alert> alerts = new List<Alert>();
            DriverLicenseData driverLicense = new DriverLicenseData();

            if (scanningRequest?.ImageForReadRaw != null)
            {
                string aamva = string.Empty;
                try
                {
                    ImageEditor repair = new ImageEditor();
                    MemoryStream ms = new MemoryStream(scanningRequest.ImageForReadRaw);
                    repair.Image.Open(ms, 0);

                    Barcode[] barcodes = null;
                    BarcodeReader reader = new BarcodeReader()
                    {
                        Horizontal = true,
                        Vertical = true,
                        Diagonal = true,
                        DrvLicID = true,
                    };

                    try
                    {
                        repair.AutoDeskew();
                        repair.AutoRotate();
                        repair.CleanNoise(3);
                        barcodes = reader.Read(repair);
                    }
                    catch (Exception ex)
                    {
                        alerts.Add(new Alert()
                        {
                            Type = AlertType.Warning,
                            Header = "Driver Scan Issue",
                            Message = ex.ToString()
                        });

                        var bMs = new MemoryStream(scanningRequest.ImageForReadRaw);
                        barcodes = reader.Read(bMs);
                    }                    
                                        
                    aamva = barcodes.First().Decode(BarcodeDecoding.aamva);
                }
                catch (Exception ex)
                {
                    alerts.Add(new Alert() { Type = AlertType.Error, Header = "Can't recognize license", Message = ex.ToString() });
                    return new Tuple<DriverLicenseData, IList<Alert>>(driverLicense, alerts);
                }
                
                if (!string.IsNullOrEmpty(aamva))
                {
                    try
                    {
                        DriverLicenseParser parser = new DriverLicenseParser(aamva);
                        driverLicense = parser.DriverLicense;
                    }
                    catch (Exception ex)
                    {
                        alerts.Add(new Alert() { Type = AlertType.Error, Header = "Can't parse recognized license", Message = ex.ToString() });
                    }
                }
                else
                {
                    alerts.Add(new Alert() { Type = AlertType.Error, Header = "Can't recognize license", Message = "Image wasn't recognized" });
                }
            }
            else
            {
                alerts.Add(new Alert() {Type = AlertType.Error, Header = "Can't recognize license", Message = "Data for recognize is empty"});
            }
            return new Tuple<DriverLicenseData, IList<Alert>>(driverLicense, alerts);
        }

        /// <summary>
        /// Recognize Void Cheque image
        /// </summary>
        /// <param name="scanningRequest">Scanned image request</param>
        /// <returns></returns>
        public Tuple<VoidChequeData, IList<Alert>> ReadVoidCheque(ScanningRequest scanningRequest)
        {
            List<Alert> alerts = new List<Alert>();
            VoidChequeData chequeData = new VoidChequeData();

            if (scanningRequest?.ImageForReadRaw != null)
            {
                IntPtr hBitmap = IntPtr.Zero;
                try
                {
                    Bitmap bmp;

                    ImageEditor repair = new ImageEditor();
                    MemoryStream ms = new MemoryStream(scanningRequest.ImageForReadRaw);
                    repair.Image.Open(ms, 0);
                    repair.AutoDeskew();
                    repair.AutoRotate();
                    repair.CleanNoise(3);
                    
                    bmp = repair.Bitmap;

                    hBitmap = bmp.GetHbitmap();
                    ClearMicr.CcMicrReader reader = new ClearMicr.CcMicrReader();
                    reader.Flags = ClearMicr.EMicrReaderFlags.emrfExtendedMicrSearch;
                    reader.Image.OpenFromBitmap(hBitmap.ToInt32());
                    int cnt = reader.FindMICR();
                    if (cnt > 0)
                    {
                        try
                        {
                            chequeData.AccountNumber = reader.MicrLine[1].Account.TextANSI;
                            var routingNumbers = (reader.MicrLine[1].Routing.TextANSI +
                                                       reader.MicrLine[1].RoutingChecksum.TextANSI).Split('-');

                            chequeData.TransitNumber = routingNumbers[0];
                            chequeData.BankNumber = routingNumbers.Length > 1 ? routingNumbers[1] : null;
                        }
                        catch (Exception ex)
                        {
                            alerts.Add(new Alert() { Type = AlertType.Error, Header = "Can't obtain recognized data", Message = ex.ToString() });
                        }
                    }
                    else
                    {
                        alerts.Add(new Alert() { Type = AlertType.Error, Header = "Can't recognize cheque", Message = "Image wasn't recognized" });
                    }
                }
                catch (Exception ex)
                {
                    alerts.Add(new Alert() { Type = AlertType.Error, Header = "Can't recognize cheque", Message = ex.ToString() });
                }
                finally
                {
                    if (hBitmap != IntPtr.Zero)
                    {
                        DeleteObject(hBitmap);
                    }
                }
            }
            else
            {
                alerts.Add(new Alert() { Type = AlertType.Error, Header = "Can't recognize cheque", Message = "Data for recognize is empty" });
            }
            return new Tuple<VoidChequeData, IList<Alert>>(chequeData, alerts);
        }

        public Tuple<VoidChequeData, IList<Alert>> ExtractCheck(ScanningRequest scanningRequest)
        {
            List<Alert> alerts = new List<Alert>();
            VoidChequeData chequeData = new VoidChequeData();

            IntPtr hBitmap = IntPtr.Zero;
            try
            {
                // Open Image
                ClearMicr.CcMicrReader micrReader = new ClearMicr.CcMicrReader();
                
                Bitmap bmp;
                var ms = new MemoryStream(scanningRequest.ImageForReadRaw);
                bmp = new Bitmap(ms);
                hBitmap = bmp.GetHbitmap();

                // don't recognize OpenFromBitmap ??
                micrReader.Image.OpenFromBitmap(hBitmap.ToInt32());
                micrReader.Flags = ClearMicr.EMicrReaderFlags.emrfNoExtractDeskew;
                // Do actual reading      
                ClearImage.CiImage oImage;
                oImage = micrReader.ExtractCheck();

                if (oImage != null && micrReader.MicrCount > 0)
                {
                    try
                    {
                        chequeData.AccountNumber = micrReader.MicrLine[1].Account.TextANSI;
                        var routingNumbers = (micrReader.MicrLine[1].Routing.TextANSI +
                                              micrReader.MicrLine[1].RoutingChecksum.TextANSI).Split('-');

                        chequeData.TransitNumber = routingNumbers[0];
                        chequeData.BankNumber = routingNumbers.Length > 1 ? routingNumbers[1] : null;
                    }
                    catch (Exception ex)
                    {
                        alerts.Add(new Alert() { Type = AlertType.Error, Header = "Can't obtain recognized data", Message = ex.ToString() });
                    }
                }
                else
                {
                    alerts.Add(new Alert() { Type = AlertType.Error, Header = "Can't recognize cheque", Message = "Image wasn't recognized" });
                }               
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert() { Type = AlertType.Error, Header = "Can't recognize cheque", Message = ex.ToString() });
            }
            finally
            {
                if (hBitmap != IntPtr.Zero)
                {
                    DeleteObject(hBitmap);
                }
            }
            return new Tuple<VoidChequeData, IList<Alert>>(chequeData, alerts);
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
    }
}
