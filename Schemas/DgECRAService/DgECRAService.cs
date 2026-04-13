using System;
using System.IO;
using System.IO.Compression;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Threading;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Terrasoft.Configuration;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using Lookup = DgMasterData.DgLookupConst;
using DgSubmission.DgHistorySubmissionService;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;

namespace DgSubmission.DgSubmissionService
{
    public class ECRAService : SubmissionService
    {
        private string importId;
        protected WorkbookPart workbookPart;
        protected Worksheet currentSheet;
        protected string currentSheetId;
        protected string currentSheetName;
        
        public ECRAService(UserConnection UserConnection, string ImportId) : base(UserConnection)
        {
            this.importId = ImportId;
        }
        
        public virtual ECRAServiceResult Import()
        {
            var result = new ECRAServiceResult();
			
			using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
				dbExecutor.StartTransaction();
				
				try {
					using (SpreadsheetDocument doc = SpreadsheetDocument.Open(GetImportFile(), false)) {
						this.workbookPart = doc.WorkbookPart;

						GetCRMGroup();
						GetSubmission();
						GetLineDetail();
						Validation();

						if(!string.IsNullOrEmpty(this.crmGroup.GroupNo) || !string.IsNullOrEmpty(this.crmGroup.SubParentGroupNo)) {
							SetParentCRMGroup(dbExecutor);
							SetSubParentCRMGroup(dbExecutor);
							SetSubmissionIntegration();
						}

						CRMGroupSave();
						SubmissionSave(dbExecutor);
						LineDetailSave(dbExecutor);

						HistorySubmissionService.SubmitFromImport(
							UserConnection: UserConnection,
							SubmissionId: this.submission.Id,
							CreatedById: UserConnection.CurrentUser.ContactId
						);
					}

					result.UploadId = this.importId.ToString();
					result.SubmissionId = this.submission.Id != Guid.Empty ? this.submission.Id.ToString() : null;
					result.SerialNumber = this.submission.SerialNumber;
					result.Success = true;
					
					dbExecutor.CommitTransaction();
				}
				catch (Exception e) {
					result.Message = e.Message;
					dbExecutor.RollbackTransaction();
					// RollbackSubmission();
				}
			}

            return result;
        }

        public virtual string GetECRAType()
        {
            string type = string.Empty;
            try {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(GetImportFile(), true)) {
					type = GetType(doc);
                }
            } catch(Exception e) {
				throw;
            }
			
            return type;
        }
		
		protected string GetType(SpreadsheetDocument doc)
		{
			WorkbookPart workbookPart = doc.WorkbookPart;
			string sheetId = workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault()?.Id;
			Worksheet sheet = ((WorksheetPart)workbookPart.GetPartById(sheetId)).Worksheet;

            CoreFilePropertiesPart corePart = doc.CoreFilePropertiesPart;
            XmlDocument xmlProperties = new XmlDocument();
            xmlProperties.Load(corePart.GetStream());

            string title = xmlProperties.GetElementsByTagName("title", "http://purl.org/dc/elements/1.1/").Item(0).InnerText;
            var titleRef = new Dictionary<string, string>() {
            	{"ecra", "eCRA"},
				{"dsms", "Digi Business Company Registration Form"},
                {"m2m", "Digi Business Company Registration Form"},
                {"eira", "DiGi Business - Individual Registration Form"},
				{"dch", "DiGi Business - Individual Registration Form"}
			};

            var cellRef = new Dictionary<string, string>() {
				{"ecra", "R1"},
				{"dsms", "O1"},
				{"m2m", "O1"},
				{"eira", "AJ3"},
				{"dch", "AI3"}
			};
			
			var cellValueRef = new Dictionary<string, string>() {
				{"ecra", "Ref: Ver"},
                {"dsms", "RA DSMS"},
                {"m2m", "eCRA M2M"},
                {"eira", "Ref: Ver"},
				{"dch", "Ref: Ver DCH"}
			};

			foreach (string key in titleRef.Keys) {
				string cellVal = GetCellValue(doc, sheet, cellRef[key]);
				bool isCellValid = cellVal.IndexOf(cellValueRef[key], StringComparison.OrdinalIgnoreCase) >= 0;
				bool isTitleValid = title.IndexOf(titleRef[key], StringComparison.OrdinalIgnoreCase) >= 0;
				
				if(isCellValid && isTitleValid) {
					return key;
				}
			}
			
			return string.Empty;
		}

        public virtual string GetImportFile() 
        {
            string path = (string)SysSettings.GetValue(UserConnection, "DgLaravelDefaultStoragePath") + "ECRA";
            string fileName = this.importId + ".xlsx";
            return Path.Combine(path, fileName);
        }

        protected virtual void GetCRMGroup()
        {
            string sheetName = "ECRA Pg1";
            SetSheet(sheetName);

            this.crmGroup = new CRMGroup();

            this.crmGroup.GroupName = GetCellValue<string>(sheetName, "A19");
            this.crmGroup.GroupNo = GetCellValue<string>(sheetName, "AA55");
            this.crmGroup.SubParentGroupNo = GetCellValue<string>(sheetName, "AA56");
            this.crmGroup.SubParentGroupName = GetCellValue<string>(sheetName, "AA57");
            this.crmGroup.BRN = GetCellValue<string>(sheetName, "A21");

            string[] expiryBRNList = {
                GetCellValue<string>(sheetName, "L21"), 
                GetCellValue<string>(sheetName, "N21"), 
                GetCellValue<string>(sheetName, "P21")
            };
            string expiryBRNString = expiryBRNList
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("-", expiryBRNList) : null;

            try {
                if(!string.IsNullOrEmpty(expiryBRNString)) {
                    this.crmGroup.BRNExpiryDate = GetValidDateTime(expiryBRNString);
                }   
            } catch (Exception e) {
                throw new Exception($"Value in Sheet '{sheetName}' column 'L21, N21, P21': Expiry BRN is invalid ({expiryBRNString})): "+e.Message);
            }

            string[] dateIncorparationList = {
                GetCellValue<string>(sheetName, "J29"), 
                GetCellValue<string>(sheetName, "L29"), 
                GetCellValue<string>(sheetName, "N29")
            };
            string dateIncorparationString = dateIncorparationList
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("-", dateIncorparationList) : null;

            try {
                if(!string.IsNullOrEmpty(dateIncorparationString)) {
                    this.crmGroup.DateIncorparation = GetValidDateTime(dateIncorparationString);
                }
            } catch (Exception e) {
                throw new Exception($"Value in Sheet '{sheetName}' column 'J29, L29, N29' Date Incorparation is invalid ({dateIncorparationString})): "+e.Message);
            }
            
            this.crmGroup.CompanyIncorparation = GetCellValue<string>(sheetName, "L8");
            this.crmGroup.ExistingCustomerMobile = GetCellValue<string>(sheetName, "H9");
            this.crmGroup.DNO = GetCellValue<string>(sheetName, "D11");
            this.crmGroup.Others = GetCellValue<string>(sheetName, "P11");
            this.crmGroup.DNOIdType = GetCellValue<string>(sheetName, "H12");
            this.crmGroup.DNOIdNo = GetCellValue<string>(sheetName, "H13");
            this.crmGroup.DNOCompanyName = GetCellValue<string>(sheetName, "H14");
            this.crmGroup.DNOAccountNo = GetCellValue<string>(sheetName, "H15");

            string[] legalAddressArr = {
                GetCellValue<string>(sheetName, "A23"), 
                GetCellValue<string>(sheetName, "A24")
            };
            string legalAddress = legalAddressArr
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("\n", legalAddressArr) : legalAddressArr[0];
            this.crmGroup.LegalAddress = new Address() {
                StreetAddress = legalAddress,
                City = GetCellValue<string>(sheetName, "D25"),
                State = GetCellValue<string>(sheetName, "D26"),
                Country = GetCellValue<string>(sheetName, "D27"),
                PostCode = GetCellValue<string>(sheetName, "O25")
            };

            string[] billAddressArr = {
                GetCellValue<string>(sheetName, "A35"), 
                GetCellValue<string>(sheetName, "A36")
            };
            string billAddress = billAddressArr
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("\n", billAddressArr) : billAddressArr[0];
            this.crmGroup.BillAddress = new Address() {
                StreetAddress = billAddress,
                City = GetCellValue<string>(sheetName, "O36"),
                State = GetCellValue<string>(sheetName, "C37"),
                Country = GetCellValue<string>(sheetName, "C38"),
                PostCode = GetCellValue<string>(sheetName, "O37")
            };

            string[] deliveryAddressArr = {
                GetCellValue<string>(sheetName, "A35"), 
                GetCellValue<string>(sheetName, "A36")
            };
            string deliveryAddress = deliveryAddressArr
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("\n", deliveryAddressArr) : deliveryAddressArr[0];
            this.crmGroup.DeliveryAddress = new Address() {
                StreetAddress = deliveryAddress,
                City = GetCellValue<string>(sheetName, "O42"),
                State = GetCellValue<string>(sheetName, "C43"),
                Country = GetCellValue<string>(sheetName, "C44"),
                PostCode = GetCellValue<string>(sheetName, "O43")
            };

            this.crmGroup.Admin1 = new PIC() {
                Name = GetCellValue<string>(sheetName, "D46"),
                IdType = "NRIC",
                IdNo = GetCellValue<string>(sheetName, "D47"),
                MobileNo = GetCellValue<string>(sheetName, "D48"),
                TelNo = GetCellValue<string>(sheetName, "D50"),
                Email = GetCellValue<string>(sheetName, "G51"),
                Designation = GetCellValue<string>(sheetName, "D49")
            };

            this.crmGroup.Admin2 = new PIC() {
                Name = GetCellValue<string>(sheetName, "D56"),
                IdType = "NRIC",
                IdNo = GetCellValue<string>(sheetName, "D57"),
                MobileNo = GetCellValue<string>(sheetName, "D58"),
                TelNo = GetCellValue<string>(sheetName, "D60"),
                Designation = GetCellValue<string>(sheetName, "D59")
            };

            this.crmGroup.Auth1 = new PIC() {
                Name = GetCellValue<string>(sheetName, "U42"),
                IdType = "NRIC",
                IdNo = GetCellValue<string>(sheetName, "AA43"),
                MobileNo = GetCellValue<string>(sheetName, "AA44"),
                Email = GetCellValue<string>(sheetName, "AA46"),
                Designation = GetCellValue<string>(sheetName, "AA45")
            };
            this.crmGroup.Auth1.TelNo = this.crmGroup.Auth1.MobileNo;

            if(GetCheckboxValue("Check Box 6")) {
                this.crmGroup.Auth2 = new PIC() {
                    Name = this.crmGroup.Auth1.Name,
                    IdType = this.crmGroup.Auth1.IdType,
                    IdNo = this.crmGroup.Auth1.IdNo,
                    MobileNo = this.crmGroup.Auth1.MobileNo,
                    Email = this.crmGroup.Auth1.Email,
                    Designation = this.crmGroup.Auth1.Designation
                };
            }
        
            this.crmGroup.TelNo = GetCellValue<string>(sheetName, "D28");
            this.crmGroup.IndustrialSegment = GetCellValue<string>(sheetName, "J30");
            this.crmGroup.EnterpriseCustomerType = GetCellValue<string>(sheetName, "J31");
            this.crmGroup.AccountValue = GetCellValue<string>(sheetName, "AA59");
            this.crmGroup.DefaultSMS = GetCheckboxValue("Check Box 14");
            this.crmGroup.PaperChargeableStandard = GetCheckboxValue("Check Box 15");
            this.crmGroup.PaperChargeableItemised = GetCheckboxValue("Check Box 16");
            this.crmGroup.EmailBillWithPDF = GetCheckboxValue("Check Box 47");
            this.crmGroup.AutoBilling = GetCheckboxValue("Check Box 8");
            this.crmGroup.IsDevicePrice = GetCheckboxValue("Check Box 9");
			
			try {
				this.crmGroup.DevicePriceAmount = this.crmGroup.IsDevicePrice ? GetCellValue<decimal>(sheetName, "AF12") : 0;
			} catch(Exception e) {
				throw new Exception($"Value in Sheet '{sheetName}' column 'AF12': Device Price Amount is invalid. {e.Message}");
			}
            
            this.crmGroup.IsAdvancePaymentDeposit = GetCheckboxValue("Check Box 52");
			
			try {
				this.crmGroup.AdvancePaymentAmount = this.crmGroup.IsAdvancePaymentDeposit ? GetCellValue<decimal>(sheetName, "AF13") : 0;	
			} catch(Exception e) {
				throw new Exception($"Value in Sheet '{sheetName}' column 'AF13': Advance Payment Amount is invalid. {e.Message}");
			}
			
			this.crmGroup.BillMedium = GetParentBillMedium();
            this.crmGroup.BillType = GetParentBillType();
            this.crmGroup.BillCarrier = GetParentBillCarrier();
            this.crmGroup.BillDetail = GetParentBillDetail();
            this.crmGroup.BillingEmailAddress = GetCellValue<string>(sheetName, "G39");
            this.crmGroup.Language = "English";

            this.crmGroup.PrimaryOfferId = Guid.Parse("e2ea4aae-e3a5-41c8-aa39-d2e2672e57fb");
            this.crmGroup.SuppOffer1Id = Guid.Parse("0242fc0e-d5ca-4e79-9246-b497b1f3817c");
            this.crmGroup.SuppOffer2Id = Guid.Parse("506619aa-630d-4519-b0be-6a57ef0930a1");

            sheetName = "Line Registration Pg 1";
            SetSheet(sheetName);
            this.crmGroup.SalesChannel = GetCellValue<string>(sheetName, "D31");
            this.crmGroup.DealerName = GetCellValue<string>(sheetName, "D32");
            this.crmGroup.DealerCode = GetCellValue<string>(sheetName, "D33");
        }

        protected virtual void GetSubmission()
        {
            if(this.crmGroup == null) {
                throw new Exception("This method can only be called if the CRM Group property is already defined");
            }

            string sheetName = "ECRA Pg1";
            SetSheet(sheetName);

            this.submission = new Submission();
            this.submission.SourceId = Lookup.Source.ECRA;
            this.submission.SubmissionType = GetCellValue<string>(sheetName, "H10");
            this.submission.SubscriberType = "Corporate";
            this.submission.CompanyName = this.crmGroup.GroupName;
            this.submission.CustomerName = this.crmGroup.GroupName;
            this.submission.CMSId = GetCellValue<string>(sheetName, "AA58");

            string[] last4DigitCardNumber = {
                GetCellValue<string>(sheetName, "AB15"), 
                GetCellValue<string>(sheetName, "AD15"),
                GetCellValue<string>(sheetName, "AF15"),
                GetCellValue<string>(sheetName, "AH15")
            };
            this.submission.Last4DigitCardNumber = last4DigitCardNumber
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("", last4DigitCardNumber) : null;

            this.submission.IdNo = this.crmGroup.BRN;
            this.submission.IdType = "BRN";
            this.submission.Title = "Mr";
            this.submission.DateOfBirth = GetValidDateTime("3-6-1956");
            this.submission.Gender = "Male";

            sheetName = "Line Registration Pg 1";
            SetSheet(sheetName);
            this.submission.SubmissionRemark = GetCellValue<string>(sheetName, "K31");
            
			try {
				this.submission.TotalDevicePrice = GetCellValue<decimal>(sheetName, "CS30");
			} catch(Exception e) {
				throw new Exception($"Value in Sheet '{sheetName}' column 'CS30': Total Device Price is invalid. {e.Message}");
			}
			
			try {
				this.submission.TotalCreditExposure = GetCellValue<decimal>(sheetName, "CS31");
			} catch(Exception e) {
				throw new Exception($"Value in Sheet '{sheetName}' column 'CS31': Total Credit Exposure is invalid. {e.Message}");
			}
        }

        protected virtual void GetLineDetail()
        {
            if(this.submission == null) {
                throw new Exception("This method can only be called if the Submission property is already defined");
            }

            string sheetName = "Line Registration Pg 1";
            SetSheet(sheetName);

            this.lineDetail = new List<LineDetail>();
            int startRow = 5;
            int startCheckbox = 65;

            for (int i = 0; i < 25; i++) {
                if(startCheckbox == 71) {
                    startCheckbox += 2;
                }
                
                var line = new LineDetail();

                if(i == 0) {
                    this.submission.CardType = GetCellValue<string>(sheetName, $"CG{startRow}");
                    this.submission.CardOwnerName = GetCellValue<string>(sheetName, $"CH{startRow}");
                    this.submission.BankIssuer = GetCellValue<string>(sheetName, $"CI{startRow}");
                    this.crmGroup.PaymentMode = GetPaymentMode(GetCellValue<string>(sheetName, $"BD{startRow}"));
                }

                string msisdn = GetCellValue<string>(sheetName, $"C{startRow}");
                if(string.IsNullOrEmpty(msisdn)) {
                    startRow++;
                    startCheckbox++;
                    continue;
                }

                line.No = GetCellValue<int>(sheetName, $"A{startRow}");
                line.Username = GetCellValue<string>(sheetName, $"B{startRow}");
                line.MSISDN = msisdn;
                line.RatePlan = GetCellValue<string>(sheetName, $"D{startRow}");
                line.AdvancePayment = GetCellValue<string>(sheetName, $"K{startRow}");
                line.Vas = GetCellValue<string>(sheetName, $"P{startRow}");
                line.Tos = GetCellValue<string>(sheetName, $"AY{startRow}");
                line.Contract = GetCellValue<string>(sheetName, $"AZ{startRow}");
                line.PrMode = GetCellValue<string>(sheetName, $"BA{startRow}");
				
				try {
					line.CreditLimit = GetCellValue<decimal>(sheetName, $"BB{startRow}");
				} catch(Exception e) {
					throw new Exception($"Value in Sheet '{sheetName}' column 'BB{startRow}': Credit Limit is invalid. {e.Message}");
				}

                string autoBillingString = GetCellValue<string>(sheetName, $"BC{startRow}");
                line.AutoBilling = string.IsNullOrEmpty(autoBillingString) ? false : autoBillingString.ToLower() == "yes" ? true : false;

                line.PhoneModel = GetCellValue<string>(sheetName, $"BF{startRow}");
                line.Remark = GetCellValue<string>(sheetName, $"BP{startRow}");
                line.DeviceBundleType = GetCellValue<string>(sheetName, $"BQ{startRow}");
                line.BillMedium = GetCellValue<string>(sheetName, $"BR{startRow}");
                line.Imsi = GetCellValue<string>(sheetName, $"BS{startRow}");
                line.SimCardSerialNo = GetCellValue<string>(sheetName, $"BU{startRow}");
                
                string conceptPaperString = GetCellValue<string>(sheetName, $"BV{startRow}");
                line.ConceptPaper = string.IsNullOrEmpty(conceptPaperString) ? false : conceptPaperString.ToLower() == "yes" ? true : false;
                
                line.Bundle1 = GetCellValue<string>(sheetName, $"BW{startRow}");
                line.Element1 = GetCellValue<string>(sheetName, $"BX{startRow}");
                line.Bundle2 = GetCellValue<string>(sheetName, $"BY{startRow}");
                line.Element2 = GetCellValue<string>(sheetName, $"BZ{startRow}");
                line.Bundle3 = GetCellValue<string>(sheetName, $"CA{startRow}");
                line.Element3_1 = GetCellValue<string>(sheetName, $"CB{startRow}");
                line.Element3_2 = GetCellValue<string>(sheetName, $"CC{startRow}");
                line.Element3_3 = GetCellValue<string>(sheetName, $"CD{startRow}");
                line.GoDigiPro = GetCheckboxValue($"Check Box {startCheckbox}");
                line.RemoveIDD = GetCellValue<string>(sheetName, $"CF{startRow}");
                line.PromoCode = GetCellValue<string>(sheetName, $"CR{startRow}");
				
				try {
					line.DigiSellingPrice = GetCellValue<decimal>(sheetName, $"CS{startRow}");
				} catch(Exception e) {
					throw new Exception($"Value in Sheet '{sheetName}' column 'CS{startRow}': Digi Selling Price is invalid. {e.Message}");
				}
                
				try {
					line.MonthlyRental = GetCellValue<decimal>(sheetName, $"CT{startRow}");
				} catch(Exception e) {
					throw new Exception($"Value in Sheet '{sheetName}' column 'CT{startRow}': Monthly Rental is invalid. {e.Message}");
				}
				
				try {
					line.CrExposure = GetCellValue<decimal>(sheetName, $"CU{startRow}");
				} catch(Exception e) {
					throw new Exception($"Value in Sheet '{sheetName}' column 'CU{startRow}': CR Exposure is invalid. {e.Message}");
				}
                
				line.MandatoryOffer1 = GetCellValue<string>(sheetName, $"E{startRow}");
                line.MandatoryOffer2 = GetCellValue<string>(sheetName, $"F{startRow}");
                line.MandatoryOffer3 = GetCellValue<string>(sheetName, $"G{startRow}");
                line.DataBundle = GetCellValue<string>(sheetName, $"H{startRow}");
                line.DataElement = GetCellValue<string>(sheetName, $"I{startRow}");
                line.AdvBundle = GetCellValue<string>(sheetName, $"J{startRow}");
                line.ContractBundle = GetCellValue<string>(sheetName, $"L{startRow}");
                line.ContractElement = GetCellValue<string>(sheetName, $"M{startRow}");
                line.Automatic10 = GetCellValue<string>(sheetName, $"N{startRow}");
                line.Automatic11 = GetCellValue<string>(sheetName, $"O{startRow}");
                line.CpBundle1 = GetCellValue<string>(sheetName, $"Q{startRow}");
                line.CpElement1 = GetCellValue<string>(sheetName, $"R{startRow}");
                line.CpBunlde2 = GetCellValue<string>(sheetName, $"S{startRow}");
                line.CpElement2 = GetCellValue<string>(sheetName, $"T{startRow}");
                line.CpBundle3 = GetCellValue<string>(sheetName, $"U{startRow}");
                line.CpElement31 = GetCellValue<string>(sheetName, $"V{startRow}");
                line.CpElement32 = GetCellValue<string>(sheetName, $"W{startRow}");
                line.CpElement3_3 = GetCellValue<string>(sheetName, $"X{startRow}");

                startCheckbox++;
                startRow++;

                this.lineDetail.Add(line);
            }
        }

        protected virtual void SetSheet(string SheetName)
        {
            this.currentSheetName = SheetName;
            this.currentSheetId = this.workbookPart.Workbook.Descendants<Sheet>()
                .FirstOrDefault(s => s.Name.Equals(SheetName))?.Id;
            this.currentSheet = ((WorksheetPart)this.workbookPart.GetPartById(this.currentSheetId)).Worksheet;
        }

        // perbaiki return untuk tipe data lainnya
        protected virtual T GetCellValue<T>(string SheetName, string AddressName)
        {
            string value = string.Empty;
            if(this.currentSheetName != SheetName) {
                SetSheet(SheetName);
            }

            Cell cell = this.currentSheet.Descendants<Cell>()
                .Where(c => c.CellReference == AddressName)
                .FirstOrDefault();

            value = cell.InnerText.Trim();
            if(cell.InnerText.Length == 0) {
                if(typeof(T) == typeof(decimal) || typeof(T) == typeof(int)) {
                    value = "0";
                }

                return (T) Convert.ChangeType(value, typeof(T));
            }

            if(cell.CellFormula != null && !string.IsNullOrEmpty(cell.CellFormula.Text.Trim())) {
                value = cell.CellValue.Text.Trim();
                if((typeof(T) == typeof(decimal) || typeof(T) == typeof(int)) && string.IsNullOrEmpty(cell.CellValue.Text.Trim())) {
                    value = "0";
                }
                return (T) Convert.ChangeType(value, typeof(T));
            }
            
            if(cell.DataType == null) {
                if((typeof(T) == typeof(decimal) || typeof(T) == typeof(int)) && string.IsNullOrEmpty(value)) {
                    value = "0";
                }
                return (T) Convert.ChangeType(value, typeof(T));
            }
            
            switch (cell.DataType.Value) {
                case CellValues.SharedString:
                    value = this.workbookPart.SharedStringTablePart.SharedStringTable.ChildElements.GetItem(int.Parse(value)).InnerText.Trim();
                    break;

                case CellValues.Boolean:
                    switch (value) {
                        case "0":
                            value = "FALSE";
                            break;
                        default:
                            value = "TRUE";
                            break;
                    }
                    break;
            }

            return (T) Convert.ChangeType(value, typeof(T));
        }

        protected string GetCellValue(SpreadsheetDocument doc, Worksheet sheet, string cellName)
        {
            string value = string.Empty;

            Cell cell = sheet.Descendants<Cell>()
                .Where(c => c.CellReference == cellName)
                .FirstOrDefault();

            if(cell.InnerText.Length == 0) {
                return value.Trim();
            }

            if(cell.CellFormula != null && !string.IsNullOrEmpty(cell.CellFormula.Text)) {
                return cell.CellValue.Text.Trim();
            }
			
			if(cell.DataType.Value == CellValues.SharedString) {
				return doc.WorkbookPart.SharedStringTablePart.SharedStringTable.ChildElements.GetItem(int.Parse(cell.InnerText)).InnerText.Trim();
			}

            return cell.InnerText.Trim();
        }

        protected virtual bool GetCheckboxValue(string ElementName)
        {   
            var wsPart = ((WorksheetPart)this.workbookPart.GetPartById(this.currentSheetId));
            var controlElement = this.currentSheet.Descendants<Control>()
                .Where(item => item.Name.Value == ElementName)
                .FirstOrDefault();
            if(controlElement == null) {
                throw new Exception($"Checkbox {ElementName} not found in sheet");
            }

            StringValue controlId = controlElement.Id;
            var controlProperies = (ControlPropertiesPart)wsPart.GetPartById(controlId);

            return controlProperies.FormControlProperties.Checked == "Checked";
        }
    }
	
    public class ECRAServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string UploadId { get; set; }
        public string SubmissionId { get; set; }
        public string SerialNumber { get; set; }
    }
}