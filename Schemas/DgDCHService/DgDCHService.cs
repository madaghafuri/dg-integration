using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
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
using ISAEntityHelper.EntityHelper;

namespace DgSubmission.DgSubmissionService
{
    public class DCHService : EIRAService
    {
        public DCHService(UserConnection UserConnection, string ImportId) : base(UserConnection, ImportId)
        {}

        protected override void GetCRMGroup()
        {
            string sheetName = "eIRA";
            SetSheet(sheetName);

            this.crmGroup = new CRMGroup();

            this.crmGroup.GroupName = GetCellValue<string>(sheetName, "A23");
            this.crmGroup.SubParentGroupNo = GetCellValue<string>(sheetName, "Z55");
            this.crmGroup.SubParentGroupName = GetCellValue<string>(sheetName, "Z54");
            this.crmGroup.BRN = GetCellValue<string>(sheetName, "H24");
            
            this.crmGroup.DNO = GetCellValue<string>(sheetName, "C8");
            this.crmGroup.Others = GetCellValue<string>(sheetName, "P8");
            this.crmGroup.DNOIdType = GetCellValue<string>(sheetName, "E9");
            this.crmGroup.DNOIdNo = GetCellValue<string>(sheetName, "E10");
            this.crmGroup.DNOCompanyName = GetCellValue<string>(sheetName, "H11");
            this.crmGroup.DNOAccountNo = GetCellValue<string>(sheetName, "H12");

            string[] legalAddressArr = {
                GetCellValue<string>(sheetName, "A28"), 
                GetCellValue<string>(sheetName, "A29")
            };
            string legalAddress = legalAddressArr
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("\n", legalAddressArr) : legalAddressArr[0];
            this.crmGroup.LegalAddress = new Address() {
                StreetAddress = legalAddress,
                City = GetCellValue<string>(sheetName, "O29"),
                State = GetCellValue<string>(sheetName, "C30"),
                Country = GetCellValue<string>(sheetName, "C31"),
                PostCode = GetCellValue<string>(sheetName, "O30")
            };

            this.crmGroup.BillAddress = new Address() {
                StreetAddress = this.crmGroup.LegalAddress.StreetAddress,
                City = this.crmGroup.LegalAddress.City,
                State = this.crmGroup.LegalAddress.State,
                Country = this.crmGroup.LegalAddress.Country,
                PostCode = this.crmGroup.LegalAddress.PostCode
            };

            string[] deliveryAddressArr = {
                GetCellValue<string>(sheetName, "A34"), 
                GetCellValue<string>(sheetName, "A35")
            };
            string deliveryAddress = deliveryAddressArr
                .Where(item => string.IsNullOrEmpty(item))
                .ToArray()
                .Length == 0 ? string.Join("\n", deliveryAddressArr) : deliveryAddressArr[0];
            this.crmGroup.DeliveryAddress = new Address() {
                StreetAddress = deliveryAddress,
                City = GetCellValue<string>(sheetName, "O35"),
                State = GetCellValue<string>(sheetName, "C36"),
                Country = GetCellValue<string>(sheetName, "C37"),
                PostCode = GetCellValue<string>(sheetName, "O36")
            };

            this.crmGroup.Admin1 = new PIC() {
                Name = GetCellValue<string>(sheetName, "A17"),
                IdType = GetCellValue<string>(sheetName, "C18"),
                IdNo = GetCellValue<string>(sheetName, "J18"),
                MobileNo = GetCellValue<string>(sheetName, "J21"),
                Email = GetCellValue<string>(sheetName, "J20")
            };
            this.crmGroup.Admin1.TelNo = this.crmGroup.Admin1.MobileNo;

            this.crmGroup.Auth1 = new PIC() {
                Name = this.crmGroup.Admin1.Name,
                IdType = this.crmGroup.Admin1.IdType,
                IdNo = this.crmGroup.Admin1.IdNo,
                MobileNo = this.crmGroup.Admin1.MobileNo,
                TelNo = this.crmGroup.Admin1.TelNo,
                Email = this.crmGroup.Admin1.Email,
                Designation = this.crmGroup.Admin1.Designation
            };
        
            this.crmGroup.TelNo = this.crmGroup.Admin1.MobileNo;
            // this.crmGroup.AutoBilling = GetCheckboxValue("Check Box 15");
            // this.crmGroup.IsDevicePrice = GetCheckboxValue("Check Box 16");
            // this.crmGroup.DevicePriceAmount = this.crmGroup.IsDevicePrice ? GetCellValue<decimal>(sheetName, "L47") : 0;
            // this.crmGroup.IsAdvancePaymentDeposit = GetCheckboxValue("Check Box 17");
            // this.crmGroup.AdvancePaymentAmount = this.crmGroup.IsAdvancePaymentDeposit ? GetCellValue<decimal>(sheetName, "L48") : 0;
            this.crmGroup.BillingEmailAddress = this.crmGroup.Admin1.Email;
            this.crmGroup.Language = "English";

            this.crmGroup.PrimaryOfferId = Guid.Parse("e2ea4aae-e3a5-41c8-aa39-d2e2672e57fb");
            this.crmGroup.SuppOffer1Id = Guid.Parse("0242fc0e-d5ca-4e79-9246-b497b1f3817c");
            this.crmGroup.SuppOffer2Id = Guid.Parse("506619aa-630d-4519-b0be-6a57ef0930a1");

            this.crmGroup.SalesChannel = GetCellValue<string>(sheetName, "AA57");
            this.crmGroup.DealerName = GetCellValue<string>(sheetName, "AA59");
            this.crmGroup.DealerCode = GetCellValue<string>(sheetName, "AA60");
        }
    }
}