using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using Terrasoft.Configuration;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Process;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using DgBaseService.DgGenericResponse;
using System.Text.RegularExpressions;
using ISAHttpRequest.ISAHttpRequest;
using DgIntegration.DgValidateCorporatePortInService;
using LookupConst = DgMasterData.DgLookupConst;
using DgSubmission.DgHistorySubmissionService;
using DgSubmission.DgLineDetail;
using DgCSGIntegration;
using DgCRMIntegration;

namespace DgIntegration.DgLineActivation
{
    public class MNPActivationService
    {
        private UserConnection userConnection;
		private UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}

        private CRMService CRMService;
        private CSGService CSGService;
        private List<LineDetail> lineDetailSelected;
        private Guid submissionId;

        public MNPActivationService(UserConnection UserConnection, List<LineDetail> LineSelected)
        {
            this.userConnection = UserConnection;
            this.lineDetailSelected = LineSelected;
            this.submissionId = LineSelected.Select(item => item.SubmissionId).FirstOrDefault();

            this.CSGService = new CSGService(UserConnection);
            this.CRMService = new CRMService(UserConnection, true, "MNP");
        }

        public virtual async Task<List<LineResult>> Process()
        {
            var result = new List<LineResult>();

            // checkSIMCard validation
            this.lineDetailSelected = await this.lineDetailSelected.MNPIntegration(UserConnection);
            List<int> indexValidLine = this.lineDetailSelected
                .Where(item => item.IntegrationMessage.Count == 0)
                .Select((item, index) => index)
                .ToList();
            
            // jika tidak ada yg valid dari checkSIMCard
            if(indexValidLine.Count == 0 || this.lineDetailSelected.Count != indexValidLine.Count) {
                return LineActivation.Response(this.lineDetailSelected);
            }

            var firstLine = this.lineDetailSelected.FirstOrDefault();
            string groupId = firstLine.SubParentGroupID;
            if(string.IsNullOrEmpty(groupId) && !string.IsNullOrEmpty(firstLine.SubParentGroupNo)) {
                var queryVPN = await CRMService.QueryVPNGroupSubscriberByGroupNo(firstLine.SubParentGroupNo);
                if(queryVPN == null || (queryVPN != null && queryVPN.Count == 0)) {
                    throw new Exception($"Corporate customer cannot be found in CRM based on submitted Group Number {firstLine.SubParentGroupNo}");
                }

                groupId = queryVPN.FirstOrDefault().groupId;
                UpdateGroupId(firstLine.CRMGroupId, groupId);
            }

            await ValidateCorporatePortIn();
            return LineActivation.Response(this.lineDetailSelected);
        }
  
        protected virtual async Task ValidateCorporatePortIn()
        {            
            try {
                var portInTransactionID = ValidateCorporatePortInService.GenerateTransactionId();
                var portInMessageID = ValidateCorporatePortInService.GenerateMessageId();
                var validateCorporatePortIn = await this.CSGService.ValidateCorporatePortIn(this.lineDetailSelected, portInTransactionID, portInMessageID);
               
                if (validateCorporatePortIn != null) {
                    foreach (var item in this.lineDetailSelected) {
                        UpdateLineDetail(item.Id, portInMessageID, portInTransactionID, validateCorporatePortIn.CSGHeader.ReferenceID);
                        HistorySubmissionService.ReleaseActivation(
                            UserConnection: UserConnection,
                            LineDetailId: item.Id,
                            CreatedById: UserConnection.CurrentUser.ContactId
                        );   
                    }
                }

            } catch (Exception e) {
                foreach (var item  in this.lineDetailSelected) {
                    new Update(UserConnection, "DgLineDetail")
                        .Set("DgReleased", Column.Parameter(false))
                        .Set("DgActivationOrderID", Column.Parameter(string.Empty))
                        .Set("DgActivationTransactionId", Column.Parameter(string.Empty))
                        .Set("DgPortInMessageID", Column.Parameter(string.Empty))
                        .Set("DgPortInTransactionID", Column.Parameter(string.Empty))
                        .Set("DgReleasedDate", Column.Parameter(null, "DateTime"))
                        .Set("DgReleasedById", Column.Parameter(null, "Guid"))
                        .Where("Id").IsEqual(Column.Parameter(item.Id))
                        .Execute();
                }
                
                throw e;
            }
        }

        protected virtual void UpdateLineDetail(Guid Id, string PortInMessage, string PortInTransactionId, string ReferenceId)
        {
            var schema = UserConnection.EntitySchemaManager.GetInstanceByName("DgLineDetail");
            var entity = schema.CreateEntity(UserConnection);

            entity.FetchFromDB("Id", Id);
            entity.SetColumnValue("DgActivationStatusId", LookupConst.ActivationStatus.Released);
            entity.SetColumnValue("DgReleasedDate", DateTime.UtcNow);
            entity.SetColumnValue("DgReleasedById", UserConnection.CurrentUser.ContactId);

            entity.SetColumnValue("DgPortInMessageID", PortInMessage);
            entity.SetColumnValue("DgPortInTransactionID", PortInTransactionId);
            entity.SetColumnValue("DgActivationTransactionId", ReferenceId);
            entity.SetColumnValue("DgActivationOrderID", ReferenceId);

            entity.Save(false);
        }

        protected virtual void UpdateGroupId(Guid CRMGroupId, string GroupID)
        {
            new Update(UserConnection, "DgCRMGroup")
                .Set("DgSubParentGroupID", Column.Parameter(GroupID))
                .Where("Id").IsEqual(Column.Parameter(CRMGroupId))
            .Execute();

            for(int i=0; i<this.lineDetailSelected.Count; i++) {
                this.lineDetailSelected[i].SubParentGroupID = GroupID;
            }
        }
    }
}