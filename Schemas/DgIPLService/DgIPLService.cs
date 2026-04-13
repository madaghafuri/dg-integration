using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Configuration;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Newtonsoft.Json;
using DgMasterData;
using DgCRMIntegration;
using DgIntegration.DgUpdateOffering;
using ISAIntegrationSetup;
using LookupConst = DgMasterData.DgLookupConst;

namespace DgIntegration.DgIPLService
{
    public class IPLService
    {
        private UserConnection userConnection;
        private UserConnection UserConnection {
            get {
                return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
            }
        }
        private string IPLDownloadPath;
        private string IPLCompletePath;
        private string IPLErrorPath;
        
        public IPLService(UserConnection UserConnection)
        {
            this.userConnection = UserConnection;

			var setup = IntegrationSetup.GetAllDefaultCustomAuth(UserConnection, "IPL", string.Empty);
			this.IPLDownloadPath = setup.FirstOrDefault(item => item.Key == "DownloadPath")?.Value;
			this.IPLCompletePath = setup.FirstOrDefault(item => item.Key == "CompletePath")?.Value;
			this.IPLErrorPath = setup.FirstOrDefault(item => item.Key == "ErrorPath")?.Value;

            // this.IPLDownloadPath = IntegrationSetup.GetCustomAuthValue(UserConnection, "IPL", "DownloadPath", string.Empty);
            // this.IPLCompletePath = IntegrationSetup.GetCustomAuthValue(UserConnection, "IPL", "CompletePath", string.Empty);
            // this.IPLErrorPath = IntegrationSetup.GetCustomAuthValue(UserConnection, "IPL", "ErrorPath", string.Empty);
        }

        protected string GetFullFilePath(string Path, string Search)
        {
            string[] dirs = Directory.GetFiles(Path, $"*{Search}*.*");
            return dirs.Length > 0 ? dirs[0] : string.Empty;
        }

        #region OFFERING
        
        public IPLResponse Offering()
        {
            var result = new IPLResponse();
            string filename = GetFullFilePath(this.IPLDownloadPath, "OFFER_");

			var offeringDeviceFromFile = new List<OFFERDEVICE>();
			var offeringDeviceFromDB = CRMHelper.GetOfferingRSRC(UserConnection, new List<Offering>());

			try {
				offeringDeviceFromFile = GetOfferingDevice();
			} catch (Exception) {}

            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                try {
                    string moveTo = Path.Combine(this.IPLCompletePath, Path.GetFileName(filename));
                    if(string.IsNullOrEmpty(filename)) {
                        throw new Exception("File IPL OFFER not found");
                    }

                    dbExecutor.StartTransaction();

                    new Delete(UserConnection)
                        .From("NCCF_TBLOFFERING")
                        .Execute();

                    var lines = System.IO.File.ReadLines(filename);
                    foreach (string line in lines) {
                        string[] columns = line.Split('|');

                        var nccfOffering = new OFFERING();
                        nccfOffering.BUSINESS_ID = columns[0];
                        nccfOffering.TELECOM_TYPE = columns[1];
                        nccfOffering.PAYMENT_TYPE = columns[2];
                        nccfOffering.OFFER_TYPE = columns[3];
                        nccfOffering.OFFER_ID = columns[4];
                        nccfOffering.OFFER_NAME = Regex.Replace(columns[5]?.Trim() ?? string.Empty, @"\s+", " ");
                        nccfOffering.OFFER_DESC = columns[6];
                        nccfOffering.PLAN_TYPE = columns[7];
                        nccfOffering.ACTIVE_FLAG = columns[8];

                        try {
                            if(!string.IsNullOrEmpty(columns[9])) {
                                nccfOffering.EFFECTIVE_DATE = DateTime
                                    .ParseExact(columns[9], "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture)
                                    .ToString("yyyy-MM-dd HH:mm:ss");
                            }

                            if(!string.IsNullOrEmpty(columns[10])) {
                                nccfOffering.EXPIRE_DATE = DateTime
                                    .ParseExact(columns[10], "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture)
                                    .ToString("yyyy-MM-dd HH:mm:ss");
                            }
                        } catch(Exception e) {}

                        nccfOffering.PRODUCT_LIST = columns[11];
                        nccfOffering.BUNDLE_FLAG = columns[12];
                        nccfOffering.IS_PROMOTION = columns[13];
                        nccfOffering.SUBSCRIBER_TYPE = columns[14];
                        nccfOffering.FAMILY_LEVEL = columns[15];
                        nccfOffering.CONTRACT_ID = columns[16];
                        nccfOffering.MAX_ORDER_TIMES = columns[17];
						
						// jika device
						if(nccfOffering.OFFER_NAME.Substring(0, 2) == "EB") {
							var offeringRSRC_file = offeringDeviceFromFile
								.Where(item => item.OFFER_ID == nccfOffering.OFFER_ID && item.DEVICE_DESCRIPTION == nccfOffering.OFFER_NAME)
								.ToList();

                            var offeringRSRC_db = offeringDeviceFromDB
                                .Where(item => item.OfferID == nccfOffering.OFFER_ID && item.DeviceDescription == nccfOffering.OFFER_NAME)
                                .ToList();

                            if(offeringRSRC_file.Count > 0) {
                                foreach(var offerRSRC in offeringRSRC_file) {
                                    var nccfOffering_detail = new OFFERING();

                                    nccfOffering_detail.BUSINESS_ID = nccfOffering.BUSINESS_ID;
                                    nccfOffering_detail.TELECOM_TYPE = nccfOffering.TELECOM_TYPE;
                                    nccfOffering_detail.PAYMENT_TYPE = nccfOffering.PAYMENT_TYPE;
                                    nccfOffering_detail.OFFER_TYPE = nccfOffering.OFFER_TYPE;
                                    nccfOffering_detail.OFFER_ID = nccfOffering.OFFER_ID;
                                    nccfOffering_detail.OFFER_NAME = offerRSRC.CONTRACT_NAME;
                                    nccfOffering_detail.OFFER_DESC = offerRSRC.CONTRACT_NAME;
                                    nccfOffering_detail.PLAN_TYPE = nccfOffering.PLAN_TYPE;
                                    nccfOffering_detail.ACTIVE_FLAG = nccfOffering.ACTIVE_FLAG;
                                    nccfOffering_detail.EFFECTIVE_DATE = nccfOffering.EFFECTIVE_DATE;
                                    nccfOffering_detail.EXPIRE_DATE = nccfOffering.EXPIRE_DATE;
                                    nccfOffering_detail.PRODUCT_LIST = nccfOffering.PRODUCT_LIST;
                                    nccfOffering_detail.BUNDLE_FLAG = nccfOffering.BUNDLE_FLAG;
                                    nccfOffering_detail.IS_PROMOTION = nccfOffering.IS_PROMOTION;
                                    nccfOffering_detail.SUBSCRIBER_TYPE = nccfOffering.SUBSCRIBER_TYPE;
                                    nccfOffering_detail.FAMILY_LEVEL = nccfOffering.FAMILY_LEVEL;
                                    nccfOffering_detail.CONTRACT_ID = offerRSRC.CONTRACT_ID;
                                    nccfOffering_detail.MAX_ORDER_TIMES = nccfOffering.MAX_ORDER_TIMES;
                                    nccfOffering_detail.ORACLE_ITEM_CODE = offerRSRC.ORACLE_ITEM_CODE;
                                    nccfOffering_detail.ORACLE_PACKAGE_CODE = offerRSRC.ORACLE_PACKAGE_CODE;

                                    InsertTempOffering(nccfOffering_detail);
                                }
                            } else if(offeringRSRC_db.Count > 0) {
                                foreach(var offerRSRC in offeringRSRC_db) {
                                    var nccfOffering_detail = new OFFERING();

                                    nccfOffering_detail.BUSINESS_ID = nccfOffering.BUSINESS_ID;
                                    nccfOffering_detail.TELECOM_TYPE = nccfOffering.TELECOM_TYPE;
                                    nccfOffering_detail.PAYMENT_TYPE = nccfOffering.PAYMENT_TYPE;
                                    nccfOffering_detail.OFFER_TYPE = nccfOffering.OFFER_TYPE;
                                    nccfOffering_detail.OFFER_ID = nccfOffering.OFFER_ID;
                                    nccfOffering_detail.OFFER_NAME = offerRSRC.ContractName;
                                    nccfOffering_detail.OFFER_DESC = offerRSRC.ContractName;
                                    nccfOffering_detail.PLAN_TYPE = nccfOffering.PLAN_TYPE;
                                    nccfOffering_detail.ACTIVE_FLAG = nccfOffering.ACTIVE_FLAG;
                                    nccfOffering_detail.EFFECTIVE_DATE = nccfOffering.EFFECTIVE_DATE;
                                    nccfOffering_detail.EXPIRE_DATE = nccfOffering.EXPIRE_DATE;
                                    nccfOffering_detail.PRODUCT_LIST = nccfOffering.PRODUCT_LIST;
                                    nccfOffering_detail.BUNDLE_FLAG = nccfOffering.BUNDLE_FLAG;
                                    nccfOffering_detail.IS_PROMOTION = nccfOffering.IS_PROMOTION;
                                    nccfOffering_detail.SUBSCRIBER_TYPE = nccfOffering.SUBSCRIBER_TYPE;
                                    nccfOffering_detail.FAMILY_LEVEL = nccfOffering.FAMILY_LEVEL;
                                    nccfOffering_detail.CONTRACT_ID = offerRSRC.ContractID;
                                    nccfOffering_detail.MAX_ORDER_TIMES = nccfOffering.MAX_ORDER_TIMES;
                                    nccfOffering_detail.ORACLE_ITEM_CODE = offerRSRC.OracleItemCode;
                                    nccfOffering_detail.ORACLE_PACKAGE_CODE = offerRSRC.OraclePackageCode;

                                    InsertTempOffering(nccfOffering_detail);
                                }
                            } else {
                                InsertTempOffering(nccfOffering);
                            }
						} else {
                            InsertTempOffering(nccfOffering);
                        }
                    }

                    var updateOfferingService = new UpdateOffering(UserConnection);
                    var updateResult = updateOfferingService.Process();

                    new Delete(UserConnection)
                        .From("NCCF_TBLOFFERING")
                        .Execute();

                    dbExecutor.CommitTransaction();

                    System.IO.File.Move(filename, moveTo);

                    result.Success = updateResult.Success;
                    result.Filename = filename;
                    result.ImportTo = "DgOffering";
                    result.AffectedRow = new AffectedRow() {
                        New = updateResult.AffectedRow?.New ?? 0,
                        Update = updateResult.AffectedRow?.Update ?? 0
                    };
                } catch(Exception e) {
                    dbExecutor.RollbackTransaction();
                    result.Message = e.Message;
					
					if(!string.IsNullOrEmpty(filename)) {
						System.IO.File.Move(filename, Path.Combine(this.IPLErrorPath, Path.GetFileName(filename)));	
					}
                }
            }

            return result;
        }

        // improve to insert bulk
        protected void InsertTempOffering(OFFERING data)
        {
            new Insert(UserConnection)
                .Into("NCCF_TBLOFFERING")
                .Set("OFFER_ID", Column.Parameter(data.OFFER_ID))
                .Set("OFFER_NAME", Column.Parameter(data.OFFER_NAME))
                .Set("OFFER_DESC", Column.Parameter(data.OFFER_DESC))
                .Set("PAYMENT_TYPE", Column.Parameter(data.PAYMENT_TYPE))
                .Set("OFFER_TYPE", Column.Parameter(data.OFFER_TYPE))
                .Set("EFFECTIVE_DATE", Column.Parameter(data.EFFECTIVE_DATE))
                .Set("EXPIRE_DATE", Column.Parameter(data.EXPIRE_DATE))
                .Set("BUSINESS_ID", Column.Parameter(data.BUSINESS_ID))
                .Set("TELECOM_TYPE", Column.Parameter(data.TELECOM_TYPE))
                .Set("PLAN_TYPE", Column.Parameter(data.PLAN_TYPE))
                .Set("ACTIVE_FLAG", Column.Parameter(data.ACTIVE_FLAG))
                .Set("PRODUCT_LIST", Column.Parameter(data.PRODUCT_LIST))
                .Set("BUNDLE_FLAG", Column.Parameter(data.BUNDLE_FLAG))
                .Set("IS_PROMOTION", Column.Parameter(data.IS_PROMOTION))
                .Set("SUBSCRIBER_TYPE", Column.Parameter(data.SUBSCRIBER_TYPE))
                .Set("FAMILY_LEVEL", Column.Parameter(data.FAMILY_LEVEL))
                .Set("CONTRACT_ID", Column.Parameter(data.CONTRACT_ID))
                .Set("MAX_ORDER_TIMES", Column.Parameter(data.MAX_ORDER_TIMES))
                .Set("ORACLE_ITEM_CODE", Column.Parameter(data.ORACLE_ITEM_CODE ?? string.Empty))
                .Set("ORACLE_PACKAGE_CODE", Column.Parameter(data.ORACLE_PACKAGE_CODE ?? string.Empty))
            .Execute();
        }

        #endregion

        #region OFFERDEVICE

        public IPLResponse OfferingDevice()
        {
            var result = new IPLResponse();
            string filename = GetFullFilePath(this.IPLDownloadPath, "OFFERDEVICE_");

            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                try {
                    string moveTo = Path.Combine(this.IPLCompletePath, Path.GetFileName(filename));
                    if(string.IsNullOrEmpty(filename)) {
                        throw new Exception("File IPL OFFERDEVICE not found");
                    }

                    dbExecutor.StartTransaction();

                    new Delete(UserConnection)
                        .From("NCCF_TBLOFFER_RSRC_PROD")
                        .Execute();

                    var offeringDevices = GetOfferingDevice();
                    foreach(var offerDevice in offeringDevices) {
                        InsertTempOfferingDevice(offerDevice);
                    }

                    var updateOffering = UpdateExistingOfferingDevice();
                    var newOffering = InsertNewOfferingDevice();

                    new Delete(UserConnection)
                        .From("NCCF_TBLOFFER_RSRC_PROD")
                        .Execute();

                    dbExecutor.CommitTransaction();

                    System.IO.File.Move(filename, moveTo);

                    result.Success = true;
                    result.Filename = filename;
                    result.ImportTo = "DgOfferingRSRC";
                    result.AffectedRow = new AffectedRow() {
                        New = newOffering,
                        Update = updateOffering
                    };
                } catch(Exception e) {
                    dbExecutor.RollbackTransaction();
                    result.Message = e.Message;

                    if(!string.IsNullOrEmpty(filename)) {
						System.IO.File.Move(filename, Path.Combine(this.IPLErrorPath, Path.GetFileName(filename)));	
					}
                }
            }

            return result;
        }

        public List<OFFERDEVICE> GetOfferingDevice()
        {
            var offeringDeviceList = new List<OFFERDEVICE>();
            string filename = GetFullFilePath(this.IPLDownloadPath, "OFFERDEVICE_");
            if(string.IsNullOrEmpty(filename)) {
                throw new Exception("File IPL OFFERDEVICE not found");
            }

            var lines = System.IO.File.ReadLines(filename);
            foreach (string line in lines) {
                string[] columns = line.Split('|');

                var offerDevice = new OFFERDEVICE();
                offerDevice.OFFER_ID = columns[0];
                offerDevice.PRODUCT_ID = columns[1];
                offerDevice.PRODUCT_NAME = columns[2];
                offerDevice.ORACLE_ITEM_CODE = columns[3];
                offerDevice.CONTRACT_TENURE = columns[4];
                offerDevice.ORACLE_PACKAGE_CODE = columns[5];
                offerDevice.DEVICE_DESCRIPTION = columns[6];
                offerDevice.CONTRACT_ID = columns[7];
                offerDevice.CONTRACT_NAME = columns[8];
                offerDevice.COLLECTION_TYPE = columns[9];

                offeringDeviceList.Add(offerDevice);
            }

            return offeringDeviceList;
        }

        // improve to insert bulk
        protected void InsertTempOfferingDevice(OFFERDEVICE data)
        {
            new Insert(UserConnection)
                .Into("NCCF_TBLOFFER_RSRC_PROD")
                .Set("OFFER_ID", Column.Parameter(data.OFFER_ID))
                .Set("PRODUCT_ID", Column.Parameter(data.PRODUCT_ID))
                .Set("PRODUCT_NAME", Column.Parameter(data.PRODUCT_NAME))
                .Set("ORACLE_ITEM_CODE", Column.Parameter(data.ORACLE_ITEM_CODE))
                .Set("CONTRACT_TENURE", Column.Parameter(data.CONTRACT_TENURE))
                .Set("ORACLE_PACKAGE_CODE", Column.Parameter(data.ORACLE_PACKAGE_CODE))
                .Set("DEVICE_DESCRIPTION", Column.Parameter(data.DEVICE_DESCRIPTION))
                .Set("CONTRACT_ID", Column.Parameter(data.CONTRACT_ID))
                .Set("CONTRACT_NAME", Column.Parameter(data.CONTRACT_NAME))
                .Set("COLLECTION_TYPE", Column.Parameter(data.COLLECTION_TYPE))
            .Execute();
        }

        protected int UpdateExistingOfferingDevice()
        {
            string sql = @"UPDATE DgOfferingRSRC SET
                DgOfferID = A.DgOfferID,
                DgProductName = A.DgProductName,
                DgProductID = A.DgProductID,
                DgContractID = A.DgContractID,
                DgContractName = A.DgContractName,
                DgDeviceDesc = A.DgDeviceDesc,
                DgOracleItemCode = A.DgOracleItemCode,
                DgOraclePackageCode = A.DgOraclePackageCode,
                DgContractTenure = A.DgContractTenure,
                DgCollectionType = A.DgCollectionType
            FROM (
                SELECT
                    NCCF_TBLOFFER_RSRC_PROD.OFFER_ID DgOfferID,
                    NCCF_TBLOFFER_RSRC_PROD.PRODUCT_NAME DgProductName,
                    NCCF_TBLOFFER_RSRC_PROD.PRODUCT_ID DgProductID,
                    NCCF_TBLOFFER_RSRC_PROD.CONTRACT_ID DgContractID,
                    NCCF_TBLOFFER_RSRC_PROD.CONTRACT_NAME DgContractName,
                    NCCF_TBLOFFER_RSRC_PROD.DEVICE_DESCRIPTION DgDeviceDesc,
                    NCCF_TBLOFFER_RSRC_PROD.ORACLE_ITEM_CODE DgOracleItemCode,
                    NCCF_TBLOFFER_RSRC_PROD.ORACLE_PACKAGE_CODE DgOraclePackageCode,
                    NCCF_TBLOFFER_RSRC_PROD.CONTRACT_TENURE DgContractTenure,
                    NCCF_TBLOFFER_RSRC_PROD.COLLECTION_TYPE DgCollectionType
                FROM NCCF_TBLOFFER_RSRC_PROD
                INNER JOIN DgOfferingRSRC ON 
                    DgOfferID = NCCF_TBLOFFER_RSRC_PROD.OFFER_ID
                    AND DgOracleItemCode = NCCF_TBLOFFER_RSRC_PROD.ORACLE_ITEM_CODE
                    AND DgOraclePackageCode = NCCF_TBLOFFER_RSRC_PROD.ORACLE_PACKAGE_CODE
                    AND DgContractName = NCCF_TBLOFFER_RSRC_PROD.CONTRACT_NAME
            ) A
            WHERE
                DgOfferingRSRC.DgOfferID = A.DgOfferID
                AND DgOfferingRSRC.DgOracleItemCode = A.DgOracleItemCode
                AND DgOfferingRSRC.DgOraclePackageCode = A.DgOraclePackageCode
                AND DgOfferingRSRC.DgContractName = A.DgContractName";

            int affectedRow = 0;
            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                affectedRow = query.Execute(dbExecutor);
            }
            
            return affectedRow;
        }

        protected int InsertNewOfferingDevice()
        {
            string sql = @"INSERT INTO DgOfferingRSRC (
                    DgOfferID,
                    DgProductName,
                    DgProductID,
                    DgContractID,
                    DgContractName,
                    DgDeviceDesc,
                    DgOracleItemCode,
                    DgOraclePackageCode,
                    DgContractTenure,
                    DgCollectionType
                )
                SELECT
                    NCCF_TBLOFFER_RSRC_PROD.OFFER_ID,
                    NCCF_TBLOFFER_RSRC_PROD.PRODUCT_NAME,
                    NCCF_TBLOFFER_RSRC_PROD.PRODUCT_ID,
                    NCCF_TBLOFFER_RSRC_PROD.CONTRACT_ID,
                    NCCF_TBLOFFER_RSRC_PROD.CONTRACT_NAME,
                    NCCF_TBLOFFER_RSRC_PROD.DEVICE_DESCRIPTION,
                    NCCF_TBLOFFER_RSRC_PROD.ORACLE_ITEM_CODE,
                    NCCF_TBLOFFER_RSRC_PROD.ORACLE_PACKAGE_CODE,
                    NCCF_TBLOFFER_RSRC_PROD.CONTRACT_TENURE,
                    NCCF_TBLOFFER_RSRC_PROD.COLLECTION_TYPE
                FROM NCCF_TBLOFFER_RSRC_PROD
                LEFT JOIN DgOfferingRSRC ON 
                    DgOfferID = NCCF_TBLOFFER_RSRC_PROD.OFFER_ID
                    AND DgOracleItemCode = NCCF_TBLOFFER_RSRC_PROD.ORACLE_ITEM_CODE
                    AND DgOraclePackageCode = NCCF_TBLOFFER_RSRC_PROD.ORACLE_PACKAGE_CODE
                    AND DgContractName = NCCF_TBLOFFER_RSRC_PROD.CONTRACT_NAME
                WHERE DgOfferingRSRC.Id IS NULL";
            
            int affectedRow = 0;
            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                affectedRow = query.Execute(dbExecutor);
            }

            return affectedRow;
        }

        #endregion

        #region OFFERREL
        
        public IPLResponse OfferingRelationship()
        {
            var result = new IPLResponse();
            string filename = GetFullFilePath(this.IPLDownloadPath, "OFFERREL_");
            
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                try {
                    string moveTo = Path.Combine(this.IPLCompletePath, Path.GetFileName(filename));
                    if(string.IsNullOrEmpty(filename)) {
                        throw new Exception("File IPL OFFERREL not found");
                    }

                    dbExecutor.StartTransaction();

                    new Delete(UserConnection)
                        .From("NCCF_TBLOFFER_RELATIONSHIP")
                        .Execute();

                    var lines = System.IO.File.ReadLines(filename);
                    foreach (string line in lines) {
                        string[] columns = line.Split('|');

                        var offerRel = new OFFERREL();
                        offerRel.OFFER_ID = columns[0];
                        offerRel.OTHER_OFFER_ID = columns[1];
                        offerRel.RELATIONSHIP_TYPE = columns[2];

                        InsertTempOfferingRelationship(offerRel);
                    }

                    var updateOffering = UpdateExistingOfferingRelationship();
                    var newOffering = InsertNewOfferingRelationship();

                    new Delete(UserConnection)
                        .From("NCCF_TBLOFFER_RSRC_PROD")
                        .Execute();

                    dbExecutor.CommitTransaction();

                    System.IO.File.Move(filename, moveTo);

                    result.Success = true;
                    result.Filename = filename;
                    result.ImportTo = "DgOfferingRelationship";
                    result.AffectedRow = new AffectedRow() {
                        New = newOffering,
                        Update = updateOffering
                    };
                } catch(Exception e) {
                    dbExecutor.RollbackTransaction();
                    result.Message = e.Message;

                    if(!string.IsNullOrEmpty(filename)) {
						System.IO.File.Move(filename, Path.Combine(this.IPLErrorPath, Path.GetFileName(filename)));	
					}
                }
            }

            return result;
        }

        protected void InsertTempOfferingRelationship(OFFERREL data)
        {
            new Insert(UserConnection)
                .Into("NCCF_TBLOFFER_RELATIONSHIP")
                .Set("OFFER_ID", Column.Parameter(data.OFFER_ID))
                .Set("OTHER_OFFER_ID", Column.Parameter(data.OTHER_OFFER_ID))
                .Set("RELATIONSHIP_TYPE", Column.Parameter(data.RELATIONSHIP_TYPE))
            .Execute();
        }

        protected int InsertNewOfferingRelationship()
        {
            string sql = @"INSERT INTO DgOfferingRelationship (
                    DgName,
                    DgOfferID,
                    DgOtherOffer,
                    DgRelationshipType
                )
                SELECT
                    NCCF_TBLOFFER_RELATIONSHIP.OFFER_ID,
                    NCCF_TBLOFFER_RELATIONSHIP.OFFER_ID,
                    NCCF_TBLOFFER_RELATIONSHIP.OTHER_OFFER_ID,
                    NCCF_TBLOFFER_RELATIONSHIP.RELATIONSHIP_TYPE
                FROM NCCF_TBLOFFER_RELATIONSHIP
                LEFT JOIN DgOfferingRelationship ON 
                    DgOfferID = NCCF_TBLOFFER_RELATIONSHIP.OFFER_ID
                    AND DgOtherOffer = NCCF_TBLOFFER_RELATIONSHIP.OTHER_OFFER_ID
                WHERE DgOfferingRelationship.Id IS NULL";
            
            int affectedRow = 0;
            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                affectedRow = query.Execute(dbExecutor);
            }

            return affectedRow;
        }

        protected int UpdateExistingOfferingRelationship()
        {
            string sql = @"UPDATE DgOfferingRelationship SET
                DgName = A.DgName,
                DgOfferID = A.DgOfferID,
                DgOtherOffer = A.DgOtherOffer,
                DgRelationshipType = A.DgRelationshipType
            FROM (
                SELECT
                    NCCF_TBLOFFER_RELATIONSHIP.OFFER_ID DgName,
                    NCCF_TBLOFFER_RELATIONSHIP.OFFER_ID DgOfferID,
                    NCCF_TBLOFFER_RELATIONSHIP.OTHER_OFFER_ID DgOtherOffer,
                    NCCF_TBLOFFER_RELATIONSHIP.RELATIONSHIP_TYPE DgRelationshipType
                FROM NCCF_TBLOFFER_RELATIONSHIP
                INNER JOIN DgOfferingRelationship ON 
                    DgOfferID = NCCF_TBLOFFER_RELATIONSHIP.OFFER_ID
                    AND DgOtherOffer = NCCF_TBLOFFER_RELATIONSHIP.OTHER_OFFER_ID
            ) A
            WHERE
                DgOfferingRelationship.DgOfferID = A.DgOfferID
                AND DgOfferingRelationship.DgOtherOffer = A.DgOtherOffer";

            int affectedRow = 0;
            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                affectedRow = query.Execute(dbExecutor);
            }
            
            return affectedRow;
        }
        
        #endregion

        #region OFFERBUNDLE
        
        public IPLResponse OfferingBundle()
        {
            var result = new IPLResponse();
            string filename = GetFullFilePath(this.IPLDownloadPath, "OFFERBUNDLE_");
            
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                try {
                    string moveTo = Path.Combine(this.IPLCompletePath, Path.GetFileName(filename));
                    if(string.IsNullOrEmpty(filename)) {
                        throw new Exception("File IPL OFFERBUNDLE not found");
                    }

                    dbExecutor.StartTransaction();

                    new Delete(UserConnection)
                        .From("NCCF_TBLOFFER_BUNDLE")
                        .Execute();

                    var lines = System.IO.File.ReadLines(filename);
                    foreach (string line in lines) {
                        string[] columns = line.Split('|');

                        var offerBundle = new OFFERBUNDLE();
                        offerBundle.OFFER_ID = columns[0];
                        offerBundle.GROUP_ID = columns[1];
                        offerBundle.GROUP_NAME = columns[2];
                        offerBundle.ELEMENT_ID = columns[3];
                        offerBundle.RELATIONSHIP_TYPE = columns[4];
                        offerBundle.MIN_NUM = columns[5];
                        offerBundle.MAX_NUM = columns[6];

                        InsertTempOfferingBundle(offerBundle);
                    }

                    var updateOffering = UpdateExistingOfferingBundle();
                    var newOffering = InsertNewOfferingBundle();

                    new Delete(UserConnection)
                        .From("NCCF_TBLOFFER_BUNDLE")
                        .Execute();

                    dbExecutor.CommitTransaction();

                    System.IO.File.Move(filename, moveTo);

                    result.Success = true;
                    result.Filename = filename;
                    result.ImportTo = "DgOfferingBundle";
                    result.AffectedRow = new AffectedRow() {
                        New = newOffering,
                        Update = updateOffering
                    };
                } catch(Exception e) {
                    dbExecutor.RollbackTransaction();
                    result.Message = e.Message;

                    if(!string.IsNullOrEmpty(filename)) {
						System.IO.File.Move(filename, Path.Combine(this.IPLErrorPath, Path.GetFileName(filename)));	
					}
                }
            }

            return result;
        }

        protected void InsertTempOfferingBundle(OFFERBUNDLE data)
        {
            new Insert(UserConnection)
                .Into("NCCF_TBLOFFER_BUNDLE")
                .Set("OFFER_ID", Column.Parameter(data.OFFER_ID))
                .Set("GROUP_ID", Column.Parameter(data.GROUP_ID))
                .Set("GROUP_NAME", Column.Parameter(data.GROUP_NAME))
                .Set("ELEMENT_ID", Column.Parameter(data.ELEMENT_ID))
                .Set("RELATIONSHIP_TYPE", Column.Parameter(data.RELATIONSHIP_TYPE))
                .Set("MIN_NUM", Column.Parameter(data.MIN_NUM))
                .Set("MAX_NUM", Column.Parameter(data.MAX_NUM))
            .Execute();
        }

        protected int InsertNewOfferingBundle()
        {
            string sql = @"INSERT INTO DgOfferingBundle (
                    DgOfferID,
                    DgGroupID,
                    DgGroupName,
                    DgElementID,
                    DgRelationshipType,
                    DgMinNum,
                    DgMaxNum
                )
                SELECT
                    NCCF_TBLOFFER_BUNDLE.OFFER_ID,
                    NCCF_TBLOFFER_BUNDLE.GROUP_ID,
                    NCCF_TBLOFFER_BUNDLE.GROUP_NAME,
                    NCCF_TBLOFFER_BUNDLE.ELEMENT_ID,
                    NCCF_TBLOFFER_BUNDLE.RELATIONSHIP_TYPE,
                    NCCF_TBLOFFER_BUNDLE.MIN_NUM,
                    NCCF_TBLOFFER_BUNDLE.MAX_NUM
                FROM NCCF_TBLOFFER_BUNDLE
                LEFT JOIN DgOfferingBundle ON 
                    DgOfferID = NCCF_TBLOFFER_BUNDLE.OFFER_ID
                    AND DgElementID = NCCF_TBLOFFER_BUNDLE.ELEMENT_ID
                WHERE DgOfferingBundle.Id IS NULL";
            
            int affectedRow = 0;
            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                affectedRow = query.Execute(dbExecutor);
            }

            return affectedRow;
        }

        protected int UpdateExistingOfferingBundle()
        {
            string sql = @"UPDATE DgOfferingBundle SET
                DgOfferID = A.DgOfferID,
                DgGroupID = A.DgGroupID,
                DgGroupName = A.DgGroupName,
                DgElementID = A.DgElementID,
                DgRelationshipType = A.DgRelationshipType,
                DgMinNum = A.DgMinNum,
                DgMaxNum = A.DgMaxNum
            FROM (
                SELECT
                    NCCF_TBLOFFER_BUNDLE.OFFER_ID DgOfferID,
                    NCCF_TBLOFFER_BUNDLE.GROUP_ID DgGroupID,
                    NCCF_TBLOFFER_BUNDLE.GROUP_NAME DgGroupName,
                    NCCF_TBLOFFER_BUNDLE.ELEMENT_ID DgElementID,
                    NCCF_TBLOFFER_BUNDLE.RELATIONSHIP_TYPE DgRelationshipType,
                    NCCF_TBLOFFER_BUNDLE.MIN_NUM DgMinNum,
                    NCCF_TBLOFFER_BUNDLE.MAX_NUM DgMaxNum
                FROM NCCF_TBLOFFER_BUNDLE
                INNER JOIN DgOfferingBundle ON 
                    DgOfferID = NCCF_TBLOFFER_BUNDLE.OFFER_ID
                    AND DgElementID = NCCF_TBLOFFER_BUNDLE.ELEMENT_ID
            ) A
            WHERE
                DgOfferingBundle.DgOfferID = A.DgOfferID
                AND DgOfferingBundle.DgElementID = A.DgElementID";

            int affectedRow = 0;
            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                affectedRow = query.Execute(dbExecutor);
            }
            
            return affectedRow;
        }

        #endregion

		#region OFFERUPDOWNGRADE
		
		public IPLResponse OfferUpdowngrade()
        {
            var result = new IPLResponse();
            string filename = GetFullFilePath(this.IPLDownloadPath, "OFFERUPDOWNGRADE_");
            
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                try {
                    string moveTo = Path.Combine(this.IPLCompletePath, Path.GetFileName(filename));
                    if(string.IsNullOrEmpty(filename)) {
                        throw new Exception("File IPL OFFERUPDOWNGRADE not found");
                    }

                    dbExecutor.StartTransaction();

                    new Delete(UserConnection)
                        .From("NCCF_TBLOFFERING_UPDOWNGRADE")
                        .Execute();

                    var lines = System.IO.File.ReadLines(filename);
                    foreach (string line in lines) {
                        string[] columns = line.Split('|');

                        var offerUpdowngrade = new OFFERUPDOWNGRADE();
                        offerUpdowngrade.OFFER_ID = columns[0];
                        offerUpdowngrade.OTHER_OFFER_ID = columns[1];
                        offerUpdowngrade.MIGRATE_TYPE = columns[2];

                        InsertTempOfferUpdowngrade(offerUpdowngrade);
                    }

                    var updateOffering = UpdateExistingOfferUpdowngrade();
                    var newOffering = InsertNewOfferUpdowngrade();

                    new Delete(UserConnection)
                        .From("NCCF_TBLOFFERING_UPDOWNGRADE")
                        .Execute();

                    dbExecutor.CommitTransaction();

                    System.IO.File.Move(filename, moveTo);

                    result.Success = true;
                    result.Filename = filename;
                    result.ImportTo = "DgOfferingUpdowngrade";
                    result.AffectedRow = new AffectedRow() {
                        New = newOffering,
                        Update = updateOffering
                    };
                } catch(Exception e) {
                    dbExecutor.RollbackTransaction();
                    result.Message = e.Message;

                    if(!string.IsNullOrEmpty(filename)) {
						System.IO.File.Move(filename, Path.Combine(this.IPLErrorPath, Path.GetFileName(filename)));	
					}
                }
            }

            return result;
        }

		protected void InsertTempOfferUpdowngrade(OFFERUPDOWNGRADE data)
        {
            new Insert(UserConnection)
                .Into("NCCF_TBLOFFERING_UPDOWNGRADE")
                .Set("OFFER_ID", Column.Parameter(data.OFFER_ID))
                .Set("OTHER_OFFER_ID", Column.Parameter(data.OTHER_OFFER_ID))
                .Set("MIGRATE_TYPE", Column.Parameter(data.MIGRATE_TYPE))
            .Execute();
        }

        protected int InsertNewOfferUpdowngrade()
        {
            string sql = @"INSERT INTO DgOfferingUpdowngrade (
                    DgOfferID,
                    DgOtherOfferID,
                    DgMigrateType
                )
                SELECT
                    NCCF_TBLOFFERING_UPDOWNGRADE.OFFER_ID,
                    NCCF_TBLOFFERING_UPDOWNGRADE.OTHER_OFFER_ID,
                    NCCF_TBLOFFERING_UPDOWNGRADE.MIGRATE_TYPE
                FROM NCCF_TBLOFFERING_UPDOWNGRADE
                LEFT JOIN DgOfferingUpdowngrade ON 
                    DgOfferID = NCCF_TBLOFFERING_UPDOWNGRADE.OFFER_ID
                    AND DgOtherOfferID = NCCF_TBLOFFERING_UPDOWNGRADE.OTHER_OFFER_ID
                WHERE DgOfferingUpdowngrade.Id IS NULL";
            
            int affectedRow = 0;
            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                affectedRow = query.Execute(dbExecutor);
            }

            return affectedRow;
        }

        protected int UpdateExistingOfferUpdowngrade()
        {
            string sql = @"UPDATE DgOfferingUpdowngrade SET
                DgOfferID = A.DgOfferID,
                DgOtherOfferID = A.DgOtherOfferID,
                DgMigrateType = A.DgMigrateType
            FROM (
                SELECT
                    NCCF_TBLOFFERING_UPDOWNGRADE.OFFER_ID DgOfferID,
                    NCCF_TBLOFFERING_UPDOWNGRADE.OTHER_OFFER_ID DgOtherOfferID,
                    NCCF_TBLOFFERING_UPDOWNGRADE.MIGRATE_TYPE DgMigrateType
                FROM NCCF_TBLOFFERING_UPDOWNGRADE
                INNER JOIN DgOfferingUpdowngrade ON 
                    DgOfferID = NCCF_TBLOFFERING_UPDOWNGRADE.OFFER_ID
                    AND DgOtherOfferID = NCCF_TBLOFFERING_UPDOWNGRADE.OTHER_OFFER_ID
            ) A
            WHERE
                DgOfferingUpdowngrade.DgOfferID = A.DgOfferID
                AND DgOfferingUpdowngrade.DgOtherOfferID = A.DgOtherOfferID";

            int affectedRow = 0;
            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                affectedRow = query.Execute(dbExecutor);
            }
            
            return affectedRow;
        }

		#endregion
    }

    public class IPLResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Filename { get; set; }
        public string ImportTo { get; set; }
        public AffectedRow AffectedRow { get; set; }
    }

    public class AffectedRow
    {
        public int New { get; set; }
        public int Update { get; set; }
    }

    public class OFFERING
    {
        public string BUSINESS_ID { get; set; }
        public string TELECOM_TYPE { get; set; }
        public string PAYMENT_TYPE { get; set; }
        public string OFFER_TYPE { get; set; }
        public string OFFER_ID { get; set; }
        public string OFFER_NAME { get; set; }
        public string OFFER_DESC { get; set; }
        public string PLAN_TYPE { get; set; }
        public string ACTIVE_FLAG { get; set; }
        public string EFFECTIVE_DATE { get; set; }
        public string EXPIRE_DATE { get; set; }
        public string PRODUCT_LIST { get; set; }
        public string BUNDLE_FLAG { get; set; }
        public string IS_PROMOTION { get; set; }
        public string SUBSCRIBER_TYPE { get; set; }
        public string FAMILY_LEVEL { get; set; }
        public string CONTRACT_ID { get; set; }
        public string MAX_ORDER_TIMES { get; set; }
        public string ORACLE_ITEM_CODE { get; set; }
        public string ORACLE_PACKAGE_CODE { get; set; }
    }

    public class OFFERDEVICE
    {
        public string OFFER_ID { get; set; }
        public string PRODUCT_ID { get; set; }
        public string PRODUCT_NAME { get; set; }
        public string ORACLE_ITEM_CODE { get; set; }
        public string CONTRACT_TENURE { get; set; }
        public string ORACLE_PACKAGE_CODE { get; set; }
        public string DEVICE_DESCRIPTION { get; set; }
        public string CONTRACT_ID { get; set; }
        public string CONTRACT_NAME { get; set; }
        public string COLLECTION_TYPE { get; set; }
    }

    public class OFFERREL
    {
        public string OFFER_ID { get; set; }
        public string OTHER_OFFER_ID { get; set; }
        public string RELATIONSHIP_TYPE { get; set; }
    }

    public class OFFERBUNDLE
    {
        public string OFFER_ID { get; set; }
        public string GROUP_ID { get; set; }
        public string GROUP_NAME { get; set; }
        public string ELEMENT_ID { get; set; }
        public string RELATIONSHIP_TYPE { get; set; }
        public string MIN_NUM { get; set; }
        public string MAX_NUM { get; set; }
    }

	public class OFFERUPDOWNGRADE
	{
		public string OFFER_ID { get; set; }
		public string OTHER_OFFER_ID { get; set; }
		public string MIGRATE_TYPE { get; set; }
	}
}