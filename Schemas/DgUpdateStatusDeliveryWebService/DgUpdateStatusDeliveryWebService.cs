using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Core.Scheduler;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Newtonsoft.Json;
using Quartz;
using DgBaseService.DgGenericResponse;
using DgBaseService.DgHelpers;
using DgSubmission.DgLineDetail;
using ISAHttpRequest.ISAHttpRequest;
using ISAEntityHelper.EntityHelper;
using LookupConst = DgMasterData.DgLookupConst;
using DgIntegration.DgUpdateStatusDelivery;
using ResponseModel = DgIntegration.DgUpdateStatusDelivery.Response;

namespace Terrasoft.Configuration.DgUpdateStatusDeliveryWebService
{
    [XmlSerializerFormat]
    [ServiceContract]
	[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class NCCFAPIv2: BaseService
    {
		private SystemUserConnection _systemUserConnection;
		private SystemUserConnection SystemUserConnection
		{
			get
			{
				return _systemUserConnection ?? (_systemUserConnection = (SystemUserConnection)AppConnection.SystemUserConnection);
			}
		}

		[OperationContract]
		[WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml)]
		public ResponseModel.Envelope UpdateStatusDelivery(Stream Envelope)
		{
			var service = new UpdateStatusDeliveryService(SystemUserConnection);
			return service.Process(Envelope);
		}
	}
}