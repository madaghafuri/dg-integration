using System;
using System.Collections.Generic;

namespace DgIntegration.DgCommonInventory
{
    #region Token
        
    public class TokenRequest
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string grant_type { get; set; }
		
		public Dictionary<string, string> ToDictionary()
		{
			return new Dictionary<string, string>() {
				{"client_id", this.client_id},
				{"client_secret", this.client_secret},
				{"grant_type", this.grant_type}
			};
		}
    }

    public class TokenSuccessResponse
    {
        public string access_token { get; set; }
        public string scope { get; set; }
        public string token_type { get; set; }
        public long expires_in { get; set; }
    }

    public class TokenErrorResponse
    {
        public string error_description { get; set; }
        public string error { get; set; }
    }

    #endregion

    #region CheckStock
        
    public class CheckStockRequest
    {
        public string itemLocationId { get; set; }
        public string itemCode { get; set; }
        public string userId { get; set; }
    }

    public class CheckStockResponse
    {
        public List<CheckStockResult> result { get; set; }
    }

    public class CheckStockResult
    {
        public string dtpprice { get; set; }
        public string rrpPrice { get; set; }
        public string otpPrice { get; set; }
        public string cmtpPrice { get; set; }
        public string sapMaterialCode { get; set; }
        public string itemCode { get; set; }
        public string deviceModelDesc { get; set; }
		public string commonCode { get; set; }
        public string deviceTypeId { get; set; }
        public string color { get; set; }
        public string itemLocationId { get; set; }
        public string deviceModelId { get; set; }
        public string manufacturerId { get; set; }
        public string model { get; set; }
        public string inventoryItemTypeId { get; set; }
        public int TotalUnAvailableQty { get; set; }
        public int ReservedQty { get; set; }
        public int InTransitQty { get; set; }
        public int RTVQty { get; set; }
        public int TotalAvailableQty { get; set; }
        public int CustReservedQty { get; set; }
        public int AdjustUnavailQty { get; set; }
        public int totalQntity { get; set; }    
    }

    #endregion

    #region Reserve Stock
    public class ReserveStockRequest
    {
        public stockReserveQuantityInput stockReserveQuantityInput { get; set; }
    }

    public class stockReserveQuantityInput
    {
        public string storeId { get; set; }
        public string reservationId { get; set; }
        public listOfItemDetailRequest listOfItemDetailRequest { get; set; }
    }

    public class listOfItemDetailRequest
    {
        public List<itemDetailRequest> itemDetailRequest { get; set; }
    }

    public class itemDetailRequest
    {
        public List<listOfAttributes> listOfAttributes { get; set; }
        public string ProductType { get; set; }
        public string Quantity { get; set; }
    }

    public class listOfAttributes
    {
        public List<attributes> attributes { get; set; }
    }

    public class attributes
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class ReserveStockResponse
    {
        public stockReserveQuantityOutput stockReserveQuantityOutput { get; set; }
    }
    public class stockReserveQuantityOutput
    {
        public string status { get; set; }
        public string storeId { get; set; }
        public listOfItemDetail listOfItemDetail { get; set; }
    }

    public class listOfItemDetail
    {
        public List<itemDetail> itemDetail { get; set; }
    }

    public class itemDetail
    {
        public string partNumber { get; set; }
        public string reservationStatus { get; set; }
        public string category { get; set; }
        public string brand { get; set; }
        public listOfAttributes listOfAttributes { get; set; }
    }

    #endregion

    #region Unreserve Stock

    public class UnreserveStockRequest
    {
        public stockUnreserveQuantityRequest stockUnreserveQuantityRequest { get; set; } 
    }

    public class stockUnreserveQuantityRequest
    {
        public requestBody requestBody { get; set; }
        public requestHeader requestHeader { get; set; }
    }

    public class requestBody
    {
        public string reservationId { get; set; }
        public string storeId { get; set; }
        public string userId { get; set; }
    }

    public class requestHeader
    {
        public string eventName { get; set; }
        public string sourceSystem { get; set; }
    }

    public class UnreserveStockResponse
    {
        public stockUnreserveQuantityResponse stockUnreserveQuantityResponse { get; set; }
    }

    public class stockUnreserveQuantityResponse
    {
        public responseHeader responseHeader { get; set; }
        public responseBody responseBody { get; set; }
    }

    public class responseHeader
    {
        public string eventName { get; set; }
        public string transDateTime { get; set; }
        public string statusCode { get; set; }
        public string statusDescription { get; set; }
        public string eAIId { get; set; }
    }

    public class responseBody
    {
        public string storeId { get; set; }
        public listOfItemDetails listOfItemDetails { get; set; }
    }

    public class listOfItemDetails
    {
        public itemDetails itemDetails { get; set; }
    }

    public class itemDetails
    {
        public string reservationId { get; set; }
    }

    #endregion

    #region CreateDelivery
    
    public class CreateDeliveryRequest
    {
        public requestBodyCD requestBody { get; set; }
    }

    public class requestBodyCD
    {
        public string brnno { get; set; }
        public string city { get; set; }
        public string contactno { get; set; }
        public string name1 { get; set; }
        public string country { get; set; }
        public string deliveryaddr1 { get; set; }
        public string deliveryaddr2 { get; set; }
        public string info1 { get; set; }
        public string info2 { get; set; }
        public string info3 { get; set; }
        public string info4 { get; set; }
        public string info5 { get; set; }
        public string postcode { get; set; }
        public string saleschannel { get; set; }
        public string statecode { get; set; }
        public string storeid { get; set; }
        public string orderno { get; set; }
        public string reservationid { get; set; }
        public List<itemlist> itemlist { get; set; }
    }

    public class itemlist 
    {
        public string item_code { get; set; }
        public string item_no { get; set; }
        public string msisdn { get; set; }
        public string quantity { get; set; }
        public string text { get; set; }
    }

	public class CreateDeliveryResponse
	{
		public responseBodyCD responseBody { get; set; }
	}

	public class responseBodyCD 
	{
		public string status { get; set; }
		public string messages { get; set; }
	}

    #endregion

    #region Error

    public class ErrorResponse
    {
        public requestError requestError { get; set; }
    }

    public class requestError
    {
        public serviceException serviceException { get; set; }
    }

    public class serviceException
    {
        public string messageId { get; set; }
        public string text { get; set; }
    }

    public class UnauthorizedResponse
    {
        public fault fault { get; set; }
    }

    public class fault
    {
        public string code { get; set; }
        public string message { get; set; }
        public string description { get; set; }
    }

    #endregion
}