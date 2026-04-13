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
using LookupConst = DgMasterData.DgLookupConst;

namespace DgIntegration.DgUpdateOffering
{
    public class UpdateOffering
    {
        private UserConnection userConnection;
        protected UserConnection UserConnection {
            get {
                return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
            }
        }

        public UpdateOffering(UserConnection UserConnection)
        {
            this.userConnection = UserConnection;

            int totalDataTemp = GetTotalTemp();
            if(totalDataTemp == 0) {
                throw new Exception("There is no data in temp");
            }
        }

        public UpdateResult Process()
        {
            var result = new UpdateResult();
            result.AffectedRow = new AffectedRow();
            
            result.AffectedRow.Update = UpdateUniqueExistingOffering();
            result.AffectedRow.New = NewOffering();

            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection())
            {
                dbExecutor.StartTransaction();
                try {
                    var resultDuplicate = UpdateDuplicateExistingOffering();
                    result.AffectedRow.Update += resultDuplicate.Update;
                    result.AffectedRow.New += resultDuplicate.New;
                    
                    dbExecutor.CommitTransaction();
                } catch(Exception e) {
                    dbExecutor.RollbackTransaction();
                    result.Message = e.Message;
                }
            }

            if(string.IsNullOrEmpty(result.Message)) {
                result.Success = true;
            }

            return result;
        }
        
        protected int GetTotalTemp()
        {
            string sql = @"SELECT COUNT(*) TOTAL FROM NCCF_TBLOFFERING";
            
            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                using (IDataReader dataReader = query.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        return dataReader.GetColumnValue<int>("TOTAL");
                    }
                }
            }

            return 0;
        }

        protected int UpdateUniqueExistingOffering()
        {
            string sql = @"UPDATE DgOffering SET
                DgOfferID = A.DgOfferID, 
                DgOfferName = A.DgOfferName, 
                DgOfferDesc = A.DgOfferDesc, 
                DgPaymentType = A.DgPaymentType, 
                DgOfferTypeId = A.DgOfferTypeId, 
                DgEffectiveDate = A.DgEffectiveDate, 
                DgExpiryDate = A.DgExpiryDate, 
                DgBusinessID = A.DgBusinessID, 
                DgTelcomType = A.DgTelcomType, 
                DgPlanType = A.DgPlanType, 
                DgActiveFlag = A.DgActiveFlag, 
                DgDgProductList = A.DgDgProductList, 
                DgBundleFlag = A.DgBundleFlag, 
                DgIsPromotion = A.DgIsPromotion,
                DgSubscriberTypeId = A.DgSubscriberTypeId,
                DgFamilyLevel = A.DgFamilyLevel,
                DgContractId = A.DgContractId,
                DgMaxOrdersTime = A.DgMaxOrdersTime,
                DgOracleItemCode = A.DgOracleItemCode,
                DgOraclePackageCode = A.DgOraclePackageCode
            FROM (
                SELECT
                    DgOffering.Id Id,
                    NCCF_TBLOFFERING.OFFER_ID DgOfferID,
                    NCCF_TBLOFFERING.OFFER_NAME DgOfferName,
                    NCCF_TBLOFFERING.OFFER_DESC DgOfferDesc,
                    NCCF_TBLOFFERING.PAYMENT_TYPE DgPaymentType,
                    CASE
                        WHEN NCCF_TBLOFFERING.OFFER_TYPE IS NULL THEN NULL
                        WHEN NCCF_TBLOFFERING.OFFER_TYPE = '' THEN NULL
                        WHEN NCCF_TBLOFFERING.OFFER_TYPE = '1' THEN '732FA776-EB9F-4D46-B3D7-5C5C94634F82'
                        ELSE 'D905DF6C-F999-4B94-8D2A-1CE773980852'
                    END DgOfferTypeId,
                    CASE
                        WHEN NCCF_TBLOFFERING.EFFECTIVE_DATE IS NULL THEN NULL
                        WHEN NCCF_TBLOFFERING.EFFECTIVE_DATE = '' THEN NULL
                        ELSE DATEADD(HOUR, -8, CONVERT(datetime2, NCCF_TBLOFFERING.EFFECTIVE_DATE, 23))
                    END DgEffectiveDate,
                    CASE
                        WHEN NCCF_TBLOFFERING.EXPIRE_DATE IS NULL THEN NULL
                        WHEN NCCF_TBLOFFERING.EXPIRE_DATE = '' THEN NULL
                        ELSE DATEADD(HOUR, -8, CONVERT(datetime2, NCCF_TBLOFFERING.EXPIRE_DATE, 23))
                    END DgExpiryDate,
                    NCCF_TBLOFFERING.BUSINESS_ID DgBusinessID,
                    NCCF_TBLOFFERING.TELECOM_TYPE DgTelcomType,
                    NCCF_TBLOFFERING.PLAN_TYPE DgPlanType,
                    CASE
                        WHEN NCCF_TBLOFFERING.ACTIVE_FLAG = '1' THEN 1
                        WHEN NCCF_TBLOFFERING.ACTIVE_FLAG = '0' THEN 0
                        ELSE 0
                    END DgActiveFlag,
                    NCCF_TBLOFFERING.PRODUCT_LIST DgDgProductList,
                    CASE
                        WHEN NCCF_TBLOFFERING.BUNDLE_FLAG = '1' THEN 1
                        WHEN NCCF_TBLOFFERING.BUNDLE_FLAG = '0' THEN 0
                        ELSE 0
                    END DgBundleFlag,
                    NCCF_TBLOFFERING.IS_PROMOTION DgIsPromotion,
                    DgSubscriberType.Id DgSubscriberTypeId,
                    NCCF_TBLOFFERING.FAMILY_LEVEL DgFamilyLevel,
                    NCCF_TBLOFFERING.CONTRACT_ID DgContractId,
                    CAST(NCCF_TBLOFFERING.MAX_ORDER_TIMES AS INT) DgMaxOrdersTime,
                    NCCF_TBLOFFERING.ORACLE_ITEM_CODE DgOracleItemCode,
                    NCCF_TBLOFFERING.ORACLE_PACKAGE_CODE DgOraclePackageCode
                FROM DgOffering
                INNER JOIN (
                    SELECT
                        OFFER_ID,
                        COUNT(*) TOTAL
                    FROM NCCF_TBLOFFERING
                    GROUP BY OFFER_ID
                    HAVING COUNT(*) = 1
                ) NCCF_OFFERING ON NCCF_OFFERING.OFFER_ID = DgOffering.DgOfferID
                INNER JOIN NCCF_TBLOFFERING ON NCCF_TBLOFFERING.OFFER_ID = NCCF_OFFERING.OFFER_ID
                LEFT JOIN DgSubscriberType ON DgSubscriberType.DgCode = NCCF_TBLOFFERING.SUBSCRIBER_TYPE
            ) A
            WHERE DgOffering.Id = A.Id";

            int affectedRow = 0;
            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                affectedRow = query.Execute(dbExecutor);
            }
            
            return affectedRow;
        }

        protected int NewOffering()
        {
            string sql = @"INSERT INTO DgOffering (
                DgOfferID, 
                DgOfferName, 
                DgOfferDesc, 
                DgPaymentType, 
                DgOfferTypeId, 
                DgEffectiveDate, 
                DgExpiryDate, 
                DgBusinessID, 
                DgTelcomType, 
                DgPlanType, 
                DgActiveFlag, 
                DgDgProductList, 
                DgBundleFlag, 
                DgIsPromotion,
                DgSubscriberTypeId,
                DgFamilyLevel,
                DgContractId,
                DgMaxOrdersTime,
                DgOracleItemCode,
                DgOraclePackageCode
            )
            SELECT
                NCCF_TBLOFFERING.OFFER_ID DgOfferID,
                NCCF_TBLOFFERING.OFFER_NAME DgOfferName,
                NCCF_TBLOFFERING.OFFER_DESC DgOfferDesc,
                NCCF_TBLOFFERING.PAYMENT_TYPE DgPaymentType,
                CASE
                    WHEN NCCF_TBLOFFERING.OFFER_TYPE IS NULL THEN NULL
                    WHEN NCCF_TBLOFFERING.OFFER_TYPE = '' THEN NULL
                    WHEN NCCF_TBLOFFERING.OFFER_TYPE = '1' THEN '732FA776-EB9F-4D46-B3D7-5C5C94634F82'
                    ELSE 'D905DF6C-F999-4B94-8D2A-1CE773980852'
                END DgOfferTypeId,
                CASE
                    WHEN NCCF_TBLOFFERING.EFFECTIVE_DATE IS NULL THEN NULL
                    WHEN NCCF_TBLOFFERING.EFFECTIVE_DATE = '' THEN NULL
                    ELSE DATEADD(HOUR, -8, CONVERT(datetime2, NCCF_TBLOFFERING.EFFECTIVE_DATE, 23))
                END DgEffectiveDate,
                CASE
                    WHEN NCCF_TBLOFFERING.EXPIRE_DATE IS NULL THEN NULL
                    WHEN NCCF_TBLOFFERING.EXPIRE_DATE = '' THEN NULL
                    ELSE DATEADD(HOUR, -8, CONVERT(datetime2, NCCF_TBLOFFERING.EXPIRE_DATE, 23))
                END DgExpiryDate,
                NCCF_TBLOFFERING.BUSINESS_ID DgBusinessID,
                NCCF_TBLOFFERING.TELECOM_TYPE DgTelcomType,
                NCCF_TBLOFFERING.PLAN_TYPE DgPlanType,
                CASE
                    WHEN NCCF_TBLOFFERING.ACTIVE_FLAG = '1' THEN 1
                    WHEN NCCF_TBLOFFERING.ACTIVE_FLAG = '0' THEN 0
                    ELSE 0
                END DgActiveFlag,
                NCCF_TBLOFFERING.PRODUCT_LIST DgDgProductList,
                CASE
                    WHEN NCCF_TBLOFFERING.BUNDLE_FLAG = '1' THEN 1
                    WHEN NCCF_TBLOFFERING.BUNDLE_FLAG = '0' THEN 0
                    ELSE 0
                END DgBundleFlag,
                NCCF_TBLOFFERING.IS_PROMOTION DgIsPromotion,
                DgSubscriberType.Id DgSubscriberTypeId,
                NCCF_TBLOFFERING.FAMILY_LEVEL DgFamilyLevel,
                NCCF_TBLOFFERING.CONTRACT_ID DgContractId,
                CAST(NCCF_TBLOFFERING.MAX_ORDER_TIMES AS INT) DgMaxOrdersTime,
                NCCF_TBLOFFERING.ORACLE_ITEM_CODE DgOracleItemCode,
                NCCF_TBLOFFERING.ORACLE_PACKAGE_CODE DgOraclePackageCode
            FROM NCCF_TBLOFFERING
            INNER JOIN (
                SELECT
                    DISTINCT
                    NCCF_TBLOFFERING.OFFER_ID OFFER_ID
                FROM NCCF_TBLOFFERING
                LEFT JOIN DgOffering ON DgOffering.DgOfferID = NCCF_TBLOFFERING.OFFER_ID
                WHERE
                    DgOffering.DgOfferID IS NULL
            ) NEW_OFFERING ON NEW_OFFERING.OFFER_ID = NCCF_TBLOFFERING.OFFER_ID
            LEFT JOIN DgSubscriberType ON DgSubscriberType.DgCode = NCCF_TBLOFFERING.SUBSCRIBER_TYPE";

            int affectedRow = 0;
            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                affectedRow = query.Execute(dbExecutor);
            }
            
            return affectedRow;
        }

        protected AffectedRow UpdateDuplicateExistingOffering()
        {
            var affectedRow = new AffectedRow();
            var offeringNotExists = new List<Guid>();
            var subscriberTypeList = GetSubscriberType();

            var duplicate = GetDuplicate();
            foreach(string offerId in duplicate) {
                var offering = GetOffering(offerId);
                var offeringNCCF = GetOfferingNCCF(offerId);
                
                var offeringFound = new List<Guid>();
                foreach(var offerNCCF in offeringNCCF) {
                    Guid offerType = Guid.Empty;
                    if(offerNCCF.OFFER_TYPE == "1") {
                        offerType = LookupConst.OfferType.PrimaryOffering;
                    } else if(offerNCCF.OFFER_TYPE == "2") {
                        offerType = LookupConst.OfferType.SupplementaryOffering;
                    }

                    Guid subscriberType = subscriberTypeList
                        .Where(item => item.FirstOrDefault().Key == offerNCCF.SUBSCRIBER_TYPE)
                        .FirstOrDefault()?
                        .FirstOrDefault().Value ?? Guid.Empty;

                    int index = offering.FindIndex(item => {
                        return offerNCCF.OFFER_ID == item.DgOfferID 
                            && offerNCCF.OFFER_NAME == item.DgOfferName
                            && offerType == item.DgOfferTypeId
                            && subscriberType == item.DgSubscriberTypeId 
                            && offerNCCF.CONTRACT_ID == item.DgContractId
                            && offerNCCF.ORACLE_ITEM_CODE == item.DgOracleItemCode
                            && offerNCCF.ORACLE_PACKAGE_CODE == item.DgOraclePackageCode;
                    });

                    bool activeFlag = offerNCCF.ACTIVE_FLAG == "1" ? true : false;
                    bool bundleFlag = offerNCCF.BUNDLE_FLAG == "1" ? true : false;

                    int maxOrderTimes = 0;
                    Int32.TryParse(offerNCCF.MAX_ORDER_TIMES, out maxOrderTimes);

                    DateTime effDate = DateTime.MinValue;
                    if(!string.IsNullOrEmpty(offerNCCF.EFFECTIVE_DATE)) {
                        DateTime.TryParseExact(offerNCCF.EFFECTIVE_DATE, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out effDate);
                        effDate = effDate.AddHours(-8);
                    }

                    DateTime expDate = DateTime.MinValue;
                    if(!string.IsNullOrEmpty(offerNCCF.EXPIRE_DATE)) {
                        DateTime.TryParseExact(offerNCCF.EXPIRE_DATE, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out expDate);
                        expDate = expDate.AddHours(-8);
                    }

                    if(index == -1) { // insert offering baru
                        var insert = new Insert(UserConnection)
                            .Into("DgOffering")
                            .Set("DgOfferID", Column.Parameter(offerNCCF.OFFER_ID))
                            .Set("DgOfferName", Column.Parameter(offerNCCF.OFFER_NAME))
                            .Set("DgOfferDesc", Column.Parameter(offerNCCF.OFFER_DESC))
                            .Set("DgPaymentType", Column.Parameter(offerNCCF.PAYMENT_TYPE))
                            .Set("DgBusinessID", Column.Parameter(offerNCCF.BUSINESS_ID))
                            .Set("DgTelcomType", Column.Parameter(offerNCCF.TELECOM_TYPE))
                            .Set("DgPlanType", Column.Parameter(offerNCCF.PLAN_TYPE))
                            .Set("DgActiveFlag", Column.Parameter(activeFlag))
                            .Set("DgDgProductList", Column.Parameter(offerNCCF.PRODUCT_LIST))
                            .Set("DgBundleFlag", Column.Parameter(bundleFlag))
                            .Set("DgIsPromotion", Column.Parameter(offerNCCF.IS_PROMOTION))
                            .Set("DgFamilyLevel", Column.Parameter(offerNCCF.FAMILY_LEVEL))
                            .Set("DgContractId", Column.Parameter(offerNCCF.CONTRACT_ID))
                            .Set("DgMaxOrdersTime", Column.Parameter(maxOrderTimes))
                            .Set("DgOracleItemCode", Column.Parameter(offerNCCF.ORACLE_ITEM_CODE))
                            .Set("DgOraclePackageCode", Column.Parameter(offerNCCF.ORACLE_PACKAGE_CODE));
                        
                        if(offerType != Guid.Empty) {
                            insert.Set("DgOfferTypeId", Column.Parameter(offerType));
                        }

                        if(subscriberType != Guid.Empty) {
                            insert.Set("DgSubscriberTypeId", Column.Parameter(subscriberType));
                        }

                        if(!string.IsNullOrEmpty(offerNCCF.EFFECTIVE_DATE) && effDate != DateTime.MinValue) {
                            insert.Set("DgEffectiveDate", Column.Parameter(effDate));
                       
                        }

                        if(!string.IsNullOrEmpty(offerNCCF.EXPIRE_DATE) && expDate != DateTime.MinValue) {
                            insert.Set("DgExpiryDate", Column.Parameter(expDate));
                        }

                        insert.Execute();

                        affectedRow.New++;
                    } else { // update existing
                        Guid updateId = offering[index].Id;
                        var update = new Update(UserConnection, "DgOffering")
                            .Set("DgOfferName", Column.Parameter(offerNCCF.OFFER_NAME))
                            .Set("DgOfferDesc", Column.Parameter(offerNCCF.OFFER_DESC))
                            .Set("DgPaymentType", Column.Parameter(offerNCCF.PAYMENT_TYPE))
                            .Set("DgBusinessID", Column.Parameter(offerNCCF.BUSINESS_ID))
                            .Set("DgTelcomType", Column.Parameter(offerNCCF.TELECOM_TYPE))
                            .Set("DgPlanType", Column.Parameter(offerNCCF.PLAN_TYPE))
                            .Set("DgActiveFlag", Column.Parameter(activeFlag))
                            .Set("DgDgProductList", Column.Parameter(offerNCCF.PRODUCT_LIST))
                            .Set("DgBundleFlag", Column.Parameter(bundleFlag))
                            .Set("DgIsPromotion", Column.Parameter(offerNCCF.IS_PROMOTION))
                            .Set("DgFamilyLevel", Column.Parameter(offerNCCF.FAMILY_LEVEL))
                            .Set("DgContractId", Column.Parameter(offerNCCF.CONTRACT_ID))
                            .Set("DgMaxOrdersTime", Column.Parameter(maxOrderTimes))
                            .Set("DgOracleItemCode", Column.Parameter(offerNCCF.ORACLE_ITEM_CODE))
                            .Set("DgOraclePackageCode", Column.Parameter(offerNCCF.ORACLE_PACKAGE_CODE));

                        if(offerType != Guid.Empty) {
                            update.Set("DgOfferTypeId", Column.Parameter(offerType));
                        }

                        if(subscriberType != Guid.Empty) {
                            update.Set("DgSubscriberTypeId", Column.Parameter(subscriberType));
                        }

                        if(!string.IsNullOrEmpty(offerNCCF.EFFECTIVE_DATE) && effDate != DateTime.MinValue) {
                            update.Set("DgEffectiveDate", Column.Parameter(effDate));
                       
                        }

                        if(!string.IsNullOrEmpty(offerNCCF.EXPIRE_DATE) && expDate != DateTime.MinValue) {
                            update.Set("DgExpiryDate", Column.Parameter(expDate));
                        }

                        update
                            .Where("Id").IsEqual(Column.Parameter(updateId))
                            .Execute();

                        offeringFound.Add(updateId);
                        affectedRow.Update++;
                    }
                }
            }

            return affectedRow;
        }

        protected List<string> GetDuplicate()
        {
            var result = new List<string>();
            string sql = @"SELECT
                OFFER_ID,
                COUNT(*) TOTAL
            FROM NCCF_TBLOFFERING
            GROUP BY OFFER_ID
            HAVING COUNT(*) > 1
            ORDER BY OFFER_ID ASC";

            var query = new CustomQuery(UserConnection, sql);
            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                using (IDataReader dataReader = query.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        result.Add(dataReader.GetColumnValue<string>("OFFER_ID"));
                    }
                }
            }

            return result;
        }

        protected List<DgOffering> GetOffering(string OfferID)
        {
            var result = new List<DgOffering>();
            string sql = @"SELECT * FROM DgOffering WHERE DgOfferID = @OfferID";

            var query = new CustomQuery(UserConnection, sql);
            query.Parameters.Add("@OfferID", OfferID);

            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                using (IDataReader dataReader = query.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        result.Add(new DgOffering() {
                            Id = dataReader.GetColumnValue<Guid>("Id"),
                            DgOfferID = dataReader.GetColumnValue<string>("DgOfferID"),
                            DgOfferName = dataReader.GetColumnValue<string>("DgOfferName"),
                            DgOfferDesc = dataReader.GetColumnValue<string>("DgOfferDesc"),
                            DgPaymentType = dataReader.GetColumnValue<string>("DgPaymentType"),
                            DgOfferTypeId = dataReader.GetColumnValue<Guid>("DgOfferTypeId"),
                            DgEffectiveDate = dataReader.GetColumnValue<DateTime>("DgEffectiveDate"),
                            DgExpiryDate = dataReader.GetColumnValue<DateTime>("DgExpiryDate"),
                            DgBusinessID = dataReader.GetColumnValue<string>("DgBusinessID"),
                            DgTelcomType = dataReader.GetColumnValue<string>("DgTelcomType"),
                            DgPlanType = dataReader.GetColumnValue<string>("DgPlanType"),
                            DgActiveFlag = dataReader.GetColumnValue<bool>("DgActiveFlag"),
                            DgDgProductList = dataReader.GetColumnValue<string>("DgDgProductList"),
                            DgBundleFlag = dataReader.GetColumnValue<bool>("DgBundleFlag"),
                            DgIsPromotion = dataReader.GetColumnValue<string>("DgIsPromotion"),
                            DgSubscriberTypeId = dataReader.GetColumnValue<Guid>("DgSubscriberTypeId"),
                            DgFamilyLevel = dataReader.GetColumnValue<string>("DgFamilyLevel"),
                            DgContractId = dataReader.GetColumnValue<string>("DgContractId"),
                            DgMaxOrdersTime = dataReader.GetColumnValue<int>("DgMaxOrdersTime"),
                            DgOracleItemCode = dataReader.GetColumnValue<string>("DgOracleItemCode"),
                            DgOraclePackageCode = dataReader.GetColumnValue<string>("DgOraclePackageCode")
                        });
                    }
                }
            }

            return result;
        }

        protected List<NCCF_TBLOFFERING> GetOfferingNCCF(string OfferID)
        {
            var result = new List<NCCF_TBLOFFERING>();
            string sql = @"SELECT * FROM NCCF_TBLOFFERING WHERE OFFER_ID = @OfferID";

            var query = new CustomQuery(UserConnection, sql);
            query.Parameters.Add("@OfferID", OfferID);

            using (DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                dbExecutor.CommandTimeout = 0;
                using (IDataReader dataReader = query.ExecuteReader(dbExecutor)) {
                    while (dataReader.Read()) {
                        result.Add(new NCCF_TBLOFFERING() {
                            OFFER_ID = dataReader.GetColumnValue<string>("OFFER_ID"),
                            OFFER_NAME = dataReader.GetColumnValue<string>("OFFER_NAME"),
                            OFFER_DESC = dataReader.GetColumnValue<string>("OFFER_DESC"),
                            PAYMENT_TYPE = dataReader.GetColumnValue<string>("PAYMENT_TYPE"),
                            OFFER_TYPE = dataReader.GetColumnValue<string>("OFFER_TYPE"),
                            EFFECTIVE_DATE = dataReader.GetColumnValue<string>("EFFECTIVE_DATE"),
                            EXPIRE_DATE = dataReader.GetColumnValue<string>("EXPIRE_DATE"),
                            BUSINESS_ID = dataReader.GetColumnValue<string>("BUSINESS_ID"),
                            TELECOM_TYPE = dataReader.GetColumnValue<string>("TELECOM_TYPE"),
                            PLAN_TYPE = dataReader.GetColumnValue<string>("PLAN_TYPE"),
                            ACTIVE_FLAG = dataReader.GetColumnValue<string>("ACTIVE_FLAG"),
                            PRODUCT_LIST = dataReader.GetColumnValue<string>("PRODUCT_LIST"),
                            BUNDLE_FLAG = dataReader.GetColumnValue<string>("BUNDLE_FLAG"),
                            IS_PROMOTION = dataReader.GetColumnValue<string>("IS_PROMOTION"),
                            FAMILY_LEVEL = dataReader.GetColumnValue<string>("FAMILY_LEVEL"),
                            CONTRACT_ID = dataReader.GetColumnValue<string>("CONTRACT_ID"),
                            MAX_ORDER_TIMES = dataReader.GetColumnValue<string>("MAX_ORDER_TIMES"),
                            ORACLE_ITEM_CODE = dataReader.GetColumnValue<string>("ORACLE_ITEM_CODE"),
                            ORACLE_PACKAGE_CODE = dataReader.GetColumnValue<string>("ORACLE_PACKAGE_CODE"),
                            SUBSCRIBER_TYPE = dataReader.GetColumnValue<string>("SUBSCRIBER_TYPE")
                        });
                    }
                }
            }

            return result;
        }

        protected List<Dictionary<string, Guid>> GetSubscriberType()
        {
            var result = new List<Dictionary<string, Guid>>();

            var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgSubscriberType");
            var columns = new Dictionary<string, EntitySchemaQueryColumn>();
            columns.Add("Id", esq.AddColumn("Id"));
            columns.Add("Code", esq.AddColumn("DgCode"));

            esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.NotEqual, "DgCode", string.Empty));
            var entities = esq.GetEntityCollection(UserConnection);
            foreach(var entity in entities) {
                result.Add(new Dictionary<string, Guid>() {
                    {
                        entity.GetTypedColumnValue<string>(columns["Code"].Name),
                        entity.GetTypedColumnValue<Guid>(columns["Id"].Name)
                    }
                });
            }

            return result;
        }
    }

    public class UpdateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public AffectedRow AffectedRow { get; set; }
    }

    public class AffectedRow
    {
        public int New { get; set; }
        public int Update { get; set; }
    }

    public class DgOffering
    {
        public Guid Id { get; set; }
        public string DgOfferID { get; set; }
        public string DgOfferName { get; set; }
        public string DgOfferDesc { get; set; }
        public string DgPaymentType { get; set; }
        public Guid DgOfferTypeId { get; set; }
        public DateTime DgEffectiveDate { get; set; }
        public DateTime DgExpiryDate { get; set; }
        public string DgBusinessID { get; set; }
        public string DgTelcomType { get; set; }
        public string DgPlanType { get; set; }
        public bool DgActiveFlag { get; set; }
        public string DgDgProductList { get; set; }
        public bool DgBundleFlag { get; set; }
        public string DgIsPromotion { get; set; }
        public Guid DgSubscriberTypeId { get; set; }
        public string DgFamilyLevel { get; set; }
        public string DgContractId { get; set; }
        public int DgMaxOrdersTime { get; set; }
        public string DgOracleItemCode { get; set; }
        public string DgOraclePackageCode { get; set; }
    }

    public class NCCF_TBLOFFERING
    {
        public string OFFER_ID { get; set; }
        public string OFFER_NAME { get; set; }
        public string OFFER_DESC { get; set; }
        public string PAYMENT_TYPE { get; set; }
        public string OFFER_TYPE { get; set; }
        public string EFFECTIVE_DATE { get; set; }
        public string EXPIRE_DATE { get; set; }
        public string BUSINESS_ID { get; set; }
        public string TELECOM_TYPE { get; set; }
        public string PLAN_TYPE { get; set; }
        public string ACTIVE_FLAG { get; set; }
        public string PRODUCT_LIST { get; set; }
        public string BUNDLE_FLAG { get; set; }
        public string IS_PROMOTION { get; set; }
        public string FAMILY_LEVEL { get; set; }
        public string CONTRACT_ID { get; set; }
        public string MAX_ORDER_TIMES { get; set; }
        public string ORACLE_ITEM_CODE { get; set; }
        public string ORACLE_PACKAGE_CODE { get; set; }
        public string SUBSCRIBER_TYPE { get; set; }
    }
}