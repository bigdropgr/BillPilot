using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Net.Mail;

namespace BillPilot
{
    // ===================== DATABASE MANAGER =====================
    public class DatabaseManager
    {
        private string connectionString;
        private readonly string dbPath;
        private static readonly object dbLock = new object();

        public DatabaseManager()
        {
            dbPath = Path.Combine(Application.StartupPath, "billpilot_data.db");
            connectionString = $"Data Source={dbPath};Version=3;Journal Mode=WAL;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            lock (dbLock)
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    var commands = new[]
                    {
                        @"CREATE TABLE IF NOT EXISTS Users (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Username TEXT UNIQUE NOT NULL,
                            PasswordHash TEXT NOT NULL,
                            Salt TEXT NOT NULL,
                            CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                            IsActive BOOLEAN DEFAULT 1,
                            IsFirstLogin BOOLEAN DEFAULT 1
                        )",

                        @"CREATE TABLE IF NOT EXISTS Clients (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            FirstName TEXT NOT NULL,
                            LastName TEXT NOT NULL,
                            BusinessName TEXT,
                            VATNumber TEXT,
                            Email TEXT,
                            Phone TEXT,
                            Address TEXT,
                            Category TEXT DEFAULT 'Regular',
                            Tags TEXT,
                            CreditLimit DECIMAL(10,2) DEFAULT 0,
                            PaymentTermsDays INTEGER DEFAULT 30,
                            Notes TEXT,
                            Balance DECIMAL(10,2) DEFAULT 0,
                            CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                            IsActive BOOLEAN DEFAULT 1
                        )",

                        @"CREATE TABLE IF NOT EXISTS Services (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            Description TEXT,
                            BasePrice DECIMAL(10,2) NOT NULL,
                            CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                            IsActive BOOLEAN DEFAULT 1
                        )",

                        @"CREATE TABLE IF NOT EXISTS ClientServices (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ClientId INTEGER NOT NULL,
                            ServiceId INTEGER NOT NULL,
                            ServiceType TEXT NOT NULL,
                            CustomPrice DECIMAL(10,2),
                            Period TEXT,
                            ChargeDay INTEGER,
                            StartDate DATE NOT NULL,
                            EndDate DATE,
                            LastPaidDate DATE,
                            NextPaymentDate DATE,
                            IsActive BOOLEAN DEFAULT 1,
                            CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY (ClientId) REFERENCES Clients(Id),
                            FOREIGN KEY (ServiceId) REFERENCES Services(Id)
                        )",

                        @"CREATE TABLE IF NOT EXISTS Payments (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ClientId INTEGER NOT NULL,
                            ServiceId INTEGER,
                            ClientServiceId INTEGER,
                            PaymentType TEXT NOT NULL,
                            DueDate DATE NOT NULL,
                            PaidDate DATE,
                            Amount DECIMAL(10,2) NOT NULL,
                            IsPaid BOOLEAN DEFAULT 0,
                            IsOverdue BOOLEAN DEFAULT 0,
                            PaymentMethod TEXT,
                            Reference TEXT,
                            Notes TEXT,
                            CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                            CreatedBy TEXT,
                            FOREIGN KEY (ClientId) REFERENCES Clients(Id),
                            FOREIGN KEY (ServiceId) REFERENCES Services(Id),
                            FOREIGN KEY (ClientServiceId) REFERENCES ClientServices(Id)
                        )",

                        @"CREATE TABLE IF NOT EXISTS ContactHistory (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ClientId INTEGER NOT NULL,
                            ContactDate DATETIME NOT NULL,
                            ContactType TEXT NOT NULL,
                            Notes TEXT,
                            CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                            CreatedBy TEXT,
                            FOREIGN KEY (ClientId) REFERENCES Clients(Id)
                        )"
                    };

                    foreach (var command in commands)
                    {
                        using (var cmd = new SQLiteCommand(command, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    CreateDefaultAdmin(connection);
                }
            }
        }

        private void CreateDefaultAdmin(SQLiteConnection connection)
        {
            using (var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM Users", connection))
            {
                var userCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (userCount == 0)
                {
                    var salt = CryptoHelper.GenerateSalt();
                    var hashedPassword = CryptoHelper.HashPassword("admin123", salt);

                    using (var insertCmd = new SQLiteCommand(
                        "INSERT INTO Users (Username, PasswordHash, Salt, IsFirstLogin) VALUES (@username, @password, @salt, 1)",
                        connection))
                    {
                        insertCmd.Parameters.AddWithValue("@username", "admin");
                        insertCmd.Parameters.AddWithValue("@password", hashedPassword);
                        insertCmd.Parameters.AddWithValue("@salt", salt);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(connectionString);
        }

        public void BackupDatabase(string backupPath)
        {
            lock (dbLock)
            {
                try
                {
                    File.Copy(dbPath, backupPath, true);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to backup database: {ex.Message}");
                }
            }
        }

        public void RestoreDatabase(string backupPath)
        {
            lock (dbLock)
            {
                try
                {
                    if (File.Exists(backupPath))
                    {
                        File.Copy(backupPath, dbPath, true);
                    }
                    else
                    {
                        throw new FileNotFoundException("Backup file not found.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to restore database: {ex.Message}");
                }
            }
        }
    }

    // ===================== DATA MODELS =====================
    public class Client
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string BusinessName { get; set; }
        public string VATNumber { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Category { get; set; }
        public string Tags { get; set; }
        public decimal CreditLimit { get; set; }
        public int PaymentTermsDays { get; set; }
        public string Notes { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }

        public string FullName => $"{FirstName} {LastName}";
        public string DisplayName => string.IsNullOrEmpty(BusinessName) ? FullName : $"{FullName} ({BusinessName})";
    }

    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal BasePrice { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class ClientService
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceType { get; set; }
        public decimal? CustomPrice { get; set; }
        public string Period { get; set; }
        public int? ChargeDay { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? LastPaidDate { get; set; }
        public DateTime? NextPaymentDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        public string ClientName { get; set; }
        public string ServiceName { get; set; }
        public decimal Price => CustomPrice ?? 0;
    }

    public class Payment
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int? ServiceId { get; set; }
        public int? ClientServiceId { get; set; }
        public string PaymentType { get; set; } // "OneOff" or "Periodic"
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
        public bool IsOverdue { get; set; }
        public string PaymentMethod { get; set; }
        public string Reference { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }

        // Navigation properties
        public string ClientName { get; set; }
        public string ServiceName { get; set; }
    }

    public class ContactHistory
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public DateTime ContactDate { get; set; }
        public string ContactType { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsFirstLogin { get; set; }
    }

    // ===================== CRYPTO HELPER =====================
    public static class CryptoHelper
    {
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string password, string salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), 10000))
            {
                byte[] hash = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(hash);
            }
        }

        public static bool VerifyPassword(string password, string salt, string hash)
        {
            string hashedPassword = HashPassword(password, salt);
            return hashedPassword == hash;
        }
    }

    // ===================== VALIDATION HELPER =====================
    public static class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return true; // Email is optional

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return true; // Phone is optional

            // Remove common phone characters
            var cleaned = Regex.Replace(phone, @"[\s\-\(\)\+]", "");
            return Regex.IsMatch(cleaned, @"^\d{7,15}$");
        }

        public static bool IsValidPassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password) && password.Length >= 8;
        }

        public static bool IsValidVATNumber(string vatNumber)
        {
            if (string.IsNullOrWhiteSpace(vatNumber))
                return true; // VAT is optional

            // Basic VAT validation - adjust based on country requirements
            return Regex.IsMatch(vatNumber, @"^[A-Z0-9]{8,12}$");
        }

        public static string GetValidationError(string fieldName, string value, string validationType)
        {
            switch (validationType.ToLower())
            {
                case "required":
                    return string.IsNullOrWhiteSpace(value) ? $"{fieldName} is required." : null;
                case "email":
                    return !IsValidEmail(value) ? $"Please enter a valid email address." : null;
                case "phone":
                    return !IsValidPhone(value) ? $"Please enter a valid phone number." : null;
                case "password":
                    return !IsValidPassword(value) ? $"Password must be at least 8 characters long." : null;
                case "vat":
                    return !IsValidVATNumber(value) ? $"Please enter a valid VAT number." : null;
                default:
                    return null;
            }
        }
    }

    // ===================== LOGGING MANAGER =====================
    public static class LogManager
    {
        private static readonly string logPath = Path.Combine(Application.StartupPath, "BillPilot.log");
        private static readonly object lockObj = new object();

        public static void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public static void LogError(string message, Exception ex = null)
        {
            var fullMessage = ex != null ? $"{message}: {ex.Message}\n{ex.StackTrace}" : message;
            WriteLog("ERROR", fullMessage);
        }

        public static void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        private static void WriteLog(string level, string message)
        {
            lock (lockObj)
            {
                try
                {
                    var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                    File.AppendAllText(logPath, logEntry + Environment.NewLine);
                }
                catch
                {
                    // Fail silently if logging fails
                }
            }
        }
    }

    // ===================== LOCALIZATION MANAGER =====================
    public static class LocalizationManager
    {
        private static Dictionary<string, Dictionary<string, string>> translations;
        private static string currentLanguage = "gr"; // Greek as default

        static LocalizationManager()
        {
            InitializeTranslations();
        }

        private static void InitializeTranslations()
        {
            translations = new Dictionary<string, Dictionary<string, string>>();

            // English translations
            translations["en"] = new Dictionary<string, string>
            {
                {"app_title", "BillPilot - Business Management System"},
                {"login_title", "BillPilot Login"},
                {"username", "Username"},
                {"password", "Password"},
                {"login", "Login"},
                {"logout", "Logout"},
                {"login_failed", "Invalid username or password."},
                {"session_expired", "Session expired. Please login again."},
                {"dashboard", "Dashboard"},
                {"clients", "Clients"},
                {"services", "Services"},
                {"payments", "Payments"},
                {"upcoming_payments", "Upcoming Payments"},
                {"delayed_payments", "Delayed Payments"},
                {"reports", "Reports"},
                {"settings", "Settings"},
                {"language", "Language"},
                {"backup", "Backup Database"},
                {"restore", "Restore Database"},
                {"exit", "Exit"},
                {"add", "Add"},
                {"edit", "Edit"},
                {"delete", "Delete"},
                {"save", "Save"},
                {"cancel", "Cancel"},
                {"refresh", "Refresh"},
                {"search", "Search"},
                {"export", "Export"},
                {"import", "Import"},
                {"print", "Print"},
                {"yes", "Yes"},
                {"no", "No"},
                {"ok", "OK"},
                {"error", "Error"},
                {"warning", "Warning"},
                {"info", "Information"},
                {"success", "Success"},
                {"confirm", "Confirm"},
                {"close", "Close"},
                {"select", "Select"},
                {"all", "All"},
                {"none", "None"},
                {"from", "From"},
                {"to", "To"},
                {"date", "Date"},
                {"time", "Time"},
                {"total", "Total"},
                {"amount", "Amount"},
                {"balance", "Balance"},
                {"paid", "Paid"},
                {"unpaid", "Unpaid"},
                {"overdue", "Overdue"},
                {"pending", "Pending"},
                {"active", "Active"},
                {"inactive", "Inactive"},
                {"enabled", "Enabled"},
                {"disabled", "Disabled"},
                {"first_name", "First Name"},
                {"last_name", "Last Name"},
                {"full_name", "Full Name"},
                {"business_name", "Business Name"},
                {"vat_number", "VAT Number"},
                {"email", "Email"},
                {"phone", "Phone"},
                {"address", "Address"},
                {"city", "City"},
                {"postal_code", "Postal Code"},
                {"country", "Country"},
                {"notes", "Notes"},
                {"description", "Description"},
                {"category", "Category"},
                {"tags", "Tags"},
                {"credit_limit", "Credit Limit"},
                {"payment_terms", "Payment Terms (Days)"},
                {"service_name", "Service Name"},
                {"base_price", "Base Price"},
                {"custom_price", "Custom Price"},
                {"service_type", "Service Type"},
                {"one_off", "One-off"},
                {"periodic", "Periodic"},
                {"period", "Period"},
                {"weekly", "Weekly"},
                {"monthly", "Monthly"},
                {"quarterly", "Quarterly"},
                {"yearly", "Yearly"},
                {"due_date", "Due Date"},
                {"payment_date", "Payment Date"},
                {"payment_method", "Payment Method"},
                {"cash", "Cash"},
                {"bank_transfer", "Bank Transfer"},
                {"credit_card", "Credit Card"},
                {"check", "Check"},
                {"reference", "Reference"},
                {"contact_history", "Contact History"},
                {"contact_type", "Contact Type"},
                {"contact_date", "Contact Date"},
                {"days_overdue", "Days Overdue"},
                {"total_clients", "Total Clients"},
                {"total_services", "Total Services"},
                {"total_revenue", "Total Revenue"},
                {"total_outstanding", "Total Outstanding"},
                {"quick_actions", "Quick Actions"},
                {"recent_activity", "Recent Activity"},
                {"add_client", "Add Client"},
                {"edit_client", "Edit Client"},
                {"delete_client", "Delete Client"},
                {"add_service", "Add Service"},
                {"edit_service", "Edit Service"},
                {"delete_service", "Delete Service"},
                {"mark_as_paid", "Mark as Paid"},
                {"generate_report", "Generate Report"},
                {"revenue_report", "Revenue Report"},
                {"outstanding_report", "Outstanding Report"},
                {"service_performance", "Service Performance"},
                {"client_profitability", "Client Profitability"},
                {"export_to_excel", "Export to Excel"},
                {"export_to_pdf", "Export to PDF"},
                {"from_date", "From Date"},
                {"to_date", "To Date"},
                {"change_password", "Change Password"},
                {"old_password", "Old Password"},
                {"new_password", "New Password"},
                {"confirm_password", "Confirm Password"},
                {"password_changed", "Password changed successfully"},
                {"passwords_not_match", "Passwords do not match"},
                {"invalid_old_password", "Invalid old password"},
                {"first_login_message", "Welcome! For security, please change your password."},
                {"password_requirements", "Password must be at least 8 characters long."},
                {"required_field", "This field is required."},
                {"invalid_email", "Please enter a valid email address."},
                {"invalid_phone", "Please enter a valid phone number."},
                {"operation_completed", "Operation completed successfully."},
                {"confirm_delete", "Are you sure you want to delete this item?"},
                {"no_selection", "Please select an item first."},
                {"no_data", "No data available."},
                {"loading", "Loading..."},
                {"processing", "Processing..."},
                {"backup_success", "Database backed up successfully."},
                {"restore_success", "Database restored successfully."},
                {"export_success", "Data exported successfully."},
                {"import_success", "Data imported successfully."},
                {"view_details", "View Details"},
                {"client_services", "Client Services"},
                {"manage_services", "Manage Services"},
                {"last_paid_date", "Last Paid Date"},
                {"next_payment_date", "Next Payment Date"},
                {"charge_day", "Charge Day"},
                {"start_date", "Start Date"},
                {"end_date", "End Date"},
                {"welcome", "Welcome to BillPilot"},
                {"subtitle", "Your Complete Business Management Solution"},
                {"delayed_payments_notice", "You have {0} delayed payments!"},
                {"payment_amount", "Payment Amount"},
                {"client", "Client"},
                {"process_payment", "Process Payment"},
                {"edit_payment", "Edit Payment"},
                {"search_by", "Search by"},
                {"all_fields", "All Fields"},
                {"payment_for_months", "Payment for {0} months"},
                {"payment_periods", "Payment Periods"}
            };

            // Greek translations
            translations["gr"] = new Dictionary<string, string>
            {
                {"app_title", "BillPilot - Σύστημα Διαχείρισης Επιχείρησης"},
                {"login_title", "Σύνδεση στο BillPilot"},
                {"username", "Όνομα χρήστη"},
                {"password", "Κωδικός"},
                {"login", "Σύνδεση"},
                {"logout", "Αποσύνδεση"},
                {"login_failed", "Λάθος όνομα χρήστη ή κωδικός."},
                {"session_expired", "Η συνεδρία έληξε. Παρακαλώ συνδεθείτε ξανά."},
                {"dashboard", "Ταμπλό"},
                {"clients", "Πελάτες"},
                {"services", "Υπηρεσίες"},
                {"payments", "Πληρωμές"},
                {"upcoming_payments", "Επερχόμενες Πληρωμές"},
                {"delayed_payments", "Καθυστερημένες Πληρωμές"},
                {"reports", "Αναφορές"},
                {"settings", "Ρυθμίσεις"},
                {"language", "Γλώσσα"},
                {"backup", "Αντίγραφο Ασφαλείας"},
                {"restore", "Επαναφορά"},
                {"exit", "Έξοδος"},
                {"add", "Προσθήκη"},
                {"edit", "Επεξεργασία"},
                {"delete", "Διαγραφή"},
                {"save", "Αποθήκευση"},
                {"cancel", "Ακύρωση"},
                {"refresh", "Ανανέωση"},
                {"search", "Αναζήτηση"},
                {"export", "Εξαγωγή"},
                {"import", "Εισαγωγή"},
                {"print", "Εκτύπωση"},
                {"yes", "Ναι"},
                {"no", "Όχι"},
                {"ok", "Εντάξει"},
                {"error", "Σφάλμα"},
                {"warning", "Προειδοποίηση"},
                {"info", "Πληροφορία"},
                {"success", "Επιτυχία"},
                {"confirm", "Επιβεβαίωση"},
                {"close", "Κλείσιμο"},
                {"select", "Επιλογή"},
                {"all", "Όλα"},
                {"none", "Κανένα"},
                {"from", "Από"},
                {"to", "Έως"},
                {"date", "Ημερομηνία"},
                {"time", "Ώρα"},
                {"total", "Σύνολο"},
                {"amount", "Ποσό"},
                {"balance", "Υπόλοιπο"},
                {"paid", "Πληρωμένο"},
                {"unpaid", "Απλήρωτο"},
                {"overdue", "Εκπρόθεσμο"},
                {"pending", "Εκκρεμές"},
                {"active", "Ενεργό"},
                {"inactive", "Ανενεργό"},
                {"enabled", "Ενεργοποιημένο"},
                {"disabled", "Απενεργοποιημένο"},
                {"first_name", "Όνομα"},
                {"last_name", "Επώνυμο"},
                {"full_name", "Ονοματεπώνυμο"},
                {"business_name", "Επωνυμία Επιχείρησης"},
                {"vat_number", "ΑΦΜ"},
                {"email", "Email"},
                {"phone", "Τηλέφωνο"},
                {"address", "Διεύθυνση"},
                {"city", "Πόλη"},
                {"postal_code", "Τ.Κ."},
                {"country", "Χώρα"},
                {"notes", "Σημειώσεις"},
                {"description", "Περιγραφή"},
                {"category", "Κατηγορία"},
                {"tags", "Ετικέτες"},
                {"credit_limit", "Πιστωτικό Όριο"},
                {"payment_terms", "Όροι Πληρωμής (Ημέρες)"},
                {"service_name", "Όνομα Υπηρεσίας"},
                {"base_price", "Βασική Τιμή"},
                {"custom_price", "Ειδική Τιμή"},
                {"service_type", "Τύπος Υπηρεσίας"},
                {"one_off", "Εφάπαξ"},
                {"periodic", "Περιοδική"},
                {"period", "Περίοδος"},
                {"weekly", "Εβδομαδιαία"},
                {"monthly", "Μηνιαία"},
                {"quarterly", "Τριμηνιαία"},
                {"yearly", "Ετήσια"},
                {"due_date", "Ημερομηνία Λήξης"},
                {"payment_date", "Ημερομηνία Πληρωμής"},
                {"payment_method", "Μέθοδος Πληρωμής"},
                {"cash", "Μετρητά"},
                {"bank_transfer", "Τραπεζική Μεταφορά"},
                {"credit_card", "Πιστωτική Κάρτα"},
                {"check", "Επιταγή"},
                {"reference", "Αναφορά"},
                {"contact_history", "Ιστορικό Επικοινωνίας"},
                {"contact_type", "Τύπος Επικοινωνίας"},
                {"contact_date", "Ημερομηνία Επικοινωνίας"},
                {"days_overdue", "Ημέρες Καθυστέρησης"},
                {"total_clients", "Σύνολο Πελατών"},
                {"total_services", "Σύνολο Υπηρεσιών"},
                {"total_revenue", "Συνολικά Έσοδα"},
                {"total_outstanding", "Συνολικά Εκκρεμή"},
                {"quick_actions", "Γρήγορες Ενέργειες"},
                {"recent_activity", "Πρόσφατη Δραστηριότητα"},
                {"add_client", "Προσθήκη Πελάτη"},
                {"edit_client", "Επεξεργασία Πελάτη"},
                {"delete_client", "Διαγραφή Πελάτη"},
                {"add_service", "Προσθήκη Υπηρεσίας"},
                {"edit_service", "Επεξεργασία Υπηρεσίας"},
                {"delete_service", "Διαγραφή Υπηρεσίας"},
                {"mark_as_paid", "Σήμανση ως Πληρωμένο"},
                {"generate_report", "Δημιουργία Αναφοράς"},
                {"revenue_report", "Αναφορά Εσόδων"},
                {"outstanding_report", "Αναφορά Εκκρεμών"},
                {"service_performance", "Απόδοση Υπηρεσιών"},
                {"client_profitability", "Κερδοφορία Πελατών"},
                {"export_to_excel", "Εξαγωγή σε Excel"},
                {"export_to_pdf", "Εξαγωγή σε PDF"},
                {"from_date", "Από Ημερομηνία"},
                {"to_date", "Έως Ημερομηνία"},
                {"change_password", "Αλλαγή Κωδικού"},
                {"old_password", "Παλιός Κωδικός"},
                {"new_password", "Νέος Κωδικός"},
                {"confirm_password", "Επιβεβαίωση Κωδικού"},
                {"password_changed", "Ο κωδικός άλλαξε επιτυχώς"},
                {"passwords_not_match", "Οι κωδικοί δεν ταιριάζουν"},
                {"invalid_old_password", "Λάθος παλιός κωδικός"},
                {"first_login_message", "Καλώς ήρθατε! Για ασφάλεια, παρακαλώ αλλάξτε τον κωδικό σας."},
                {"password_requirements", "Ο κωδικός πρέπει να έχει τουλάχιστον 8 χαρακτήρες."},
                {"required_field", "Αυτό το πεδίο είναι υποχρεωτικό."},
                {"invalid_email", "Παρακαλώ εισάγετε έγκυρη διεύθυνση email."},
                {"invalid_phone", "Παρακαλώ εισάγετε έγκυρο αριθμό τηλεφώνου."},
                {"operation_completed", "Η λειτουργία ολοκληρώθηκε επιτυχώς."},
                {"confirm_delete", "Είστε σίγουροι ότι θέλετε να διαγράψετε αυτό το στοιχείο;"},
                {"no_selection", "Παρακαλώ επιλέξτε πρώτα ένα στοιχείο."},
                {"no_data", "Δεν υπάρχουν δεδομένα."},
                {"loading", "Φόρτωση..."},
                {"processing", "Επεξεργασία..."},
                {"backup_success", "Το αντίγραφο ασφαλείας δημιουργήθηκε επιτυχώς."},
                {"restore_success", "Η βάση δεδομένων επαναφέρθηκε επιτυχώς."},
                {"export_success", "Τα δεδομένα εξήχθησαν επιτυχώς."},
                {"import_success", "Τα δεδομένα εισήχθησαν επιτυχώς."},
                {"view_details", "Προβολή Λεπτομερειών"},
                {"client_services", "Υπηρεσίες Πελάτη"},
                {"manage_services", "Διαχείριση Υπηρεσιών"},
                {"last_paid_date", "Τελευταία Πληρωμή"},
                {"next_payment_date", "Επόμενη Πληρωμή"},
                {"charge_day", "Ημέρα Χρέωσης"},
                {"start_date", "Ημερομηνία Έναρξης"},
                {"end_date", "Ημερομηνία Λήξης"},
                {"welcome", "Καλώς ήρθατε στο BillPilot"},
                {"subtitle", "Η Ολοκληρωμένη Λύση Διαχείρισης Επιχείρησης"},
                {"delayed_payments_notice", "Έχετε {0} καθυστερημένες πληρωμές!"},
                {"payment_amount", "Ποσό Πληρωμής"},
                {"client", "Πελάτης"},
                {"process_payment", "Επεξεργασία Πληρωμής"},
                {"edit_payment", "Επεξεργασία Πληρωμής"},
                {"search_by", "Αναζήτηση κατά"},
                {"all_fields", "Όλα τα Πεδία"},
                {"payment_for_months", "Πληρωμή για {0} μήνες"},
                {"payment_periods", "Περίοδοι Πληρωμής"}
            };
        }

        public static string GetString(string key)
        {
            if (translations.ContainsKey(currentLanguage) &&
                translations[currentLanguage].ContainsKey(key))
            {
                return translations[currentLanguage][key];
            }

            // Fallback to English
            if (translations["en"].ContainsKey(key))
            {
                return translations["en"][key];
            }

            // Return key if not found
            return key;
        }

        public static void SetLanguage(string language)
        {
            if (translations.ContainsKey(language))
            {
                currentLanguage = language;
                LogManager.LogInfo($"Language changed to: {language}");
            }
        }

        public static string CurrentLanguage => currentLanguage;

        public static string[] AvailableLanguages => new string[] { "en", "gr" };
    }

    // ===================== SESSION MANAGER =====================
    public static class SessionManager
    {
        private static DateTime lastActivity;
        private static int sessionTimeoutMinutes = 30;
        private static string currentUser;
        private static Timer sessionTimer;
        private static bool isFirstLogin = false;

        public static event EventHandler SessionExpired;

        public static void StartSession(string username, bool firstLogin = false)
        {
            currentUser = username;
            lastActivity = DateTime.Now;
            isFirstLogin = firstLogin;

            if (sessionTimer != null)
                sessionTimer.Dispose();

            sessionTimer = new Timer();
            sessionTimer.Interval = 60000; // Check every minute
            sessionTimer.Tick += CheckSession;
            sessionTimer.Start();

            LogManager.LogInfo($"Session started for user: {username}");
        }

        public static void UpdateActivity()
        {
            lastActivity = DateTime.Now;
        }

        private static void CheckSession(object sender, EventArgs e)
        {
            if (DateTime.Now.Subtract(lastActivity).TotalMinutes > sessionTimeoutMinutes)
            {
                LogManager.LogWarning($"Session expired for user: {currentUser}");
                EndSession();
                SessionExpired?.Invoke(null, EventArgs.Empty);
            }
        }

        public static void EndSession()
        {
            LogManager.LogInfo($"Session ended for user: {currentUser}");
            currentUser = null;
            isFirstLogin = false;
            if (sessionTimer != null)
            {
                sessionTimer.Stop();
                sessionTimer.Dispose();
                sessionTimer = null;
            }
        }

        public static string CurrentUser => currentUser;
        public static bool IsFirstLogin => isFirstLogin;
        public static bool IsLoggedIn => !string.IsNullOrEmpty(currentUser);
    }
}