using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace DgIntegration.DgGetAccountService.Response
{
	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Envelope 
	{
		[XmlElement(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
		public Body Body { get; set; }

		[XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");

                return ns;
            }
            set { }
        }
	}

	[XmlRoot(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
	public class Body 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public getAccountsResponse getAccountsResponse { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
	public class getAccountsResponse 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public GetAccountsReply GetAccountsReply { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public ResultOfOperationReply ResultOfOperationReply { get; set; }

		[XmlAttribute(Namespace="http://www.w3.org/2000/xmlns/")]
		public string sch { get; set; }

		[XmlAttribute(Namespace="http://www.w3.org/2000/xmlns/")]
		public string bas { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
	public class GetAccountsReply 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public getAccountResult getAccountResult { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class getAccountResult 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string accountId { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string customerId { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string accountCode { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string billcycleType { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string title { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string accountName { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string converge_flag { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string billLanguage { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string initialCreditLimit { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string status { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string creditLimitNotifyPercentages { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string acla { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string noDunningFlag { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string creditTerm { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string email { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string isPaymentResponsible { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public addressInfo addressInfo { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public paymentModeInfo paymentModeInfo { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public relaSubscribers relaSubscribers { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string info2 { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class addressInfo 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string address1 { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string address2 { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string addressCity { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string addressCountry { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string addressPostCode { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string addressProvince { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string contactType { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string email1 { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string smsNo { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class paymentModeInfo 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string bankAcctNo { get; set; }
		
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string tokenId { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string bankIssuer { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string cardExpDate { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string cardType { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string ownerName { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string paymentId { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string paymentMode { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class relaSubscribers 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string subscriberId { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string relaType { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
	public class ResultOfOperationReply 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string resultCode { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string resultMessage { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string beId { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string transactionId { get; set; }
	}
}