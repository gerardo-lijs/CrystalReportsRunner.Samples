namespace WpfCaller;

using LijsDev.CrystalReportsRunner.Core;

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;

public partial class MainWindow : Window
{
    /// <summary>
    /// Shared Crystal Reports Engine
    /// </summary>
    private CrystalReportsEngine? _engineInstance;
    private WindowLocation? _lastLocation;

    public MainWindow()
    {
        InitializeComponent();
        Closed += MainWindow_Closed;
    }

    /// <summary>
    /// Create engine if needed for first time or if runner process no longer available.
    /// </summary>
    private void EnsureEngineAvailable()
    {
        if (_engineInstance is null)
        {
            // Create new engine if needed
            _engineInstance = CreateEngine();
        }
        else
        {
            // Create new engine if runner process is dead
            if (!_engineInstance.IsRunnerProcessAvailable())
            {
                _engineInstance.Dispose();
                _engineInstance = CreateEngine();
            }
        }
    }

    private async void MainWindow_Closed(object? sender, EventArgs e)
    {
        if (_engineInstance is not null)
        {
            await _engineInstance.CloseRunner();
            _engineInstance.Dispose();
        }
    }

    private async void ShowButton_Click(object sender, RoutedEventArgs e)
    {
        LoadingBorder.Visibility = Visibility.Visible;

        try
        {
            EnsureEngineAvailable();
            if (_engineInstance is null) throw new InvalidProgramException($"{nameof(_engineInstance)} can't be null here after calling EnsureEngineAvailable.");

            ConfigureWindowLocationAndSize();

            // Show
            var report = CreateReport();
            var windowHandle = new WindowHandle(new WindowInteropHelper(this).EnsureHandle());
            await _engineInstance.ShowReport(report, owner: windowHandle);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString());
        }
        finally
        {
            LoadingBorder.Visibility = Visibility.Collapsed;
        }
    }

    private void ConfigureWindowLocationAndSize()
    {
        if (_lastLocation is not null)
        {
            _engineInstance!.ViewerSettings.WindowLocationLeft = _lastLocation.Left;
            _engineInstance.ViewerSettings.WindowLocationTop = _lastLocation.Top;
            _engineInstance.ViewerSettings.WindowLocationHeight = _lastLocation.Height;
            _engineInstance.ViewerSettings.WindowLocationWidth = _lastLocation.Width;
            _engineInstance.ViewerSettings.WindowInitialPosition = ReportViewerWindowStartPosition.Manual;
        }
    }

    private async void ShowDialogButton_Click(object sender, RoutedEventArgs e)
    {
        LoadingBorder.Visibility = Visibility.Visible;

        try
        {
            EnsureEngineAvailable();
            if (_engineInstance is null) throw new InvalidProgramException($"{nameof(_engineInstance)} can't be null here after calling EnsureEngineAvailable.");

            ConfigureWindowLocationAndSize();

            // Show
            var report = CreateReport();
            var windowHandle = new WindowHandle(new WindowInteropHelper(this).EnsureHandle());
            var result = await _engineInstance.ShowReportDialog(report, owner: windowHandle);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString());
        }
        finally
        {
            LoadingBorder.Visibility = Visibility.Collapsed;
        }
    }

    private async void ExportReportButton_Click(object sender, RoutedEventArgs e)
    {
        LoadingBorder.Visibility = Visibility.Visible;

        try
        {
            EnsureEngineAvailable();
            if (_engineInstance is null) throw new InvalidProgramException($"{nameof(_engineInstance)} can't be null here after calling EnsureEngineAvailable.");

            // Export
            var report = CreateReport();
            var dstFilename = "sample_report.pdf";
            await _engineInstance.Export(report, ReportExportFormats.PDF, dstFilename, overwrite: true);

            Process.Start(new ProcessStartInfo
            {
                FileName = dstFilename,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString());
        }
        finally
        {
            LoadingBorder.Visibility = Visibility.Collapsed;
        }
    }

    private async void ExportReportToStreamButton_Click(object sender, RoutedEventArgs e)
    {
        LoadingBorder.Visibility = Visibility.Visible;

        try
        {
            EnsureEngineAvailable();
            if (_engineInstance is null) throw new InvalidProgramException($"{nameof(_engineInstance)} can't be null here after calling EnsureEngineAvailable.");

            // Export
            var report = CreateReport();
            using var reportStream = await _engineInstance.Export(report, ReportExportFormats.PDF);

            var dstFilename = "sample_report_stream.pdf";
            if (System.IO.File.Exists(dstFilename)) System.IO.File.Delete(dstFilename);
            using var sw = new System.IO.FileStream(dstFilename, System.IO.FileMode.Create);
            await reportStream.CopyToAsync(sw);

            Process.Start(new ProcessStartInfo
            {
                FileName = dstFilename,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString());
        }
        finally
        {
            LoadingBorder.Visibility = Visibility.Collapsed;
        }
    }

    private static Report CreateReport()
    {
        var report = new Report("SampleReport.rpt", "Sample Report")
        {
            Connection = CrystalReportsConnectionFactory.CreateSqlConnection(".\\SQLEXPRESS", "CrystalReportsSample")
        };
        report.Parameters.Add("ReportFrom", new DateTime(2022, 01, 01));
        report.Parameters.Add("UserName", "Muhammad");
        return report;
    }

    /// <summary>
    /// Simple sample report without database connection, sending the DataSet with int/string/byte[] fields.
    /// </summary>
    private static Report CreateReportDataSet()
    {
        var report = new Report("SampleReportDataset.rpt", "Sample Report Dataset");
        report.Parameters.Add("ReportFrom", new DateTime(2022, 01, 01));
        report.Parameters.Add("UserName", "Gerardo");

        // Create dataset
        var sampleReportDataset = new System.Data.DataSet();

        // Create table
        var personsTable = new System.Data.DataTable("Persons");
        sampleReportDataset.Tables.Add(personsTable);
        personsTable.Columns.Add("Id", typeof(int));
        personsTable.Columns.Add("Name", typeof(string));
        personsTable.Columns.Add("Age", typeof(int));
        personsTable.Columns.Add("PersonImage", typeof(byte[]));

        // Add rows
        personsTable.Rows.Add(1, "Gerardo", "42", System.IO.File.ReadAllBytes("sampleImage1.jpg"));
        personsTable.Rows.Add(2, "Khalifa", "24", System.IO.File.ReadAllBytes("sampleImage2.jpg"));

        report.DataSets.Add(sampleReportDataset);

        return report;
    }

    private CrystalReportsEngine CreateEngine()
    {
        // NOTE: Create CrystalReportsSample using Schema.sql in the \samples\shared folder
        var engine = new CrystalReportsEngine();

        // Method 2: Without Connection string
        // using var engine = new CrystalReportsEngine();

        // ========== Customizing Viewer Settings (optional) ===========

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

        engine.ViewerSettings.ShowReportTabs = false;
        engine.ViewerSettings.ShowRefreshButton = false;
        engine.ViewerSettings.ShowCopyButton = false;
        engine.ViewerSettings.ShowGroupTreeButton = false;
        engine.ViewerSettings.ShowParameterPanelButton = false;
        engine.ViewerSettings.EnableDrillDown = false;
        engine.ViewerSettings.ToolPanelView = ReportViewerToolPanelViewType.None;
        engine.ViewerSettings.ShowCloseButton = false;
        engine.ViewerSettings.EnableRefresh = false;
        engine.ViewerSettings.WindowInitialState = ReportViewerWindowState.Normal;

        // Set viewer Icon
        engine.ViewerSettings.WindowIcon = System.IO.File.ReadAllBytes("SampleIcon.png");

        // Optional we can also set culture for Crystal Reports Viewer UI to match the one used in your application
        //engine.ViewerSettings.SetUICulture(Thread.CurrentThread.CurrentUICulture);
        //engine.ViewerSettings.SetUICulture(System.Globalization.CultureInfo.GetCultureInfo("es-ES"));

        engine.FormClosed += Engine_FormClosed;
        engine.FormLoaded += Engine_FormLoaded;

        return engine;
    }

    private void Engine_FormLoaded(object? sender, FormLoadedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            LoadingBorder.Visibility = Visibility.Collapsed;
        });

        Debug.WriteLine($"Form Loaded for {e.ReportFileName}. Handle: {e.WindowHandle.Handle}.");
    }

    private void Engine_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _lastLocation = e.WindowLocation;
        Debug.WriteLine($"Form Closed for {e.ReportFileName}. Location: ({e.WindowLocation.Left}, {e.WindowLocation.Top}). Size: {e.WindowLocation.Width} x {e.WindowLocation.Height}.");
    }

    private async void ShowDataSetButton_Click(object sender, RoutedEventArgs e)
    {
        LoadingBorder.Visibility = Visibility.Visible;

        try
        {
            EnsureEngineAvailable();
            if (_engineInstance is null) throw new InvalidProgramException($"{nameof(_engineInstance)} can't be null here after calling EnsureEngineAvailable.");

            ConfigureWindowLocationAndSize();

            // Show
            var report = CreateReportDataSet();
            var windowHandle = new WindowHandle(new WindowInteropHelper(this).EnsureHandle());
            await _engineInstance.ShowReport(report, owner: windowHandle);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString());
        }
        finally
        {
            LoadingBorder.Visibility = Visibility.Collapsed;
        }
    }

    private async void PrintReportButton_Click(object sender, RoutedEventArgs e)
    {
        LoadingBorder.Visibility = Visibility.Visible;

        try
        {
            EnsureEngineAvailable();
            if (_engineInstance is null) throw new InvalidProgramException($"{nameof(_engineInstance)} can't be null here after calling EnsureEngineAvailable.");

            // Export
            var report = CreateReport();
            await _engineInstance.Print(report);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString());
        }
        finally
        {
            LoadingBorder.Visibility = Visibility.Collapsed;
        }
    }
}
