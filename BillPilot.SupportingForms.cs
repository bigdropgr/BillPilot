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
        private ChargeManager chargeManager;
        private DataGridView upcomingGrid;
        private DateTimePicker fromDatePicker, toDatePicker;
        private Button refreshButton, processChargeButton;
        private Label totalLabel;

        public UpcomingPaymentsControl(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
            this.chargeManager = new ChargeManager(dbManager);
            InitializeComponent();
            LoadUpcomingCharges();
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
            refreshButton.Click += (s, e) => LoadUpcomingCharges();

            processChargeButton = new Button();
            processChargeButton.Text = LocalizationManager.GetString("process_charge");
            processChargeButton.Location = new Point(750, 15);
            processChargeButton.Size = new Size(120, 30);
            processChargeButton.UseVisualStyleBackColor = true;
            processChargeButton.Click += ProcessChargeButton_Click;

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
                DataPropertyName = "NextChargeDate",
                HeaderText = LocalizationManager.GetString("next_charge_date"),
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" }
            });
            upcomingGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Period", HeaderText = LocalizationManager.GetString("period"), Width = 100 });

            // Style the grid
            upcomingGrid.EnableHeadersVisualStyles = false;
            upcomingGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 173, 78);
            upcomingGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            upcomingGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Bold);
            upcomingGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);

            this.Controls.AddRange(new Control[] {
                fromLabel, fromDatePicker, toLabel, toDatePicker, refreshButton, processChargeButton, totalLabel, upcomingGrid
            });
        }

        private void LoadUpcomingCharges()
        {
            try
            {
                var upcomingCharges = chargeManager.GetUpcomingAutoCharges(fromDatePicker.Value, toDatePicker.Value);
                upcomingGrid.DataSource = upcomingCharges;

                // Calculate and display total
                decimal total = upcomingCharges.Sum(c => c.Amount);
                totalLabel.Text = LocalizationManager.GetString("total") + $": €{total:F2}";
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error loading upcoming charges", ex);
                MessageBox.Show("Error loading upcoming charges. Please check the log for details.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ProcessChargeButton_Click(object sender, EventArgs e)
        {
            if (upcomingGrid.SelectedRows.Count > 0)
            {
                var result = MessageBox.Show("Process selected auto-charge now?",
                    LocalizationManager.GetString("confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        var selectedCharge = (UpcomingCharge)upcomingGrid.SelectedRows[0].DataBoundItem;

                        // Process the charge
                        var clientServiceManager = new ClientServiceManager(dbManager);
                        clientServiceManager.ProcessAutoCharge(selectedCharge.Id);

                        MessageBox.Show("Charge processed successfully!",
                            LocalizationManager.GetString("success"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LoadUpcomingCharges();
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError("Error processing charge", ex);
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

    // ===================== DELAYED PAYMENTS CONTROL =====================
    public partial class DelayedPaymentsControl : UserControl
    {
        private DatabaseManager dbManager;
        private ChargeManager chargeManager;
        private DataGridView delayedGrid;
        private Button refreshButton, sendReminderButton, markPaidButton;
        private Label totalLabel;

        public DelayedPaymentsControl(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
            this.chargeManager = new ChargeManager(dbManager);
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

            sendReminderButton = new Button();
            sendReminderButton.Text = LocalizationManager.GetString("send_reminder");
            sendReminderButton.Location = new Point(130, 15);
            sendReminderButton.Size = new Size(120, 30);
            sendReminderButton.UseVisualStyleBackColor = true;
            sendReminderButton.Click += SendReminderButton_Click;

            markPaidButton = new Button();
            markPaidButton.Text = LocalizationManager.GetString("mark_as_paid");
            markPaidButton.Location = new Point(260, 15);
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
            delayedGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientId", HeaderText = "ID", Visible = false });
            delayedGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ClientName", HeaderText = LocalizationManager.GetString("client"), Width = 200 });
            delayedGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TotalDue",
                HeaderText = LocalizationManager.GetString("amount"),
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });
            delayedGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "OldestDueDate",
                HeaderText = LocalizationManager.GetString("due_date"),
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" }
            });
            delayedGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DaysOverdue", HeaderText = LocalizationManager.GetString("days_overdue"), Width = 100 });
            delayedGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Phone", HeaderText = LocalizationManager.GetString("phone"), Width = 120 });
            delayedGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Email", HeaderText = LocalizationManager.GetString("email"), Width = 200 });

            // Style the grid
            delayedGrid.EnableHeadersVisualStyles = false;
            delayedGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(217, 83, 79);
            delayedGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            delayedGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Bold);
            delayedGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);

            // Color code based on days overdue
            delayedGrid.CellFormatting += DelayedGrid_CellFormatting;

            this.Controls.AddRange(new Control[] {
                refreshButton, sendReminderButton, markPaidButton, totalLabel, delayedGrid
            });
        }

        private void DelayedGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && delayedGrid.Columns[e.ColumnIndex].Name == "DaysOverdue")
            {
                var daysOverdue = Convert.ToInt32(e.Value);
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
                var delayedPayments = chargeManager.GetOverdueCharges();
                delayedGrid.DataSource = delayedPayments;

                // Calculate total
                decimal total = delayedPayments.Sum(d => d.TotalDue);
                totalLabel.Text = LocalizationManager.GetString("total_outstanding") + $": €{total:F2}";
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error loading delayed payments", ex);
                MessageBox.Show("Error loading delayed payments. Please check the log for details.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SendReminderButton_Click(object sender, EventArgs e)
        {
            if (delayedGrid.SelectedRows.Count > 0)
            {
                var selectedPayment = (DelayedPayment)delayedGrid.SelectedRows[0].DataBoundItem;

                // In a real application, this would send an email/SMS
                MessageBox.Show($"Reminder would be sent to:\n\nClient: {selectedPayment.ClientName}\nEmail: {selectedPayment.Email}\nPhone: {selectedPayment.Phone}\nAmount Due: €{selectedPayment.TotalDue:F2}\nDays Overdue: {selectedPayment.DaysOverdue}",
                    "Send Reminder", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Log the reminder
                var contactHistory = new ContactHistory
                {
                    ClientId = selectedPayment.ClientId,
                    ContactDate = DateTime.Now,
                    ContactType = "Payment Reminder",
                    Notes = $"Payment reminder sent for overdue amount: €{selectedPayment.TotalDue:F2} ({selectedPayment.DaysOverdue} days overdue)",
                    CreatedBy = SessionManager.CurrentUser
                };

                new ContactHistoryManager(dbManager).AddContact(contactHistory);
            }
            else
            {
                MessageBox.Show(LocalizationManager.GetString("no_selection"),
                    LocalizationManager.GetString("warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MarkPaidButton_Click(object sender, EventArgs e)
        {
            if (delayedGrid.SelectedRows.Count > 0)
            {
                var selectedPayment = (DelayedPayment)delayedGrid.SelectedRows[0].DataBoundItem;
                var addPaymentForm = new AddPaymentForm(dbManager, selectedPayment.ClientId);

                if (addPaymentForm.ShowDialog() == DialogResult.OK)
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
                HeaderText = LocalizationManager.GetString("base_price"),
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
                DataPropertyName = "NextChargeDate",
                HeaderText = LocalizationManager.GetString("next_charge_date"),
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

    // ===================== ADD CHARGE FORM =====================
    public partial class AddChargeForm : Form
    {
        private DatabaseManager dbManager;
        private ComboBox cboClient, cboService;
        private TextBox txtDescription;
        private NumericUpDown numAmount;
        private DateTimePicker dtpChargeDate, dtpDueDate;
        private RadioButton rbManual, rbAuto;
        private Button btnSave, btnCancel;

        public AddChargeForm(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Text = LocalizationManager.GetString("add_charge");

            int yPos = 20;

            // Client
            var lblClient = new Label();
            lblClient.Text = LocalizationManager.GetString("client") + ":";
            lblClient.Location = new Point(20, yPos);
            lblClient.Size = new Size(100, 20);

            cboClient = new ComboBox();
            cboClient.Location = new Point(130, yPos);
            cboClient.Size = new Size(330, 20);
            cboClient.DropDownStyle = ComboBoxStyle.DropDownList;
            cboClient.SelectedIndexChanged += CboClient_SelectedIndexChanged;
            yPos += 35;

            // Service
            var lblService = new Label();
            lblService.Text = LocalizationManager.GetString("service_name") + ":";
            lblService.Location = new Point(20, yPos);
            lblService.Size = new Size(100, 20);

            cboService = new ComboBox();
            cboService.Location = new Point(130, yPos);
            cboService.Size = new Size(330, 20);
            cboService.DropDownStyle = ComboBoxStyle.DropDownList;
            yPos += 35;

            // Charge Type
            var lblChargeType = new Label();
            lblChargeType.Text = "Charge Type:";
            lblChargeType.Location = new Point(20, yPos);
            lblChargeType.Size = new Size(100, 20);

            rbManual = new RadioButton();
            rbManual.Text = LocalizationManager.GetString("manual_charge");
            rbManual.Location = new Point(130, yPos);
            rbManual.Size = new Size(120, 20);
            rbManual.Checked = true;

            rbAuto = new RadioButton();
            rbAuto.Text = LocalizationManager.GetString("auto_charge");
            rbAuto.Location = new Point(260, yPos);
            rbAuto.Size = new Size(120, 20);
            yPos += 35;

            // Description
            var lblDescription = new Label();
            lblDescription.Text = LocalizationManager.GetString("description") + ":";
            lblDescription.Location = new Point(20, yPos);
            lblDescription.Size = new Size(100, 20);

            txtDescription = new TextBox();
            txtDescription.Location = new Point(130, yPos);
            txtDescription.Size = new Size(330, 60);
            txtDescription.Multiline = true;
            yPos += 70;

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

            // Charge Date
            var lblChargeDate = new Label();
            lblChargeDate.Text = LocalizationManager.GetString("charge_date") + ":";
            lblChargeDate.Location = new Point(20, yPos);
            lblChargeDate.Size = new Size(100, 20);

            dtpChargeDate = new DateTimePicker();
            dtpChargeDate.Location = new Point(130, yPos);
            dtpChargeDate.Size = new Size(200, 20);
            yPos += 35;

            // Due Date
            var lblDueDate = new Label();
            lblDueDate.Text = LocalizationManager.GetString("due_date") + ":";
            lblDueDate.Location = new Point(20, yPos);
            lblDueDate.Size = new Size(100, 20);

            dtpDueDate = new DateTimePicker();
            dtpDueDate.Location = new Point(130, yPos);
            dtpDueDate.Size = new Size(200, 20);
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
                lblClient, cboClient, lblService, cboService,
                lblChargeType, rbManual, rbAuto,
                lblDescription, txtDescription,
                lblAmount, numAmount,
                lblChargeDate, dtpChargeDate,
                lblDueDate, dtpDueDate,
                btnSave, btnCancel
            });
        }

        private void LoadData()
        {
            // Load clients
            var clients = new ClientManager(dbManager).GetAllClients();
            cboClient.DataSource = clients;
            cboClient.DisplayMember = "DisplayName";
            cboClient.ValueMember = "Id";

            // Set default due date based on payment terms
            dtpDueDate.Value = DateTime.Now.AddDays(30);
        }

        private void CboClient_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboClient.SelectedItem is Client client)
            {
                // Load services for this client
                var clientServices = new ClientServiceManager(dbManager).GetClientServices(client.Id);
                cboService.DataSource = clientServices;
                cboService.DisplayMember = "ServiceName";
                cboService.ValueMember = "ServiceId";

                // Set due date based on client's payment terms
                dtpDueDate.Value = dtpChargeDate.Value.AddDays(client.PaymentTermsDays);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (cboClient.SelectedItem == null)
            {
                MessageBox.Show("Please select a client.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (numAmount.Value <= 0)
            {
                MessageBox.Show("Amount must be greater than 0.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var client = (Client)cboClient.SelectedItem;
                var service = cboService.SelectedItem as ClientService;

                var charge = new Charge
                {
                    ClientId = client.Id,
                    ServiceId = service?.ServiceId,
                    ChargeType = rbManual.Checked ? "Manual" : "Auto",
                    Description = txtDescription.Text.Trim(),
                    Amount = numAmount.Value,
                    ChargeDate = dtpChargeDate.Value.Date,
                    DueDate = dtpDueDate.Value.Date,
                    CreatedBy = SessionManager.CurrentUser
                };

                new ChargeManager(dbManager).CreateCharge(charge);
                LogManager.LogInfo($"Charge created for client: {client.DisplayName}, Amount: €{charge.Amount:F2}");

                MessageBox.Show("Charge created successfully!",
                    LocalizationManager.GetString("success"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error creating charge", ex);
                MessageBox.Show(ex.Message, LocalizationManager.GetString("error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ===================== ADD PAYMENT FORM =====================
    public partial class AddPaymentForm : Form
    {
        private DatabaseManager dbManager;
        private int? preselectedClientId;
        private ComboBox cboClient, cboCharge, cboPaymentMethod;
        private TextBox txtReference, txtNotes;
        private NumericUpDown numAmount;
        private DateTimePicker dtpPaymentDate;
        private Button btnSave, btnCancel;
        private Label lblBalance;

        public AddPaymentForm(DatabaseManager dbManager, int? clientId = null)
        {
            this.dbManager = dbManager;
            this.preselectedClientId = clientId;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(500, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Text = LocalizationManager.GetString("add_payment");

            int yPos = 20;

            // Client
            var lblClient = new Label();
            lblClient.Text = LocalizationManager.GetString("client") + ":";
            lblClient.Location = new Point(20, yPos);
            lblClient.Size = new Size(100, 20);

            cboClient = new ComboBox();
            cboClient.Location = new Point(130, yPos);
            cboClient.Size = new Size(330, 20);
            cboClient.DropDownStyle = ComboBoxStyle.DropDownList;
            cboClient.SelectedIndexChanged += CboClient_SelectedIndexChanged;
            yPos += 35;

            // Outstanding Charge
            var lblCharge = new Label();
            lblCharge.Text = "Outstanding Charge:";
            lblCharge.Location = new Point(20, yPos);
            lblCharge.Size = new Size(100, 20);

            cboCharge = new ComboBox();
            cboCharge.Location = new Point(130, yPos);
            cboCharge.Size = new Size(330, 20);
            cboCharge.DropDownStyle = ComboBoxStyle.DropDownList;
            cboCharge.SelectedIndexChanged += CboCharge_SelectedIndexChanged;
            yPos += 35;

            // Balance
            lblBalance = new Label();
            lblBalance.Text = LocalizationManager.GetString("balance") + ": €0.00";
            lblBalance.Location = new Point(130, yPos);
            lblBalance.Size = new Size(200, 20);
            lblBalance.Font = new Font("Arial", 10, FontStyle.Bold);
            yPos += 35;

            // Amount
            var lblAmount = new Label();
            lblAmount.Text = LocalizationManager.GetString("payment_amount") + ":";
            lblAmount.Location = new Point(20, yPos);
            lblAmount.Size = new Size(100, 20);

            numAmount = new NumericUpDown();
            numAmount.Location = new Point(130, yPos);
            numAmount.Size = new Size(120, 20);
            numAmount.Maximum = 999999;
            numAmount.DecimalPlaces = 2;
            numAmount.ThousandsSeparator = true;
            yPos += 35;

            // Payment Date
            var lblPaymentDate = new Label();
            lblPaymentDate.Text = LocalizationManager.GetString("payment_date") + ":";
            lblPaymentDate.Location = new Point(20, yPos);
            lblPaymentDate.Size = new Size(100, 20);

            dtpPaymentDate = new DateTimePicker();
            dtpPaymentDate.Location = new Point(130, yPos);
            dtpPaymentDate.Size = new Size(200, 20);
            yPos += 35;

            // Payment Method
            var lblPaymentMethod = new Label();
            lblPaymentMethod.Text = LocalizationManager.GetString("payment_method") + ":";
            lblPaymentMethod.Location = new Point(20, yPos);
            lblPaymentMethod.Size = new Size(100, 20);

            cboPaymentMethod = new ComboBox();
            cboPaymentMethod.Location = new Point(130, yPos);
            cboPaymentMethod.Size = new Size(200, 20);
            cboPaymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPaymentMethod.Items.AddRange(new string[] {
                LocalizationManager.GetString("cash"),
                LocalizationManager.GetString("bank_transfer"),
                LocalizationManager.GetString("credit_card"),
                LocalizationManager.GetString("check")
            });
            cboPaymentMethod.SelectedIndex = 0;
            yPos += 35;

            // Reference
            var lblReference = new Label();
            lblReference.Text = LocalizationManager.GetString("reference") + ":";
            lblReference.Location = new Point(20, yPos);
            lblReference.Size = new Size(100, 20);

            txtReference = new TextBox();
            txtReference.Location = new Point(130, yPos);
            txtReference.Size = new Size(330, 20);
            yPos += 35;

            // Notes
            var lblNotes = new Label();
            lblNotes.Text = LocalizationManager.GetString("notes") + ":";
            lblNotes.Location = new Point(20, yPos);
            lblNotes.Size = new Size(100, 20);

            txtNotes = new TextBox();
            txtNotes.Location = new Point(130, yPos);
            txtNotes.Size = new Size(330, 60);
            txtNotes.Multiline = true;
            yPos += 70;

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
                lblClient, cboClient, lblCharge, cboCharge, lblBalance,
                lblAmount, numAmount,
                lblPaymentDate, dtpPaymentDate,
                lblPaymentMethod, cboPaymentMethod,
                lblReference, txtReference,
                lblNotes, txtNotes,
                btnSave, btnCancel
            });
        }

        private void LoadData()
        {
            // Load clients
            var clients = new ClientManager(dbManager).GetAllClients();
            cboClient.DataSource = clients;
            cboClient.DisplayMember = "DisplayName";
            cboClient.ValueMember = "Id";

            if (preselectedClientId.HasValue)
            {
                cboClient.SelectedValue = preselectedClientId.Value;
            }
        }

        private void CboClient_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboClient.SelectedItem is Client client)
            {
                // Load outstanding charges for this client
                var charges = new ChargeManager(dbManager).GetClientOutstandingCharges(client.Id);
                cboCharge.DataSource = charges;
                cboCharge.DisplayMember = "DisplayText";
                cboCharge.ValueMember = "Id";
            }
        }

        private void CboCharge_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboCharge.SelectedItem is Charge charge)
            {
                decimal balance = charge.Amount - charge.PaidAmount;
                lblBalance.Text = LocalizationManager.GetString("balance") + $": €{balance:F2}";
                numAmount.Value = balance;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (cboClient.SelectedItem == null)
            {
                MessageBox.Show("Please select a client.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (numAmount.Value <= 0)
            {
                MessageBox.Show("Payment amount must be greater than 0.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var client = (Client)cboClient.SelectedItem;
                var charge = cboCharge.SelectedItem as Charge;

                var payment = new Payment
                {
                    ClientId = client.Id,
                    ChargeId = charge?.Id,
                    Amount = numAmount.Value,
                    PaymentDate = dtpPaymentDate.Value.Date,
                    PaymentMethod = cboPaymentMethod.Text,
                    Reference = txtReference.Text.Trim(),
                    Notes = txtNotes.Text.Trim(),
                    CreatedBy = SessionManager.CurrentUser
                };

                new PaymentManager(dbManager).CreatePayment(payment);
                LogManager.LogInfo($"Payment recorded for client: {client.DisplayName}, Amount: €{payment.Amount:F2}");

                MessageBox.Show("Payment recorded successfully!",
                    LocalizationManager.GetString("success"), MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error recording payment", ex);
                MessageBox.Show(ex.Message, LocalizationManager.GetString("error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ===================== CHANGE PASSWORD FORM =====================
    public partial class ChangePasswordForm : Form
    {
        private DatabaseManager dbManager;
        private TextBox txtOldPassword, txtNewPassword, txtConfirmPassword;
        private Button btnSave, btnCancel;
        private bool isFirstLogin;

        public ChangePasswordForm(DatabaseManager dbManager, bool firstLogin = false)
        {
            this.dbManager = dbManager;
            this.isFirstLogin = firstLogin;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Text = LocalizationManager.GetString("change_password");

            int yPos = 20;

            // Old Password
            var lblOldPassword = new Label();
            lblOldPassword.Text = LocalizationManager.GetString("old_password") + ":";
            lblOldPassword.Location = new Point(20, yPos);
            lblOldPassword.Size = new Size(120, 20);

            txtOldPassword = new TextBox();
            txtOldPassword.Location = new Point(150, yPos);
            txtOldPassword.Size = new Size(200, 20);
            txtOldPassword.UseSystemPasswordChar = true;
            yPos += 35;

            // New Password
            var lblNewPassword = new Label();
            lblNewPassword.Text = LocalizationManager.GetString("new_password") + ":";
            lblNewPassword.Location = new Point(20, yPos);
            lblNewPassword.Size = new Size(120, 20);

            txtNewPassword = new TextBox();
            txtNewPassword.Location = new Point(150, yPos);
            txtNewPassword.Size = new Size(200, 20);
            txtNewPassword.UseSystemPasswordChar = true;
            yPos += 35;

            // Confirm Password
            var lblConfirmPassword = new Label();
            lblConfirmPassword.Text = LocalizationManager.GetString("confirm_password") + ":";
            lblConfirmPassword.Location = new Point(20, yPos);
            lblConfirmPassword.Size = new Size(120, 20);

            txtConfirmPassword = new TextBox();
            txtConfirmPassword.Location = new Point(150, yPos);
            txtConfirmPassword.Size = new Size(200, 20);
            txtConfirmPassword.UseSystemPasswordChar = true;
            yPos += 35;

            // Password requirements label
            var lblRequirements = new Label();
            lblRequirements.Text = LocalizationManager.GetString("password_requirements");
            lblRequirements.Location = new Point(20, yPos);
            lblRequirements.Size = new Size(350, 20);
            lblRequirements.Font = new Font("Arial", 8, FontStyle.Italic);
            yPos += 30;

            // Buttons
            btnSave = new Button();
            btnSave.Text = LocalizationManager.GetString("save");
            btnSave.Location = new Point(190, yPos);
            btnSave.Size = new Size(80, 30);
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button();
            btnCancel.Text = LocalizationManager.GetString("cancel");
            btnCancel.Location = new Point(280, yPos);
            btnCancel.Size = new Size(80, 30);
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += (s, e) => {
                if (isFirstLogin)
                {
                    MessageBox.Show("You must change your password on first login.",
                        LocalizationManager.GetString("warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    this.Close();
                }
            };

            this.Controls.AddRange(new Control[] {
                lblOldPassword, txtOldPassword,
                lblNewPassword, txtNewPassword,
                lblConfirmPassword, txtConfirmPassword,
                lblRequirements,
                btnSave, btnCancel
            });
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtOldPassword.Text) ||
                string.IsNullOrWhiteSpace(txtNewPassword.Text) ||
                string.IsNullOrWhiteSpace(txtConfirmPassword.Text))
            {
                MessageBox.Show("Please fill all fields.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (txtNewPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show(LocalizationManager.GetString("passwords_not_match"),
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!ValidationHelper.IsValidPassword(txtNewPassword.Text))
            {
                MessageBox.Show(LocalizationManager.GetString("password_requirements"),
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var userManager = new UserManager(dbManager);
                if (userManager.ChangePassword(SessionManager.CurrentUser, txtOldPassword.Text, txtNewPassword.Text))
                {
                    LogManager.LogInfo($"Password changed for user: {SessionManager.CurrentUser}");
                    MessageBox.Show(LocalizationManager.GetString("password_changed"),
                        LocalizationManager.GetString("success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                else
                {
                    MessageBox.Show(LocalizationManager.GetString("invalid_old_password"),
                        LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error changing password", ex);
                MessageBox.Show(ex.Message, LocalizationManager.GetString("error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}// This code is part of the BillPilot application, which is a billing and invoicing system.