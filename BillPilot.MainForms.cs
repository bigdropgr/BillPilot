using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BillPilot
{
    // ===================== PROGRAM ENTRY POINT =====================
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            LogManager.LogInfo("BillPilot application started");

            // Load language preference before showing login
            string savedLanguage = MainForm.LoadLanguagePreference();
            LocalizationManager.SetLanguage(savedLanguage);

            try
            {
                // Show login form first
                var loginForm = new LoginForm();
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    // If login successful, show main form
                    Application.Run(new MainForm());
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError("Fatal application error", ex);
                MessageBox.Show("A fatal error occurred. Please check the log file for details.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                LogManager.LogInfo("BillPilot application closed");
            }
        }
    }

    // ===================== LOGIN FORM =====================
    public partial class LoginForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnCancel;
        private ComboBox cboLanguage;
        private Label lblLanguage;
        private DatabaseManager dbManager;

        public LoginForm()
        {
            InitializeComponent();
            dbManager = new DatabaseManager();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Text = LocalizationManager.GetString("login_title");

            // Logo detection and display
            var logoPanel = new Panel();
            logoPanel.Size = new Size(360, 60);
            logoPanel.Location = new Point(20, 10);
            logoPanel.BackColor = Color.White;
            logoPanel.BorderStyle = BorderStyle.FixedSingle;

            var logoPictureBox = new PictureBox();
            logoPictureBox.Dock = DockStyle.Fill;
            logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            LoadLogo(logoPictureBox);
            logoPanel.Controls.Add(logoPictureBox);

            // Username
            var lblUsername = new Label();
            lblUsername.Text = LocalizationManager.GetString("username");
            lblUsername.Location = new Point(20, 85);
            lblUsername.Size = new Size(100, 20);

            this.txtUsername = new TextBox();
            this.txtUsername.Location = new Point(130, 83);
            this.txtUsername.Size = new Size(230, 20);
            this.txtUsername.Text = "admin"; // Default for easier testing

            // Password
            var lblPassword = new Label();
            lblPassword.Text = LocalizationManager.GetString("password");
            lblPassword.Location = new Point(20, 115);
            lblPassword.Size = new Size(100, 20);

            this.txtPassword = new TextBox();
            this.txtPassword.Location = new Point(130, 113);
            this.txtPassword.Size = new Size(230, 20);
            this.txtPassword.UseSystemPasswordChar = true;

            // Language
            this.lblLanguage = new Label();
            this.lblLanguage.Text = LocalizationManager.GetString("language");
            this.lblLanguage.Location = new Point(20, 145);
            this.lblLanguage.Size = new Size(100, 20);

            this.cboLanguage = new ComboBox();
            this.cboLanguage.Location = new Point(130, 143);
            this.cboLanguage.Size = new Size(100, 20);
            this.cboLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cboLanguage.Items.AddRange(new string[] { "Ελληνικά", "English" });
            this.cboLanguage.SelectedIndex = LocalizationManager.CurrentLanguage == "gr" ? 0 : 1;
            this.cboLanguage.SelectedIndexChanged += CboLanguage_SelectedIndexChanged;

            // Login Button
            this.btnLogin = new Button();
            this.btnLogin.Location = new Point(195, 180);
            this.btnLogin.Size = new Size(80, 30);
            this.btnLogin.Text = LocalizationManager.GetString("login");
            this.btnLogin.UseVisualStyleBackColor = true;
            this.btnLogin.Click += BtnLogin_Click;

            // Cancel Button
            this.btnCancel = new Button();
            this.btnCancel.Location = new Point(280, 180);
            this.btnCancel.Size = new Size(80, 30);
            this.btnCancel.Text = LocalizationManager.GetString("cancel");
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += (s, e) => this.Close();

            // Add controls
            this.Controls.AddRange(new Control[] {
                logoPanel, lblUsername, this.txtUsername,
                lblPassword, this.txtPassword,
                lblLanguage, this.cboLanguage,
                this.btnLogin, this.btnCancel
            });

            this.AcceptButton = this.btnLogin;
        }

        private void LoadLogo(PictureBox pictureBox)
        {
            string appPath = Application.StartupPath;
            string[] logoFiles = { "logo.png", "logo.jpg", "logo.bmp", "logo.gif" };

            foreach (string logoFile in logoFiles)
            {
                string logoPath = Path.Combine(appPath, logoFile);
                if (File.Exists(logoPath))
                {
                    try
                    {
                        pictureBox.Image = Image.FromFile(logoPath);
                        return;
                    }
                    catch
                    {
                        // Continue to next file if this one fails
                    }
                }
            }

            // Create default logo if none found
            var bitmap = new Bitmap(300, 50);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.FillRectangle(Brushes.DarkBlue, 0, 0, 300, 50);
                using (var font = new Font("Arial", 20, FontStyle.Bold))
                {
                    g.DrawString("BillPilot", font, Brushes.White, 85, 10);
                }
            }
            pictureBox.Image = bitmap;
        }

        private void CboLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            LocalizationManager.SetLanguage(cboLanguage.SelectedIndex == 0 ? "gr" : "en");
            UpdateUILanguage();
        }

        private void UpdateUILanguage()
        {
            this.Text = LocalizationManager.GetString("login_title");
            this.Controls.OfType<Label>().FirstOrDefault(l => l.Top == 85).Text = LocalizationManager.GetString("username");
            this.Controls.OfType<Label>().FirstOrDefault(l => l.Top == 115).Text = LocalizationManager.GetString("password");
            this.lblLanguage.Text = LocalizationManager.GetString("language");
            this.btnLogin.Text = LocalizationManager.GetString("login");
            this.btnCancel.Text = LocalizationManager.GetString("cancel");
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Please enter username and password.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var userManager = new UserManager(dbManager);
            bool isFirstLogin = false;

            if (userManager.ValidateLogin(txtUsername.Text, txtPassword.Text, out isFirstLogin))
            {
                LogManager.LogInfo($"User logged in: {txtUsername.Text}");
                SessionManager.StartSession(txtUsername.Text, isFirstLogin);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                LogManager.LogWarning($"Failed login attempt for user: {txtUsername.Text}");
                MessageBox.Show(LocalizationManager.GetString("login_failed"),
                    LocalizationManager.GetString("error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Clear();
                txtUsername.Focus();
            }
        }
    }

    // ===================== MAIN FORM =====================
    public partial class MainForm : Form
    {
        private DatabaseManager dbManager;
        private ClientManager clientManager;
        private ServiceManager serviceManager;
        private PaymentManager paymentManager;
        private MenuStrip menuStrip;
        private TabControl mainTabControl;
        private TabPage dashboardTab, clientsTab, servicesTab, upcomingTab, delayedTab, reportsTab;
        private PictureBox logoPictureBox;
        private Timer refreshTimer;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel userLabel;

        public MainForm()
        {
            dbManager = new DatabaseManager();
            clientManager = new ClientManager(dbManager);
            serviceManager = new ServiceManager(dbManager);
            paymentManager = new PaymentManager(dbManager);
            InitializeComponent();
            LoadDashboardData();

            // Set up session monitoring
            SessionManager.SessionExpired += OnSessionExpired;

            // Set up auto-refresh timer
            refreshTimer = new Timer();
            refreshTimer.Interval = 300000; // Refresh every 5 minutes
            refreshTimer.Tick += (s, e) => {
                LoadDashboardData();
                CheckAndCreateUpcomingPayments();
            };
            refreshTimer.Start();

            // Check if first login
            if (SessionManager.IsFirstLogin)
            {
                MessageBox.Show(LocalizationManager.GetString("first_login_message"),
                    LocalizationManager.GetString("warning"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                var changePasswordForm = new ChangePasswordForm(dbManager, true);
                changePasswordForm.ShowDialog();
            }

            // Check for delayed payments and show notification
            CheckDelayedPayments();

            // Create initial upcoming payments
            CheckAndCreateUpcomingPayments();
        }

        private void CheckDelayedPayments()
        {
            var overdueCount = paymentManager.GetOverduePaymentCount();
            if (overdueCount > 0)
            {
                MessageBox.Show(string.Format(LocalizationManager.GetString("delayed_payments_notice"), overdueCount),
                    LocalizationManager.GetString("warning"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void CheckAndCreateUpcomingPayments()
        {
            try
            {
                var clientServiceManager = new ClientServiceManager(dbManager);
                clientServiceManager.CheckAndCreateUpcomingPayments();
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error checking upcoming payments", ex);
            }
        }

        private void OnSessionExpired(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler(OnSessionExpired), sender, e);
                return;
            }

            MessageBox.Show(LocalizationManager.GetString("session_expired"),
                LocalizationManager.GetString("warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Application.Restart();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.Text = LocalizationManager.GetString("app_title");
            this.Icon = SystemIcons.Application;

            // Track user activity
            this.MouseMove += (s, e) => SessionManager.UpdateActivity();
            this.KeyPress += (s, e) => SessionManager.UpdateActivity();

            // Create menu
            CreateMenu();

            // Create main tab control
            mainTabControl = new TabControl();
            mainTabControl.Dock = DockStyle.Fill;
            mainTabControl.Font = new Font("Arial", 10);

            // Standard tab appearance
            mainTabControl.Appearance = TabAppearance.Normal;
            mainTabControl.SizeMode = TabSizeMode.Normal;

            // Create tabs
            CreateDashboardTab();
            CreateClientsTab();
            CreateServicesTab();
            CreateUpcomingPaymentsTab();
            CreateDelayedPaymentsTab();
            CreateReportsTab();

            mainTabControl.TabPages.AddRange(new TabPage[] {
                dashboardTab, clientsTab, servicesTab, upcomingTab, delayedTab, reportsTab
            });

            // Create status bar
            CreateStatusBar();

            // Add controls in proper order
            this.Controls.Add(mainTabControl);
            this.Controls.Add(statusStrip);
            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;
        }

        private void CreateMenu()
        {
            menuStrip = new MenuStrip();

            // File Menu
            var fileMenu = new ToolStripMenuItem(LocalizationManager.GetString("settings"));

            var changePasswordMenu = new ToolStripMenuItem(LocalizationManager.GetString("change_password"));
            changePasswordMenu.Click += ChangePasswordMenu_Click;

            var backupMenu = new ToolStripMenuItem(LocalizationManager.GetString("backup"));
            backupMenu.Click += BackupMenu_Click;

            var restoreMenu = new ToolStripMenuItem(LocalizationManager.GetString("restore"));
            restoreMenu.Click += RestoreMenu_Click;

            var logoutMenu = new ToolStripMenuItem(LocalizationManager.GetString("logout"));
            logoutMenu.Click += (s, e) => {
                var result = MessageBox.Show("Are you sure you want to logout?",
                    LocalizationManager.GetString("confirm"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Application.Restart();
                }
            };

            var exitMenu = new ToolStripMenuItem(LocalizationManager.GetString("exit"));
            exitMenu.Click += (s, e) => Application.Exit();

            fileMenu.DropDownItems.AddRange(new ToolStripItem[] {
                changePasswordMenu, new ToolStripSeparator(),
                backupMenu, restoreMenu, new ToolStripSeparator(),
                logoutMenu, new ToolStripSeparator(), exitMenu
            });

            // Language menu
            var languageMenu = new ToolStripMenuItem(LocalizationManager.GetString("language"));
            var greekMenu = new ToolStripMenuItem("Ελληνικά");
            greekMenu.Click += (s, e) => ChangeLanguage("gr");
            var englishMenu = new ToolStripMenuItem("English");
            englishMenu.Click += (s, e) => ChangeLanguage("en");

            languageMenu.DropDownItems.AddRange(new ToolStripItem[] {
                greekMenu, englishMenu
            });

            // Help menu
            var helpMenu = new ToolStripMenuItem("Help");
            var aboutMenu = new ToolStripMenuItem("About BillPilot");
            aboutMenu.Click += (s, e) => MessageBox.Show(
                "BillPilot v1.0\n\nPortable Business Management System\n\n© 2024 BillPilot Software",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            helpMenu.DropDownItems.Add(aboutMenu);

            menuStrip.Items.AddRange(new ToolStripItem[] {
                fileMenu, languageMenu, helpMenu
            });

            this.Controls.Add(menuStrip);
        }

        private void CreateStatusBar()
        {
            statusStrip = new StatusStrip();

            statusLabel = new ToolStripStatusLabel();
            statusLabel.Text = "Ready";
            statusLabel.Spring = true;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;

            userLabel = new ToolStripStatusLabel();
            userLabel.Text = $"User: {SessionManager.CurrentUser}";
            userLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;

            var dateLabel = new ToolStripStatusLabel();
            dateLabel.Text = DateTime.Now.ToString("dd/MM/yyyy");
            dateLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;

            statusStrip.Items.AddRange(new ToolStripItem[] {
                statusLabel, userLabel, dateLabel
            });
        }

        private void CreateDashboardTab()
        {
            dashboardTab = new TabPage(LocalizationManager.GetString("dashboard"));
            dashboardTab.BackColor = Color.FromArgb(248, 249, 250);

            // Logo section
            var logoPanel = new Panel();
            logoPanel.Location = new Point(20, 20);
            logoPanel.Size = new Size(200, 100);
            logoPanel.BackColor = Color.White;
            logoPanel.BorderStyle = BorderStyle.FixedSingle;

            logoPictureBox = new PictureBox();
            logoPictureBox.Dock = DockStyle.Fill;
            logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            logoPictureBox.BackColor = Color.White;

            // Try to load logo
            LoadLogo();

            logoPanel.Controls.Add(logoPictureBox);

            // Welcome section
            var welcomeLabel = new Label();
            welcomeLabel.Text = LocalizationManager.GetString("welcome");
            welcomeLabel.Font = new Font("Arial", 18, FontStyle.Bold);
            welcomeLabel.ForeColor = Color.FromArgb(51, 51, 51);
            welcomeLabel.Location = new Point(240, 30);
            welcomeLabel.AutoSize = true;

            var subtitleLabel = new Label();
            subtitleLabel.Text = LocalizationManager.GetString("subtitle");
            subtitleLabel.Font = new Font("Arial", 12, FontStyle.Italic);
            subtitleLabel.ForeColor = Color.FromArgb(108, 117, 125);
            subtitleLabel.Location = new Point(240, 65);
            subtitleLabel.AutoSize = true;

            // Stats cards
            var statsPanel = new FlowLayoutPanel();
            statsPanel.Location = new Point(20, 140);
            statsPanel.Size = new Size(1140, 150);
            statsPanel.FlowDirection = FlowDirection.LeftToRight;
            statsPanel.WrapContents = false;
            statsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Create stat cards
            var clientsCard = CreateStatCard(LocalizationManager.GetString("total_clients"), "0", Color.FromArgb(52, 144, 220));
            var servicesCard = CreateStatCard(LocalizationManager.GetString("total_services"), "0", Color.FromArgb(92, 184, 92));
            var revenueCard = CreateStatCard(LocalizationManager.GetString("total_revenue"), "€0", Color.FromArgb(240, 173, 78));
            var outstandingCard = CreateStatCard(LocalizationManager.GetString("total_outstanding"), "€0", Color.FromArgb(217, 83, 79));

            statsPanel.Controls.AddRange(new Control[] {
                clientsCard, servicesCard, revenueCard, outstandingCard
            });

            // Quick actions section
            var quickActionsLabel = new Label();
            quickActionsLabel.Text = LocalizationManager.GetString("quick_actions");
            quickActionsLabel.Font = new Font("Arial", 14, FontStyle.Bold);
            quickActionsLabel.Location = new Point(20, 310);
            quickActionsLabel.AutoSize = true;

            var quickActionsPanel = CreateQuickActionsPanel();

            // Recent activity section
            var activityLabel = new Label();
            activityLabel.Text = LocalizationManager.GetString("recent_activity");
            activityLabel.Font = new Font("Arial", 14, FontStyle.Bold);
            activityLabel.Location = new Point(620, 310);
            activityLabel.AutoSize = true;

            var activityPanel = CreateRecentActivityPanel();

            dashboardTab.Controls.AddRange(new Control[] {
                logoPanel, welcomeLabel, subtitleLabel, statsPanel,
                quickActionsLabel, quickActionsPanel, activityLabel, activityPanel
            });
        }

        private Panel CreateQuickActionsPanel()
        {
            var panel = new Panel();
            panel.Location = new Point(20, 340);
            panel.Size = new Size(580, 300);
            panel.BackColor = Color.White;
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            var btnAddClient = new Button();
            btnAddClient.Text = LocalizationManager.GetString("add_client");
            btnAddClient.Location = new Point(20, 20);
            btnAddClient.Size = new Size(250, 40);
            btnAddClient.UseVisualStyleBackColor = true;
            btnAddClient.Font = new Font("Arial", 10);
            btnAddClient.Image = SystemIcons.Shield.ToBitmap();
            btnAddClient.ImageAlign = ContentAlignment.MiddleLeft;
            btnAddClient.TextAlign = ContentAlignment.MiddleRight;
            btnAddClient.Click += (s, e) => {
                mainTabControl.SelectedTab = clientsTab;
                var clientListControl = clientsTab.Controls.OfType<ClientListControl>().FirstOrDefault();
                clientListControl?.ShowAddClientDialog();
            };

            var btnAddService = new Button();
            btnAddService.Text = LocalizationManager.GetString("add_service");
            btnAddService.Location = new Point(290, 20);
            btnAddService.Size = new Size(250, 40);
            btnAddService.UseVisualStyleBackColor = true;
            btnAddService.Font = new Font("Arial", 10);
            btnAddService.Click += (s, e) => {
                mainTabControl.SelectedTab = servicesTab;
                var serviceListControl = servicesTab.Controls.OfType<ServiceListControl>().FirstOrDefault();
                serviceListControl?.ShowAddServiceDialog();
            };

            var btnGenerateReport = new Button();
            btnGenerateReport.Text = LocalizationManager.GetString("generate_report");
            btnGenerateReport.Location = new Point(20, 80);
            btnGenerateReport.Size = new Size(520, 40);
            btnGenerateReport.UseVisualStyleBackColor = true;
            btnGenerateReport.Font = new Font("Arial", 10);
            btnGenerateReport.Click += (s, e) => mainTabControl.SelectedTab = reportsTab;

            var btnViewUpcoming = new Button();
            btnViewUpcoming.Text = LocalizationManager.GetString("upcoming_payments");
            btnViewUpcoming.Location = new Point(20, 140);
            btnViewUpcoming.Size = new Size(250, 40);
            btnViewUpcoming.UseVisualStyleBackColor = true;
            btnViewUpcoming.Font = new Font("Arial", 10);
            btnViewUpcoming.Click += (s, e) => mainTabControl.SelectedTab = upcomingTab;

            var btnViewDelayed = new Button();
            btnViewDelayed.Text = LocalizationManager.GetString("delayed_payments");
            btnViewDelayed.Location = new Point(290, 140);
            btnViewDelayed.Size = new Size(250, 40);
            btnViewDelayed.UseVisualStyleBackColor = true;
            btnViewDelayed.Font = new Font("Arial", 10);
            btnViewDelayed.Click += (s, e) => mainTabControl.SelectedTab = delayedTab;

            panel.Controls.AddRange(new Control[] {
                btnAddClient, btnAddService, btnGenerateReport, btnViewUpcoming, btnViewDelayed
            });

            return panel;
        }

        private void LoadLogo()
        {
            string appPath = Application.StartupPath;
            string[] logoFiles = { "logo.png", "logo.jpg", "logo.bmp", "logo.gif" };

            foreach (string logoFile in logoFiles)
            {
                string logoPath = Path.Combine(appPath, logoFile);
                if (File.Exists(logoPath))
                {
                    try
                    {
                        logoPictureBox.Image = Image.FromFile(logoPath);
                        return;
                    }
                    catch
                    {
                        // Continue to next file if this one fails
                    }
                }
            }

            // If no logo found, create a simple default
            CreateDefaultLogo();
        }

        private void CreateDefaultLogo()
        {
            var bitmap = new Bitmap(150, 80);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.FillRectangle(Brushes.DarkBlue, 0, 0, 150, 80);
                g.FillRectangle(Brushes.White, 10, 10, 130, 60);
                using (var font = new Font("Arial", 16, FontStyle.Bold))
                {
                    g.DrawString("BillPilot", font, Brushes.DarkBlue, 25, 25);
                }
            }
            logoPictureBox.Image = bitmap;
        }

        private Panel CreateStatCard(string title, string value, Color color)
        {
            var card = new Panel();
            card.Size = new Size(270, 120);
            card.BackColor = Color.White;
            card.BorderStyle = BorderStyle.FixedSingle;
            card.Margin = new Padding(10);

            var colorBar = new Panel();
            colorBar.BackColor = color;
            colorBar.Dock = DockStyle.Top;
            colorBar.Height = 4;

            var iconPanel = new Panel();
            iconPanel.Size = new Size(50, 50);
            iconPanel.Location = new Point(20, 35);
            iconPanel.BackColor = Color.FromArgb(30, color);

            var valueLabel = new Label();
            valueLabel.Text = value;
            valueLabel.Font = new Font("Arial", 24, FontStyle.Bold);
            valueLabel.ForeColor = color;
            valueLabel.Location = new Point(80, 25);
            valueLabel.AutoSize = true;
            valueLabel.Name = "value_" + title.Replace(" ", "");

            var titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.Font = new Font("Arial", 12);
            titleLabel.ForeColor = Color.FromArgb(108, 117, 125);
            titleLabel.Location = new Point(80, 65);
            titleLabel.AutoSize = true;

            card.Controls.AddRange(new Control[] { colorBar, iconPanel, valueLabel, titleLabel });
            return card;
        }

        private Panel CreateRecentActivityPanel()
        {
            var panel = new Panel();
            panel.Location = new Point(620, 340);
            panel.Size = new Size(540, 300);
            panel.BackColor = Color.White;
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var activityList = new ListBox();
            activityList.Dock = DockStyle.Fill;
            activityList.Font = new Font("Arial", 10);
            activityList.BorderStyle = BorderStyle.None;
            activityList.Name = "activityList";
            activityList.DrawMode = DrawMode.OwnerDrawFixed;
            activityList.ItemHeight = 30;
            activityList.DrawItem += ActivityList_DrawItem;

            // Load recent activities
            LoadRecentActivities(activityList);

            panel.Controls.Add(activityList);
            return panel;
        }

        private void ActivityList_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();
            var item = ((ListBox)sender).Items[e.Index].ToString();
            var isPaid = item.Contains("Paid:");
            var brush = isPaid ? Brushes.Green : Brushes.DarkRed;

            e.Graphics.DrawString(item, e.Font, brush, e.Bounds.Left + 5, e.Bounds.Top + 8);
            e.DrawFocusRectangle();
        }

        private void LoadRecentActivities(ListBox listBox)
        {
            try
            {
                listBox.Items.Clear();

                // Get recent payments
                var recentPayments = paymentManager.GetRecentPayments(10);
                foreach (var payment in recentPayments)
                {
                    if (payment.IsPaid)
                    {
                        listBox.Items.Add($"Paid: {payment.ClientName} - €{payment.Amount:F2} ({payment.PaidDate.Value:dd/MM/yyyy})");
                    }
                    else
                    {
                        listBox.Items.Add($"Due: {payment.ClientName} - €{payment.Amount:F2} ({payment.DueDate:dd/MM/yyyy})");
                    }
                }

                if (listBox.Items.Count == 0)
                {
                    listBox.Items.Add("No recent activity to display.");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error loading recent activities", ex);
                listBox.Items.Clear();
                listBox.Items.Add("Error loading activities.");
            }
        }

        private void CreateClientsTab()
        {
            clientsTab = new TabPage(LocalizationManager.GetString("clients"));

            var clientListControl = new ClientListControl(dbManager);
            clientListControl.Dock = DockStyle.Fill;
            clientsTab.Controls.Add(clientListControl);
        }

        private void CreateServicesTab()
        {
            servicesTab = new TabPage(LocalizationManager.GetString("services"));

            var serviceListControl = new ServiceListControl(dbManager);
            serviceListControl.Dock = DockStyle.Fill;
            servicesTab.Controls.Add(serviceListControl);
        }

        private void CreateUpcomingPaymentsTab()
        {
            upcomingTab = new TabPage(LocalizationManager.GetString("upcoming_payments"));

            var upcomingControl = new UpcomingPaymentsControl(dbManager);
            upcomingControl.Dock = DockStyle.Fill;
            upcomingTab.Controls.Add(upcomingControl);
        }

        private void CreateDelayedPaymentsTab()
        {
            delayedTab = new TabPage(LocalizationManager.GetString("delayed_payments"));

            var delayedControl = new DelayedPaymentsControl(dbManager);
            delayedControl.Dock = DockStyle.Fill;
            delayedTab.Controls.Add(delayedControl);
        }

        private void CreateReportsTab()
        {
            reportsTab = new TabPage(LocalizationManager.GetString("reports"));

            var reportsControl = new ReportsControl(dbManager);
            reportsControl.Dock = DockStyle.Fill;
            reportsTab.Controls.Add(reportsControl);
        }

        private void LoadDashboardData()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(LoadDashboardData));
                return;
            }

            try
            {
                statusLabel.Text = "Loading dashboard data...";

                // Update client count
                var clients = clientManager.GetAllClients();
                if (clients != null)
                    UpdateStatCard("total_clients", clients.Count.ToString());

                // Update services count
                var services = serviceManager.GetAllServices();
                if (services != null)
                    UpdateStatCard("total_services", services.Count.ToString());

                // Update revenue
                var totalRevenue = paymentManager.GetTotalRevenue();
                UpdateStatCard("total_revenue", $"€{totalRevenue:F2}");

                // Update outstanding
                var totalOutstanding = paymentManager.GetTotalOutstanding();
                UpdateStatCard("total_outstanding", $"€{totalOutstanding:F2}");

                // Update recent activities if dashboard is selected
                if (mainTabControl.SelectedTab == dashboardTab)
                {
                    var activityPanel = dashboardTab.Controls.OfType<Panel>()
                        .FirstOrDefault(p => p.Location.X == 620 && p.Location.Y == 340);
                    if (activityPanel != null)
                    {
                        var listBox = activityPanel.Controls.Find("activityList", false).FirstOrDefault() as ListBox;
                        if (listBox != null)
                        {
                            LoadRecentActivities(listBox);
                        }
                    }
                }

                statusLabel.Text = "Ready";
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error loading dashboard data", ex);
                statusLabel.Text = "Error loading data";
            }
        }

        private void UpdateStatCard(string cardName, string value)
        {
            var cardIdentifier = "value_" + cardName.Replace(" ", "");
            foreach (Control control in dashboardTab.Controls)
            {
                if (control is FlowLayoutPanel panel)
                {
                    var valueLabel = panel.Controls.Find(cardIdentifier, true).FirstOrDefault() as Label;
                    if (valueLabel != null)
                    {
                        valueLabel.Text = value;
                        break;
                    }
                }
            }
        }

        private void ChangeLanguage(string language)
        {
            LocalizationManager.SetLanguage(language);

            // Save language preference to a settings file
            SaveLanguagePreference(language);

            // Update all UI elements without restarting
            UpdateAllUITexts();

            MessageBox.Show("Language changed successfully!",
                LocalizationManager.GetString("success"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void SaveLanguagePreference(string language)
        {
            try
            {
                string settingsPath = Path.Combine(Application.StartupPath, "settings.ini");
                File.WriteAllText(settingsPath, $"Language={language}");
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error saving language preference", ex);
            }
        }

        public static string LoadLanguagePreference()
        {
            try
            {
                string settingsPath = Path.Combine(Application.StartupPath, "settings.ini");
                if (File.Exists(settingsPath))
                {
                    string content = File.ReadAllText(settingsPath);
                    if (content.StartsWith("Language="))
                    {
                        return content.Substring(9);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error loading language preference", ex);
            }
            return "gr"; // Default to Greek
        }

        private void UpdateAllUITexts()
        {
            // Update form title
            this.Text = LocalizationManager.GetString("app_title");

            // Update menu items
            menuStrip.Items[0].Text = LocalizationManager.GetString("settings");
            var fileMenu = menuStrip.Items[0] as ToolStripMenuItem;
            fileMenu.DropDownItems[0].Text = LocalizationManager.GetString("change_password");
            fileMenu.DropDownItems[2].Text = LocalizationManager.GetString("backup");
            fileMenu.DropDownItems[3].Text = LocalizationManager.GetString("restore");
            fileMenu.DropDownItems[5].Text = LocalizationManager.GetString("logout");
            fileMenu.DropDownItems[7].Text = LocalizationManager.GetString("exit");

            menuStrip.Items[1].Text = LocalizationManager.GetString("language");

            // Update tab titles
            dashboardTab.Text = LocalizationManager.GetString("dashboard");
            clientsTab.Text = LocalizationManager.GetString("clients");
            servicesTab.Text = LocalizationManager.GetString("services");
            upcomingTab.Text = LocalizationManager.GetString("upcoming_payments");
            delayedTab.Text = LocalizationManager.GetString("delayed_payments");
            reportsTab.Text = LocalizationManager.GetString("reports");

            // Update dashboard elements
            UpdateDashboardTexts();

            // Update status bar
            statusLabel.Text = "Ready";
            userLabel.Text = $"User: {SessionManager.CurrentUser}";

            // Refresh current tab's content
            if (mainTabControl.SelectedTab == clientsTab)
            {
                var clientControl = clientsTab.Controls.OfType<ClientListControl>().FirstOrDefault();
                clientControl?.UpdateUILanguage();
            }
            else if (mainTabControl.SelectedTab == servicesTab)
            {
                var serviceControl = servicesTab.Controls.OfType<ServiceListControl>().FirstOrDefault();
                serviceControl?.UpdateUILanguage();
            }
        }

        private void UpdateDashboardTexts()
        {
            // Find and update welcome label
            var welcomeLabel = dashboardTab.Controls.OfType<Label>()
                .FirstOrDefault(l => l.Location.X == 240 && l.Location.Y == 30);
            if (welcomeLabel != null)
                welcomeLabel.Text = LocalizationManager.GetString("welcome");

            // Find and update subtitle label
            var subtitleLabel = dashboardTab.Controls.OfType<Label>()
                .FirstOrDefault(l => l.Location.X == 240 && l.Location.Y == 65);
            if (subtitleLabel != null)
                subtitleLabel.Text = LocalizationManager.GetString("subtitle");

            // Update quick actions label
            var quickActionsLabel = dashboardTab.Controls.OfType<Label>()
                .FirstOrDefault(l => l.Location.Y == 310 && l.Location.X == 20);
            if (quickActionsLabel != null)
                quickActionsLabel.Text = LocalizationManager.GetString("quick_actions");

            // Update recent activity label
            var activityLabel = dashboardTab.Controls.OfType<Label>()
                .FirstOrDefault(l => l.Location.Y == 310 && l.Location.X == 620);
            if (activityLabel != null)
                activityLabel.Text = LocalizationManager.GetString("recent_activity");

            // Update stat card titles
            UpdateStatCardTitles();

            // Update quick action buttons
            UpdateQuickActionButtons();
        }

        private void UpdateStatCardTitles()
        {
            var statsPanel = dashboardTab.Controls.OfType<FlowLayoutPanel>()
                .FirstOrDefault(p => p.Location.Y == 140);

            if (statsPanel != null)
            {
                var cards = statsPanel.Controls.OfType<Panel>().ToList();
                if (cards.Count >= 4)
                {
                    UpdateStatCardTitle(cards[0], LocalizationManager.GetString("total_clients"));
                    UpdateStatCardTitle(cards[1], LocalizationManager.GetString("total_services"));
                    UpdateStatCardTitle(cards[2], LocalizationManager.GetString("total_revenue"));
                    UpdateStatCardTitle(cards[3], LocalizationManager.GetString("total_outstanding"));
                }
            }
        }

        private void UpdateStatCardTitle(Panel card, string newTitle)
        {
            var titleLabel = card.Controls.OfType<Label>()
                .FirstOrDefault(l => l.Location.Y == 65);
            if (titleLabel != null)
                titleLabel.Text = newTitle;
        }

        private void UpdateQuickActionButtons()
        {
            var quickActionsPanel = dashboardTab.Controls.OfType<Panel>()
                .FirstOrDefault(p => p.Location.X == 20 && p.Location.Y == 340);

            if (quickActionsPanel != null)
            {
                var buttons = quickActionsPanel.Controls.OfType<Button>().ToList();
                if (buttons.Count >= 5)
                {
                    buttons[0].Text = LocalizationManager.GetString("add_client");
                    buttons[1].Text = LocalizationManager.GetString("add_service");
                    buttons[2].Text = LocalizationManager.GetString("generate_report");
                    buttons[3].Text = LocalizationManager.GetString("upcoming_payments");
                    buttons[4].Text = LocalizationManager.GetString("delayed_payments");
                }
            }
        }

        private void ChangePasswordMenu_Click(object sender, EventArgs e)
        {
            var changePasswordForm = new ChangePasswordForm(dbManager);
            changePasswordForm.ShowDialog();
        }

        private void BackupMenu_Click(object sender, EventArgs e)
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Database files (*.db)|*.db|All files (*.*)|*.*";
                saveDialog.DefaultExt = "db";
                saveDialog.FileName = $"BillPilot_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        dbManager.BackupDatabase(saveDialog.FileName);
                        MessageBox.Show(LocalizationManager.GetString("backup_success"),
                            LocalizationManager.GetString("success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LogManager.LogInfo($"Database backed up to: {saveDialog.FileName}");
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError("Backup failed", ex);
                        MessageBox.Show(ex.Message, LocalizationManager.GetString("error"),
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void RestoreMenu_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Restoring a database will replace all current data. Are you sure you want to continue?",
                LocalizationManager.GetString("warning"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Database files (*.db)|*.db|All files (*.*)|*.*";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        dbManager.RestoreDatabase(openDialog.FileName);
                        MessageBox.Show(LocalizationManager.GetString("restore_success"),
                            LocalizationManager.GetString("success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LogManager.LogInfo($"Database restored from: {openDialog.FileName}");
                        LoadDashboardData();
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError("Restore failed", ex);
                        MessageBox.Show(ex.Message, LocalizationManager.GetString("error"),
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            SessionManager.EndSession();
            refreshTimer?.Dispose();
        }
    }

    // ===================== CLIENT EDIT FORM =====================
    public partial class ClientEditForm : Form
    {
        private DatabaseManager dbManager;
        private ClientManager clientManager;
        private Client client;
        private bool isEdit;

        private TextBox txtFirstName, txtLastName, txtBusinessName, txtVATNumber;
        private TextBox txtEmail, txtPhone, txtAddress, txtTags, txtNotes;
        private NumericUpDown numCreditLimit, numPaymentTerms;
        private ComboBox cboCategory;
        private Button btnSave, btnCancel, btnViewHistory, btnManageServices;

        public ClientEditForm(DatabaseManager dbManager, Client client)
        {
            this.dbManager = dbManager;
            this.clientManager = new ClientManager(dbManager);
            this.client = client;
            this.isEdit = client != null;

            InitializeComponent();
            LoadClientData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(600, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Text = isEdit ? LocalizationManager.GetString("edit_client") : LocalizationManager.GetString("add_client");

            int yPos = 20;
            int labelWidth = 120;
            int textBoxWidth = 400;

            // First Name
            var lblFirstName = new Label();
            lblFirstName.Text = LocalizationManager.GetString("first_name") + ":";
            lblFirstName.Location = new Point(20, yPos);
            lblFirstName.Size = new Size(labelWidth, 20);
            txtFirstName = new TextBox();
            txtFirstName.Location = new Point(150, yPos);
            txtFirstName.Size = new Size(textBoxWidth, 20);
            yPos += 30;

            // Last Name
            var lblLastName = new Label();
            lblLastName.Text = LocalizationManager.GetString("last_name") + ":";
            lblLastName.Location = new Point(20, yPos);
            lblLastName.Size = new Size(labelWidth, 20);
            txtLastName = new TextBox();
            txtLastName.Location = new Point(150, yPos);
            txtLastName.Size = new Size(textBoxWidth, 20);
            yPos += 30;

            // Business Name
            var lblBusinessName = new Label();
            lblBusinessName.Text = LocalizationManager.GetString("business_name") + ":";
            lblBusinessName.Location = new Point(20, yPos);
            lblBusinessName.Size = new Size(labelWidth, 20);
            txtBusinessName = new TextBox();
            txtBusinessName.Location = new Point(150, yPos);
            txtBusinessName.Size = new Size(textBoxWidth, 20);
            yPos += 30;

            // VAT Number
            var lblVATNumber = new Label();
            lblVATNumber.Text = LocalizationManager.GetString("vat_number") + ":";
            lblVATNumber.Location = new Point(20, yPos);
            lblVATNumber.Size = new Size(labelWidth, 20);
            txtVATNumber = new TextBox();
            txtVATNumber.Location = new Point(150, yPos);
            txtVATNumber.Size = new Size(textBoxWidth, 20);
            yPos += 30;

            // Email
            var lblEmail = new Label();
            lblEmail.Text = LocalizationManager.GetString("email") + ":";
            lblEmail.Location = new Point(20, yPos);
            lblEmail.Size = new Size(labelWidth, 20);
            txtEmail = new TextBox();
            txtEmail.Location = new Point(150, yPos);
            txtEmail.Size = new Size(textBoxWidth, 20);
            yPos += 30;

            // Phone
            var lblPhone = new Label();
            lblPhone.Text = LocalizationManager.GetString("phone") + ":";
            lblPhone.Location = new Point(20, yPos);
            lblPhone.Size = new Size(labelWidth, 20);
            txtPhone = new TextBox();
            txtPhone.Location = new Point(150, yPos);
            txtPhone.Size = new Size(textBoxWidth, 20);
            yPos += 30;

            // Address
            var lblAddress = new Label();
            lblAddress.Text = LocalizationManager.GetString("address") + ":";
            lblAddress.Location = new Point(20, yPos);
            lblAddress.Size = new Size(labelWidth, 20);
            txtAddress = new TextBox();
            txtAddress.Location = new Point(150, yPos);
            txtAddress.Size = new Size(textBoxWidth, 60);
            txtAddress.Multiline = true;
            txtAddress.ScrollBars = ScrollBars.Vertical;
            yPos += 70;

            // Category
            var lblCategory = new Label();
            lblCategory.Text = LocalizationManager.GetString("category") + ":";
            lblCategory.Location = new Point(20, yPos);
            lblCategory.Size = new Size(labelWidth, 20);
            cboCategory = new ComboBox();
            cboCategory.Location = new Point(150, yPos);
            cboCategory.Size = new Size(200, 20);
            cboCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cboCategory.Items.AddRange(new string[] { "Regular", "VIP", "New", "Inactive" });
            cboCategory.SelectedIndex = 0;
            yPos += 30;

            // Tags
            var lblTags = new Label();
            lblTags.Text = LocalizationManager.GetString("tags") + ":";
            lblTags.Location = new Point(20, yPos);
            lblTags.Size = new Size(labelWidth, 20);
            txtTags = new TextBox();
            txtTags.Location = new Point(150, yPos);
            txtTags.Size = new Size(textBoxWidth, 20);
            yPos += 30;

            // Credit Limit
            var lblCreditLimit = new Label();
            lblCreditLimit.Text = LocalizationManager.GetString("credit_limit") + ":";
            lblCreditLimit.Location = new Point(20, yPos);
            lblCreditLimit.Size = new Size(labelWidth, 20);
            numCreditLimit = new NumericUpDown();
            numCreditLimit.Location = new Point(150, yPos);
            numCreditLimit.Size = new Size(120, 20);
            numCreditLimit.Maximum = 999999;
            numCreditLimit.DecimalPlaces = 2;
            numCreditLimit.ThousandsSeparator = true;
            yPos += 30;

            // Payment Terms
            var lblPaymentTerms = new Label();
            lblPaymentTerms.Text = LocalizationManager.GetString("payment_terms") + ":";
            lblPaymentTerms.Location = new Point(20, yPos);
            lblPaymentTerms.Size = new Size(labelWidth, 20);
            numPaymentTerms = new NumericUpDown();
            numPaymentTerms.Location = new Point(150, yPos);
            numPaymentTerms.Size = new Size(120, 20);
            numPaymentTerms.Maximum = 365;
            numPaymentTerms.Value = 30;
            yPos += 30;

            // Notes
            var lblNotes = new Label();
            lblNotes.Text = LocalizationManager.GetString("notes") + ":";
            lblNotes.Location = new Point(20, yPos);
            lblNotes.Size = new Size(labelWidth, 20);
            txtNotes = new TextBox();
            txtNotes.Location = new Point(150, yPos);
            txtNotes.Size = new Size(textBoxWidth, 60);
            txtNotes.Multiline = true;
            txtNotes.ScrollBars = ScrollBars.Vertical;
            yPos += 70;

            // Buttons
            if (isEdit)
            {
                btnViewHistory = new Button();
                btnViewHistory.Text = LocalizationManager.GetString("contact_history");
                btnViewHistory.Location = new Point(20, yPos);
                btnViewHistory.Size = new Size(120, 30);
                btnViewHistory.UseVisualStyleBackColor = true;
                btnViewHistory.Click += BtnViewHistory_Click;

                btnManageServices = new Button();
                btnManageServices.Text = LocalizationManager.GetString("manage_services");
                btnManageServices.Location = new Point(150, yPos);
                btnManageServices.Size = new Size(120, 30);
                btnManageServices.UseVisualStyleBackColor = true;
                btnManageServices.Click += BtnManageServices_Click;
            }

            btnSave = new Button();
            btnSave.Text = LocalizationManager.GetString("save");
            btnSave.Location = new Point(370, yPos);
            btnSave.Size = new Size(80, 30);
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button();
            btnCancel.Text = LocalizationManager.GetString("cancel");
            btnCancel.Location = new Point(460, yPos);
            btnCancel.Size = new Size(80, 30);
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] {
                lblFirstName, txtFirstName, lblLastName, txtLastName,
                lblBusinessName, txtBusinessName, lblVATNumber, txtVATNumber,
                lblEmail, txtEmail, lblPhone, txtPhone, lblAddress, txtAddress,
                lblCategory, cboCategory, lblTags, txtTags,
                lblCreditLimit, numCreditLimit, lblPaymentTerms, numPaymentTerms,
                lblNotes, txtNotes
            });

            if (isEdit)
            {
                this.Controls.Add(btnViewHistory);
                this.Controls.Add(btnManageServices);
            }

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
        }

        private void LoadClientData()
        {
            if (isEdit && client != null)
            {
                txtFirstName.Text = client.FirstName;
                txtLastName.Text = client.LastName;
                txtBusinessName.Text = client.BusinessName;
                txtVATNumber.Text = client.VATNumber;
                txtEmail.Text = client.Email;
                txtPhone.Text = client.Phone;
                txtAddress.Text = client.Address;
                cboCategory.Text = client.Category;
                txtTags.Text = client.Tags;
                numCreditLimit.Value = client.CreditLimit;
                numPaymentTerms.Value = client.PaymentTermsDays;
                txtNotes.Text = client.Notes;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (ValidateForm())
            {
                var clientToSave = new Client
                {
                    Id = isEdit ? client.Id : 0,
                    FirstName = txtFirstName.Text.Trim(),
                    LastName = txtLastName.Text.Trim(),
                    BusinessName = txtBusinessName.Text.Trim(),
                    VATNumber = txtVATNumber.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Phone = txtPhone.Text.Trim(),
                    Address = txtAddress.Text.Trim(),
                    Category = cboCategory.Text,
                    Tags = txtTags.Text.Trim(),
                    CreditLimit = numCreditLimit.Value,
                    PaymentTermsDays = (int)numPaymentTerms.Value,
                    Notes = txtNotes.Text.Trim()
                };

                try
                {
                    if (isEdit)
                    {
                        clientManager.UpdateClient(clientToSave);
                        LogManager.LogInfo($"Client updated: {clientToSave.FullName}");
                    }
                    else
                    {
                        clientManager.CreateClient(clientToSave);
                        LogManager.LogInfo($"Client created: {clientToSave.FullName}");
                    }

                    this.DialogResult = DialogResult.OK;
                }
                catch (Exception ex)
                {
                    LogManager.LogError("Error saving client", ex);
                    MessageBox.Show(ex.Message, LocalizationManager.GetString("error"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                MessageBox.Show(LocalizationManager.GetString("first_name") + " " + LocalizationManager.GetString("required_field"),
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtFirstName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                MessageBox.Show(LocalizationManager.GetString("last_name") + " " + LocalizationManager.GetString("required_field"),
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtLastName.Focus();
                return false;
            }

            if (!ValidationHelper.IsValidEmail(txtEmail.Text))
            {
                MessageBox.Show(LocalizationManager.GetString("invalid_email"),
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtEmail.Focus();
                return false;
            }

            if (!ValidationHelper.IsValidPhone(txtPhone.Text))
            {
                MessageBox.Show(LocalizationManager.GetString("invalid_phone"),
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPhone.Focus();
                return false;
            }

            return true;
        }

        private void BtnViewHistory_Click(object sender, EventArgs e)
        {
            var historyForm = new ContactHistoryForm(dbManager, client.Id);
            historyForm.ShowDialog();
        }

        private void BtnManageServices_Click(object sender, EventArgs e)
        {
            var servicesForm = new ClientServicesForm(dbManager, client.Id);
            servicesForm.ShowDialog();
        }
    }

    // ===================== SERVICE EDIT FORM =====================
    public partial class ServiceEditForm : Form
    {
        private DatabaseManager dbManager;
        private ServiceManager serviceManager;
        private Service service;
        private bool isEdit;

        private TextBox txtName, txtDescription;
        private NumericUpDown numBasePrice;
        private Button btnSave, btnCancel;

        public ServiceEditForm(DatabaseManager dbManager, Service service)
        {
            this.dbManager = dbManager;
            this.serviceManager = new ServiceManager(dbManager);
            this.service = service;
            this.isEdit = service != null;

            InitializeComponent();
            LoadServiceData();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(500, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Text = isEdit ? LocalizationManager.GetString("edit_service") : LocalizationManager.GetString("add_service");

            int yPos = 20;
            int labelWidth = 100;
            int textBoxWidth = 320;

            // Service Name
            var lblName = new Label();
            lblName.Text = LocalizationManager.GetString("service_name") + ":";
            lblName.Location = new Point(20, yPos);
            lblName.Size = new Size(labelWidth, 20);
            txtName = new TextBox();
            txtName.Location = new Point(130, yPos);
            txtName.Size = new Size(textBoxWidth, 20);
            yPos += 35;

            // Description
            var lblDescription = new Label();
            lblDescription.Text = LocalizationManager.GetString("description") + ":";
            lblDescription.Location = new Point(20, yPos);
            lblDescription.Size = new Size(labelWidth, 20);
            txtDescription = new TextBox();
            txtDescription.Location = new Point(130, yPos);
            txtDescription.Size = new Size(textBoxWidth, 80);
            txtDescription.Multiline = true;
            txtDescription.ScrollBars = ScrollBars.Vertical;
            yPos += 95;

            // Base Price
            var lblBasePrice = new Label();
            lblBasePrice.Text = LocalizationManager.GetString("base_price") + " (€):";
            lblBasePrice.Location = new Point(20, yPos);
            lblBasePrice.Size = new Size(labelWidth, 20);
            numBasePrice = new NumericUpDown();
            numBasePrice.Location = new Point(130, yPos);
            numBasePrice.Size = new Size(120, 20);
            numBasePrice.Maximum = 999999;
            numBasePrice.DecimalPlaces = 2;
            numBasePrice.ThousandsSeparator = true;
            yPos += 50;

            // Buttons
            btnSave = new Button();
            btnSave.Text = LocalizationManager.GetString("save");
            btnSave.Location = new Point(290, yPos);
            btnSave.Size = new Size(80, 30);
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button();
            btnCancel.Text = LocalizationManager.GetString("cancel");
            btnCancel.Location = new Point(380, yPos);
            btnCancel.Size = new Size(80, 30);
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] {
                lblName, txtName, lblDescription, txtDescription, lblBasePrice, numBasePrice,
                btnSave, btnCancel
            });
        }

        private void LoadServiceData()
        {
            if (isEdit && service != null)
            {
                txtName.Text = service.Name;
                txtDescription.Text = service.Description;
                numBasePrice.Value = service.BasePrice;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (ValidateForm())
            {
                var serviceToSave = new Service
                {
                    Id = isEdit ? service.Id : 0,
                    Name = txtName.Text.Trim(),
                    Description = txtDescription.Text.Trim(),
                    BasePrice = numBasePrice.Value
                };

                try
                {
                    if (isEdit)
                    {
                        serviceManager.UpdateService(serviceToSave);
                        LogManager.LogInfo($"Service updated: {serviceToSave.Name}");
                    }
                    else
                    {
                        serviceManager.CreateService(serviceToSave);
                        LogManager.LogInfo($"Service created: {serviceToSave.Name}");
                    }

                    this.DialogResult = DialogResult.OK;
                }
                catch (Exception ex)
                {
                    LogManager.LogError("Error saving service", ex);
                    MessageBox.Show(ex.Message, LocalizationManager.GetString("error"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(LocalizationManager.GetString("service_name") + " " + LocalizationManager.GetString("required_field"),
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtName.Focus();
                return false;
            }

            if (numBasePrice.Value <= 0)
            {
                MessageBox.Show("Base price must be greater than 0.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                numBasePrice.Focus();
                return false;
            }

            return true;
        }
    }

    // ===================== CLIENT LIST CONTROL =====================
    public partial class ClientListControl : UserControl
    {
        private DatabaseManager dbManager;
        private ClientManager clientManager;
        private DataGridView clientsGrid;
        private Button addButton, editButton, deleteButton, refreshButton;
        private TextBox searchBox;
        private ComboBox searchTypeCombo;
        private Label searchLabel, searchTypeLabel;
        private List<Client> allClients;

        public ClientListControl(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
            this.clientManager = new ClientManager(dbManager);
            InitializeComponent();
            LoadClients();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(248, 249, 250);

            // Search controls
            searchLabel = new Label();
            searchLabel.Location = new Point(20, 20);
            searchLabel.Size = new Size(60, 20);
            searchLabel.Text = LocalizationManager.GetString("search") + ":";

            searchBox = new TextBox();
            searchBox.Location = new Point(90, 18);
            searchBox.Size = new Size(200, 20);
            searchBox.TextChanged += (s, e) => PerformSearch();

            searchTypeLabel = new Label();
            searchTypeLabel.Location = new Point(300, 20);
            searchTypeLabel.Size = new Size(80, 20);
            searchTypeLabel.Text = LocalizationManager.GetString("search_by") + ":";

            searchTypeCombo = new ComboBox();
            searchTypeCombo.Location = new Point(390, 18);
            searchTypeCombo.Size = new Size(120, 20);
            searchTypeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            searchTypeCombo.Items.AddRange(new string[] {
                LocalizationManager.GetString("all_fields"),
                LocalizationManager.GetString("first_name"),
                LocalizationManager.GetString("last_name"),
                LocalizationManager.GetString("business_name"),
                LocalizationManager.GetString("vat_number")
            });
            searchTypeCombo.SelectedIndex = 0;
            searchTypeCombo.SelectedIndexChanged += (s, e) => PerformSearch();

            // Buttons
            addButton = new Button();
            addButton.Location = new Point(530, 15);
            addButton.Size = new Size(100, 30);
            addButton.Text = LocalizationManager.GetString("add_client");
            addButton.UseVisualStyleBackColor = true;
            addButton.Click += AddButton_Click;

            editButton = new Button();
            editButton.Location = new Point(640, 15);
            editButton.Size = new Size(100, 30);
            editButton.Text = LocalizationManager.GetString("edit_client");
            editButton.UseVisualStyleBackColor = true;
            editButton.Click += EditButton_Click;

            deleteButton = new Button();
            deleteButton.Location = new Point(750, 15);
            deleteButton.Size = new Size(100, 30);
            deleteButton.Text = LocalizationManager.GetString("delete");
            deleteButton.UseVisualStyleBackColor = true;
            deleteButton.Click += DeleteButton_Click;

            refreshButton = new Button();
            refreshButton.Location = new Point(860, 15);
            refreshButton.Size = new Size(100, 30);
            refreshButton.Text = LocalizationManager.GetString("refresh");
            refreshButton.UseVisualStyleBackColor = true;
            refreshButton.Click += (s, e) => LoadClients();

            // DataGridView
            clientsGrid = new DataGridView();
            clientsGrid.Location = new Point(20, 60);
            clientsGrid.Size = new Size(this.Width - 40, this.Height - 80);
            clientsGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            clientsGrid.AutoGenerateColumns = false;
            clientsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            clientsGrid.MultiSelect = false;
            clientsGrid.ReadOnly = true;
            clientsGrid.AllowUserToAddRows = false;
            clientsGrid.AllowUserToResizeRows = false;
            clientsGrid.RowHeadersVisible = false;
            clientsGrid.BackgroundColor = Color.White;
            clientsGrid.BorderStyle = BorderStyle.Fixed3D;
            clientsGrid.CellDoubleClick += (sender, e) => {
                if (e.RowIndex >= 0) EditButton_Click(sender, e);
            };

            // Add columns
            clientsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "ID", Visible = false });
            clientsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FirstName", HeaderText = LocalizationManager.GetString("first_name"), Width = 120 });
            clientsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LastName", HeaderText = LocalizationManager.GetString("last_name"), Width = 120 });
            clientsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "BusinessName", HeaderText = LocalizationManager.GetString("business_name"), Width = 150 });
            clientsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "VATNumber", HeaderText = LocalizationManager.GetString("vat_number"), Width = 120 });
            clientsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Email", HeaderText = LocalizationManager.GetString("email"), Width = 150 });
            clientsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Phone", HeaderText = LocalizationManager.GetString("phone"), Width = 120 });
            clientsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Balance", HeaderText = LocalizationManager.GetString("balance"), Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });

            // Style the grid
            clientsGrid.EnableHeadersVisualStyles = false;
            clientsGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 144, 220);
            clientsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            clientsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Bold);
            clientsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);

            this.Controls.AddRange(new Control[] {
                searchLabel, searchBox, searchTypeLabel, searchTypeCombo,
                addButton, editButton, deleteButton, refreshButton, clientsGrid
            });
        }

        public void UpdateUILanguage()
        {
            // Update labels
            searchLabel.Text = LocalizationManager.GetString("search") + ":";
            searchTypeLabel.Text = LocalizationManager.GetString("search_by") + ":";

            // Update search type combo items
            searchTypeCombo.Items.Clear();
            searchTypeCombo.Items.AddRange(new string[] {
                LocalizationManager.GetString("all_fields"),
                LocalizationManager.GetString("first_name"),
                LocalizationManager.GetString("last_name"),
                LocalizationManager.GetString("business_name"),
                LocalizationManager.GetString("vat_number")
            });
            searchTypeCombo.SelectedIndex = 0;

            // Update buttons
            addButton.Text = LocalizationManager.GetString("add_client");
            editButton.Text = LocalizationManager.GetString("edit_client");
            deleteButton.Text = LocalizationManager.GetString("delete");
            refreshButton.Text = LocalizationManager.GetString("refresh");

            // Update grid column headers
            clientsGrid.Columns[1].HeaderText = LocalizationManager.GetString("first_name");
            clientsGrid.Columns[2].HeaderText = LocalizationManager.GetString("last_name");
            clientsGrid.Columns[3].HeaderText = LocalizationManager.GetString("business_name");
            clientsGrid.Columns[4].HeaderText = LocalizationManager.GetString("vat_number");
            clientsGrid.Columns[5].HeaderText = LocalizationManager.GetString("email");
            clientsGrid.Columns[6].HeaderText = LocalizationManager.GetString("phone");
            clientsGrid.Columns[7].HeaderText = LocalizationManager.GetString("balance");
        }

        private void LoadClients()
        {
            try
            {
                allClients = clientManager.GetAllClients();
                clientsGrid.DataSource = allClients;
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error loading clients", ex);
                MessageBox.Show("Error loading clients. Please check the log for details.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PerformSearch()
        {
            var searchTerm = searchBox.Text.ToLower().Trim();
            var searchType = searchTypeCombo.SelectedIndex;

            if (string.IsNullOrEmpty(searchTerm))
            {
                clientsGrid.DataSource = allClients;
                return;
            }

            List<Client> filteredClients;

            switch (searchType)
            {
                case 0: // All Fields
                    filteredClients = allClients.Where(c =>
                        c.FirstName.ToLower().Contains(searchTerm) ||
                        c.LastName.ToLower().Contains(searchTerm) ||
                        (c.BusinessName ?? "").ToLower().Contains(searchTerm) ||
                        (c.VATNumber ?? "").ToLower().Contains(searchTerm) ||
                        (c.Email ?? "").ToLower().Contains(searchTerm) ||
                        (c.Phone ?? "").ToLower().Contains(searchTerm)).ToList();
                    break;
                case 1: // First Name
                    filteredClients = allClients.Where(c =>
                        c.FirstName.ToLower().Contains(searchTerm)).ToList();
                    break;
                case 2: // Last Name
                    filteredClients = allClients.Where(c =>
                        c.LastName.ToLower().Contains(searchTerm)).ToList();
                    break;
                case 3: // Business Name
                    filteredClients = allClients.Where(c =>
                        (c.BusinessName ?? "").ToLower().Contains(searchTerm)).ToList();
                    break;
                case 4: // VAT Number
                    filteredClients = allClients.Where(c =>
                        (c.VATNumber ?? "").ToLower().Contains(searchTerm)).ToList();
                    break;
                default:
                    filteredClients = allClients;
                    break;
            }

            clientsGrid.DataSource = filteredClients;
        }

        public void ShowAddClientDialog()
        {
            AddButton_Click(this, EventArgs.Empty);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            var addForm = new ClientEditForm(dbManager, null);
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadClients();
            }
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            if (clientsGrid.SelectedRows.Count > 0)
            {
                var selectedClient = (Client)clientsGrid.SelectedRows[0].DataBoundItem;
                var editForm = new ClientEditForm(dbManager, selectedClient);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    LoadClients();
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
            if (clientsGrid.SelectedRows.Count > 0)
            {
                var result = MessageBox.Show(LocalizationManager.GetString("confirm_delete"),
                    LocalizationManager.GetString("warning"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var selectedClient = (Client)clientsGrid.SelectedRows[0].DataBoundItem;
                    try
                    {
                        clientManager.DeleteClient(selectedClient.Id);
                        LoadClients();
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError("Error deleting client", ex);
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

    // ===================== SERVICE LIST CONTROL =====================
    public partial class ServiceListControl : UserControl
    {
        private DatabaseManager dbManager;
        private ServiceManager serviceManager;
        private DataGridView servicesGrid;
        private Button addButton, editButton, deleteButton, refreshButton;
        private TextBox searchBox;
        private Label searchLabel;
        private List<Service> allServices;

        public ServiceListControl(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
            this.serviceManager = new ServiceManager(dbManager);
            InitializeComponent();
            LoadServices();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(248, 249, 250);

            // Search controls
            searchLabel = new Label();
            searchLabel.Location = new Point(20, 20);
            searchLabel.Size = new Size(60, 20);
            searchLabel.Text = LocalizationManager.GetString("search") + ":";

            searchBox = new TextBox();
            searchBox.Location = new Point(90, 18);
            searchBox.Size = new Size(300, 20);
            searchBox.TextChanged += (s, e) => PerformSearch();

            // Buttons
            addButton = new Button();
            addButton.Location = new Point(410, 15);
            addButton.Size = new Size(110, 30);
            addButton.Text = LocalizationManager.GetString("add_service");
            addButton.UseVisualStyleBackColor = true;
            addButton.Click += AddButton_Click;

            editButton = new Button();
            editButton.Location = new Point(530, 15);
            editButton.Size = new Size(110, 30);
            editButton.Text = LocalizationManager.GetString("edit_service");
            editButton.UseVisualStyleBackColor = true;
            editButton.Click += EditButton_Click;

            deleteButton = new Button();
            deleteButton.Location = new Point(650, 15);
            deleteButton.Size = new Size(100, 30);
            deleteButton.Text = LocalizationManager.GetString("delete");
            deleteButton.UseVisualStyleBackColor = true;
            deleteButton.Click += DeleteButton_Click;

            refreshButton = new Button();
            refreshButton.Location = new Point(760, 15);
            refreshButton.Size = new Size(100, 30);
            refreshButton.Text = LocalizationManager.GetString("refresh");
            refreshButton.UseVisualStyleBackColor = true;
            refreshButton.Click += (s, e) => LoadServices();

            // DataGridView
            servicesGrid = new DataGridView();
            servicesGrid.Location = new Point(20, 60);
            servicesGrid.Size = new Size(this.Width - 40, this.Height - 80);
            servicesGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            servicesGrid.AutoGenerateColumns = false;
            servicesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            servicesGrid.MultiSelect = false;
            servicesGrid.ReadOnly = true;
            servicesGrid.AllowUserToAddRows = false;
            servicesGrid.AllowUserToResizeRows = false;
            servicesGrid.RowHeadersVisible = false;
            servicesGrid.BackgroundColor = Color.White;
            servicesGrid.BorderStyle = BorderStyle.Fixed3D;
            servicesGrid.CellDoubleClick += (sender, e) => {
                if (e.RowIndex >= 0) EditButton_Click(sender, e);
            };

            // Add columns
            servicesGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "ID", Visible = false });
            servicesGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = LocalizationManager.GetString("service_name"), Width = 200 });
            servicesGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Description", HeaderText = LocalizationManager.GetString("description"), Width = 350 });
            servicesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "BasePrice",
                HeaderText = LocalizationManager.GetString("base_price"),
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
            });

            // Style the grid
            servicesGrid.EnableHeadersVisualStyles = false;
            servicesGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(92, 184, 92);
            servicesGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            servicesGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Bold);
            servicesGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);

            this.Controls.AddRange(new Control[] {
                searchLabel, searchBox,
                addButton, editButton, deleteButton, refreshButton, servicesGrid
            });
        }

        public void UpdateUILanguage()
        {
            // Update labels
            searchLabel.Text = LocalizationManager.GetString("search") + ":";

            // Update buttons
            addButton.Text = LocalizationManager.GetString("add_service");
            editButton.Text = LocalizationManager.GetString("edit_service");
            deleteButton.Text = LocalizationManager.GetString("delete");
            refreshButton.Text = LocalizationManager.GetString("refresh");

            // Update grid column headers
            servicesGrid.Columns[1].HeaderText = LocalizationManager.GetString("service_name");
            servicesGrid.Columns[2].HeaderText = LocalizationManager.GetString("description");
            servicesGrid.Columns[3].HeaderText = LocalizationManager.GetString("base_price");
        }

        private void LoadServices()
        {
            try
            {
                allServices = serviceManager.GetAllServices();
                servicesGrid.DataSource = allServices;
            }
            catch (Exception ex)
            {
                LogManager.LogError("Error loading services", ex);
                MessageBox.Show("Error loading services. Please check the log for details.",
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PerformSearch()
        {
            var searchTerm = searchBox.Text.ToLower().Trim();

            if (string.IsNullOrEmpty(searchTerm))
            {
                servicesGrid.DataSource = allServices;
                return;
            }

            var filteredServices = allServices.Where(s =>
                s.Name.ToLower().Contains(searchTerm) ||
                (s.Description ?? "").ToLower().Contains(searchTerm)).ToList();

            servicesGrid.DataSource = filteredServices;
        }

        public void ShowAddServiceDialog()
        {
            AddButton_Click(this, EventArgs.Empty);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            var addForm = new ServiceEditForm(dbManager, null);
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadServices();
            }
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            if (servicesGrid.SelectedRows.Count > 0)
            {
                var selectedService = (Service)servicesGrid.SelectedRows[0].DataBoundItem;
                var editForm = new ServiceEditForm(dbManager, selectedService);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    LoadServices();
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
                    var selectedService = (Service)servicesGrid.SelectedRows[0].DataBoundItem;
                    try
                    {
                        serviceManager.DeleteService(selectedService.Id);
                        LoadServices();
                    }
                    catch (Exception ex)
                    {
                        LogManager.LogError("Error deleting service", ex);
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

            // Password requirements info
            var lblInfo = new Label();
            lblInfo.Text = LocalizationManager.GetString("password_requirements");
            lblInfo.Location = new Point(20, yPos);
            lblInfo.Size = new Size(350, 20);
            lblInfo.Font = new Font("Arial", 8, FontStyle.Italic);
            yPos += 35;

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
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] {
                lblOldPassword, txtOldPassword,
                lblNewPassword, txtNewPassword,
                lblConfirmPassword, txtConfirmPassword,
                lblInfo, btnSave, btnCancel
            });

            if (isFirstLogin)
            {
                lblOldPassword.Text = "Default Password:";
                txtOldPassword.Text = "admin123";
                txtOldPassword.ReadOnly = true;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Validate new password
            if (!ValidationHelper.IsValidPassword(txtNewPassword.Text))
            {
                MessageBox.Show(LocalizationManager.GetString("password_requirements"),
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtNewPassword.Focus();
                return;
            }

            // Check passwords match
            if (txtNewPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show(LocalizationManager.GetString("passwords_not_match"),
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtConfirmPassword.Focus();
                return;
            }

            // Change password
            var userManager = new UserManager(dbManager);
            if (userManager.ChangePassword(SessionManager.CurrentUser, txtOldPassword.Text, txtNewPassword.Text))
            {
                MessageBox.Show(LocalizationManager.GetString("password_changed"),
                    LocalizationManager.GetString("success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show(LocalizationManager.GetString("invalid_old_password"),
                    LocalizationManager.GetString("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtOldPassword.Focus();
            }
        }
    }
}