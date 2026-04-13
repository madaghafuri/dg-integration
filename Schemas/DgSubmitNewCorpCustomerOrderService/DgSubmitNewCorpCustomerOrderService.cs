using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Xml.Serialization;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using DgBaseService.DgGenericResponse;
using DgBaseService.DgHelpers;
using DgMasterData.DgLookupConst;
using ISAHttpRequest.ISAHttpRequest;
using ISAIntegrationSetup;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;
using SubmitNewCorpCustomerOrder_Request = DgIntegration.DgSubmitNewCorpCustomerOrderService.Request;
using SubmitNewCorpCustomerOrder_Response = DgIntegration.DgSubmitNewCorpCustomerOrderService.Response;

namespace DgIntegration.DgSubmitNewCorpCustomerOrderService
{
    public class SubmitNewCorpCustomerOrderService
    {
        private HTTPRequest httpRequest;
        private UserConnection userConnection;
		protected UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}

        public string url;
        public string endpoint;
		private string username;
		private string password;
        private string section;
        private string actionCode;
        private string orderId;
        private string hierarcy;
        private Guid recordId;

        private SubmitNewCorpCustomerOrder_Request.Envelope param;
        private SubmitNewCorpCustomerOrder_Response.Envelope response;
        private string stringResponse;
        private string errorResponse;

        public SubmitNewCorpCustomerOrderService(UserConnection UserConnection)
        {
            this.userConnection = UserConnection;
            
            var setup = IntegrationSetup.Get(UserConnection, "CSG", "SubmitNewCorpCustomerOrder");
            if(setup == null) {
                throw new Exception("SubmitNewCorpCustomerOrder hasn't been set up for integration");
            }
            
            this.url = setup.BaseUrl;
            this.endpoint = setup.EndpointUrl;
			this.username = setup.Authentication.Basic.Username;
            this.password = setup.Authentication.Basic.Password;
			
            this.httpRequest = new HTTPRequest(this.url, UserConnection);
        }

        public virtual async Task Request()
        {
            this.errorResponse = string.Empty;
            this.stringResponse = null;

            if(this.recordId != null && this.recordId != Guid.Empty) {
                this.httpRequest.SetLogRecordId(this.recordId);
            }

            try {
                if(this.param == null) {
                    throw new Exception("Request param is empty");
                }

                string xml = HTTPRequest.XmlToString<SubmitNewCorpCustomerOrder_Request.Envelope>(this.param);
                var req = await this.httpRequest
                    .SetLogName("Save CRM: SubmitNewCorpCustomerOrder")
                    .SetLogSection("Submission")
                    .AddHeader("User-Agent", "DevDigi")
                .Post(endpoint, xml, ContentType.Xml);

                if(!req.Success || !string.IsNullOrEmpty(req.Error)) {
                    if(!string.IsNullOrEmpty(req.Error)) {
                        throw new Exception(req.Error);
                    }

                    if(!string.IsNullOrEmpty(req.Body)) {
                        throw new Exception(req.Body);
                    }

                    throw new Exception(req.StatusCode);
                }

                if(string.IsNullOrEmpty(req.Body)) {
                    throw new Exception("Response is empty");
                }

                this.stringResponse = req.Body;
            } catch (Exception e) {
                this.errorResponse = e.Message;

                return;
            }

            try {
                this.response = HTTPRequest.XmlToObject<SubmitNewCorpCustomerOrder_Response.Envelope>(this.stringResponse);
                string status = this.response?.Header?.CSGHeader?.Status ?? string.Empty;
                if(string.IsNullOrEmpty(status)) {
                    throw new Exception("Failed convert xml string to object");
                }
            } catch (Exception e) {
                this.errorResponse = !string.IsNullOrEmpty(this.stringResponse) ? this.stringResponse : e.Message;
            }
        }

        public virtual SubmitNewCorpCustomerOrderService SetParam(string Xml)
        {
            try {
                return this.SetParam(HTTPRequest.XmlToObject<SubmitNewCorpCustomerOrder_Request.Envelope>(Xml));   
            } catch (Exception e) {
                throw new Exception($"Xml is not valid: {e.Message}");
            }
        }

        public virtual SubmitNewCorpCustomerOrderService SetParam(SubmitNewCorpCustomerOrder_Request.Envelope Param)
        {
            this.param = Param;
            return this;
        }

        public virtual SubmitNewCorpCustomerOrderService SetParam(Guid RecordId, string ActionCode = "", string OrderId = "", string Hierarcy = "1")
        {
            this.recordId = RecordId;
            this.hierarcy = Hierarcy;
            this.actionCode = ActionCode;
            this.orderId = OrderId;

            this.param = BuildRequest();

            return this;
        }

        public virtual SubmitNewCorpCustomerOrder_Request.Envelope GetRequest()
        {
            return this.param ?? null;
        }

        public virtual string GetStringRequest()
        {
            return HTTPRequest.XmlToString<SubmitNewCorpCustomerOrder_Request.Envelope>(this.param);
        }

        public virtual SubmitNewCorpCustomerOrder_Response.Envelope GetResponse()
        {
            return this.response ?? null;
        }

        public virtual string GetStringResponse()
        {
            return this.response == null || !string.IsNullOrEmpty(this.errorResponse) ? 
                this.stringResponse : 
                HTTPRequest.XmlToString<SubmitNewCorpCustomerOrder_Response.Envelope>(this.response);
        }

        public virtual bool IsSuccessResponse()
        {
            if(!string.IsNullOrEmpty(this.errorResponse) && !string.IsNullOrEmpty(this.stringResponse)) {
                try {
                    XDocument xmlDoc = XDocument.Parse(this.stringResponse);
                    var _status = xmlDoc.Descendants("Status").FirstOrDefault();
                    if (_status == null) {
                        return false;
                    }
                    
                    return _status.Value == "Successful" ? true : false;
                } catch(Exception e) {
                    return false;
                }
            }

            if(this.response == null) {
                return false;
            }

            string status = this.response.Header?.CSGHeader?.Status ?? string.Empty;
            return status == "Successful" ? true : false;
        }

        public virtual string GetErrorResponse()
        {
            if(!string.IsNullOrEmpty(this.errorResponse) && !string.IsNullOrEmpty(this.stringResponse)) {
                try {
                    XDocument xmlDoc = XDocument.Parse(this.stringResponse);
                    var _status = xmlDoc.Descendants("Status").FirstOrDefault();
                    var _errorCode = xmlDoc.Descendants("ErrorCode").FirstOrDefault();
                    var _errorDescription = xmlDoc.Descendants("ErrorDescription").FirstOrDefault();

                    if(_status == null) {
                        return this.stringResponse;
                    }

                    if(_status.Value != "Successful") {
                        return $"{_status.Value} - {_errorCode.Value}: {_errorDescription.Value}";
                    }

                    return string.Empty;
                } catch(Exception e) {
                    return e.Message;
                }
            }

            if(this.response == null) {
                return this.errorResponse ?? string.Empty;
            }

            var csgHeader = this.response.Header?.CSGHeader;
            if(csgHeader == null) {
                return "CSG Header is empty";
            }

            var status = csgHeader.Status;
            var errorCode = csgHeader.ErrorCode;
            var errorDescription = csgHeader.ErrorDescription;

            if(status != "Successful") {
                return $"{status} - {errorCode}: {errorDescription}";
            }

            return string.Empty;
        }

        protected virtual SubmitNewCorpCustomerOrder_Request.Envelope BuildRequest()
        {
            var result = new SubmitNewCorpCustomerOrder_Request.Envelope();
            
            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgSubmission");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();

            foreach (string col in GetQuery()) {
                columns.Add(col, esq.AddColumn(col));
            }

            var entity = esq.GetEntity(UserConnection, this.recordId);
            if(entity == null) {
                return null;
            }

            result.Header = new SubmitNewCorpCustomerOrder_Request.Header() {
                Security = Helper.GenerateUsernameToken(this.username, this.password),
                CSGHeader = new SubmitNewCorpCustomerOrder_Request.CSGHeader() {
                    SourceSystemID = "NCCF",
                    ReferenceID = Helper.GenerateReferenceId("NCCF"),
                    ChannelMedia = "WEB",
                    BusinessUnit = "Digi"
                }
            };

            string corporateName = this.hierarcy == "1" ? 
                entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgGroupName"].Name) :
                entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgGroupSubParentName"].Name);
            string corporateEmail = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgCompanyEmail"].Name);
            
            DateTime brnExp = entity.GetTypedColumnValue<DateTime>(columns["DgCRMGroup.DgBRNExpiredDate"].Name);
            string brnExpString = string.Empty;
            if(brnExp != DateTime.MinValue) {
                brnExpString = brnExp.ToString("yyyy-MM-dd");
            }

            DateTime incorporationDate = entity.GetTypedColumnValue<DateTime>(columns["DgCRMGroup.DgDateIncorporation"].Name);
            string incorporationDateString = string.Empty;
            if(incorporationDate != DateTime.MinValue) {
                incorporationDateString = incorporationDate.ToString("yyyy-MM-dd");
            }

            var picInfoList = new List<SubmitNewCorpCustomerOrder_Request.PICInfosRecord>();

            string malaysiaCode = "123";
            string nonMalay = "228";
            string admin1Name = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgAdministrationName1"].Name);
            if(!string.IsNullOrEmpty(admin1Name)) {
                string admin1IdType = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgIDTypeAdm1.Name"].Name);
				if(admin1IdType == "Armed Force") {
					admin1IdType += "s";
				}
				
                picInfoList.Add(new SubmitNewCorpCustomerOrder_Request.PICInfosRecord() {
                    Name = admin1Name,
                    Race = "4",
                    PhoneNumber = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgMobilePhone1"].Name),
                    Email = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgAdministrationEmail1"].Name),
                    IsNotificationPerson = "true",
                    PicType = "ADMINISTRATOR",
                    IdType = admin1IdType.Replace(" ", "").ToUpper(),
                    IdNumber = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgIdNo1"].Name),
                    Nationality = admin1IdType == "Passport" ? nonMalay : malaysiaCode,
                });
            }

            string admin2Name = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgAdministrationName2"].Name);
            if(!string.IsNullOrEmpty(admin2Name)) {
                string admin2IdType = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgIDTypeAdm2.Name"].Name);
				if(admin2IdType == "Armed Force") {
					admin2IdType += "s";
				}
				
                picInfoList.Add(new SubmitNewCorpCustomerOrder_Request.PICInfosRecord() {
                    Name = admin2Name,
                    Race = "4",
                    PhoneNumber = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgMobilePhone2"].Name),
                    Email = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgAdministrationEmail2"].Name),
                    IsNotificationPerson = "false",
                    PicType = "ADMINISTRATOR",
                    IdType = admin2IdType.Replace(" ", "").ToUpper(),
                    IdNumber = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgIdNo2"].Name),
                    Nationality = admin2IdType == "Passport" ? nonMalay : malaysiaCode,
                });
            }

            string auth1Name = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgAuthorizedName1"].Name);
            if(!string.IsNullOrEmpty(auth1Name)) {
                string auth1IdType = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgIDTypeAuth1.Name"].Name);
				if(auth1IdType == "Armed Force") {
					auth1IdType += "s";
				}
				
                picInfoList.Add(new SubmitNewCorpCustomerOrder_Request.PICInfosRecord() {
                    Name = auth1Name,
                    Race = "4",
                    PhoneNumber = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgAuthorizedMobilePhone1"].Name),
                    Email = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgAuthorizedEmail1"].Name),
                    IsNotificationPerson = "false",
                    PicType = "AUTHORIZEDSIGNATORY",
                    IdType = auth1IdType.Replace(" ", "").ToUpper(),
                    IdNumber = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgAuthorizedIdNo1"].Name),
                    Nationality = auth1IdType == "Passport" ? nonMalay : malaysiaCode, 
                });
            }

            string auth2Name = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgAuthorizedName2"].Name);
            if(!string.IsNullOrEmpty(auth2Name)) {
                string auth2IdType = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgIDTypeAuth2.Name"].Name);
				if(auth2IdType == "Armed Force") {
					auth2IdType += "s";
				}
				
                picInfoList.Add(new SubmitNewCorpCustomerOrder_Request.PICInfosRecord() {
                    Name = auth2Name,
                    Race = "4",
                    PhoneNumber = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgAuthorizedMobilePhone2"].Name),
                    Email = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgAuthorizedEmail2"].Name),
                    IsNotificationPerson = "false",
                    PicType = "AUTHORIZEDSIGNATORY",
                    IdType = auth2IdType.Replace(" ", "").ToUpper(),
                    IdNumber = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgAuthorizedIdNo2"].Name),
                    Nationality = auth2IdType == "Passport" ? nonMalay : malaysiaCode, 
                });
            }

            var addressList = new List<SubmitNewCorpCustomerOrder_Request.AddressRecord>();
            addressList.Add(new SubmitNewCorpCustomerOrder_Request.AddressRecord() {
                AddressType = "LEGAL",
                PrimaryAddress = new SubmitNewCorpCustomerOrder_Request.PrimaryAddress() {
                    AddressLine1 = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgLegalAddress"].Name),
                    PostCode = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgPostcode.Name"].Name),
                    City = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgCity.DgCSGCode"].Name),
                    State = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgState.DgCSGCode"].Name),
                    Country = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgCountry.DgCSGCode"].Name),
                }
            });
            addressList.Add(new SubmitNewCorpCustomerOrder_Request.AddressRecord() {
                AddressType = "DELIVERY",
                PrimaryAddress = new SubmitNewCorpCustomerOrder_Request.PrimaryAddress() {
                    AddressLine1 = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgDeliveryaddress"].Name),
                    PostCode = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgPostcodeAdmInformationDelivery.Name"].Name),
                    City = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgCityAdmInformationDelivery.DgCSGCode"].Name),
                    State = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgStateAdmInfoDelivery.DgCSGCode"].Name),
                    Country = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgCountryAdmInformationDelivery.DgCSGCode"].Name),
                }
            });
            addressList.Add(new SubmitNewCorpCustomerOrder_Request.AddressRecord() {
                AddressType = "CORRESPONDENCE",
                PrimaryAddress = new SubmitNewCorpCustomerOrder_Request.PrimaryAddress() {
                    AddressLine1 = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgBillingAddress"].Name),
                    PostCode = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgPostcodeAdmInformationBilling.Name"].Name),
                    City = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgCityAdmInformationBilling.DgCSGCode"].Name),
                    State = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgStateAdmInfoBilling.DgCSGCode"].Name),
                    Country = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgCountryAdmInformationBilling.DgCSGCode"].Name),
                }
            });

            var addressListAccount = new List<SubmitNewCorpCustomerOrder_Request.AddressRecord>() {
                new SubmitNewCorpCustomerOrder_Request.AddressRecord() {
                    AddressType = "ACCOUNTDELIVERY",
                    PrimaryAddress = new SubmitNewCorpCustomerOrder_Request.PrimaryAddress() {
                        AddressLine1 = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgDeliveryaddress"].Name),
                        PostCode = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgPostcodeAdmInformationDelivery.Name"].Name),
                        City = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgCityAdmInformationDelivery.DgCSGCode"].Name),
                        State = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgStateAdmInfoDelivery.DgCSGCode"].Name),
                        Country = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgCountryAdmInformationDelivery.DgCSGCode"].Name),
                    }
                },
                new SubmitNewCorpCustomerOrder_Request.AddressRecord() {
                    AddressType = "BILL",
                    PrimaryAddress = new SubmitNewCorpCustomerOrder_Request.PrimaryAddress() {
                        AddressLine1 = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgBillingAddress"].Name),
                        PostCode = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgPostcodeAdmInformationBilling.Name"].Name),
                        City = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgCityAdmInformationBilling.DgCSGCode"].Name),
                        State = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgStateAdmInfoBilling.DgCSGCode"].Name),
                        Country = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgCountryAdmInformationBilling.DgCSGCode"].Name),
                    }
                }
            };

            var suppList = new List<SubmitNewCorpCustomerOrder_Request.SuppOffRecord>();
            string suppOffer1 = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgSuppOffer1.DgCode"].Name);
            if(!string.IsNullOrEmpty(suppOffer1)) {
                suppList.Add(new SubmitNewCorpCustomerOrder_Request.SuppOffRecord() {
                    OfferId = suppOffer1,
                    OfferName = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgSuppOffer1.Name"].Name)
                });
            }

            string suppOffer2 = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgSuppOffer2.DgCode"].Name);
            if(!string.IsNullOrEmpty(suppOffer2)) {
                suppList.Add(new SubmitNewCorpCustomerOrder_Request.SuppOffRecord() {
                    OfferId = suppOffer2,
                    OfferName = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgSuppOffer2.Name"].Name)
                });
            }

            string suppOffer3 = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgSuppOffer3.DgCode"].Name);
            if(!string.IsNullOrEmpty(suppOffer3)) {
                suppList.Add(new SubmitNewCorpCustomerOrder_Request.SuppOffRecord() {
                    OfferId = suppOffer3,
                    OfferName = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgSuppOffer3.Name"].Name)
                });
            }

            string suppOffer4 = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgSuppOffer4.DgCode"].Name);
            if(!string.IsNullOrEmpty(suppOffer4)) {
                suppList.Add(new SubmitNewCorpCustomerOrder_Request.SuppOffRecord() {
                    OfferId = suppOffer4,
                    OfferName = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgSuppOffer4.Name"].Name)
                });
            }

            string suppOffer5 = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgSuppOffer5.DgCode"].Name);
            if(!string.IsNullOrEmpty(suppOffer5)) {
                suppList.Add(new SubmitNewCorpCustomerOrder_Request.SuppOffRecord() {
                    OfferId = suppOffer5,
                    OfferName = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgSuppOffer5.Name"].Name)
                });
            }

            string suppOffer6 = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgSuppOffer6.DgCode"].Name);
            if(!string.IsNullOrEmpty(suppOffer6)) {
                suppList.Add(new SubmitNewCorpCustomerOrder_Request.SuppOffRecord() {
                    OfferId = suppOffer6,
                    OfferName = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgSuppOffer6.Name"].Name)
                });
            }

            string dealerId = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgDealer.DgDealerID"].Name);
            
            /**
                Key = Industrial Segment Code
                Value = Business Nature Code
                
                Related Industrial Segment & Business Nature
            **/
            var businessSegment = new Dictionary<string, string>
            {
                {"2", "7"},
                {"3", "11"},
                {"8", "15"},
                {"9", "17"}
            };
            var industrySegmentCode = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgIndustrialSegment.DgCode"].Name);

            result.Body = new SubmitNewCorpCustomerOrder_Request.Body() {
                SubmitNewCorpCustomerOrderRequest = new SubmitNewCorpCustomerOrder_Request.SubmitNewCorpCustomerOrderRequest() {
                    ValidationResult = new SubmitNewCorpCustomerOrder_Request.ValidationResult() {
                        ActionCode = this.actionCode
                    },
                    OrderId = this.orderId,
                    IsRequirePaymentCollection = "false",
                    CorporateCustomer = new SubmitNewCorpCustomerOrder_Request.CorporateCustomer() {
                        CorporateName = corporateName,
                        BusinessRegistrationNumber = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgBRN"].Name),
                        Tin = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgTINNumber"].Name),
                        SST = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgSST"].Name), 
                        BRNExpiryDate = brnExpString,
                        CorporatePhoneNumber = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgTelNo"].Name),
                        CorporateEmail = corporateEmail,
                        IncorporationDate = incorporationDateString,
                        IndustrySegment = industrySegmentCode,
                        BusinessNature = businessSegment.ContainsKey(industrySegmentCode) ? businessSegment[industrySegmentCode] : string.Empty,
                        TelecomUsage = "5",
                        CorporateCustomerType = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgEnterpriseCustomerType.DgCode"].Name),
                        CorporateHierarchy = new SubmitNewCorpCustomerOrder_Request.CorporateHierarchy() {
                            Hierarchy = this.hierarcy
                        },
                        AccountManagerInfo = new SubmitNewCorpCustomerOrder_Request.AccountManagerInfo() {
                            Name = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgDealer.DgDealerName"].Name),
                            PhoneNumber = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgDealer.DgDealerHandphone"].Name),
                            Email = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgDealer.DgDealerEmail"].Name),
                            DealerCode = dealerId,
                        }
                    },
                    CorporateGroup = new SubmitNewCorpCustomerOrder_Request.CorporateGroup() {
                        CorporateGroupName = corporateName,
                        CorporateGroupType = "M",
                        PICInfosList = new SubmitNewCorpCustomerOrder_Request.PICInfosList() {
                            PICInfosRecord = picInfoList
                        },
                        Account = new SubmitNewCorpCustomerOrder_Request.Account() {
                            NewAccount = new SubmitNewCorpCustomerOrder_Request.NewAccount() {
                                BillMediumList = new SubmitNewCorpCustomerOrder_Request.BillMediumList() {
                                    BillMedium = GetBillMedium(this.hierarcy, entity.GetTypedColumnValue<Guid>(columns["DgCRMGroup.DgBillMediumName.Id"].Name))
                                },
                                AccountName = corporateName,
                                Email = corporateEmail,
                                SMSNotificationMSISDN = Helper.GetValidMSISDN(entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgMobilePhone1"].Name)),
                                AddressList = new SubmitNewCorpCustomerOrder_Request.AddressList() {
                                    AddressRecord = addressListAccount
                                },
                                PaymentModeInfo = new SubmitNewCorpCustomerOrder_Request.PaymentModeInfo() {
                                    PaymentMode = "1"
                                },
                            },
                        },
                        PrimaryOffering = new SubmitNewCorpCustomerOrder_Request.PrimaryOffering() {
                            OfferId = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgPrimaryOffer.DgCode"].Name),
                            OfferName = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgPrimaryOffer.Name"].Name)
                        },
                        SuppOffList = new SubmitNewCorpCustomerOrder_Request.SuppOffList() {
                            SuppOffRecord = suppList
                        }
                    },
                    Dealer = new SubmitNewCorpCustomerOrder_Request.Dealer() {
                        DealerCode = dealerId,
                        DealerUserId = dealerId
                    },
                }
            };

            if(this.hierarcy == "2") {
                result.Body
                    .SubmitNewCorpCustomerOrderRequest
                    .CorporateCustomer
                    .CorporateHierarchy.TopParentCustomerId = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgTopParentCustomerId"].Name);
                result.Body
                    .SubmitNewCorpCustomerOrderRequest
                    .CorporateCustomer
                    .CorporateHierarchy.ParentCustomerId = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgTopParentCustomerId"].Name);

                result.Body
                    .SubmitNewCorpCustomerOrderRequest
                    .CorporateGroup
                    .FeesList = new SubmitNewCorpCustomerOrder_Request.FeesList() {
                        FeesRecord = new SubmitNewCorpCustomerOrder_Request.FeesRecord() {
                            FeeType = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgFeeType"].Name),
                            FeeItemCode = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgFeeItemCode"].Name),
                            FeeAmount = entity.GetTypedColumnValue<decimal>(columns["DgCRMGroup.DgFeeAmount"].Name).ToString(),
                            OriginalFeeAmount = entity.GetTypedColumnValue<decimal>(columns["DgCRMGroup.DgOriginalFeeAmount"].Name).ToString(),
                            PaymentType = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgPaymentType"].Name),
                            OFSCode = entity.GetTypedColumnValue<string>(columns["DgCRMGroup.DgOFSCode"].Name),
                            Waive = new SubmitNewCorpCustomerOrder_Request.Waive() {
                                ManualWaiveAmount = "0"
                            },
                            TaxList = new SubmitNewCorpCustomerOrder_Request.TaxList() {
                                TaxRecord = new List<SubmitNewCorpCustomerOrder_Request.TaxRecord>() {
                                    new SubmitNewCorpCustomerOrder_Request.TaxRecord
                                    {
                                        TaxCode = "V",
                                        TaxName = "VAT",
                                        TaxAmount = "0.6"
                                    }
                                }
                            }
                        }
                    };
            }

            result.Body
                .SubmitNewCorpCustomerOrderRequest
                .CorporateCustomer
                .PICInfosList = new SubmitNewCorpCustomerOrder_Request.PICInfosList() {
                    PICInfosRecord = picInfoList
                };

            result.Body
                .SubmitNewCorpCustomerOrderRequest
                .CorporateCustomer
                .AddressList = new SubmitNewCorpCustomerOrder_Request.AddressList() {
                    AddressRecord = addressList
                };

            return result;
        }

        public virtual List<string> GetQuery()
        {
            return new List<string>() {
                "DgCRMGroup.DgGroupNo",
                "DgCRMGroup.DgGroupName",
                "DgCRMGroup.DgGroupSubParentName",
                "DgCRMGroup.DgBRN",
                "DgCRMGroup.DgBRNExpiredDate",
                "DgCRMGroup.DgTelNo",
                "DgCRMGroup.DgCompanyEmail",
                "DgCRMGroup.DgDateIncorporation",
                "DgCRMGroup.DgIndustrialSegment.DgCode",
                "DgCRMGroup.DgEnterpriseCustomerType.DgCode",
                "DgCRMGroup.DgTopParentCustomerId",
                "DgCRMGroup.DgParentCustomerId",
                "DgCRMGroup.DgCorporateNumber",
                "DgCRMGroup.DgBillMediumName.Id",
                "DgCRMGroup.DgPaymentMode",

                "DgCRMGroup.DgDealer.DgDealerID",
                "DgCRMGroup.DgDealer.DgDealerName",
                "DgCRMGroup.DgDealer.DgDealerHandphone",
                "DgCRMGroup.DgDealer.DgDealerEmail",

                "DgCRMGroup.DgAdministrationName1",
                "DgCRMGroup.DgMobilePhone1",
                "DgCRMGroup.DgAdministrationEmail1",
                "DgCRMGroup.DgIDTypeAdm1.Name",
                "DgCRMGroup.DgIdNo1",

                "DgCRMGroup.DgAdministrationName2",
                "DgCRMGroup.DgMobilePhone2",
                "DgCRMGroup.DgAdministrationEmail2",
                "DgCRMGroup.DgIDTypeAdm2.Name",
                "DgCRMGroup.DgIdNo2",

                "DgCRMGroup.DgAuthorizedName1",
                "DgCRMGroup.DgAuthorizedMobilePhone1",
                "DgCRMGroup.DgAuthorizedEmail1",
                "DgCRMGroup.DgIDTypeAuth1.Name",
                "DgCRMGroup.DgAuthorizedIdNo1",

                "DgCRMGroup.DgAuthorizedName2",
                "DgCRMGroup.DgAuthorizedMobilePhone2",
                "DgCRMGroup.DgAuthorizedEmail2",
                "DgCRMGroup.DgIDTypeAuth2.Name",
                "DgCRMGroup.DgAuthorizedIdNo2",

                "DgCRMGroup.DgLegalAddress",
                "DgCRMGroup.DgCity.DgCSGCode",
                "DgCRMGroup.DgPostcode.Name",
                "DgCRMGroup.DgState.DgCSGCode",
                "DgCRMGroup.DgCountry.DgCSGCode",

                "DgCRMGroup.DgBillingAddress",
                "DgCRMGroup.DgCityAdmInformationBilling.DgCSGCode",
                "DgCRMGroup.DgPostcodeAdmInformationBilling.Name",
                "DgCRMGroup.DgStateAdmInfoBilling.DgCSGCode",
                "DgCRMGroup.DgCountryAdmInformationBilling.DgCSGCode",

                "DgCRMGroup.DgDeliveryaddress",
                "DgCRMGroup.DgCityAdmInformationDelivery.DgCSGCode",
                "DgCRMGroup.DgPostcodeAdmInformationDelivery.Name",
                "DgCRMGroup.DgStateAdmInfoDelivery.DgCSGCode",
                "DgCRMGroup.DgCountryAdmInformationDelivery.DgCSGCode",

                "DgCRMGroup.DgPrimaryOffer.DgCode",
                "DgCRMGroup.DgPrimaryOffer.Name",
                "DgCRMGroup.DgSuppOffer1.DgCode",
                "DgCRMGroup.DgSuppOffer1.Name",
                "DgCRMGroup.DgSuppOffer2.DgCode",
                "DgCRMGroup.DgSuppOffer2.Name",
                "DgCRMGroup.DgSuppOffer3.DgCode",
                "DgCRMGroup.DgSuppOffer3.Name",
                "DgCRMGroup.DgSuppOffer4.DgCode",
                "DgCRMGroup.DgSuppOffer4.Name",
                "DgCRMGroup.DgSuppOffer5.DgCode",
                "DgCRMGroup.DgSuppOffer5.Name",
                "DgCRMGroup.DgSuppOffer6.DgCode",
                "DgCRMGroup.DgSuppOffer6.Name",

                "DgCRMGroup.DgFeeType",
                "DgCRMGroup.DgFeeItemCode",
                "DgCRMGroup.DgFeeAmount",
                "DgCRMGroup.DgOriginalFeeAmount",
                "DgCRMGroup.DgPaymentType",
                "DgCRMGroup.DgOFSCode",
                "DgCRMGroup.DgTINNumber",
                "DgCRMGroup.DgSST"                
            };
        }

        protected virtual List<string> GetBillMedium(string Hierarchy, Guid BillMediumId)
        {
            var result = new List<string>();

            if (Hierarchy == "1")  {
                if (BillMediumId == BillMediumName.EmailBillWithPDF) {
                    result = new List<string>() {
                        "1001", "1003"
                    };
                } else {
                    result = new List<string>() {
                        "1001"
                    };
                }
            } else if (Hierarchy == "2")  {
                if (BillMediumId == BillMediumName.EmailBillWithPDF) {
                    result = new List<string>() {
                        "1001", "1003"
                    };
                } else if (BillMediumId == BillMediumName.PaperBillChargeableStandard) {
                    result = new List<string>() {
                        "1011", "1008", "1001"
                    };
                } else if (BillMediumId == BillMediumName.PaperBillChargeableItemized) {
                    result = new List<string>() {
                        "1012", "1008", "1001"
                    };
                } else {
					result = new List<string>() {
                        "1001", "1008"
                    };
				}
            }

            return result;
        }
    }
}