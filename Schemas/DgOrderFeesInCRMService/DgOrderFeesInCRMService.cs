using System;
using System.IO;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using System.Threading.Tasks;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DgCSGIntegration;
using DgBaseService.DgGenericResponse;
using DgBaseService.DgHelpers;
using DgMasterData.DgLookupConst;
using ISAEntityHelper.EntityHelper;
using LookupConst = DgMasterData.DgLookupConst;

namespace DgCSGIntegration.DgOrderFees
{
    public class OrderFeesInCRMService
    {
        private UserConnection UserConnection;
        private Guid CRMGroupId;
		private CSGService csgService;
		
        public OrderFeesInCRMService(UserConnection userConnection, Guid CRMGroupId)
        {
            this.UserConnection = userConnection;
            this.CRMGroupId = CRMGroupId;
            this.csgService = new CSGService(UserConnection);
        }

        public virtual async Task<GeneralResponse> Process()
        {
            var result = new GeneralResponse();
            try {
                var orderFees = new OrderFees(UserConnection);
                var param = orderFees.GetParamByCRMGroup(CRMGroupId);
                var orderFeesRes = await this.csgService.OrderFees(orderFees, param, "DgCRMGroup", CRMGroupId);
				
                UpdateCRMGroup(orderFeesRes.RetrieveFeesForOrderResponse?.FeesList?.FeesRecord.FirstOrDefault());
                result.Success = true;
            } catch (Exception e) {
                result.Message = e.Message;
            }

            return result;
        }

        protected virtual void UpdateCRMGroup(FeesRecord data) 
        {
            new Update(UserConnection, "DgCRMGroup")
                .Set("DgFeeName", Column.Parameter(data.FeeName))
                .Set("DgFeeAmount", Column.Parameter(data.FeeAmount))
                .Set("DgFeeType", Column.Parameter(data.FeeType))
                .Set("DgFeeItemCode", Column.Parameter(data.FeeItemCode))
                .Set("DgOriginalFeeAmount", Column.Parameter(data.OriginalFeeAmount))
                .Set("DgPaymentType", Column.Parameter(data.PaymentType))
                .Set("DgOFSCode", Column.Parameter(data.OFSCode))
                .Set("DgTax", Column.Parameter(data?.TaxList?.TaxRecord.First()?.TaxAmount ?? 0))
                .Where("Id").IsEqual(Column.Parameter(CRMGroupId))
            .Execute();
        }
    }
}