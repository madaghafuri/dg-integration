namespace DgIntegration.DgUpdateOrderStatus31V2WebService
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.ServiceModel.Activation;
    using Terrasoft.Core;
    using Terrasoft.Web.Common;
    using Terrasoft.Core.Entities;
    using System.Threading.Tasks;
    using System.Xml.Serialization;
    using System.IO;
    using Newtonsoft.Json;
    using System.Xml.Linq;
    using System.Xml;
    using System.Net;
    using System.Xml.Serialization;
    using System.Text.RegularExpressions;
    using ISAEntityHelper.EntityHelper;
    using Terrasoft.Core.DB;
    using CSGResponse = DgIntegration.DgActivationCallbackService.CSGResponse;
    using DgIntegration.DgActivationCallbackService;
    
    [XmlSerializerFormat]
    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class UpdateOrderStatus31V2WebService : BaseService
    {
        private string _result;
		private SystemUserConnection _systemUserConnection;
		private SystemUserConnection SystemUserConnection
		{
			get
			{
				return _systemUserConnection ?? (_systemUserConnection = (SystemUserConnection)AppConnection.SystemUserConnection);
			}
		}

        [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml)]
        public CSGResponse.Envelope CallBack(Stream request)
        {
            var service = new ActivationCallbackService(SystemUserConnection);
			return service.Process<CSGResponse.Envelope>(request);
        }
    }
}