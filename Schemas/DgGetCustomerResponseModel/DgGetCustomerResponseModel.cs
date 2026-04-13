using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace DgIntegration.DgGetCustomerService.Response
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
		public getCustomersResponse getCustomersResponse { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
	public class getCustomersResponse 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public GetCustomersReply GetCustomersReply { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public ResultOfOperationReply ResultOfOperationReply { get; set; }

		[XmlNamespaceDeclarations]
		public XmlSerializerNamespaces xmlns
		{
			get
			{
				XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
				ns.Add("sch", "http://oss.huawei.com/webservice/external/services/schema");
				ns.Add("bas", "http://oss.huawei.com/webservice/external/services/basetype/");
				return ns;
			}
			set { }
		}
	}

	[XmlRoot(ElementName="GetCustomersReply", Namespace="http://oss.huawei.com/webservice/external/services/schema")]
	public class GetCustomersReply 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public List<getCustomersResult> getCustomersResult { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
		public string totalNumOfRecords { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/schema")]
	public class getCustomersResult 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string customerId { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string customerFlag { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string customerCode { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string idType { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string idNumber { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string title { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string firstName { get; set; }
		
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string nationality { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string customerLang { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string customerLevel { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string customerGroup { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string race { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string occupation { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string customerDateofBirth { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string customerGender { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string maritalStatus { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string createDate { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string customerStatus { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public List<customerAddressInfos> customerAddressInfos { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public corporationInfo corporationInfo { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public customerRelationInfos customerRelationInfos { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string info1 { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class customerAddressInfos 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string addressCountry { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string addressProvince { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string addressCity { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string addressPostCode { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string address1 { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string address2 { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string contactType { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class corporationInfo 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string corpNumber { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string companyName { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string hierarchy { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string topParentCustomerId { get; set; }
		
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string parentCustomerId { get; set; }
		
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public List<subCustomerList> subCustomerList { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string businessRegistrationNumber { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string expiryDateofBRN { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string industrySegment { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string phoneNumber { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string email { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string geographicalSpread { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string sow { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string accountValue { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string dateofIncorporation { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string numberofEmployees { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string enterpriseCustomerType { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public List<picInfos> picInfos { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public accountManagerInfo accountManagerInfo { get; set; }
	}
	
	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class subCustomerList 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string companyName { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string customerId { get; set; }
	}
	
	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class picInfos
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string email { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string gender { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string idNumber { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string idType { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string name { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string nationality { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string phoneNumber { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string picSeq { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string picType { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string race { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string title { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string isNotificationPerson { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class accountManagerInfo
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string name { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string phoneNumber { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string dealerCode { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string email { get; set; }
	}

	[XmlRoot(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
	public class customerRelationInfos 
	{
		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string relaEmail { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string relaName1 { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string relaSeq { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string relaTel1 { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string beginTimeForWeekend { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string endTimeForWeekend { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string beginTimeForBusiDay { get; set; }

		[XmlElement(Namespace="http://oss.huawei.com/webservice/external/services/basetype/")]
		public string endTimeForBusiDay { get; set; }
	}

	[XmlRoot(Namespace = "http://oss.huawei.com/webservice/external/services/schema")]
    public class ResultOfOperationReply
    {
        [XmlElement(Namespace = "http://oss.huawei.com/webservice/external/services/basetype/")]
        public string resultCode { get; set; }

        [XmlElement(Namespace = "http://oss.huawei.com/webservice/external/services/basetype/")]
        public string resultMessage { get; set; }

        [XmlElement(Namespace = "http://oss.huawei.com/webservice/external/services/basetype/")]
        public string beId { get; set; }
        
        [XmlElement(Namespace = "http://oss.huawei.com/webservice/external/services/basetype/")]
        public string transactionId { get; set; }
    }
}