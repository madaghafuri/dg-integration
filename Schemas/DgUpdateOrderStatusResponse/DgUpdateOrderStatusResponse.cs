using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.UpdateOrderStatusResponse.Response
{
	[XmlRoot(ElementName="GUID", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
	public class GUID {
		[XmlAttribute(AttributeName="nil", Namespace="http://www.w3.org/2001/XMLSchema-instance")]
		public string Nil { get; set; }
	}

	[XmlRoot(ElementName="InstanceId", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
	public class InstanceId {
		[XmlAttribute(AttributeName="nil", Namespace="http://www.w3.org/2001/XMLSchema-instance")]
		public string Nil { get; set; }
	}

	[XmlRoot(ElementName="responseHeader", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
	public class ResponseHeader {
		[XmlElement(ElementName="ChannelId", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
		public string ChannelId { get; set; }
		[XmlElement(ElementName="ChannelMedia", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
		public string ChannelMedia { get; set; }
		[XmlElement(ElementName="GUID", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
		public GUID GUID { get; set; }
		[XmlElement(ElementName="InstanceId", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
		public InstanceId InstanceId { get; set; }
		[XmlElement(ElementName="ReferenceId", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
		public string ReferenceId { get; set; }
	}

	[XmlRoot(ElementName="resultStatus", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
	public class ResultStatus {
		[XmlElement(ElementName="ErrorCode", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
		public string ErrorCode { get; set; }
		[XmlElement(ElementName="ErrorDescription", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
		public string ErrorDescription { get; set; }
		[XmlElement(ElementName="StatusCode", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
		public string StatusCode { get; set; }
	}

	[XmlRoot(ElementName="UpdateOrderStatusResult", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
	public class UpdateOrderStatusResult {
		[XmlElement(ElementName="responseHeader", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
		public ResponseHeader ResponseHeader { get; set; }
		[XmlElement(ElementName="resultStatus", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
		public ResultStatus ResultStatus { get; set; }
	}

	[XmlRoot(ElementName="UpdateOrderStatusResponse", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
	public class UpdateOrderStatusResponse {
		[XmlElement(ElementName="UpdateOrderStatusResult", Namespace="http://schemas.datacontract.org/2004/07/DgNCCFIntegration.DgUpdateOrderStatusService.Response")]
		public UpdateOrderStatusResult UpdateOrderStatusResult { get; set; }
		[XmlAttribute(AttributeName="xmlns")]
		public string Xmlns { get; set; }
		[XmlAttribute(AttributeName="i", Namespace="http://www.w3.org/2000/xmlns/")]
		public string I { get; set; }
	}

}
