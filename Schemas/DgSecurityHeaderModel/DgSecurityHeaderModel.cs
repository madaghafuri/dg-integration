using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace DgBaseService.DgHelpers
{
    [XmlRoot(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
    public class Security 
    {
        [XmlElement(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
        public UsernameToken UsernameToken { get; set; }

        [XmlAttribute(Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
        public string mustUnderstand 
        { 
            get
            {
                return "1";
            } 
            set {}     
        }

        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns
        {
            get
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("wsse", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
                ns.Add("wsu", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");

                return ns;
            }
            set {}
        }
    }

    [XmlRoot(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
	public class UsernameToken 
    {
		[XmlElement(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public string Username { get; set; }

		[XmlElement(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public Password Password { get; set; }

		[XmlElement(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
		public Nonce Nonce { get; set; }

		[XmlElement(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
		public string Created { get; set; }

		[XmlAttribute(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
		public string Id { get; set; }
	}

    [XmlRoot(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
	public class Password 
    {
		[XmlAttribute]
		public string Type 
        { 
            get 
            {
                return "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest";
            }
            set {}
        }
        
		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(Namespace="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
	public class Nonce 
    {
		[XmlAttribute]
		public string EncodingType 
        { 
            get
            {
                return "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";
            } 
            set {} 
        }

		[XmlText]
		public string Text { get; set; }
	}	
}