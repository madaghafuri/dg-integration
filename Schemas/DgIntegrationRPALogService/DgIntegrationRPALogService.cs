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
using DgMasterData.DgLookupConst;

namespace DgIntegration.DgIntegrationRPALogService
{
    public class IntegrationRPALogService
    {
        private UserConnection userConnection;
		private UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}
        
        public IntegrationRPALogService(UserConnection userConnection_ = null) 
        {
        	if(userConnection_ != null) {
				userConnection = userConnection_;
			}
        }

        public GeneralResponse IntegrasiRPALog()
        {
          	var result = new GeneralResponse();
          	
			try {
                var statusRPALogDoBot = RPALogDoBot();
                if(!statusRPALogDoBot.Success) {
                    result.Message = statusRPALogDoBot.Message;
                }

                var statusRPALogPreBot = RPALogPreBot();
                if(!statusRPALogPreBot.Success) {
                    result.Message = statusRPALogPreBot.Message;
                }

                var statusRPALogActBot = RPALogActBot();
                if(!statusRPALogActBot.Success) {
                    result.Message = statusRPALogActBot.Message;
                }

                result.Success = true;
            } catch(Exception error) {
                result.Message = error.Message;
            }
          
          	return result;
        }

        public GeneralResponse RPALogDoBot() //RPA_NCCF_WIP_S1
        {
          	var result = new GeneralResponse();
          	
			try {
                var temp = new List<string>();
                string filePath = @"D:\RPA\RPA_NCCF_WIP_S1\AppProcessing.log";
                if (System.IO.File.Exists(filePath))
                {
                    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                    {
                        var line = string.Empty;

                        while ((line = streamReader.ReadLine()) != null)
                        {
                            List<string> data = new List<string>(line.Split(new string[] { "|" }, StringSplitOptions.None));

                            // throw new Exception(JsonConvert.SerializeObject( new {
                            //     datadata = data[0]
                            // }, Formatting.Indented));

                            string serialNumber = data[0].Split('/')[2];
                            var insert = new Insert(UserConnection)
                                .Into("DgRPALog")
                                .Set("Id", Column.Parameter(Guid.NewGuid()))
                                .Set("CreatedById", Column.Parameter(UserConnection.CurrentUser.ContactId))
                                .Set("ModifiedById", Column.Parameter(UserConnection.CurrentUser.ContactId))
                                .Set("DgSerialNumber", Column.Parameter(serialNumber))
                                .Set("DgDate", Column.Parameter(data[1]))
                                .Set("DgRPATypeId", Column.Parameter(RPAType.DoBOT));
                            insert.Execute();
                            
                        
                        }
                    }
                    result.Message = "Success";
                    
                } else {
                    result.Message = "Could not find file " + filePath;
                }
                result.Success = true;
            } catch(Exception error) {
                result.Message = error.Message;
            }
          	return result;
        }

        public GeneralResponse RPALogPreBot() //RPA_NCCF_WIP_S2
        {
          	var result = new GeneralResponse();
          	
			try {
                var temp = new List<string>();
                string filePath = @"D:\RPA\RPA_NCCF_WIP_S2\AppProcessing.log";
                if (System.IO.File.Exists(filePath))
                {
                    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                    {
                        var line = string.Empty;

                        while ((line = streamReader.ReadLine()) != null)
                        {
                            List<string> data = new List<string>(line.Split(new string[] { "|" }, StringSplitOptions.None));

                            // throw new Exception(JsonConvert.SerializeObject( new {
                            //     datadata = data[0]
                            // }, Formatting.Indented));

                            string serialNumber = data[0].Split('/')[2];
                            var insert = new Insert(UserConnection)
                                .Into("DgRPALog")
                                .Set("Id", Column.Parameter(Guid.NewGuid()))
                                .Set("CreatedById", Column.Parameter(UserConnection.CurrentUser.ContactId))
                                .Set("ModifiedById", Column.Parameter(UserConnection.CurrentUser.ContactId))
                                .Set("DgSerialNumber", Column.Parameter(serialNumber))
                                .Set("DgDate", Column.Parameter(data[1]))
                                .Set("DgRPATypeId", Column.Parameter(RPAType.PreBOT));
                            insert.Execute();
                            
                        
                        }
                    }
                    result.Message = "Success";
                    
                } else {
                    result.Message = "Could not find file " + filePath;
                }
                result.Success = true;
            } catch(Exception error) {
                result.Message = error.Message;
            }
          	return result;
        }

        public GeneralResponse RPALogActBot() //RPA_NCCF_WIP_S3
        {
          	var result = new GeneralResponse();
          	
			try {
                var temp = new List<string>();
                string filePath = @"D:\RPA\RPA_NCCF_WIP_S3\AppProcessing.log";
                if (System.IO.File.Exists(filePath))
                {
                    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                    {
                        var line = string.Empty;

                        while ((line = streamReader.ReadLine()) != null)
                        {
                            List<string> data = new List<string>(line.Split(new string[] { "|" }, StringSplitOptions.None));

                            // throw new Exception(JsonConvert.SerializeObject( new {
                            //     datadata = data[0]
                            // }, Formatting.Indented));

                            string serialNumber = data[0].Split('/')[2];
                            var insert = new Insert(UserConnection)
                                .Into("DgRPALog")
                                .Set("Id", Column.Parameter(Guid.NewGuid()))
                                .Set("CreatedById", Column.Parameter(UserConnection.CurrentUser.ContactId))
                                .Set("ModifiedById", Column.Parameter(UserConnection.CurrentUser.ContactId))
                                .Set("DgSerialNumber", Column.Parameter(serialNumber))
                                .Set("DgDate", Column.Parameter(data[1]))
                                .Set("DgRPATypeId", Column.Parameter(RPAType.ActBOT));
                            insert.Execute();
                            
                        
                        }
                    }
                    result.Message = "Success";
                    
                } else {
                    result.Message = "Could not find file " + filePath;
                }
                result.Success = true;
            } catch(Exception error) {
                result.Message = error.Message;
            }
          	return result;
        }
    }
}