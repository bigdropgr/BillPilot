using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BillPilot
{
    // ===================== UPCOMING PAYMENTS CONTROL =====================
    public partial class UpcomingPaymentsControl : UserControl
    {
        private DatabaseManager dbManager;
        private PaymentManager paymentManager;
        private DataGridView upcomingGrid;
        private DateTimePicker fromDatePicker, toDatePicker;
        private Button refreshButton, editPaymentButton;
        private Label totalLabel;

        public UpcomingPaymentsControl(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
            this.paymentManager = new PaymentManager(dbManager);
            InitializeComponent();
            LoadUpcomingPayments();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(248, 249, 250);

            // Date range controls
            var fromLabel = new Label();
            fromLabel.Text = LocalizationManager.GetString("from_date") + ":";
            fromLabel.Location = new Point(20, 20);
            fromLabel.Size = new Size(80, 20);

            fromDatePicker = new DateTimePicker();
            fromDatePicker.Location = new Point(110, 18);
            fromDatePicker.Size = new Size(200, 20);
            fromDatePicker.Value = DateTime.Now.Date;

            var toLabel = new Label();
            toLabel.Text = LocalizationManager.GetString("to_date") + ":";
            toLabel.Location = new Point(330, 20);
            toLabel.Size = new Size(80, 20);

            toDatePicker = new DateTimePicker();
            toDatePicker.Location = new Point(420, 18);
            toDatePicker.Size = new Size(200, 20);
            toDatePicker.Value = DateTime.Now.AddDays(30).Date;

            refreshButton = new Button();
            refreshButton.Text = LocalizationManager.GetString("refresh");
            refreshButton.Location = new Point(640, 15);
            refreshButton.Size = new Size(100, 30);
            refreshButton.UseVisualStyleBackColor = true;
            refreshButton.Click += (s, e) => LoadUpcomingPayments();

            editPaymentButton = new Button();
            editPaymentButton.Text = LocalizationManager.GetString("edit_payment");
            editPaymentButton.Location = new Point(750, 15);
            editPaymentButton.Size = new Size(120, 30);
            editPaymentButton.UseVisualStyleBackColor = true;
            editPaymentButton.Click += EditPaymentButton_Click;

            totalLabel = new Label();
            totalLabel.Text = LocalizationManager.GetString("total") + ": €0.00";
            totalLabel.Location = new Point(890, 20);
            totalLabel.Size = new Size(200, 20);
            totalLabel.Font = new Font("Arial", 11, FontStyle.Bold);
            totalLabel.ForeColor = Color.DarkGreen;

            // DataGridView
            upcomingGrid = new DataGridView();
            upcomingGrid.Location = new Point(20, 60);
            upcomingGrid.Size = new Size(this.Width - 40, this.Height - 80);
            upcomingGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            upcomingGrid.AutoGenerateColumns = false;
            upcomingGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            upcomingGrid.MultiSelect = false;
            upcomingGrid.ReadOnly = true;
            upcomingGrid.AllowUserToAddRows = false;
            upcomingGrid.AllowUserToResizeRows = false;
            upcomingGrid.RowHeadersVisible = false;
            upcomingGrid.BackgroundColor = Color.White;
            upcomingGrid.BorderStyle = BorderStyle.Fixed3D;

            // Add columns
            upcomingGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "ID", Visible = false });
            upcomingGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = LocalizationManager.GetString("client"), Width = 200 });
            upcomingGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ServiceName", HeaderText = LocalizationManager.GetString("service_name"), Width = 200 });
            upcomingGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Amount",
                HeaderText = LocalizationManager.GetString("amount"),
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });
            upcomingGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DueDate",
                HeaderText = LocalizationManager.GetString("due_date"),
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" }
            });
            upcomingGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PaymentType", HeaderText = LocalizationManager.GetString("service_type"), Width = 100 });

            // Style the grid
            upcomingGrid.EnableHeadersVisualStyles = false;
            upcomingGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 173, 78);
            upcomingGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            upcomingGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Bold);
            upcomingGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);

            this.Controls.AddRange(new Control[] {
                fromLabel, fromDatePicker, toLabel, toDatePicker, refreshButton, editPaymentButton, totalLabel, upcomingGrid
            });
        }

        private void LoadUpcomingPayments()
        {
            try
            {
                var upcomingPayments = paymentManager.GetUpcomingPayments(fromDatePicker.Value, toDatePicker.Value);
                upcomingGrid.DataSource = upcomingPayments;

                // Calculate and display total
                decimal total = upcomingPayments.Sum(p => p.Amount);
                totalLabel.Text = LocalizationManager.GetString("total") + $": €{total:F2}";
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error loading upcoming payments", ex);
                MessageBox.Show("Error loading upcoming payments. Please check the log for details.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditPaymentButton_Click(object sender, EventArgs e)
        {
            if (upcomingGrid.SelectedRows.Count > 0)
            {
                var selectedPayment = (Payment)upcomingGrid.SelectedRows[0].DataBoundItem;
                var editForm = new EditPaymentForm(dbManager, selectedPayment);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    LoadUpcomingPayments();
                }
            }
            else
            {
                MessageBox.Show(LocalizationManager.GetString("no_selection"),
                    LocalizationManager.GetString("warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    // ===================== DELAYED PAYMENTS CONTROL =====================
    public partial class DelayedPaymentsControl : UserControl
    {
        private DatabaseManager dbManager;
        private PaymentManager paymentManager;
        private DataGridView delayedGrid;
        private Button refreshButton, markPaidButton;
        private Label totalLabel;

        public DelayedPaymentsControl(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
            this.paymentManager = new PaymentManager(dbManager);
            InitializeComponent();
            LoadDelayedPayments();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(248, 249, 250);

            // Controls
            refreshButton = new Button();
            refreshButton.Text = LocalizationManager.GetString("refresh");
            refreshButton.Location = new Point(20, 15);
            refreshButton.Size = new Size(100, 30);
            refreshButton.UseVisualStyleBackColor = true;
            refreshButton.Click += (s, e) => LoadDelayedPayments();

            markPaidButton = new Button();
            markPaidButton.Text = LocalizationManager.GetString("mark_as_paid");
            markPaidButton.Location = new Point(130, 15);
            markPaidButton.Size = new Size(120, 30);
            markPaidButton.UseVisualStyleBackColor = true;
            markPaidButton.Click += MarkPaidButton_Click;

            totalLabel = new Label();
            totalLabel.Text = LocalizationManager.GetString("total_outstanding") + ": €0.00";
            totalLabel.Location = new Point(this.Width - 300, 20);
            totalLabel.Size = new Size(250, 20);
            totalLabel.Font = new Font("Arial", 12, FontStyle.Bold);
            totalLabel.ForeColor = Color.Red;
            totalLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // DataGridView
            delayedGrid = new DataGridView();
            delayedGrid.Location = new Point(20, 60);
            delayedGrid.Size = new Size(this.Width - 40, this.Height - 80);
            delayedGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            delayedGrid.AutoGenerateColumns = false;
            delayedGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            delayedGrid.MultiSelect = false;
            delayedGrid.ReadOnly = true;
            delayedGrid.AllowUserToAddRows = false;
            delayedGrid.AllowUserToResizeRows = false;
            delayedGrid.RowHeadersVisible = false;
            delayedGrid.BackgroundColor = Color.White;
            delayedGrid.BorderStyle = BorderStyle.Fixed3D;

            // Add columns
            delayedGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "ID", Visible = false });
            delayedGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = LocalizationManager.GetString("client"), Width = 200 });
            delayedGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ServiceName", HeaderText = LocalizationManager.GetString("service_name"), Width = 200 });
            delayedGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Amount",
                HeaderText = LocalizationManager.GetString("amount"),
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });
            delayedGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "DueDate",
                HeaderText = LocalizationManager.GetString("due_date"),
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" }
            });
            delayedGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Notes", HeaderText = "Contact Info", Width = 250 });

            // Style the grid
            delayedGrid.EnableHeadersVisualStyles = false;
            delayedGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(217, 83, 79);
            delayedGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            delayedGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Bold);
            delayedGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);

            // Color code based on days overdue
            delayedGrid.CellFormatting += DelayedGrid_CellFormatting;

            this.Controls.AddRange(new Control[] {
                refreshButton, markPaidButton, totalLabel, delayedGrid
            });
        }

        private void DelayedGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && delayedGrid.Columns[e.ColumnIndex].Name == "DueDate")
            {
                var payment = (Payment)delayedGrid.Rows[e.RowIndex].DataBoundItem;
                var daysOverdue = (DateTime.Now.Date - payment.DueDate).Days;
                
                if (daysOverdue > 60)
                {
                    e.CellStyle.BackColor = Color.FromArgb(255, 220, 220);
                    e.CellStyle.ForeColor = Color.DarkRed;
                }
                else if (daysOverdue > 30)
                {
                    e.CellStyle.BackColor = Color.FromArgb(255, 240, 220);
                    e.CellStyle.ForeColor = Color.DarkOrange;
                }
            }
        }

        private void LoadDelayedPayments()
        {
            try
            {
                var delayedPayments = paymentManager.GetOverduePayments();
                delayedGrid.DataSource = delayedPayments;

                // Calculate total
                decimal total = delayedPayments.Sum(p => p.Amount);
                totalLabel.Text = LocalizationManager.GetString("total_outstanding") + $": €{total:F2}";
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error loading delayed payments", ex);
                MessageBox.Show("Error loading delayed payments. Please check the log for details.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MarkPaidButton_Click(object sender, EventArgs e)
        {
            if (delayedGrid.SelectedRows.Count > 0)
            {
                var selectedPayment = (Payment)delayedGrid.SelectedRows[0].DataBoundItem;
                var markPaidForm = new MarkPaymentPaidForm(dbManager, selectedPayment);

                if (markPaidForm.ShowDialog() == DialogResult.OK)
                {
                    LoadDelayedPayments();
                }
            }
            else
            {
                MessageBox.Show(LocalizationManager.GetString("no_selection"),
                    LocalizationManager.GetString("warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    // ===================== REPORTS CONTROL =====================
    public partial class ReportsControl : UserControl
    {
        private DatabaseManager dbManager;
        private TabControl reportsTabControl;
        private TabPage revenueTab, outstandingTab, performanceTab, profitabilityTab;

        public ReportsControl(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            reportsTabControl = new TabControl();
            reportsTabControl.Dock = DockStyle.Fill;

            // Revenue Report Tab
            revenueTab = new TabPage(LocalizationManager.GetString("revenue_report"));
            CreateRevenueReport();

            // Outstanding Report Tab
            outstandingTab = new TabPage(LocalizationManager.GetString("outstanding_report"));
            CreateOutstandingReport();

            // Service Performance Tab
            performanceTab = new TabPage(LocalizationManager.GetString("service_performance"));
            CreatePerformanceReport();

            // Client Profitability Tab
            profitabilityTab = new TabPage(LocalizationManager.GetString("client_profitability"));
            CreateProfitabilityReport();

            reportsTabControl.TabPages.AddRange(new TabPage[] {
                revenueTab, outstandingTab, performanceTab, profitabilityTab
            });

            this.Controls.Add(reportsTabControl);
        }

        private void CreateRevenueReport()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.FromArgb(248, 249, 250);

            // Date range controls
            var fromLabel = new Label();
            fromLabel.Text = LocalizationManager.GetString("from_date") + ":";
            fromLabel.Location = new Point(20, 20);
            fromLabel.Size = new Size(80, 20);

            var fromDatePicker = new DateTimePicker();
            fromDatePicker.Location = new Point(110, 18);
            fromDatePicker.Size = new Size(200, 20);
            fromDatePicker.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            var toLabel = new Label();
            toLabel.Text = LocalizationManager.GetString("to_date") + ":";
            toLabel.Location = new Point(330, 20);
            toLabel.Size = new Size(80, 20);

            var toDatePicker = new DateTimePicker();
            toDatePicker.Location = new Point(420, 18);
            toDatePicker.Size = new Size(200, 20);

            var generateButton = new Button();
            generateButton.Text = LocalizationManager.GetString("generate_report");
            generateButton.Location = new Point(640, 15);
            generateButton.Size = new Size(120, 30);
            generateButton.UseVisualStyleBackColor = true;

            var exportButton = new Button();
            exportButton.Text = LocalizationManager.GetString("export_to_excel");
            exportButton.Location = new Point(770, 15);
            exportButton.Size = new Size(120, 30);
            exportButton.UseVisualStyleBackColor = true;

            // Results grid
            var resultsGrid = new DataGridView();
            resultsGrid.Location = new Point(20, 60);
            resultsGrid.Size = new Size(panel.Width - 40, panel.Height - 180);
            resultsGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            resultsGrid.ReadOnly = true;
            resultsGrid.AllowUserToAddRows = false;
            resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            resultsGrid.BackgroundColor = Color.White;

            // Summary panel
            var summaryPanel = new Panel();
            summaryPanel.Location = new Point(20, panel.Height - 100);
            summaryPanel.Size = new Size(panel.Width - 40, 80);
            summaryPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            summaryPanel.BackColor = Color.White;
            summaryPanel.BorderStyle = BorderStyle.FixedSingle;

            var totalRevenueLabel = new Label();
            totalRevenueLabel.Text = LocalizationManager.GetString("total_revenue") + ": €0.00";
            totalRevenueLabel.Location = new Point(20, 20);
            totalRevenueLabel.Size = new Size(300, 30);
            totalRevenueLabel.Font = new Font("Arial", 14, FontStyle.Bold);

            summaryPanel.Controls.Add(totalRevenueLabel);

            generateButton.Click += (s, e) => {
                try
                {
                    var reportManager = new ReportManager(dbManager);
                    var revenueData = reportManager.GetRevenueReport(fromDatePicker.Value, toDatePicker.Value);
                    resultsGrid.DataSource = revenueData;

                    decimal total = revenueData.Sum(r => r.Amount);
                    totalRevenueLabel.Text = LocalizationManager.GetString("total_revenue") + $": €{total:F2}";
                }
                catch (Exception ex)
                {
                    LogManager.LogError("Error generating revenue report", ex);
                    MessageBox.Show("Error generating report. Please check the log for details.",
                        LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            exportButton.Click += (s, e) => {
                ExportGridToExcel(resultsGrid, "Revenue_Report");
            };

            panel.Controls.AddRange(new Control[] {
                fromLabel, fromDatePicker, toLabel, toDatePicker, generateButton, exportButton,
                resultsGrid, summaryPanel
            });

            revenueTab.Controls.Add(panel);
        }

        private void CreateOutstandingReport()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.FromArgb(248, 249, 250);

            var generateButton = new Button();
            generateButton.Text = LocalizationManager.GetString("generate_report");
            generateButton.Location = new Point(20, 15);
            generateButton.Size = new Size(120, 30);
            generateButton.UseVisualStyleBackColor = true;

            var exportButton = new Button();
            exportButton.Text = LocalizationManager.GetString("export_to_excel");
            exportButton.Location = new Point(150, 15);
            exportButton.Size = new Size(120, 30);
            exportButton.UseVisualStyleBackColor = true;

            // Results grid
            var resultsGrid = new DataGridView();
            resultsGrid.Location = new Point(20, 60);
            resultsGrid.Size = new Size(panel.Width - 40, panel.Height - 80);
            resultsGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            resultsGrid.ReadOnly = true;
            resultsGrid.AllowUserToAddRows = false;
            resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            resultsGrid.BackgroundColor = Color.White;

            generateButton.Click += (s, e) => {
                try
                {
                    var reportManager = new ReportManager(dbManager);
                    var outstandingData = reportManager.GetOutstandingReport();
                    resultsGrid.DataSource = outstandingData;
                }
                catch (Exception ex)
                {
                    LogManager.LogError("Error generating outstanding report", ex);
                    MessageBox.Show("Error generating report. Please check the log for details.",
                        LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            exportButton.Click += (s, e) => {
                ExportGridToExcel(resultsGrid, "Outstanding_Report");
            };

            panel.Controls.AddRange(new Control[] {
                generateButton, exportButton, resultsGrid
            });

            outstandingTab.Controls.Add(panel);
        }

        private void CreatePerformanceReport()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.FromArgb(248, 249, 250);

            var generateButton = new Button();
            generateButton.Text = LocalizationManager.GetString("generate_report");
            generateButton.Location = new Point(20, 15);
            generateButton.Size = new Size(120, 30);
            generateButton.UseVisualStyleBackColor = true;

            var exportButton = new Button();
            exportButton.Text = LocalizationManager.GetString("export_to_excel");
            exportButton.Location = new Point(150, 15);
            exportButton.Size = new Size(120, 30);
            exportButton.UseVisualStyleBackColor = true;

            // Results grid
            var resultsGrid = new DataGridView();
            resultsGrid.Location = new Point(20, 60);
            resultsGrid.Size = new Size(panel.Width - 40, panel.Height - 80);
            resultsGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            resultsGrid.ReadOnly = true;
            resultsGrid.AllowUserToAddRows = false;
            resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            resultsGrid.BackgroundColor = Color.White;

            generateButton.Click += (s, e) => {
                try
                {
                    var reportManager = new ReportManager(dbManager);
                    var performanceData = reportManager.GetServicePerformanceReport();
                    resultsGrid.DataSource = performanceData;
                }
                catch (Exception ex)
                {
                    LogManager.LogError("Error generating performance report", ex);
                    MessageBox.Show("Error generating report. Please check the log for details.",
                        LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            exportButton.Click += (s, e) => {
                ExportGridToExcel(resultsGrid, "Service_Performance_Report");
            };

            panel.Controls.AddRange(new Control[] {
                generateButton, exportButton, resultsGrid
            });

            performanceTab.Controls.Add(panel);
        }

        private void CreateProfitabilityReport()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.FromArgb(248, 249, 250);

            var generateButton = new Button();
            generateButton.Text = LocalizationManager.GetString("generate_report");
            generateButton.Location = new Point(20, 15);
            generateButton.Size = new Size(120, 30);
            generateButton.UseVisualStyleBackColor = true;

            var exportButton = new Button();
            exportButton.Text = LocalizationManager.GetString("export_to_excel");
            exportButton.Location = new Point(150, 15);
            exportButton.Size = new Size(120, 30);
            exportButton.UseVisualStyleBackColor = true;

            // Results grid
            var resultsGrid = new DataGridView();
            resultsGrid.Location = new Point(20, 60);
            resultsGrid.Size = new Size(panel.Width - 40, panel.Height - 80);
            resultsGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            resultsGrid.ReadOnly = true;
            resultsGrid.AllowUserToAddRows = false;
            resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            resultsGrid.BackgroundColor = Color.White;

            generateButton.Click += (s, e) => {
                try
                {
                    var reportManager = new ReportManager(dbManager);
                    var profitabilityData = reportManager.GetClientProfitabilityReport();
                    resultsGrid.DataSource = profitabilityData;
                }
                catch (Exception ex)
                {
                    LogManager.LogError("Error generating profitability report", ex);
                    MessageBox.Show("Error generating report. Please check the log for details.",
                        LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            exportButton.Click += (s, e) => {
                ExportGridToExcel(resultsGrid, "Client_Profitability_Report");
            };

            panel.Controls.AddRange(new Control[] {
                generateButton, exportButton, resultsGrid
            });

            profitabilityTab.Controls.Add(panel);
        }

        private void ExportGridToExcel(DataGridView grid, string fileName)
        {
            try
            {
                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                    saveDialog.DefaultExt = "csv";
                    saveDialog.FileName = $"{fileName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        var csv = new StringBuilder();

                        // Headers
                        var headers = grid.Columns.Cast<DataGridViewColumn>()
                            .Where(c => c.Visible)
                            .Select(c => c.HeaderText);
                        csv.AppendLine(string.Join(",", headers));

                        // Data
                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            var cells = row.Cells.Cast<DataGridViewCell>()
                                .Where((c, i) => grid.Columns[i].Visible)
                                .Select(c => c.Value?.ToString()?.Replace(",", ";") ?? "");
                            csv.AppendLine(string.Join(",", cells));
                        }

                        File.WriteAllText(saveDialog.FileName, csv.ToString());

                        MessageBox.Show(LocalizationManager.GetString("export_success"),
                            LocalizationManager.GetString("success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error exporting to Excel", ex);
                MessageBox.Show("Error exporting data. Please check the log for details.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ===================== CONTACT HISTORY FORM =====================
    public partial class ContactHistoryForm : Form
    {
        private DatabaseManager dbManager;
        private int clientId;
        private DataGridView historyGrid;
        private Button addButton, closeButton;

        public ContactHistoryForm(DatabaseManager dbManager, int clientId)
        {
            this.dbManager = dbManager;
            this.clientId = clientId;
            InitializeComponent();
            LoadHistory();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = LocalizationManager.GetString("contact_history");

            // Buttons
            addButton = new Button();
            addButton.Text = "Add Contact";
            addButton.Location = new Point(20, 15);
            addButton.Size = new Size(100, 30);
            addButton.UseVisualStyleBackColor = true;
            addButton.Click += AddButton_Click;

            closeButton = new Button();
            closeButton.Text = LocalizationManager.GetString("close");
            closeButton.Location = new Point(680, 15);
            closeButton.Size = new Size(80, 30);
            closeButton.UseVisualStyleBackColor = true;
            closeButton.Click += (s, e) => this.Close();

            // DataGridView
            historyGrid = new DataGridView();
            historyGrid.Location = new Point(20, 60);
            historyGrid.Size = new Size(760, 380);
            historyGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            historyGrid.AutoGenerateColumns = false;
            historyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            historyGrid.MultiSelect = false;
            historyGrid.ReadOnly = true;
            historyGrid.AllowUserToAddRows = false;
            historyGrid.AllowUserToResizeRows = false;
            historyGrid.RowHeadersVisible = false;
            historyGrid.BackgroundColor = Color.White;

            // Add columns
            historyGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ContactDate",
                HeaderText = LocalizationManager.GetString("date"),
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
            });
            historyGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ContactType",
                HeaderText = LocalizationManager.GetString("contact_type"),
                Width = 100
            });
            historyGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Notes",
                HeaderText = LocalizationManager.GetString("notes"),
                Width = 400
            });
            historyGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CreatedBy",
                HeaderText = "Created By",
                Width = 120
            });

            this.Controls.AddRange(new Control[] {
                addButton, closeButton, historyGrid
            });
        }

        private void LoadHistory()
        {
            try
            {
                var contacts = new ContactHistoryManager(dbManager).GetClientContacts(clientId);
                historyGrid.DataSource = contacts;
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error loading contact history", ex);
                MessageBox.Show("Error loading contact history. Please check the log for details.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            var addForm = new AddContactForm(dbManager, clientId);
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadHistory();
            }
        }
    }

    // ===================== CLIENT SERVICES FORM =====================
    public partial class ClientServicesForm : Form
    {
        private DatabaseManager dbManager;
        private int clientId;
        private DataGridView servicesGrid;
        private Button addButton, editButton, deleteButton, closeButton;

        public ClientServicesForm(DatabaseManager dbManager, int clientId)
        {
            this.dbManager = dbManager;
            this.clientId = clientId;
            InitializeComponent();
            LoadClientServices();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = LocalizationManager.GetString("client_services");

            // Buttons
            addButton = new Button();
            addButton.Text = LocalizationManager.GetString("add_service");
            addButton.Location = new Point(20, 15);
            addButton.Size = new Size(100, 30);
            addButton.UseVisualStyleBackColor = true;
            addButton.Click += AddButton_Click;

            editButton = new Button();
            editButton.Text = LocalizationManager.GetString("edit");
            editButton.Location = new Point(130, 15);
            editButton.Size = new Size(100, 30);
            editButton.UseVisualStyleBackColor = true;
            editButton.Click += EditButton_Click;

            deleteButton = new Button();
            deleteButton.Text = LocalizationManager.GetString("delete");
            deleteButton.Location = new Point(240, 15);
            deleteButton.Size = new Size(100, 30);
            deleteButton.UseVisualStyleBackColor = true;
            deleteButton.Click += DeleteButton_Click;

            closeButton = new Button();
            closeButton.Text = LocalizationManager.GetString("close");
            closeButton.Location = new Point(780, 15);
            closeButton.Size = new Size(80, 30);
            closeButton.UseVisualStyleBackColor = true;
            closeButton.Click += (s, e) => this.Close();

            // DataGridView
            servicesGrid = new DataGridView();
            servicesGrid.Location = new Point(20, 60);
            servicesGrid.Size = new Size(860, 480);
            servicesGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            servicesGrid.AutoGenerateColumns = false;
            servicesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            servicesGrid.MultiSelect = false;
            servicesGrid.ReadOnly = true;
            servicesGrid.AllowUserToAddRows = false;
            servicesGrid.AllowUserToResizeRows = false;
            servicesGrid.RowHeadersVisible = false;
            servicesGrid.BackgroundColor = Color.White;

            // Add columns
            servicesGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "ID", Visible = false });
            servicesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ServiceName",
                HeaderText = LocalizationManager.GetString("service_name"),
                Width = 200
            });
            servicesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ServiceType",
                HeaderText = LocalizationManager.GetString("service_type"),
                Width = 100
            });
            servicesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Price",
                HeaderText = LocalizationManager.GetString("custom_price"),
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });
            servicesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Period",
                HeaderText = LocalizationManager.GetString("period"),
                Width = 100
            });
            servicesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ChargeDay",
                HeaderText = LocalizationManager.GetString("charge_day"),
                Width = 100
            });
            servicesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "NextPaymentDate",
                HeaderText = LocalizationManager.GetString("next_payment_date"),
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" }
            });
            servicesGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "IsActive",
                HeaderText = LocalizationManager.GetString("active"),
                Width = 60
            });

            this.Controls.AddRange(new Control[] {
                addButton, editButton, deleteButton, closeButton, servicesGrid
            });
        }

        private void LoadClientServices()
        {
            try
            {
                var clientServices = new ClientServiceManager(dbManager).GetClientServices(clientId);
                servicesGrid.DataSource = clientServices;
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error loading client services", ex);
                MessageBox.Show("Error loading client services. Please check the log for details.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            var addForm = new ClientServiceEditForm(dbManager, clientId, null);
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadClientServices();
            }
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            if (servicesGrid.SelectedRows.Count > 0)
            {
                var selectedService = (ClientService)servicesGrid.SelectedRows[0].DataBoundItem;
                var editForm = new ClientServiceEditForm(dbManager, clientId, selectedService);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    LoadClientServices();
                }
            }
            else
            {
                MessageBox.Show(LocalizationManager.GetString("no_selection"),
                    LocalizationManager.GetString("warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (servicesGrid.SelectedRows.Count > 0)
            {
                var result = MessageBox.Show(LocalizationManager.GetString("confirm_delete"),
                    LocalizationManager.GetString("warning"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        var selectedService = (ClientService)servicesGrid.SelectedRows[0].DataBoundItem;
                        new ClientServiceManager(dbManager).DeleteClientService(selectedService.Id);
                        LoadClientServices();
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError("Error deleting client service", ex);
                        MessageBox.Show(ex.Message, LocalizationManager.GetString("error"),
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show(LocalizationManager.GetString("no_selection"),
                    LocalizationManager.GetString("warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    // ===================== ADD CONTACT FORM =====================
    public partial class AddContactForm : Form
    {
        private DatabaseManager dbManager;
        private int clientId;
        private ComboBox cboContactType;
        private TextBox txtNotes;
        private Button btnSave, btnCancel;

        public AddContactForm(DatabaseManager dbManager, int clientId)
        {
            this.dbManager = dbManager;
            this.clientId = clientId;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(500, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Text = "Add Contact";

            // Contact Type
            var lblType = new Label();
            lblType.Text = LocalizationManager.GetString("contact_type") + ":";
            lblType.Location = new Point(20, 20);
            lblType.Size = new Size(100, 20);

            cboContactType = new ComboBox();
            cboContactType.Location = new Point(130, 18);
            cboContactType.Size = new Size(330, 20);
            cboContactType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboContactType.Items.AddRange(new string[] { "Phone Call", "Email", "Meeting", "SMS", "Other" });
            cboContactType.SelectedIndex = 0;

            // Notes
            var lblNotes = new Label();
            lblNotes.Text = LocalizationManager.GetString("notes") + ":";
            lblNotes.Location = new Point(20, 60);
            lblNotes.Size = new Size(100, 20);

            txtNotes = new TextBox();
            txtNotes.Location = new Point(130, 60);
            txtNotes.Size = new Size(330, 120);
            txtNotes.Multiline = true;
            txtNotes.ScrollBars = ScrollBars.Vertical;

            // Buttons
            btnSave = new Button();
            btnSave.Text = LocalizationManager.GetString("save");
            btnSave.Location = new Point(300, 200);
            btnSave.Size = new Size(80, 30);
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button();
            btnCancel.Text = LocalizationManager.GetString("cancel");
            btnCancel.Location = new Point(390, 200);
            btnCancel.Size = new Size(80, 30);
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] {
                lblType, cboContactType, lblNotes, txtNotes, btnSave, btnCancel
            });
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNotes.Text))
            {
                MessageBox.Show("Please enter contact notes.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtNotes.Focus();
                return;
            }

            var contact = new ContactHistory
            {
                ClientId = clientId,
                ContactType = cboContactType.Text,
                Notes = txtNotes.Text.Trim(),
                ContactDate = DateTime.Now,
                CreatedBy = SessionManager.CurrentUser
            };

            try
            {
                new ContactHistoryManager(dbManager).AddContact(contact);
                LogManager.LogInfo($"Contact history added for client ID: {clientId}");
                MessageBox.Show("Contact history added successfully.",
                    LocalizationManager.GetString("success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error adding contact history", ex);
                MessageBox.Show(ex.Message, LocalizationManager.GetString("error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ===================== CLIENT SERVICE EDIT FORM =====================
    public partial class ClientServiceEditForm : Form
    {
        private DatabaseManager dbManager;
        private int clientId;
        private ClientService clientService;
        private bool isEdit;

        private ComboBox cboService, cboServiceType, cboPeriod;
        private NumericUpDown numPrice, numChargeDay;
        private DateTimePicker dtpStartDate;
        private CheckBox chkActive;
        private Button btnSave, btnCancel;

        public ClientServiceEditForm(DatabaseManager dbManager, int clientId, ClientService clientService)
        {
            this.dbManager = dbManager;
            this.clientId = clientId;
            this.clientService = clientService;
            this.isEdit = clientService != null;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Text = isEdit ? "Edit Client Service" : "Add Client Service";

            int yPos = 20;

            // Service
            var lblService = new Label();
            lblService.Text = LocalizationManager.GetString("service_name") + ":";
            lblService.Location = new Point(20, yPos);
            lblService.Size = new Size(120, 20);

            cboService = new ComboBox();
            cboService.Location = new Point(150, yPos);
            cboService.Size = new Size(310, 20);
            cboService.DropDownStyle = ComboBoxStyle.DropDownList;
            cboService.SelectedIndexChanged += CboService_SelectedIndexChanged;
            yPos += 35;

            // Service Type
            var lblServiceType = new Label();
            lblServiceType.Text = LocalizationManager.GetString("service_type") + ":";
            lblServiceType.Location = new Point(20, yPos);
            lblServiceType.Size = new Size(120, 20);

            cboServiceType = new ComboBox();
            cboServiceType.Location = new Point(150, yPos);
            cboServiceType.Size = new Size(200, 20);
            cboServiceType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboServiceType.Items.AddRange(new string[] {
                LocalizationManager.GetString("one_off"),
                LocalizationManager.GetString("periodic")
            });
            cboServiceType.SelectedIndexChanged += CboServiceType_SelectedIndexChanged;
            yPos += 35;

            // Price
            var lblPrice = new Label();
            lblPrice.Text = LocalizationManager.GetString("custom_price") + ":";
            lblPrice.Location = new Point(20, yPos);
            lblPrice.Size = new Size(120, 20);

            numPrice = new NumericUpDown();
            numPrice.Location = new Point(150, yPos);
            numPrice.Size = new Size(120, 20);
            numPrice.Maximum = 999999;
            numPrice.DecimalPlaces = 2;
            numPrice.ThousandsSeparator = true;
            yPos += 35;

            // Period (for periodic services)
            var lblPeriod = new Label();
            lblPeriod.Text = LocalizationManager.GetString("period") + ":";
            lblPeriod.Location = new Point(20, yPos);
            lblPeriod.Size = new Size(120, 20);

            cboPeriod = new ComboBox();
            cboPeriod.Location = new Point(150, yPos);
            cboPeriod.Size = new Size(200, 20);
            cboPeriod.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPeriod.Items.AddRange(new string[] {
                LocalizationManager.GetString("weekly"),
                LocalizationManager.GetString("monthly"),
                LocalizationManager.GetString("quarterly"),
                LocalizationManager.GetString("yearly")
            });
            yPos += 35;

            // Charge Day
            var lblChargeDay = new Label();
            lblChargeDay.Text = LocalizationManager.GetString("charge_day") + ":";
            lblChargeDay.Location = new Point(20, yPos);
            lblChargeDay.Size = new Size(120, 20);

            numChargeDay = new NumericUpDown();
            numChargeDay.Location = new Point(150, yPos);
            numChargeDay.Size = new Size(60, 20);
            numChargeDay.Minimum = 1;
            numChargeDay.Maximum = 31;
            numChargeDay.Value = 1;

            var lblChargeDayHelp = new Label();
            lblChargeDayHelp.Text = "(Day of month for monthly, Day of year for yearly)";
            lblChargeDayHelp.Location = new Point(220, yPos);
            lblChargeDayHelp.Size = new Size(240, 20);
            lblChargeDayHelp.Font = new Font("Arial", 8);
            yPos += 35;

            // Start Date
            var lblStartDate = new Label();
            lblStartDate.Text = LocalizationManager.GetString("start_date") + ":";
            lblStartDate.Location = new Point(20, yPos);
            lblStartDate.Size = new Size(120, 20);

            dtpStartDate = new DateTimePicker();
            dtpStartDate.Location = new Point(150, yPos);
            dtpStartDate.Size = new Size(200, 20);
            yPos += 35;

            // Active
            chkActive = new CheckBox();
            chkActive.Text = LocalizationManager.GetString("active");
            chkActive.Location = new Point(150, yPos);
            chkActive.Size = new Size(100, 20);
            chkActive.Checked = true;
            yPos += 50;

            // Buttons
            btnSave = new Button();
            btnSave.Text = LocalizationManager.GetString("save");
            btnSave.Location = new Point(300, yPos);
            btnSave.Size = new Size(80, 30);
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button();
            btnCancel.Text = LocalizationManager.GetString("cancel");
            btnCancel.Location = new Point(390, yPos);
            btnCancel.Size = new Size(80, 30);
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] {
                lblService, cboService, lblServiceType, cboServiceType,
                lblPrice, numPrice, lblPeriod, cboPeriod,
                lblChargeDay, numChargeDay, lblChargeDayHelp,
                lblStartDate, dtpStartDate, chkActive,
                btnSave, btnCancel
            });
        }

        private void LoadData()
        {
            // Load services
            var services = new ServiceManager(dbManager).GetAllServices();
            cboService.DataSource = services;
            cboService.DisplayMember = "Name";
            cboService.ValueMember = "Id";

            if (isEdit && clientService != null)
            {
                // Load existing data
                cboService.SelectedValue = clientService.ServiceId;
                cboServiceType.Text = clientService.ServiceType;
                numPrice.Value = clientService.CustomPrice ?? clientService.Price;
                if (clientService.Period != null)
                    cboPeriod.Text = clientService.Period;
                if (clientService.ChargeDay.HasValue)
                    numChargeDay.Value = clientService.ChargeDay.Value;
                dtpStartDate.Value = clientService.StartDate;
                chkActive.Checked = clientService.IsActive;
            }
            else
            {
                cboServiceType.SelectedIndex = 0;
                cboPeriod.SelectedIndex = 1; // Monthly by default
            }
        }

        private void CboService_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboService.SelectedItem is Service service && !isEdit)
            {
                numPrice.Value = service.BasePrice;
            }
        }

        private void CboServiceType_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isPeriodic = cboServiceType.SelectedIndex == 1;
            cboPeriod.Enabled = isPeriodic;
            numChargeDay.Enabled = isPeriodic;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (cboService.SelectedItem == null)
            {
                MessageBox.Show("Please select a service.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var clientServiceToSave = new ClientService
                {
                    Id = isEdit ? clientService.Id : 0,
                    ClientId = clientId,
                    ServiceId = ((Service)cboService.SelectedItem).Id,
                    ServiceType = cboServiceType.Text,
                    CustomPrice = numPrice.Value,
                    Period = cboPeriod.Enabled ? cboPeriod.Text : null,
                    ChargeDay = numChargeDay.Enabled ? (int?)numChargeDay.Value : null,
                    StartDate = dtpStartDate.Value,
                    IsActive = chkActive.Checked
                };

                var clientServiceManager = new ClientServiceManager(dbManager);

                if (isEdit)
                {
                    clientServiceManager.UpdateClientService(clientServiceToSave);
                    LogManager.LogInfo($"Client service updated for client ID: {clientId}");
                }
                else
                {
                    clientServiceManager.CreateClientService(clientServiceToSave);
                    LogManager.LogInfo($"Client service created for client ID: {clientId}");
                }

                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error saving client service", ex);
                MessageBox.Show(ex.Message, LocalizationManager.GetString("error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ===================== EDIT PAYMENT FORM =====================
    public partial class EditPaymentForm : Form
    {
        private DatabaseManager dbManager;
        private Payment payment;
        private DateTimePicker dtpDueDate;
        private NumericUpDown numAmount;
        private TextBox txtNotes;
        private Button btnSave, btnCancel;

        public EditPaymentForm(DatabaseManager dbManager, Payment payment)
        {
            this.dbManager = dbManager;
            this.payment = payment;
            InitializeComponent();
            LoadPaymentData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Text = LocalizationManager.GetString("edit_payment");

            int yPos = 20;

            // Client Info (read-only)
            var lblClient = new Label();
            lblClient.Text = LocalizationManager.GetString("client") + ":";
            lblClient.Location = new Point(20, yPos);
            lblClient.Size = new Size(100, 20);

            var lblClientName = new Label();
            lblClientName.Text = payment.ClientName;
            lblClientName.Location = new Point(130, yPos);
            lblClientName.Size = new Size(230, 20);
            lblClientName.Font = new Font("Arial", 10, FontStyle.Bold);
            yPos += 30;

            // Service Info (read-only)
            var lblService = new Label();
            lblService.Text = LocalizationManager.GetString("service_name") + ":";
            lblService.Location = new Point(20, yPos);
            lblService.Size = new Size(100, 20);

            var lblServiceName = new Label();
            lblServiceName.Text = payment.ServiceName ?? "General Payment";
            lblServiceName.Location = new Point(130, yPos);
            lblServiceName.Size = new Size(230, 20);
            yPos += 30;

            // Due Date
            var lblDueDate = new Label();
            lblDueDate.Text = LocalizationManager.GetString("due_date") + ":";
            lblDueDate.Location = new Point(20, yPos);
            lblDueDate.Size = new Size(100, 20);

            dtpDueDate = new DateTimePicker();
            dtpDueDate.Location = new Point(130, yPos);
            dtpDueDate.Size = new Size(200, 20);
            yPos += 35;

            // Amount
            var lblAmount = new Label();
            lblAmount.Text = LocalizationManager.GetString("amount") + ":";
            lblAmount.Location = new Point(20, yPos);
            lblAmount.Size = new Size(100, 20);

            numAmount = new NumericUpDown();
            numAmount.Location = new Point(130, yPos);
            numAmount.Size = new Size(120, 20);
            numAmount.Maximum = 999999;
            numAmount.DecimalPlaces = 2;
            numAmount.ThousandsSeparator = true;
            yPos += 35;

            // Notes
            var lblNotes = new Label();
            lblNotes.Text = LocalizationManager.GetString("notes") + ":";
            lblNotes.Location = new Point(20, yPos);
            lblNotes.Size = new Size(100, 20);

            txtNotes = new TextBox();
            txtNotes.Location = new Point(130, yPos);
            txtNotes.Size = new Size(230, 60);
            txtNotes.Multiline = true;
            txtNotes.ScrollBars = ScrollBars.Vertical;
            yPos += 70;

            // Buttons
            btnSave = new Button();
            btnSave.Text = LocalizationManager.GetString("save");
            btnSave.Location = new Point(200, yPos);
            btnSave.Size = new Size(80, 30);
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button();
            btnCancel.Text = LocalizationManager.GetString("cancel");
            btnCancel.Location = new Point(290, yPos);
            btnCancel.Size = new Size(80, 30);
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] {
                lblClient, lblClientName, lblService, lblServiceName,
                lblDueDate, dtpDueDate, lblAmount, numAmount,
                lblNotes, txtNotes, btnSave, btnCancel
            });
        }

        private void LoadPaymentData()
        {
            dtpDueDate.Value = payment.DueDate;
            numAmount.Value = payment.Amount;
            txtNotes.Text = payment.Notes;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (numAmount.Value <= 0)
            {
                MessageBox.Show("Amount must be greater than 0.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Update payment in database
                using (var connection = dbManager.GetConnection())
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand(@"
                        UPDATE Payments 
                        SET DueDate = @dueDate,
                            Amount = @amount,
                            Notes = @notes
                        WHERE Id = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", payment.Id);
                        cmd.Parameters.AddWithValue("@dueDate", dtpDueDate.Value.Date);
                        cmd.Parameters.AddWithValue("@amount", numAmount.Value);
                        cmd.Parameters.AddWithValue("@notes", txtNotes.Text.Trim());
                        
                        cmd.ExecuteNonQuery();
                    }
                }

                LogManager.LogInfo($"Payment updated: ID {payment.Id}");
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error updating payment", ex);
                MessageBox.Show(ex.Message, LocalizationManager.GetString("error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ===================== MARK PAYMENT PAID FORM =====================
    public partial class MarkPaymentPaidForm : Form
    {
        private DatabaseManager dbManager;
        private Payment payment;
        private ComboBox cboPaymentMethod;
        private TextBox txtReference;
        private NumericUpDown numMonthsPaid;
        private Label lblMonthsInfo;
        private Button btnSave, btnCancel;

        public MarkPaymentPaidForm(DatabaseManager dbManager, Payment payment)
        {
            this.dbManager = dbManager;
            this.payment = payment;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(450, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;