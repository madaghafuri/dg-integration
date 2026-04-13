    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Terrasoft.Core;
    using Terrasoft.Core.DB;
    using Terrasoft.Common;
    using Newtonsoft.Json;
    using DgBaseService.DgHelpers;
    using DgSubmission.DgHistorySubmissionService;
    using DgIntegration.DgCommonInventory;
    using DgIntegration.DgMMAGOrderCreateService;
    using LookupConst = DgMasterData.DgLookupConst;
    // using DgDMS = DgIntegration.DgDMS;

    namespace DgIntegration.DgAuto3PLProcess
    {
        public class Auto3PLProcess
        {
            protected UserConnection UserConnection;
            protected CustomLog log;
            protected string errorMessage;
            protected string soId;
            protected Guid submissionId;
            protected string lineCommonInventoryStr;
            protected List<Dictionary<string, string>> lineCommonInventoryList;

            public Auto3PLProcess(UserConnection userConnection, string soId, Guid submissionId, string lineCommonInventoryStr)
            {
                this.UserConnection = userConnection;

                this.soId = soId;
                this.submissionId = submissionId;
                this.lineCommonInventoryStr = lineCommonInventoryStr;

                if(!string.IsNullOrEmpty(lineCommonInventoryStr)) {
                    this.lineCommonInventoryList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(lineCommonInventoryStr);
                }

                this.log = new CustomLog(UserConnection, string.Empty);
            }

            public virtual List<Guid> GetLineDetails(UserConnection UserConnection, string soId)
            {
                var result = new List<Guid>();

                var select = new Select(UserConnection)
                    .Column("Id")
                .From("DgLineDetail")
                .Where("DgSOId").IsEqual(Column.Parameter(soId))
                .And("DgReleasedToIPL").IsEqual(Column.Parameter(true)) as Select;
                
                using(DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
                    using(IDataReader dataReader = select.ExecuteReader(dbExecutor)) {
                        while (dataReader.Read()) {
                            result.Add(dataReader.GetColumnValue<Guid>("Id"));	
                        }
                    }
                }

                return result;
            }
        }

        // public class AutoCreateProduct : Auto3PLProcess
        // {
        //     public AutoCreateProduct(UserConnection userConnection, string soId, Guid submissionId, string lineCommonInventoryStr) : base(userConnection, soId, submissionId, lineCommonInventoryStr)
        //     {
        //         this.log.Name = $"Auto_CreateProduct_{soId}";
        //     }

        //     public async Task<bool> Run()
        //     {
        //         bool isSuccess = false;

        //         log.AddMessage($"Send Request to Create Product with SO Number: {soId}. Submission Id: {submissionId.ToString()}.", true);
        //         string commonInventoryList = string.Join(
        //             ". ", 
        //             lineCommonInventoryList
        //                 .Select(item => "Reservation ID: "+item["ReservationID"]+", Store ID: "+item["StoreID"])
        //                 .ToArray()
        //         );

        //         try {
        //             isSuccess = await CreateProduct();
        //             if (isSuccess) {
        //                 return true;
        //             }

        //             SendErrorMail(commonInventoryList);
        //         } catch (Exception e) {
        //             log.AddMessage($"Something wrong happen: {Environment.NewLine}{e.ToString()}", true);
        //         } finally {
        //             log.SaveToFile();
        //         }

        //         return isSuccess;
        //     }

        //     // protected async Task<bool> CreateProduct()
        //     // {
        //     //     bool isSuccess = true;
        //     //     string msg = string.Empty;

        //     //     var service = new DgDMS.DMSService(UserConnection);
        //     //     var param = new DgDMS.CreateProduct(UserConnection).GetParam(soId);
        //     //     log.AddMessage($"JSON Request: {Environment.NewLine}{JsonConvert.SerializeObject(param)}", true);
                
        //     //     try {
        //     //         var createProduct = await service.CreateProduct(soId);
        //     //         log.AddMessage($"JSON Response: {Environment.NewLine}{JsonConvert.SerializeObject(createProduct)}", true);

        //     //         msg = "success.";
        //     //         log.AddMessage($"Create Product is Success", true);
        //     //     } catch (Exception e) {
        //     //         isSuccess = false;
        //     //         msg = $"failed. {e.Message}";
        //     //         errorMessage = e.Message;

        //     //         log.AddMessage($"Create Product is Failed: {Environment.NewLine}{errorMessage}{Environment.NewLine}{e.ToString()}", true);
        //     //     } finally {
        //     //         HistorySubmissionService.InsertHistory(
        //     //             UserConnection: UserConnection,
        //     //             SubmissionId: submissionId,
        //     //             CreatedById: UserConnection.CurrentUser.ContactId,
        //     //             OpsId: LookupConst.Ops.ADD,
        //     //             SectionId: LookupConst.Section.RELEASED_TO_MESAD,
        //     //             Remark: $"[Create Product] SO {soId} {msg}"
        //     //         );

        //     //         UpdateLineDetail(isSuccess);
        //     //     }

        //     //     return isSuccess;
        //     // }

        //     protected void UpdateLineDetail(bool isSuccess)
        //     {
        //         var errorMessage = new List<string>();

        //         var lineDetails = GetLineDetails(UserConnection, soId);
        //         foreach (Guid id in lineDetails) {
        //             try {
        //                 var update = new Update(UserConnection, "DgLineDetail");
                        
        //                 if(isSuccess) {
        //                     update.Set("DgIsCreateDelivery", Column.Parameter(true));
        //                     update.Set("DgIsCreateMMAG", Column.Parameter(true));
        //                 } else {
        //                     update
        //                         .Set("DgReleasedToIPL", Column.Parameter(false))
        //                         .Set("DgIsUERP", Column.Parameter(false));
        //                 }

        //                 update
        //                     .Where("Id").IsEqual(Column.Parameter(id))
        //                     .Execute();
        //             } catch (Exception e) {
        //                 errorMessage.Add($"Update Id: {id} - {e.Message}");
        //             }
        //         }

        //         if(errorMessage.Count > 0) {
        //             log.AddMessage($"Update line detail fail: {Environment.NewLine}{string.Join(Environment.NewLine, errorMessage.ToArray())}", true);
        //         }
        //     }

        //     protected void SendErrorMail(string commonInventoryList)
        //     {
        //         try {
        //             string email = Terrasoft.Core.Configuration.SysSettings.GetValue<string>(UserConnection, "DgEmailNotification_CreateDeliveryOrderFailed", string.Empty);
        //             string message = $"Dear User,"
        //                 + $"<br><br>Create Product Order for {soId} has been Failed, "
        //                 + $"due to the following Exception: <strong>{errorMessage}</strong>. <br><br>"
        //                 + $"The stock for this <strong>{soId}</strong> has been cancelled. Please find the cancellation IDs below: <br>"
        //                 + commonInventoryList.Replace(". ", "<br>")
        //                 + $"<br><br>This message is auto-generated by NCCF.";

        //             var param = new MailParam() {
        //                 Subject = "NCCF Create Product Order Failed from SAP",
        //                 Message = message,
        //                 To = email,
        //                 DefaultFooterMessage = true
        //             };

        //             log.AddMessage($"Send email to {email} for error notification", true);
        //             Mail.Send(UserConnection, "nccf2-uerp-socreation@celcomdigi.com", param);
        //         } catch (Exception e) {
        //             log.AddMessage($"Send email error: {e.ToString()}", true);
        //         }
        //     }
        // }

        public class AutoCreateDelivery : Auto3PLProcess
        {        
            public AutoCreateDelivery(UserConnection userConnection, string soId, Guid submissionId, string lineCommonInventoryStr) : base(userConnection, soId, submissionId, lineCommonInventoryStr)
            {
                this.log.Name = $"Auto_CreateDelivery_{soId}";
            }

            public async Task<bool> Run()
            {
                bool isSuccess = false;
                string commonInventoryList = string.Join(
                    ". ", 
                    lineCommonInventoryList
                        .Select(item => "Reservation ID: "+item["ReservationID"]+", Store ID: "+item["StoreID"])
                        .ToArray()
                );

                log.AddMessage($"Send Request to Create Delivery with SO Number: {soId}. Submission Id: {submissionId.ToString()}. Common Inventory: {commonInventoryList}", true);
                try {
                    isSuccess = await CreateDelivery();
                    if(isSuccess) {
                        return true;
                    }

                    SendErrorMail(commonInventoryList);
                } catch (Exception e) {
                    log.AddMessage($"Something wrong happen: {Environment.NewLine}{e.ToString()}", true);
                } finally {
                    log.SaveToFile();
                }

                return isSuccess;
            }
            
            protected virtual async Task<bool> CreateDelivery()
            {
                var service = new CommonInventoryService(UserConnection);

                bool isSuccess = true;
                string msg = string.Empty;

                try {
                    var createDeliveryOrder = new CreateDeliveryOrder(UserConnection);
                    var param = createDeliveryOrder.GetParam(soId);
                    log.AddMessage($"JSON Request: {Environment.NewLine}{JsonConvert.SerializeObject(param)}", true);

                    var createDelivery = await service.CreateDeliveryOrder(createDeliveryOrder, param);
                    log.AddMessage($"JSON Response: {Environment.NewLine}{JsonConvert.SerializeObject(createDelivery)}", true);

                    msg = "success.";
                    log.AddMessage($"Create Delivery is Success", true);
                } catch (Exception e) {
                    isSuccess = false;
                    msg = $"failed. {e.Message}";
                    errorMessage = e.Message;

                    log.AddMessage($"Create Delivery is Failed: {Environment.NewLine}{errorMessage}{Environment.NewLine}{e.ToString()}", true);
                } finally {
                    HistorySubmissionService.InsertHistory(
                        UserConnection: UserConnection,
                        SubmissionId: submissionId,
                        CreatedById: UserConnection.CurrentUser.ContactId,
                        OpsId: LookupConst.Ops.ADD,
                        SectionId: LookupConst.Section.RELEASED_TO_MESAD,
                        Remark: $"[Create Delivery] SO {soId} {msg}"
                    );

                    UpdateLineDetail(isSuccess);
                }

                return isSuccess;
            }

            protected void UpdateLineDetail(bool isSuccess)
            {
                var errorMessage = new List<string>();

                var lineDetails = GetLineDetails(UserConnection, soId);
                foreach (Guid id in lineDetails) {
                    try {
                        var update = new Update(UserConnection, "DgLineDetail");
                        
                        if(isSuccess) {
                            update.Set("DgIsCreateDelivery", Column.Parameter(true));
                        } else {
                            update
                                .Set("DgReleasedToIPL", Column.Parameter(false))
                                .Set("DgIsUERP", Column.Parameter(false));
                        }

                        update
                            .Where("Id").IsEqual(Column.Parameter(id))
                            .Execute();
                    } catch (Exception e) {
                        errorMessage.Add($"Update Id: {id} - {e.Message}");
                    }
                }

                if(errorMessage.Count > 0) {
                    log.AddMessage($"Update line detail fail: {Environment.NewLine}{string.Join(Environment.NewLine, errorMessage.ToArray())}", true);
                }
            }

            protected void SendErrorMail(string commonInventoryList)
            {
                try {
                    string email = Terrasoft.Core.Configuration.SysSettings.GetValue<string>(UserConnection, "DgEmailNotification_CreateDeliveryOrderFailed", string.Empty);
                    string message = $"Dear User,"
                        + $"<br><br>Create Delivery Order for {soId} has been Failed, "
                        + $"due to the following Exception: <strong>{errorMessage}</strong>. <br><br>"
                        + $"The stock for this <strong>{soId}</strong> has been cancelled. Please find the cancellation IDs below: <br>"
                        + commonInventoryList.Replace(". ", "<br>")
                        + $"<br><br>This message is auto-generated by NCCF.";

                    var param = new MailParam() {
                        Subject = "NCCF Create Delivery Order Failed from SAP",
                        Message = message,
                        To = email,
                        DefaultFooterMessage = true
                    };

                    log.AddMessage($"Send email to {email} for error notification", true);
                    Mail.Send(UserConnection, "nccf2-uerp-socreation@celcomdigi.com", param);
                } catch (Exception e) {
                    log.AddMessage($"Send email error: {e.ToString()}", true);
                }
            }
        }

        public class AutoUnreserve : Auto3PLProcess
        {
            public AutoUnreserve(UserConnection userConnection, string soId, Guid submissionId, string lineCommonInventoryStr) : base(userConnection, soId, submissionId, lineCommonInventoryStr)
            {
                this.log.Name = $"Auto_Unreserve_{soId}";
            }

            public async Task<bool> Run()
            {
                bool isSuccess = false;
                string commonInventoryList = string.Join(
                    ". ", 
                    lineCommonInventoryList
                        .Select(item => "Reservation ID: "+item["ReservationID"]+", Store ID: "+item["StoreID"])
                        .ToArray()
                );

                log.AddMessage($"Send Request to Unreserve with SO Number: {soId}. Submission Id: {submissionId.ToString()}. Common Inventory: {commonInventoryList}", true);
                try {
                    isSuccess = await Unreserve();
                } catch (Exception e) {
                    log.AddMessage($"Something wrong happen: {Environment.NewLine}{e.ToString()}", true);
                } finally {
                    log.SaveToFile();
                }

                return isSuccess;
            }

            protected virtual async Task<bool> Unreserve()
            {
                var service = new CommonInventoryService(UserConnection);
                
                var errorList = new List<string>();
                foreach (var item in lineCommonInventoryList) {
                    await Task.Delay(1000);

                    string reservationID = item["ReservationID"];
                    string storeID = item["StoreID"];

                    log.AddMessage($"Reservation ID: {reservationID}. Store ID: {storeID}", true);

                    try {
                        var unreserveStock = new UnreserveStock(UserConnection);
                        var param = unreserveStock.GetParam(reservationID, storeID);
                        log.AddMessage($"JSON Request: {Environment.NewLine}{JsonConvert.SerializeObject(param)}", true);

                        var unreserve = await service.Unreserve(unreserveStock, param);
                        log.AddMessage($"JSON Response: {Environment.NewLine}{JsonConvert.SerializeObject(unreserve)}", true);
                        
                        HistorySubmissionService.InsertHistory(
                            UserConnection: UserConnection,
                            SubmissionId: submissionId,
                            CreatedById: UserConnection.CurrentUser.ContactId,
                            OpsId: LookupConst.Ops.ADD,
                            SectionId: LookupConst.Section.RELEASED_TO_MESAD,
                            Remark: $"[Unreserve] Reservation ID: {reservationID} Store ID: {storeID} success"
                        );

                        new Update(UserConnection, "DgLineDetail")
                            .Set("DgIsCommon", Column.Parameter(false))
                            .Set("DgReservationID", Column.Parameter(string.Empty))
                            .Where("DgReservationID").IsEqual(Column.Parameter(reservationID))
                        .Execute();

                        log.AddMessage($"Unreserve Reservation ID: {reservationID}. Store ID: {storeID} is success", true);
                    } catch (Exception e) {
                        errorList.Add($"Unreserve Reservation ID: {reservationID} Store ID: {storeID} failed. {e.Message}");
                        
                        log.AddMessage($"Unreserve Reservation ID: {reservationID}. Store ID: {storeID} is failed. {Environment.NewLine}{e.ToString()}", true);
                    }
                }

                if(errorList.Count > 0) {
                    HistorySubmissionService.InsertHistory(
                        UserConnection: UserConnection,
                        SubmissionId: submissionId,
                        CreatedById: UserConnection.CurrentUser.ContactId,
                        OpsId: LookupConst.Ops.ADD,
                        SectionId: LookupConst.Section.RELEASED_TO_MESAD,
                        Remark: $"[Unreserve] {string.Join(Environment.NewLine, errorList)}"
                    );
                }

                return true;
            }
        }

        // public class AutoUnreserveDMS : Auto3PLProcess
        // {
        //     public AutoUnreserveDMS(UserConnection userConnection, string soId, Guid submissionId) : base(userConnection, soId, submissionId, "")
        //     {
        //         this.log.Name = $"Auto_Unreserve_{soId}";
        //     }

        //     public async Task<bool> Run()
        //     {
        //         bool isSuccess = false;

        //         log.AddMessage($"Send Request to Unreserve with SO Number: {soId}. Submission Id: {submissionId.ToString()}.", true);
        //         try {
        //             isSuccess = await UnreserveDMS();
        //         } catch (Exception e) {
        //             log.AddMessage($"Something wrong happen: {Environment.NewLine}{e.ToString()}", true);
        //         } finally {
        //             log.SaveToFile();
        //         }

        //         return isSuccess;
        //     }

        //     // protected async Task<bool> UnreserveDMS()
        //     // {
        //     //     var service = new DgDMS.DMSService(UserConnection);
        //     //     var unreserve = new DgDMS.UnreserveStock(UserConnection);

        //     //     string reservationID = GetReservationId(soId);
        //     //     var errorList = new List<string>();
                
        //     //     var param = unreserve.GetParam(reservationID);
        //     //     log.AddMessage($"JSON Request: {Environment.NewLine}{JsonConvert.SerializeObject(param)}", true);
                
        //     //     await Task.Delay(1000);

        //     //     try {
        //     //         var unreserveService = await service.UnreserveStock(param);
        //     //         log.AddMessage($"JSON Response: {Environment.NewLine}{JsonConvert.SerializeObject(unreserveService)}", true);

        //     //         HistorySubmissionService.InsertHistory(
        //     //             UserConnection: UserConnection,
        //     //             SubmissionId: submissionId,
        //     //             CreatedById: UserConnection.CurrentUser.ContactId,
        //     //             OpsId: LookupConst.Ops.ADD,
        //     //             SectionId: LookupConst.Section.RELEASED_TO_MESAD,
        //     //             Remark: $"[Unreserve] Reservation ID: {reservationID} success"
        //     //         );

        //     //         new Update(UserConnection, "DgLineDetail")
        //     //             .Set("DgIsCommon", Column.Parameter(false))
        //     //             .Set("DgReservationID", Column.Parameter(string.Empty))
        //     //             .Where("DgReservationID").IsEqual(Column.Parameter(reservationID))
        //     //         .Execute();
        //     //         log.AddMessage($"Unreserve Reservation ID: {reservationID} is success", true);
        //     //     } catch (Exception e) {
        //     //         errorList.Add($"Unreserve Reservation ID: {reservationID} failed. {e.Message}");
        //     //         log.AddMessage($"Unreserve Reservation ID: {reservationID} {Environment.NewLine}{e.ToString()}", true);
        //     //     }

        //     //     if(errorList.Count > 0) {
        //     //         HistorySubmissionService.InsertHistory(
        //     //             UserConnection: UserConnection,
        //     //             SubmissionId: submissionId,
        //     //             CreatedById: UserConnection.CurrentUser.ContactId,
        //     //             OpsId: LookupConst.Ops.ADD,
        //     //             SectionId: LookupConst.Section.RELEASED_TO_MESAD,
        //     //             Remark: $"[Unreserve] {string.Join(Environment.NewLine, errorList)}"
        //     //         );
        //     //     }

        //     //     return true;
        //     // }

        //     protected string GetReservationId(string SOID)
        //     {
        //         var result = new List<string>();

        //         var select = new Select(UserConnection)
        //             .Column("DgReservationID")
        //             .From("DgLineDetail")
        //             .Where("DgSOId").IsEqual(Column.Parameter(soId))
        //             .And("DgReleasedToIPL").IsEqual(Column.Parameter(true)) as Select;
                    
        //             using(DBExecutor dbExecutor = UserConnection.EnsureDBConnection()) {
        //                 using(IDataReader dataReader = select.ExecuteReader(dbExecutor)) {
        //                     while (dataReader.Read()) {
        //                         result.Add(dataReader.GetColumnValue<string>("DgReservationID"));	
        //                     }
        //                 }
        //             }

        //         return result.FirstOrDefault() ?? string.Empty;
        //     }
        // }

        public class AutoMMAG : Auto3PLProcess
        {
            public AutoMMAG(UserConnection userConnection, string soId, Guid submissionId) : base(userConnection, soId, submissionId, "")
            {
                this.log.Name = $"Auto_MMAG_{soId}";
            }

            public async Task<bool> Run()
            {
                bool isSuccess = false;
                
                log.AddMessage($"Send Request to MMAG with SO Number: {soId}. Submission Id: {submissionId.ToString()}", true);
                try {
                    isSuccess = await MMAG();
                } catch (Exception e) {
                    log.AddMessage($"Something wrong happen: {Environment.NewLine}{e.ToString()}", true);
                } finally {
                    log.SaveToFile();
                }

                return isSuccess;
            }

            protected async Task<bool> MMAG()
            {
                var service = new MMAGOrderCreateService(UserConnection);
                try {
                    service.SetParamBySONumber(soId);
                    log.AddMessage($"XML Request: {Environment.NewLine}{service.GetStringRequest()}", true);
                    
                    await service.Request();
                    log.AddMessage($"XML Response: {Environment.NewLine}{service.GetStringResponse()}", true);
                        
                    MMAGOrderCreateService.InsertLog(UserConnection, service.GetLog(), soId, service.IsSuccessResponse() ? "SUCCESS" : "FAIL");

                    if(!service.IsSuccessResponse()) {
                        string errorMessage = service.GetErrorResponse();
                        
                        HistorySubmissionService.InsertHistory(
                            UserConnection: UserConnection,
                            SubmissionId: submissionId,
                            CreatedById: UserConnection.CurrentUser.ContactId,
                            OpsId: LookupConst.Ops.ADD,
                            SectionId: LookupConst.Section.RELEASED_TO_MESAD,
                            Remark: $"[3PL] SO {soId} failed. {errorMessage}"
                        );

                        log.AddMessage($"MMAG is Failed: {Environment.NewLine}{errorMessage}", true);
                    } else {
                        var lineDetails = GetLineDetails(UserConnection, soId);
                        foreach (Guid id in lineDetails) {
                            new Update(UserConnection, "DgLineDetail")
                                .Set("DgSODoID", Column.Parameter(soId))
                                .Set("DgIsMMAG", Column.Parameter(true))
                                .Where("Id").IsEqual(Column.Parameter(id))
                            .Execute();
                        }

                        HistorySubmissionService.Release3PL(
                            UserConnection: UserConnection,
                            SubmissionId: submissionId,
                            OFSDoNoId: soId,
                            CreatedById: UserConnection.CurrentUser.ContactId
                        );

                        log.AddMessage($"MMAG is Success", true);
                    }
                } catch (Exception e) {
                    log.AddMessage($"MMAG is Failed: {Environment.NewLine}{e.ToString()}", true);
                }

                return true;
            }
        }
    }