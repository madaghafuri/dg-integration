namespace DgCSGIntegration.DgOrderFees
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class OrderFeesResponse
    {
        public RetrieveFeesForOrderResponse RetrieveFeesForOrderResponse { get; set; }
    } 

    public class RetrieveFeesForOrderResponse
    {
        public FeesList FeesList { get; set; }
    } 

    public class FeesList
    {
        public List<FeesRecord> FeesRecord { get; set; }
    } 

    public class FeesRecord
    {
        public string OfferId { get; set; }
        public string FeeName { get; set; }
        public string FeeItemCode { get; set; }
        public string FeeType { get; set; }
        public string OFSCode { get; set; }
        public string PaymentType { get; set; }
        public TaxList TaxList { get; set; }
        public decimal FeeAmount { get; set; }
        public Resource Resource { get; set; }
        public decimal OriginalFeeAmount { get; set; }
    }

    public class TaxList
    {
        public List<TaxRecord> TaxRecord { get; set; }
    }

    public class TaxRecord
    {
        public decimal TaxAmount { get; set; }
        public string TaxCode { get; set; }
        public string TaxName { get; set; }
    }

    public class Resource
    {
        public string ResourceCode { get; set; }
        public string ResourceType { get; set; }
        public string ResourceModelId { get; set; } // ofs code
    } 
}