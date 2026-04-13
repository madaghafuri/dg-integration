using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Activation;
using Terrasoft.Core;
using Terrasoft.Core.DB;
using Terrasoft.Common;
using Terrasoft.Web.Common;
using Terrasoft.Web.Http.Abstractions;
using Terrasoft.Configuration;
using Newtonsoft.Json;
using CsvHelper;
using CsvHelper.TypeConversion;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using DgBaseService.DgGenericResponse;
using ISAIntegrationSetup;
using LookupConst = DgMasterData.DgLookupConst;

namespace DgIntegration.DgUERPService
{
	public class SchedulerUERP
	{
		protected UserConnection userConnection;
        protected UserConnection UserConnection {
            get {
                return userConnection ?? (UserConnection)HttpContext.Current.Session["UserConnection"];
            }
        }

		protected string SFTPHost;
		protected string SFTPUsername;
		protected string SFTPPassword;
		protected string SFTPSource;
		protected string SFTPSearchPattern;
		protected string SaveDirectory;
		protected string ProcessedFile;
		protected List<CustomAuth> Setup;

		public SchedulerUERP(UserConnection UserConnection)
		{
			this.userConnection = UserConnection;
			Init();
		}
		
		public SchedulerUERP(UserConnection UserConnection, string ProcessedFile)
		{
			this.userConnection = UserConnection;
			this.ProcessedFile = ProcessedFile;
			Init();
		}

		public GeneralResponse Process()
		{
			var result = new GeneralResponse();
			try {
				DownloadFile();
				ProcessFile();

				result.Success = true;
			} catch (Exception e) {
				string errorMessage = e.Message;
				var innerExp = e.InnerException;
				if(innerExp != null) {
					errorMessage += $" - {innerExp.Message}";
				}
				
				result.Message = errorMessage;
			}

			return result;
		}
		
		protected void Init()
		{
			this.Setup = IntegrationSetup.GetAllDefaultCustomAuth(UserConnection, "UERP", string.Empty);
            if(this.Setup == null || (this.Setup != null && this.Setup.Count == 0)) {
                throw new Exception("UERP SFTP hasn't been set up for integration");
            }

			// Staging: 10.88.3.36
			// Production: 10.88.2.116
			this.SFTPHost = this.Setup.FirstOrDefault(item => item.Key == "SFTPHost")?.Value;
			this.SFTPUsername = this.Setup.FirstOrDefault(item => item.Key == "SFTPUsername")?.Value; // sftp_nccf_uerp
			this.SFTPPassword = this.Setup.FirstOrDefault(item => item.Key == "SFTPPassword")?.Value; // P@ssw0rd123098
			this.SFTPSource = this.Setup.FirstOrDefault(item => item.Key == "SFTPSource")?.Value; // /data/UERP/3PP/NCCF/Outbound
			this.SaveDirectory = this.Setup.FirstOrDefault(item => item.Key == "SFTPSaveDirectory")?.Value; // C:\_Projects\UPLOAD_FILES\UERP
		}
		
		public void DownloadFile()
		{
			if(string.IsNullOrEmpty(SFTPHost)) {
				throw new Exception("[SFTP] Host cannot be null or empty");
			}

			if(string.IsNullOrEmpty(SFTPUsername)) {
				throw new Exception("[SFTP] Username cannot be null or empty");
			}

			if(string.IsNullOrEmpty(SFTPPassword)) {
				throw new Exception("[SFTP] Password cannot be null or empty");
			}

			if(string.IsNullOrEmpty(SFTPSource)) {
				throw new Exception("[SFTP] Directory Source cannot be null or empty");
			}

			if(string.IsNullOrEmpty(SFTPSearchPattern)) {
				throw new Exception("[SFTP] Filename cannot be null or empty");
			}

			if(string.IsNullOrEmpty(SaveDirectory)) {
				throw new Exception("[SFTP] Save Directory cannot be null or empty");
			}

			using (SftpClient sftp = new SftpClient(SFTPHost, SFTPUsername, SFTPPassword)) {
				sftp.Connect();
				try {
					var files = sftp.ListDirectory(SFTPSource);
					if(!files.Any()) {
						throw new Exception($"No Files found in SFTP Source Location: {SFTPSource}");
					}

					var file = files
						.Where(item => item.Name.Contains(SFTPSearchPattern) && Path.GetExtension(item.FullName) == ".csv")
						.FirstOrDefault();
					if(file == null) {
						throw new Exception($"Files {SFTPSearchPattern} not found in SFTP Source Location: {SFTPSource}");
					}

					string filenameFullPath = Path.Combine(SaveDirectory, file.Name);
					if (System.IO.File.Exists(filenameFullPath)) {
						System.IO.File.Delete(filenameFullPath);
					}

					using (Stream fileStream = System.IO.File.OpenWrite(filenameFullPath)) {
						sftp.DownloadFile(file.FullName, fileStream);
						sftp.DeleteFile(file.FullName);
					}

					ProcessedFile = filenameFullPath;
				} catch (Exception e) {
					throw;
				} finally {
					sftp.Disconnect();
				}
			}
		}

		public virtual void ProcessFile()
		{
			if(!System.IO.File.Exists(ProcessedFile)) {
				throw new Exception($"File {ProcessedFile} not exists in System");
			}
		}
	}
		
	public class IMEICancelUERP : SchedulerUERP
	{
		public IMEICancelUERP(UserConnection UserConnection, string ProcessedFile = "") : base(UserConnection, ProcessedFile)
		{
			SFTPSearchPattern = this.Setup.FirstOrDefault(item => item.Key == "IMEICancel_SearchPattern")?.Value;
		}

		public override void ProcessFile()
		{
			base.ProcessFile();

			using (var reader = new StreamReader(ProcessedFile))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture)) {
				var records = csv.GetRecords<IMEICancel>();
				foreach (var record in records) {
					try {
						string lineId = record.SourceLineNumber.Substring(0, record.SourceLineNumber.IndexOf('_'));
						string itemNumberPrefix = record.ItemNumber.Substring(0, record.ItemNumber.IndexOf('_'));

						if(itemNumberPrefix != "HST" && itemNumberPrefix != "SIM") {
							continue;
						}

						new Update(UserConnection, "DgLineDetail")
							.Set(itemNumberPrefix == "HST" ? "DgDeviceIMEI" : "DgSIMCardNumber", Column.Parameter(record.FromImei))
							.Where("DgLineId").IsEqual(Column.Parameter(lineId))
							.And("DgSOID").IsEqual(Column.Parameter(record.SourceOrderNumber))
							.Execute();	
					} catch (Exception e) {
						throw;
					}
				}
			}
		}
	}

	public class SOCancelUERP : SchedulerUERP
	{
		public SOCancelUERP(UserConnection UserConnection, string ProcessedFile = "") : base(UserConnection, ProcessedFile)
		{
			SFTPSearchPattern = this.Setup.FirstOrDefault(item => item.Key == "SOCancel_SearchPattern")?.Value;
		}

		public override void ProcessFile()
		{
			base.ProcessFile();
			
			using (var reader = new StreamReader(ProcessedFile))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture)) {
				var records = csv.GetRecords<SOCancel>();
				foreach (var record in records) {
					try {
						string lineId = record.SourceLineNumber.Substring(0, record.SourceLineNumber.IndexOf('_'));
						new Update(UserConnection, "DgLineDetail")
							.Set("DgReleasedToIPL", Column.Parameter(false))
							.Set("DgSOCancelDateTime", Column.Parameter(record.CancelDate))
							.Set("DgSOCancelRemark", Column.Parameter(record.CancelReason))
							.Set("DgDeliveryStatusId", Column.Parameter(LookupConst.DeliveryStatus.CancelDelivery)) // DeliveryStatus Code: 05
							.Where("DgLineId").IsEqual(Column.Parameter(lineId))
							.And("DgSOID").IsEqual(Column.Parameter(record.SourceOrderNumber))
							.Execute();
					} catch (Exception e) {
						throw;
					}
				}
			}
		}
	}

	public class IMEICancel
	{
		[Name("SALES_ORDER_NUMBER"), TypeConverter(typeof(StringTrimConverter))]
		public string SalesOrderNumber { get; set; }

		[Name("SALES_ORDER_LINE_NUMBER"), TypeConverter(typeof(StringTrimConverter))]
		public string SalesOrderLineNumber { get; set; }

		[Name("SOURCE_ORDER_NUMBER"), TypeConverter(typeof(StringTrimConverter))]
		public string SourceOrderNumber { get; set; }

		[Name("SOURCE_LINE_NUMBER"), TypeConverter(typeof(StringTrimConverter))]
		public string SourceLineNumber { get; set; }

		[Name("SUB_INVENTORY_NAME"), TypeConverter(typeof(StringTrimConverter))]
		public string SubInventoryName { get; set; }

		[Name("ITEM_NUMBER"), TypeConverter(typeof(StringTrimConverter))]
		public string ItemNumber { get; set; }

		[Name("SHIPPED_QUANTITY")]
		public int ShippedQuantity { get; set; }

		[Name("SHIPPED_DATE"), TypeConverter(typeof(DateTimeConverter))]
		public DateTime ShippedDate { get; set; }

		[Name("FROM_IMEI_"), TypeConverter(typeof(StringTrimConverter))]
		public string FromImei { get; set; }

		[Name("TO_IMEI_"), TypeConverter(typeof(StringTrimConverter))]
		public string ToImei { get; set; }
	}

	public class SOCancel
	{
		[Name("SALES_ORDER_NUMBER"), TypeConverter(typeof(StringTrimConverter))]
		public string SalesOrderNumber { get; set; }

		[Name("SALES_ORDER_LINE_NUMBER"), TypeConverter(typeof(StringTrimConverter))]
		public string SalesOrderLineNumber { get; set; }

		[Name("SOURCE_ORDER_NUMBER"), TypeConverter(typeof(StringTrimConverter))]
		public string SourceOrderNumber { get; set; }

		[Name("SOURCE_LINE_NUMBER"), TypeConverter(typeof(StringTrimConverter))]
		public string SourceLineNumber { get; set; }

		[Name("CANCEL_DATE"), TypeConverter(typeof(DateTimeConverter))]
		public DateTime CancelDate { get; set; }

		[Name("CANCEL_REASON"), TypeConverter(typeof(StringTrimConverter))]
		public string CancelReason { get; set; }
	}

	public class DateTimeConverter : DefaultTypeConverter
	{
		public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
		{
			DateTime date = DateTime.ParseExact(text.Trim(), "yyyy/MM/dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
			var tz = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
			return TimeZoneInfo.ConvertTimeToUtc(date, tz);
		}
	}

	public class StringTrimConverter : DefaultTypeConverter
	{
		public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
		{
			return text?.Trim();
		}
	}
}