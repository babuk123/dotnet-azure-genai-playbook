// LEGACY reference (.NET Framework 4.8 WCF) — shown for comparison, not built in this project.
using System.Runtime.Serialization;
using System.ServiceModel;

[ServiceContract(Namespace = "http://contoso.com/reports/2020")]
public interface IReportService
{
    [OperationContract]
    [FaultContract(typeof(ReportFault))]
    ReportResponse GetDrawdownReport(ReportRequest request);
}

[DataContract]
public class ReportRequest
{
    [DataMember] public string AccountId { get; set; }
    [DataMember] public DateTime AsOfDate { get; set; }
}

[DataContract]
public class ReportResponse
{
    [DataMember] public string AccountId { get; set; }
    [DataMember] public decimal MaxDrawdownPercent { get; set; }
}

[DataContract]
public class ReportFault { [DataMember] public string Code { get; set; } [DataMember] public string Message { get; set; } }

[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
public class ReportService : IReportService
{
    public ReportResponse GetDrawdownReport(ReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AccountId))
            throw new FaultException<ReportFault>(new ReportFault { Code = "E100", Message = "AccountId required" },
                                                  new FaultReason("Invalid request"));
        return new ReportResponse { AccountId = request.AccountId, MaxDrawdownPercent = 12.5m };
    }
}

/* web.config (excerpt)
<system.serviceModel>
  <services>
    <service name="ReportService">
      <endpoint address="" binding="basicHttpBinding" contract="IReportService" />
      <endpoint address="tcp" binding="netTcpBinding" contract="IReportService" />
    </service>
  </services>
</system.serviceModel>
*/
