using LijsDev.CrystalReportsRunner.Core;

using SampleCaller;

// ========== Initializing Engine ===========

// Method 1: With Connection string
using var engine = new CrystalReportsEngine();

// Method 2: Without Connection string
// using var engine = new CrystalReportsEngine();

// ========== Customizing Viewer Settings ===========

engine.ViewerSettings.AllowedExportFormats =
    ReportViewerExportFormats.PdfFormat |
    ReportViewerExportFormats.ExcelFormat |
    ReportViewerExportFormats.CsvFormat |
    ReportViewerExportFormats.WordFormat |
    ReportViewerExportFormats.XmlFormat |
    ReportViewerExportFormats.RtfFormat |
    ReportViewerExportFormats.ExcelRecordFormat |
    ReportViewerExportFormats.EditableRtfFormat |
    ReportViewerExportFormats.XLSXFormat |
    ReportViewerExportFormats.XmlFormat;

engine.ViewerSettings.ShowRefreshButton = false;
engine.ViewerSettings.ShowCopyButton = false;
engine.ViewerSettings.ShowGroupTreeButton = false;
engine.ViewerSettings.ShowParameterPanelButton = false;
engine.ViewerSettings.EnableDrillDown = false;
engine.ViewerSettings.ToolPanelView = ReportViewerToolPanelViewType.None;
engine.ViewerSettings.ShowCloseButton = false;
engine.ViewerSettings.EnableRefresh = false;


engine.ViewerSettings.SetUICulture(Thread.CurrentThread.CurrentUICulture);

// ========== Showing the Report ===========

var dataset = new PersonDataset();

var personsTable = dataset.Tables["Persons"];

for (int i = 0; i < 100; i++)
{
    var row = personsTable!.NewRow();
    row["Name"] = $"Person {i + 1}";
    personsTable.Rows.Add(row);
}

// Method 1: Full Control
var report = new Report("DatasetReport.rpt", "Sample Report")
{
    DataSets = new List<System.Data.DataSet> { dataset }
};

report.Parameters.Add("ComputerName", Environment.MachineName);

await engine.ShowReport(report);

Console.ReadKey();
