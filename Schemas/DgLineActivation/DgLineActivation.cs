using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using System.Threading.Tasks;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Core.Entities;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Terrasoft.Configuration;
using Newtonsoft.Json;
using DgBaseService.DgGenericResponse;
using DgCRMIntegration;
using DgSubmission.DgLineDetail;
using DgSubmission.DgHistorySubmissionService;
using DgSFAIntegation.DgSFALineActivationStatus;
using DgCRMIntegration.DgAddVPNGroupMembers;
using LookupConst = DgMasterData.DgLookupConst;

namespace DgIntegration.DgLineActivation
{
    public class LineActivation
    {
        private UserConnection userConnection;
		protected UserConnection UserConnection {
			get {
				return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
			}
		}

        private Guid submissionId;
        private Guid lineDetailId;
        private string serialNumber;
		private List<Guid> lineDetailIds;

        public LineActivation(UserConnection UserConnection, Guid RecordId, bool IsLineDetail = false) 
        {
            this.userConnection = UserConnection;

            if(!IsLineDetail) {
                this.submissionId = RecordId;
            } else {
                this.lineDetailId = RecordId;
            }
        }

        public LineActivation(UserConnection UserConnection, string SerialNumber) 
        {
            this.userConnection = UserConnection;
            this.serialNumber = SerialNumber;
        }
		
		public LineActivation(UserConnection UserConnection, List<Guid> RecordIds) 
        {
            this.userConnection = UserConnection;
            this.lineDetailIds = RecordIds;
        }

        public async Task<GeneralResponse> Activation()
        {
            var result = new GeneralResponse();

            var lineDetail = new LineDetail(UserConnection);
            var lines = new List<LineDetail>();

            try {
                if(this.submissionId != null && this.submissionId != Guid.Empty) {
                    lines = lineDetail.GetLinesActivation(this.submissionId);
                } else if(!string.IsNullOrEmpty(this.serialNumber)) {
                    lines = lineDetail.GetLinesActivation(this.serialNumber);
                } else if(this.lineDetailIds != null && this.lineDetailIds.Count > 0) {
					lines = lineDetail.GetLinesActivation(this.lineDetailIds);
				} else {
                    throw new Exception("Activation only support Submission Id, Serial Number or, Line Detail Ids");
                }

                if(lines.Count == 0) {
                    throw new Exception("No data can be provision to CRM");
                }

                var UnOpenedPage =  lines
                    .Where(item => item.ActivationStatus == null || item.ActivationStatus.Id == LookupConst.ActivationStatus.NotActivated)
                    .Where(line => {
                        if (line.OPPageOpen == null || line.OPPageOpen == DateTime.MinValue) {
                            return true;
                        }

                        var openDate = line.OPPageOpen.AddHours(8);
                        var currentDate = DateTime.UtcNow.AddHours(8);
                        var diff = currentDate - openDate;

                        return diff.TotalMinutes > 10;
                    })
                    .ToList();
                if (UnOpenedPage.Count > 0) {
                    throw new Exception("Kindly open OP page and ensure all fees box are completed and SAVED.");
                }

                result = await Activation(lines);
            } catch(Exception e) {
                result.Message = e.Message;
            }

            return result;
        }
		
		public async Task<GeneralResponse> Activation(List<LineDetail> Lines)
		{
			var result = new GeneralResponse();
            var lineDetail = new LineDetail(UserConnection);

            try {
                if(Lines.Count == 0) {
                    throw new Exception("No data can be provision to CRM");
                }

                var batchError = new Dictionary<int, string>();
                var validations = lineDetail.IsValid(Lines);

                List<LineDetail> validLines = validations
                    .Where(item => item.Result.Success)
                    .Select(item => item.Line)
                    .ToList();

                List<int> indexOfValidLines = validations
                    .Select((item, index) => new {
                        Index = index,
                        Line = item
                    })
                    .Where(item => item.Line.Result.Success)
                    .Select(item => item.Index)
                    .ToList();

                for(int i=0; i<validations.Count; i++) {
                    var valid = validations[i];
                    if(!valid.Result.Success) {
                        batchError.Add(i, valid.Result.Message);
                    }
                }
				
                if(validLines.Count == 0) {
                    throw new Exception(JsonConvert.SerializeObject(batchError.Select(item => item.Value).ToList()));
                }

                // ubah ke system setting
                List<LineDetail> mposLines = validLines
                    .Where(item => !string.IsNullOrEmpty(item.SIMCardSerialNumber) 
                        && item.SIMCardSerialNumber.Length >= 5 
                        && item.SIMCardSerialNumber.Substring(0, 5) == "11111")
                    .ToList();
                foreach (var item in mposLines) {
                    UpdateMPOS(item);
                }
				
				// ini masih salah, harus disesuaikan logic exceptnya
                var activationResult = new List<LineResult>();
                Guid submissionType = validLines.Select(item => item.SubmissionType.Id).FirstOrDefault();
                List<LineDetail> lineForActivation = validLines
					.Where(item => !mposLines.Select(m => m.Id).ToList().Contains(item.Id))
					.ToList();

                if(submissionType == LookupConst.SubmissionType.NEW) {
                    activationResult = await New(lineForActivation);
                } else if(submissionType == LookupConst.SubmissionType.COP) {
                    activationResult = await COP(lineForActivation);
                } else if(submissionType == LookupConst.SubmissionType.MNP) {
                    if (lineForActivation.Count != (Lines.Count-mposLines.Count)) {
                        throw new Exception(JsonConvert.SerializeObject(
                            batchError
                                .OrderBy(item => item.Key)
                                .Select(item => item.Value)
                                .ToList()
                        ));
                    }

                    activationResult = await MNP(lineForActivation);
                }

                foreach(int index in indexOfValidLines) {
                    var line = Lines[index];
                    var activationRes = activationResult.Find(item => item.Line.Id == line.Id);
                    if(activationRes != null && !activationRes.Result.Success) {
                        batchError.Add(index, activationRes.Result.Message);
                    }
                }
				
				var successLine = activationResult.Where(item => item.Result.Success).Select(item => item.Line).ToList();
				if((submissionType == LookupConst.SubmissionType.NEW || submissionType == LookupConst.SubmissionType.MNP) && successLine.Count > 0) {
					Guid submissionId = successLine.FirstOrDefault().SubmissionId;
					List<Guid> lineDetailIds = successLine
						.Where(item => item.PRPC != null && item.PRPC.Code == "3")
						.Select(item => item.Id)
						.ToList();
					
					if(lineDetailIds.Count > 0) {
						AddVPNGroupMembersQueue.AddQueue(UserConnection, submissionId, lineDetailIds);	
					}
				}

                if(batchError.Count > 0) {
                    throw new Exception(JsonConvert.SerializeObject(
                        batchError
                            .OrderBy(item => item.Key)
                            .Select(item => item.Value)
                            .ToList()
                    ));
                }

                result.Success = true;
            } catch(Exception e) {
                result.Message = e.Message;
            }

            return result;
		}
		
        public async Task<GeneralResponse> Cancellation()
        {
            var result = new GeneralResponse();

            try {
                if(this.lineDetailId == null || this.lineDetailId == Guid.Empty) {
                    throw new Exception("Cancellation only support Line Detail Id");
                }

                var esq = new EntitySchemaQuery(UserConnection.EntitySchemaManager, "DgLineDetail");
                
                var columns = new Dictionary<string, EntitySchemaQueryColumn>();
                columns.Add("SourceId", esq.AddColumn("DgSubmission.DgSource.Id"));
                columns.Add("ActivationStatusId", esq.AddColumn("DgActivationStatus.Id"));
                columns.Add("MSISDN", esq.AddColumn("DgMSISDN"));
                columns.Add("SerialNumber", esq.AddColumn("DgSubmission.DgSerialNumber"));

                var entity = esq.GetEntity(UserConnection, this.lineDetailId);
                Guid sourceId = entity.GetValue<Guid>(columns, "SourceId");
                Guid activationStatusId = entity.GetValue<Guid>(columns, "ActivationStatusId");
                string msisdn = entity.GetValue<string>(columns, "MSISDN");
                string serialNumber = entity.GetValue<string>(columns, "SerialNumber");
                
                if(activationStatusId == Guid.Empty || 
                    activationStatusId == LookupConst.ActivationStatus.NotActivated ||
                    activationStatusId == LookupConst.ActivationStatus.Pending ||
                    activationStatusId == LookupConst.ActivationStatus.Fail ||
                    activationStatusId == LookupConst.ActivationStatus.Reject) 
                {
                    
                    GeneralResponse sfaResult = null;
                    if(sourceId == LookupConst.Source.SFA) {
                        sfaResult = await LineActivation.SFAActivationStatus(UserConnection, new LineDetail() {
                            SerialNumber = serialNumber,
                            MSISDN = msisdn
                        }, "Cancelled");
                    }

                    if(sfaResult != null && !sfaResult.Success) {
                        throw new Exception(sfaResult.Message);
                    }
                    
                    new Update(UserConnection, "DgLineDetail")
                        .Set("DgActivationStatusId", Column.Parameter(LookupConst.ActivationStatus.Cancelled))
                        .Where("Id").IsEqual(Column.Parameter(this.lineDetailId))
                        .Execute();

                    result.Success = true;

                } else {
                    throw new Exception("Line cancellation can only be done if the line activation status is Not Activated, Pending, Fail, or Reject");
                }
            } catch (Exception e) {
                result.Message = e.Message;
            }

            return result;
        }

        public static async Task<GeneralResponse> SFAActivationStatus(UserConnection UserConnection, LineDetail Line, string Status)
		{
			var lineActivationStatus = new LineActivationStatus(UserConnection);
			return await lineActivationStatus.UpdateStatus(Line.SerialNumber, Line.MSISDN, Status);
		}

        public async Task<List<LineResult>> New(List<LineDetail> Lines)
        {
            var service = new NewActivationService(UserConnection, Lines);
            return await service.Process();
        }

        public async Task<List<LineResult>> COP(List<LineDetail> Lines)
        {
             var service = new COPActivationService(UserConnection, Lines);
             return await service.Process();
        }

        public async Task<List<LineResult>> MNP(List<LineDetail> Lines)
        {
            var service = new MNPActivationService(UserConnection, Lines);
            return await service.Process();
        }

        public static List<LineResult> Response(List<LineDetail> Lines)
        {
            var result = new List<LineResult>();

            foreach (LineDetail item in Lines) {
                bool isSuccess = item.IntegrationMessage.Count == 0;
                string message = string.Join("<br>", item.IntegrationMessage.ToArray());

                result.Add(new LineResult() {
                    Line = item,
                    Result = new GeneralResponse() {
                        Success = isSuccess,
                        Message = !isSuccess ? message : null
                    }
                });
            }

            return result;
        }

        protected virtual void UpdateMPOS(LineDetail Line)
        {
            var schema = UserConnection.EntitySchemaManager.GetInstanceByName("DgLineDetail");
            var entity = schema.CreateEntity(UserConnection);

            entity.FetchFromDB("Id", Line.Id);
            entity.SetColumnValue("DgReleasedDate", DateTime.UtcNow);
            entity.SetColumnValue("DgReleasedById", UserConnection.CurrentUser.ContactId);
            entity.SetColumnValue("DgActivationStatusId", LookupConst.ActivationStatus.Activated);

            entity.Save(false);

            HistorySubmissionService.InsertHistory(
                UserConnection: UserConnection,
                SubmissionId: Line.SubmissionId,
                CreatedById: UserConnection.CurrentUser.ContactId,
                OpsId: LookupConst.Ops.UPDATE,
                SectionId: LookupConst.Section.CRA_LINE,
                Remark: $"[MPOS] {Line.MSISDN} Status:Activated",
                MSISDN: Line.MSISDN,
                LineId: Line.LineId
            );
        }
    }
}