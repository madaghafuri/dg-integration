using System;
using System.Collections;
using System.Collections.Generic;

namespace DgIntegration.DgCreateCustomerSalesOrderUERPService
{
    public class UERPRequest 
    {
        public UERPCreateCustomerSalesOrderRequest UERPCreateCustomerSalesOrderRequest { get; set; }
    }

    public class UERPCreateCustomerSalesOrderRequest 
    {
        public string OrigSysDocumentRef { get; set; }
        public string SourceTransactionSystem { get; set; }
        public string CustomerName { get; set; }
        public string OrderType { get; set; }
        public string PaymentTerms { get; set; }
        public string SalesChannelCode { get; set; }
        public string OrderedDate { get; set; }
        public string RequestedFulfillmentOrganizationCode { get; set; }
        public string CustomerNumber { get; set; }
        public string Country { get; set; }
        public string Address1 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string CustomerProfileClassName { get; set; }
        public string TIN { get; set; }
		public string Email { get; set; }
        public string SST { get; set; }
		public string BRN { get; set; }
        public string ShipToOrg { get; set; }
        public string InvoiceToOrg { get; set; }
        public string CustomerAttributes { get; set; }
        public string CustomerNamePhonetic { get; set; }
        public List<HeaderEffBCustomer> HeaderEffBCustomer_AdditionalAddress_DetailsprivateVO { get; set; }
        public List<BillToCustomer> BillToCustomer { get; set; }
        public List<ShipToCustomer> ShipToCustomer { get; set; }
        public List<SalesOrderLine> SalesOrderLine { get; set; }
    }

    public class HeaderEffBCustomer 
    {
        public string corporateIndividualCustomerName { get; set; }
        public string addressLine1 { get; set; }
        public string addressLine2 { get; set; }
        public string addressLine3 { get; set; }
        public string postCode { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
    }

    public class BillToCustomer 
    {
        public string CustomerName { get; set; }
        public string CustomerNumber { get; set; }
    }

    public class ShipToCustomer
    {
        public string CustomerName { get; set; }
    }

    public class SalesOrderLine 
    {
        public string OrigSysLineRef { get; set; }
        public int OrderedQuantity { get; set; }
        public int UnitListPrice { get; set; }
        public int UnitSellingPrice { get; set; }
        public string InventoryItem { get; set; }
        public string OrderedDate { get; set; }
        public List<manualPriceAdjustments>  manualPriceAdjustments { get; set; }
        public List<additionalInformation> additionalInformation { get; set; }
    }

    public class manualPriceAdjustments
    {
        public int UnitSellingPrice { get; set; }
    }

    public class additionalInformation
    {
        public List<FulfillLineEffBSales> FulfillLineEffBSales_OrderLineLevelAdditional_InformationprivateVO { get; set; }
        public List<FulfillLineEffB3PPprivateVO> FulfillLineEffB3PPprivateVO { get; set; }
    }

    public class FulfillLineEffBSales
    {
        public string sfaLeadsId { get; set; }
        public string sfaOrderReservationId { get; set; }
    }

    public class FulfillLineEffB3PPprivateVO 
    {
        public string SubscriberType { get; set; }
        public string OfferType { get; set; }
        public string PromoCode { get; set; }
        public string ChargeToBill { get; set; }
    }

    public class UERPResponse
    {
        public string Acknowledgement { get; set; }
    }
}