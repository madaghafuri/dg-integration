using System;
using System.IO;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using Terrasoft.Configuration;
using Newtonsoft.Json;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using Newtonsoft.Json.Linq;
using DgBaseService.DgGenericResponse;
using DgBaseService.DgCreatioIntegrationHelper;
using DgBaseService.DgIntegrationHelperService;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;

namespace DgIntegrationSFAService
{
    public class IntegrationSFAService
    {
        private UserConnection userConnection;
		private UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}
        
		private CreatioIntegrationHelper CreatioIntegrationHelper;
        private IntegrationHelperService IntegrationHelperService;
        private string userName;
        private string userPassword;
        private string url;
        public IntegrationSFAService(UserConnection userConnection_ = null) 
        {
        	if(userConnection_ != null) {
				userConnection = userConnection_;
			}
			
			this.userName = (string)SysSettings.GetValue(UserConnection, "DgSFAUsername");
			this.userPassword = (string)SysSettings.GetValue(UserConnection, "DgSFAPassword");
			this.url = (string)SysSettings.GetValue(UserConnection, "DgSFAUrl");
			
			CreatioIntegrationHelper = new CreatioIntegrationHelper(userName, userPassword, url);
            IntegrationHelperService = new IntegrationHelperService(UserConnection);
        }

        public static Task<HttpResponseMessage> GetRequest(string url)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://dev-ma.bpmonline.asia");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            return client.GetAsync(url);
        }

        public async Task<GeneralResponse> InsertSubmission()
        {
            var result = new GeneralResponse();

            try {
				string secretKey = (string)SysSettings.GetValue(UserConnection, "DgIntegrationSFAService_SecretKey");
                var uriAPI = $"https://dev-ma.bpmonline.asia/isa/api/get-Submission?SecretKey={secretKey}";
                var response = await GetRequest(uriAPI);
                var responseBody = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<ResponseData>(responseBody);

                foreach ( var item in data.GetDataSubmissionResult) 
                {
                    InsertSubmissionData(item);
                }

                result.Success = true;
            } catch (Exception error) {
                result.Message = error.Message;
            }
            return result;
        }
		
        public async Task<GeneralResponse> InsertSubmissionFromProd()
        {
            var result = new GeneralResponse();

            try {
                var uriAPI = $"{url}/0/rest/SubmissionWebService/GetDataSubmission";
                var response = await CreatioIntegrationHelper.Request("POST", uriAPI);
				var data = JsonConvert.DeserializeObject<ResponseData>(response.ResponseBody);
				
				throw new Exception(JsonConvert.SerializeObject( new {
					data = data
				}, Formatting.Indented));
				
				foreach ( var item in data.GetDataSubmissionResult) 
                {
                    InsertSubmissionData(item);
                }
				
                result.Success = true;
            } catch (Exception error) {
                result.Message = error.Message;
            }
            return result;
        }

        public void InsertSubmissionData(SubmissionInfo Data)
        {
            Guid id = Guid.NewGuid();
            var insert = new Insert(UserConnection)
                .Into("DgSubmission")
                .Set("CreatedOn", Column.Parameter(Data.CreatedOn))
                .Set("Id", Column.Parameter(id));
            
            if (!string.IsNullOrEmpty(Data.Type)) {
                if (Data.Type == "COP") {
                    insert.Set("DgSubmissionTypeId", Column.Parameter(SubmissionTypeConst.COP));
                }

                if (Data.Type == "MNP") {
                    insert.Set("DgSubmissionTypeId", Column.Parameter(SubmissionTypeConst.MNP));
                }

                if (Data.Type == "New") {
                    insert.Set("DgSubmissionTypeId", Column.Parameter(SubmissionTypeConst.NewCustomer));
                }
            }

            if (!string.IsNullOrEmpty(Data.FL)) {
                insert.Set("DgFL", Column.Parameter(true));
            }

            if (!string.IsNullOrEmpty(Data.Region)) {
                var dataRegion = GetLookupData("DgRegion", Data.Region);

                if (dataRegion.Id != Guid.Empty) {
                    insert.Set("DgRegionId", Column.Parameter(dataRegion.Id));
                } else {
                    var idLookup = InsertDataLookup("DgRegion", Data.Region);
                    insert.Set("DgRegionId", Column.Parameter(idLookup));
                }
            }

            if (!string.IsNullOrEmpty(Data.SerialNumber)) {
                insert.Set("DgSerialNumber", Column.Parameter(Data.SerialNumber));
            }

            if (!string.IsNullOrEmpty(Data.CompanyName)) {
                insert.Set("DgCompanyName", Column.Parameter(Data.CompanyName));
            }

            // if (!string.IsNullOrEmpty(Data.IdType)) {
            //     insert.Set("DgIdType", Column.Parameter(Data.IdType));
            // }

            if (!string.IsNullOrEmpty(Data.IdNo)) {
                insert.Set("DgIdNo", Column.Parameter(Data.IdNo));
            }

            if (!string.IsNullOrEmpty(Data.CardExpiredDate)) {
                insert.Set("DgCardExpiredDate", Column.Parameter(DateTime.Parse(Data.CardExpiredDate)));
            }

            if (!string.IsNullOrEmpty(Data.Source)) {
                var dataSource = GetLookupData("DgSource", Data.Source);

                if (dataSource.Id != Guid.Empty) {
                    insert.Set("DgSourceId", Column.Parameter(dataSource.Id));
                } else {
                    var idLookup = InsertDataLookup("DgSource", Data.Source);
                    insert.Set("DgSourceId", Column.Parameter(idLookup));
                }
            }

            // if (!string.IsNullOrEmpty(Data.CreatedBy)) {
            //     var dataCreatedBy = GetLookupData("Contact", Data.CreatedBy);

            //     if (dataCreatedBy.Id != Guid.Empty) {
            //         insert.Set("CreatedById", Column.Parameter(dataCreatedBy.Id));
            //     } else {
            //         var idLookup = InsertDataLookup("Contact", Data.CreatedBy);
            //         insert.Set("CreatedById", Column.Parameter(idLookup));
            //     }
            // }

            if (!string.IsNullOrEmpty(Data.Status)) {
                insert.Set("DgCRStatus", Column.Parameter(Data.Status));
            }

            if (!string.IsNullOrEmpty(Data.Line)) {
                insert.Set("DgLine", Column.Parameter(Data.Line));
            }

            if (!string.IsNullOrEmpty(Data.DeliveryAddress)) {
                insert.Set("DgDeliveryAddress", Column.Parameter(Data.DeliveryAddress));
            }

            // if (!string.IsNullOrEmpty(Data.City)) {
            //     var dataCity = GetLookupData("DgCity", Data.City);

            //     if (dataCity.Id != Guid.Empty) {
            //         insert.Set("DgCityId", Column.Parameter(dataCity.Id));
            //     } else {
            //         var idLookup = InsertDataLookup("DgCity", Data.City);
            //         insert.Set("DgCityId", Column.Parameter(idLookup));
            //     }
            // }

            // if (!string.IsNullOrEmpty(Data.Postcode)) {
            //     var dataPostcode = GetLookupData("DgPostcode", Data.Postcode);

            //     if (dataPostcode.Id != Guid.Empty) {
            //         insert.Set("DgPostcodeId", Column.Parameter(dataPostcode.Id));
            //     } else {
            //         var idLookup = InsertDataLookup("DgPostcode", Data.Postcode);
            //         insert.Set("DgPostcodeId", Column.Parameter(idLookup));
            //     }
            // }

            // if (!string.IsNullOrEmpty(Data.Country)) {
            //     var dataCountry = GetLookupData("DgCountry", Data.Country);

            //     if (dataCountry.Id != Guid.Empty) {
            //         insert.Set("DgCountryId", Column.Parameter(dataCountry.Id));
            //     } else {
            //         var idLookup = InsertDataLookup("DgCountry", Data.Country);
            //         insert.Set("DgCountryId", Column.Parameter(idLookup));
            //     }
            // }

            if (!string.IsNullOrEmpty(Data.Administration1Name)) {
                insert.Set("DgAdministration1Name", Column.Parameter(Data.Administration1Name));
            }

            if (!string.IsNullOrEmpty(Data.MobilePhoneAdministration1)) {
                insert.Set("DgMobilePhoneAdministration1", Column.Parameter(Data.MobilePhoneAdministration1));
            }

            if (!string.IsNullOrEmpty(Data.OfficeTelNoAdministration1)) {
                insert.Set("DgOfficeTelNoAdministration1", Column.Parameter(Data.OfficeTelNoAdministration1));
            }

            if (!string.IsNullOrEmpty(Data.Administration2Name)) {
                insert.Set("DgAdministration2Name", Column.Parameter(Data.Administration2Name));
            }

            if (!string.IsNullOrEmpty(Data.MobilePhoneAdministration2)) {
                insert.Set("DgMobilePhoneAdministration2", Column.Parameter(Data.MobilePhoneAdministration2));
            }

            if (!string.IsNullOrEmpty(Data.OfficeTelNoAdministration2)) {
                insert.Set("DgOfficeTelnoAdministration2", Column.Parameter(Data.OfficeTelNoAdministration2));
            }

            if (!string.IsNullOrEmpty(Data.Authorized1Name)) {
                insert.Set("DgAuthorized1Name", Column.Parameter(Data.Authorized1Name));
            }

            if (!string.IsNullOrEmpty(Data.AuthorizedMobilePhone1)) {
                insert.Set("DgMobilePhoneAuthorized1", Column.Parameter(Data.AuthorizedMobilePhone1));
            }

            if (!string.IsNullOrEmpty(Data.AuthorizedTelNo1)) {
                insert.Set("DgOfficeTelNoAuthorized1", Column.Parameter(Data.AuthorizedTelNo1));
            }

            if (!string.IsNullOrEmpty(Data.Authorized2Name)) {
                insert.Set("DgAuthorized2Name", Column.Parameter(Data.Authorized2Name));
            }

            if (!string.IsNullOrEmpty(Data.AuthorizedMobilePhone2)) {
                insert.Set("DgMobilePhoneAuthorized2", Column.Parameter(Data.AuthorizedMobilePhone2));
            }

            if (!string.IsNullOrEmpty(Data.AuthorizedTelNo2)) {
                insert.Set("DgOfficeTelNoAuthorized2", Column.Parameter(Data.AuthorizedTelNo2));
            }

            insert.Execute();
        }

        public Guid InsertDataLookup(string SchemaName, string Name)
        {
            var id = Guid.NewGuid();
            var insert = new Insert(UserConnection)
                .Into(SchemaName)
                .Set("Id", Column.Parameter(id))
                .Set("Name", Column.Parameter(Name));

            insert.Execute();
            return id;
        }

        public LookupData GetLookupData(string SchemaName, string Name)
        {
            var result = new LookupData();
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, SchemaName);

            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("Id", esq.AddColumn("Id"));
                columns.Add("Name", esq.AddColumn("Name"));
            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Name", Name));
            var entities = esq.GetEntityCollection(UserConnection);

            foreach (Entity entity in entities)
            {
                result.Id = entity.GetTypedColumnValue<Guid>(columns["Id"].Name);
                result.Name = entity.GetTypedColumnValue<string>(columns["Name"].Name);
            }

            return result;
        }
    }

    public class CookiesData
    {
        public string ASPXAUTH { get; set; }
        public string BPMCSRF { get; set; }
        public string BPMLOADER { get; set; }
        public string SsoSessionId { get; set; }
        public string UserName { get; set; }
    }

    public class ResponseData
    {
        public List<SubmissionInfo> GetDataSubmissionResult { get; set; }
    }

    public class SubmissionInfo
    {
        public string Type { get; set; }
        public string FL { get; set; }
        public string Region { get; set; }
        public string SerialNumber { get; set; }
        public string CompanyName { get; set; }
        public string IdType { get; set; }
        public string IdNo { get; set; }
        public string CardExpiredDate { get; set; }
        public string Source { get; set; }
        public string CreatedBy { get; set; }
        public string Status { get; set; }
        public string Line { get; set; }
        public string DeliveryAddress { get; set; }
        public string City { get; set; }
        public string Postcode { get; set; }
        public string Country { get; set; }
        public string Administration1Name { get; set; }
        public string MobilePhoneAdministration1 { get; set; }
        public string OfficeTelNoAdministration1 { get; set; }
        public string Administration2Name { get; set; }
        public string MobilePhoneAdministration2 { get; set; }
        public string OfficeTelNoAdministration2 { get; set; }
        public string Authorized1Name { get; set; }
        public string AuthorizedMobilePhone1 { get; set;}
        public string AuthorizedTelNo1 { get; set;}
        public string Authorized2Name { get; set;}
        public string AuthorizedMobilePhone2 { get; set;}
        public string AuthorizedTelNo2 { get; set;}
		public DateTime CreatedOn { get; set; }
    }

    public static class SubmissionTypeConst
    {
		public static readonly Guid COP = new Guid("52bfa0b0-ae33-4df2-822d-15b31f3f6d1e");
        public static readonly Guid MNP = new Guid("1cf9b17c-9085-4646-b499-53cdf15156b2");
        public static readonly Guid NewCustomer = new Guid("8fb696c2-77af-4840-87cf-75909b565a3d");
    }
}