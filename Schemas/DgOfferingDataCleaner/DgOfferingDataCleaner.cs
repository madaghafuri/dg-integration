using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Data;
using System.Data.SqlClient;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Newtonsoft.Json;
using DgBaseService.DgHelpers;
using DgMasterData;
using LookupConst = DgMasterData.DgLookupConst;

namespace DgIntegration.DgOfferingDataCleaner
{
    public class OfferingDataCleaner
    {
        private UserConnection userConnection;
        protected UserConnection UserConnection {
            get {
                return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
            }
        }

        public OfferingDataCleaner(UserConnection UserConnection)
        {
            this.userConnection = UserConnection;
        }

        public void Clean()
        {
            CleanInvalidData();
            CleanInvalidOfferDevice();
        }

        #region Invalid Offer Device

        public void CleanInvalidOfferDevice()
        {
            var data = GetInvalidOfferDevice();
            foreach (var offer in data) {
                using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                    dbExecutor.StartTransaction();

                    try {
                        var offeringRSRC = GetOfferingRSRC(offer.OfferID, offer.OfferName).FirstOrDefault();
                        var offeringDevice = GetOffering(offer.OfferID, string.Empty);

                        bool isDeleteOfferRSRC = offeringRSRC != null;
                        if(isDeleteOfferRSRC) {
                            new Delete(UserConnection)
                                .From("DgOfferingRSRC")
                                .Where("Id").IsEqual(Column.Parameter(offeringRSRC.Id))
                            .Execute();
                        }

                        Offering validOfferingDevice = isDeleteOfferRSRC ? 
                            offeringDevice
                                .Where(item => item.OracleItemCode == offeringRSRC.OracleItemCode && item.OraclePackageCode == offeringRSRC.OraclePackageCode)
                                .FirstOrDefault() : null;
                        bool isValidExists = isDeleteOfferRSRC && validOfferingDevice != null;

                        var invalidOfferDevices = offeringDevice
                            .Where(item => string.IsNullOrEmpty(item.OracleItemCode) && string.IsNullOrEmpty(item.OraclePackageCode))
                            .ToList();

                        foreach (var invalidOfferDevice in invalidOfferDevices) {
                            try {
                                new Delete(UserConnection)
                                    .From("DgOffering")
                                    .Where("Id").IsEqual(Column.Parameter(invalidOfferDevice.Id))
                                .Execute();
                            } catch (Exception e) {
                                for (int i = 1; i <= 20; i++) {
                                    new Update(UserConnection, "DgLineDetail")
                                        .Set($"DgSuppOffer{i}Id", isValidExists ? Column.Parameter(validOfferingDevice.Id) : Column.Parameter(null, "Guid"))
                                        .Where($"DgSuppOffer{i}Id").IsEqual(Column.Parameter(invalidOfferDevice.Id))
                                    .Execute();
                                }

                                new Delete(UserConnection)
                                    .From("DgOffering")
                                    .Where("Id").IsEqual(Column.Parameter(invalidOfferDevice.Id))
                                .Execute();
                            }
                        }

                        dbExecutor.CommitTransaction();
                    } catch (Exception e) {
                        dbExecutor.RollbackTransaction();
                        throw;
                    }
                }   
            }
        }

        public List<Offering> GetInvalidOfferDevice()
        {
            var result = new List<Offering>();

            string sql = $@"SELECT
                DgOfferID,
                DgOfferName,
                COUNT(*) Duplicate
            FROM DgOffering
            WHERE 
                DgOfferName LIKE 'EB%'
                AND DgOfferTypeId = '{LookupConst.OfferType.SupplementaryOffering.ToString()}'
                AND DgSubscriberTypeId IS NULL
                AND ((DgOracleItemCode IS NULL OR DgOracleItemCode = '') OR (DgOraclePackageCode IS NULL OR DgOraclePackageCode = ''))
            GROUP BY DgOfferID, DgOfferName
            HAVING COUNT(*) > 1";

            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                
                using (IDataReader dataReader = query.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        var data = new Offering();
                        data.OfferID = dataReader.GetColumnValue<string>("DgOfferID");
                        data.OfferName = dataReader.GetColumnValue<string>("DgOfferName");

                        result.Add(data);
                    }
                }
            }

            return result;
        }

        public List<Offering> GetOffering(string OfferID = "", string OfferName = "")
        {
            var result = new List<Offering>();

            bool isOfferIDExists = !string.IsNullOrEmpty(OfferID);
            bool isOfferNameExists = !string.IsNullOrEmpty(OfferName);

            if(!isOfferIDExists && !isOfferNameExists) {
                return result;
            }

            string sql = @"SELECT 
                DgOffering.Id AS Id,
                DgOffering.DgOfferID AS OfferID,
                DgOffering.DgOfferName AS OfferName,
                DgOffering.DgOfferTypeId AS OfferType_Id,
                DgOfferType.Name AS OfferType_Name,
                DgOffering.DgSubscriberTypeId AS SubscriberType_Id,
                DgSubscriberType.Name AS SubscriberType_Name,
                DgSubscriberType.DgCode AS SubscriberType_Code,
                DgOffering.DgEffectiveDate AS EffectiveDate,
                DgOffering.DgExpiryDate AS ExpiryDate,
                DgOffering.DgBundleFlag AS BundleFlag,
                DgOffering.DgMaxOrdersTime AS MaxOrdersTime,
                DgOffering.DgOracleItemCode AS OracleItemCode,
                DgOffering.DgOraclePackageCode AS OraclePackageCode,
                DgOffering.DgOfferDesc AS OfferDesc,
                DgOffering.DgCategory AS Category,
                DgOffering.DgBusinessID AS BusinessID,
                CASE 
                    WHEN DgOffering.DgIsPromotion = '1' THEN 1
                    ELSE 0
                END AS IsPromotion,
                DgOffering.DgActiveFlag AS ActiveFlag,
                DgOffering.DgPlanType AS PlanType,
                DgOffering.DgTelcomType AS TelcomType,
                DgOffering.DgPaymentType AS PaymentType,
                DgOffering.DgFamilyLevel AS FamilyLevel,
                DgOffering.DgContractId AS ContractID,
                DgOffering.DgDgProductList AS ProductList
            FROM DgOffering
            LEFT JOIN DgOfferType ON DgOfferType.Id = DgOffering.DgOfferTypeId
            LEFT JOIN DgSubscriberType ON DgSubscriberType.Id = DgOffering.DgSubscriberTypeId";
            
            var where = new List<string>();
            if(isOfferIDExists) {
                where.Add($"DgOffering.DgOfferID = '{OfferID}'");
            }

            if(isOfferNameExists) {
                where.Add($"DgOffering.DgOfferName = '{OfferName}'");
            }

            if(where.Count > 0) {
                sql += " WHERE " + string.Join(" AND ", where.ToArray());
            }

            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                
                using (IDataReader dataReader = query.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        var data = new Offering();
                        data.Id = dataReader.GetColumnValue<Guid>("Id");
                        data.OfferID = dataReader.GetColumnValue<string>("OfferID");
                        data.OfferName = dataReader.GetColumnValue<string>("OfferName");

                        Guid offerTypeId = dataReader.GetColumnValue<Guid>("OfferType_Id");
                        data.OfferType = offerTypeId != Guid.Empty ? new DgMasterData.Lookup() {
                            Id = offerTypeId,
                            Name = dataReader.GetColumnValue<string>("OfferType_Name")
                        } : null;
                        
                        Guid susbcriberTypeId = dataReader.GetColumnValue<Guid>("SubscriberType_Id");
                        data.SubscriberType = susbcriberTypeId != Guid.Empty ? new DgMasterData.Lookup() {
                            Id = susbcriberTypeId,
                            Name = dataReader.GetColumnValue<string>("SubscriberType_Name"),
                            Code = dataReader.GetColumnValue<string>("SubscriberType_Code")
                        } : null;

                        data.EffectiveDate = dataReader.GetColumnValue<DateTime>("EffectiveDate");
                        data.ExpiryDate = dataReader.GetColumnValue<DateTime>("ExpiryDate");
                        data.BundleFlag = dataReader.GetColumnValue<bool>("BundleFlag");
                        data.MaxOrdersTime = dataReader.GetColumnValue<int>("MaxOrdersTime");
                        data.OracleItemCode = dataReader.GetColumnValue<string>("OracleItemCode");
                        data.OraclePackageCode = dataReader.GetColumnValue<string>("OraclePackageCode");
                        data.OfferDesc = dataReader.GetColumnValue<string>("OfferDesc");
                        data.Category = dataReader.GetColumnValue<string>("Category");
                        data.BusinessID = dataReader.GetColumnValue<string>("BusinessID");
                        data.IsPromotion = dataReader.GetColumnValue<int>("IsPromotion") == 1 ? true : false;
                        data.ActiveFlag = dataReader.GetColumnValue<bool>("ActiveFlag");
                        data.PlanType = dataReader.GetColumnValue<string>("PlanType");
                        data.TelcomType = dataReader.GetColumnValue<string>("TelcomType");
                        data.PaymentType = dataReader.GetColumnValue<string>("PaymentType");
                        data.FamilyLevel = dataReader.GetColumnValue<string>("FamilyLevel");
                        data.ContractID = dataReader.GetColumnValue<string>("ContractID");
                        data.ProductList = dataReader.GetColumnValue<string>("ProductList");

                        result.Add(data);
                    }
                }
            }

            return result;
        }
        
        public List<OfferingRSRC> GetOfferingRSRC(string OfferID = "", string OfferName = "")
        {
            var result = new List<OfferingRSRC>();

            bool isOfferIDExists = !string.IsNullOrEmpty(OfferID);
            bool isOfferNameExists = !string.IsNullOrEmpty(OfferName);

            if(!isOfferIDExists && !isOfferNameExists) {
                return result;
            }

            string sql = @"SELECT * FROM DgOfferingRSRC";
            
            var where = new List<string>();
            if(isOfferIDExists) {
                where.Add($"DgOfferID = '{OfferID}'");
            }

            if(isOfferNameExists) {
                where.Add($"DgContractName = '{OfferName}'");
            }

            if(where.Count > 0) {
                sql += " WHERE " + string.Join(" AND ", where.ToArray());
            }

            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                
                using (IDataReader dataReader = query.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        var data = new OfferingRSRC();
                        data.Id = dataReader.GetColumnValue<Guid>("Id");
                        data.OfferID = dataReader.GetColumnValue<string>("DgOfferID");
                        data.ContractID = dataReader.GetColumnValue<string>("DgContractID");
                        data.ContractName = dataReader.GetColumnValue<string>("DgContractName");
                        data.ContractTenure = dataReader.GetColumnValue<int>("DgContractTenure");
                        data.ProductID = dataReader.GetColumnValue<string>("DgProductID");
                        data.ProductName = dataReader.GetColumnValue<string>("DgProductName");
                        data.DeviceDescription = dataReader.GetColumnValue<string>("DgDeviceDesc");
                        data.OracleItemCode = dataReader.GetColumnValue<string>("DgOracleItemCode");
                        data.OraclePackageCode = dataReader.GetColumnValue<string>("DgOraclePackageCode");
                        data.CollectionType = dataReader.GetColumnValue<int>("DgCollectionType");

                        result.Add(data);
                    }
                }
            }

            return result;
        }
            
        #endregion Invalid Offer Device

        #region Invalid Data
        
        public void CleanInvalidData()
        {			
            var data = GetInvalidData();
            foreach (var item in data) {
                Guid validId = Guid.Empty;
                if(!string.IsNullOrEmpty(item.OfferName)) {
                    validId = GetValidOfferId(item.OfferName);
                }

                using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                    dbExecutor.CommandTimeout = 0;

                    new Update(UserConnection, "DgLineDetail")
                        .Set("DgPrimaryOfferingId", validId != Guid.Empty ? Column.Parameter(validId) : Column.Parameter(null, "Guid"))
                        .Where("DgPrimaryOfferingId").IsEqual(Column.Parameter(item.Id))
                        .Execute(dbExecutor);

                    for (int i = 1; i <= 20; i++) {
                        new Update(UserConnection, "DgLineDetail")
                            .Set($"DgSuppOffer{i}Id", validId != Guid.Empty ? Column.Parameter(validId) : Column.Parameter(null, "Guid"))
                            .Where($"DgSuppOffer{i}Id").IsEqual(Column.Parameter(item.Id))
                            .Execute(dbExecutor);
                    }
					
                   new Delete(UserConnection)
                        .From("DgOffering")
                        .Where("Id").IsEqual(Column.Parameter(item.Id))
                        .Execute(dbExecutor);
                }
            }
        }

        public List<Offering> GetInvalidData() {
            var result = new List<Offering>();

            string sql = @"SELECT 
                    Id, DgOfferID, DgOfferName  
                FROM DgOffering 
                WHERE
                    (DgOfferName IS NULL OR DgOfferName = '')
                    OR (DgOfferID IS NULL OR DgOfferID = '')
                    OR DgOfferTypeId IS NULL
                    OR DgExpiryDate IS NULL";

            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                
                using (IDataReader dataReader = query.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        var data = new Offering();
                        data.Id = dataReader.GetColumnValue<Guid>("Id");
                        data.OfferID = dataReader.GetColumnValue<string>("DgOfferID");
                        data.OfferName = dataReader.GetColumnValue<string>("DgOfferName");

                        result.Add(data);
                    }
                }
            }

            return result;
        }

        public Guid GetValidOfferId(string OfferName)
        {
            var result = Guid.Empty;

            string sql = $@"SELECT 
                    TOP 1 Id  
                FROM DgOffering 
                WHERE
                    DgOfferName = '{OfferName}'
                    AND (DgOfferID IS NOT NULL AND DgOfferID != '')
                    AND DgOfferTypeId IS NOT NULL
                    AND DgExpiryDate IS NOT NULL";
            
            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                
                using (IDataReader dataReader = query.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        result = dataReader.GetColumnValue<Guid>("Id");
                    }
                }
            }

            return result;
        }

        #endregion Invalid Data
    }
}