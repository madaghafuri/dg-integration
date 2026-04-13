using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Data;
using System.Data.SqlClient;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Newtonsoft.Json;
using ISAIntegrationSetup;
using SysSettings = Terrasoft.Core.Configuration.SysSettings;

namespace DgIntegration.DgCommonInventory
{
    public class Token
    {
        protected UserConnection UserConnection;
        public string BaseUrl;
        public string EndpointUrl;
        public string ClientId;
        public string ClientSecret;
        public string GrantType;

        public Token(UserConnection UserConnection)
        {
            this.UserConnection = UserConnection;
            
            var setup = GetSetup();
            this.BaseUrl = setup.BaseUrl;
            this.EndpointUrl = setup.EndpointUrl;

            var customAuth = setup.Authentication.Custom;
            this.ClientId = customAuth.FirstOrDefault(item => item.Key == "ClientId")?.Value;
            this.ClientSecret = customAuth.FirstOrDefault(item => item.Key == "ClientSecret")?.Value;
            this.GrantType = customAuth.FirstOrDefault(item => item.Key == "GrantType")?.Value;
        }
		
		public string GetCacheToken()
		{
			string token = string.Empty;
			var select = new Select(UserConnection)
				.Top(1)
				.Column("TextValue")
			.From("SysSettingsValue")
			.Where("SysSettingsId").IsEqual(Column.Parameter(Guid.Parse("C6C09EED-948F-4ACD-9DE0-127CA2799F7C"))) as Select;
			
			using(DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                using(IDataReader dataReader = select.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
						token = dataReader.GetColumnValue<string>("TextValue");
					}
				}
			}
			
			return token;
		}
		
		public void UpdateCacheToken(string Token)
		{
			new Update(UserConnection, "SysSettingsValue")
                .Set("TextValue", Column.Parameter(Token))
                .Where("SysSettingsId").IsEqual(Column.Parameter(Guid.Parse("C6C09EED-948F-4ACD-9DE0-127CA2799F7C")))
                .Execute();
		}

        public virtual TokenRequest GetParam()
        {
            if(string.IsNullOrEmpty(this.ClientId)) {
                throw new Exception("Client Id cannot be null or empty");
            }

            if(string.IsNullOrEmpty(this.ClientSecret)) {
                throw new Exception("Client Secret cannot be null or empty");
            }

            if(string.IsNullOrEmpty(this.GrantType)) {
                throw new Exception("Grant Type cannot be null or empty");
            }

            return new TokenRequest() {
                client_id = this.ClientId,
                client_secret = this.ClientSecret,
                grant_type = this.GrantType
            };
        }
		
		public virtual TokenRequest GetParam(string ClientId, string ClientSecret, string GrantType)
        {
            if(string.IsNullOrEmpty(ClientId)) {
                throw new Exception("Client Id cannot be null or empty");
            }

            if(string.IsNullOrEmpty(ClientSecret)) {
                throw new Exception("Client Secret cannot be null or empty");
            }

            if(string.IsNullOrEmpty(GrantType)) {
                throw new Exception("Grant Type cannot be null or empty");
            }

            return new TokenRequest() {
                client_id = ClientId,
                client_secret = ClientSecret,
                grant_type = GrantType
            };
        }

        public virtual Setup GetSetup()
        {
            var setup = IntegrationSetup.Get(UserConnection, "Common Inventory", "Token", string.Empty);
            if(setup == null) {
                throw new Exception("Common Inventory: Token hasn't been set up for integration");
            }

            return setup;
        }

        public virtual string GetErrorResponse(string ResponseBody)
        {
            if(string.IsNullOrEmpty(ResponseBody)) {
                return string.Empty;
            }

            try {
                var settings = new JsonSerializerSettings {
                    MissingMemberHandling = MissingMemberHandling.Error
                };

                var tokenErrorResponse = JsonConvert.DeserializeObject<TokenErrorResponse>(ResponseBody, settings);
                return $"{tokenErrorResponse.error}: {tokenErrorResponse.error_description}";   
            } catch (Exception e) {}

            return ResponseBody;
        }
    }
}