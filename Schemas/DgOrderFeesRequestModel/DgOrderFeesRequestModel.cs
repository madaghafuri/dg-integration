namespace DgCSGIntegration.DgOrderFees
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class OrderFeesRequest
    {
        public RetrieveFeesForOrderRequest RetrieveFeesForOrderRequest { get; set; }
    }

    public class RetrieveFeesForOrderRequest
    {
        public List<ContractList> ContractList { get; set; }
        public string OrderType { get; set; }
        public string MSISDN { get; set; }
        public SubscribedOffersList SubscribedOffersList { get; set; }
        public string CustomerId { get; set; }
        public string CustomerNationality { get; set; }
        public string DealerCode { get; set; }
        public string ICCID { get; set; }
        public string NewMSISDN { get; set; }
        public string PayType { get; set; }
        public bool PayTypeSpecified { get; set; }
        public ResourceList ResourceList { get; set; }
        public string SubscriberType { get; set; }
        public string TelecomType { get; set; }
        public UnsubscribedOffersList UnsubscribedOffersList { get; set; }
    }

    public class ContractList
    {
        public string ContractDuration { get; set; }
        public string ContractId { get; set; }
        public string ContractType { get; set; }
        public string RelatedOfferId { get; set; }
    }

    public class SubscribedOffersList
    {
        public List<string> OfferId { get; set; }
    }

    public class ResourceList
    {
        public List<ResourceRecord> ResourceRecord { get; set; }
    }

    public class ResourceRecord
    {
        public string IMEI { get; set; }
        public string OfferId { get; set; }
        public string ProductId { get; set; }
    }

    public class UnsubscribedOffersList
    {
        public List<string> OfferId { get; set; }
    }
}