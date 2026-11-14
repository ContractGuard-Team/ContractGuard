namespace ContractGuard.Application.Reports;

// Turns report data into a file. One implementation per format (Infrastructure: QuestPDF, OpenXML).
public interface IReportRenderer
{
    ReportFormat Format { get; }

    string ContentType { get; }

    byte[] Render(ReportData data);
}
