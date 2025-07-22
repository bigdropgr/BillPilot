using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace BillPilot
{
    // ===================== CLIENT MANAGER =====================
    public class ClientManager
    {
        private DatabaseManager dbManager;

        public ClientManager(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
        }

        public List<Client> GetAllClients()
        {
            var clients = new List<Client>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT c.*, 
                           COALESCE((SELECT SUM(p.Amount) 
                                    FROM Payments p 
                                    WHERE p.ClientId = c.Id AND p.IsPaid = 0), 0) as Balance
                    FROM Clients c 
                    WHERE c.IsActive = 1 
                    ORDER BY c.FirstName, c.LastName", connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            clients.Add(MapClientFromReader(reader));
                        }
                    }
                }
            }
            return clients;
        }

        public Client GetClient(int id)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT c.*, 
                           COALESCE((SELECT SUM(p.Amount) 
                                    FROM Payments p 
                                    WHERE p.ClientId = c.Id AND p.IsPaid = 0), 0) as Balance
                    FROM Clients c 
                    WHERE c.Id = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapClientFromReader(reader);
                        }
                    }
                }
            }
            return null;
        }

        public int CreateClient(Client client)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    INSERT INTO Clients (FirstName, LastName, BusinessName, VATNumber, Email, Phone, Address, Category, Tags, CreditLimit, PaymentTermsDays, Notes)
                    VALUES (@firstName, @lastName, @businessName, @vatNumber, @email, @phone, @address, @category, @tags, @creditLimit, @paymentTerms, @notes);
                    SELECT last_insert_rowid();", connection))
                {
                    cmd.Parameters.AddWithValue("@firstName", client.FirstName);
                    cmd.Parameters.AddWithValue("@lastName", client.LastName);
                    cmd.Parameters.AddWithValue("@businessName", client.BusinessName ?? "");
                    cmd.Parameters.AddWithValue("@vatNumber", client.VATNumber ?? "");
                    cmd.Parameters.AddWithValue("@email", client.Email ?? "");
                    cmd.Parameters.AddWithValue("@phone", client.Phone ?? "");
                    cmd.Parameters.AddWithValue("@address", client.Address ?? "");
                    cmd.Parameters.AddWithValue("@category", client.Category ?? "Regular");
                    cmd.Parameters.AddWithValue("@tags", client.Tags ?? "");
                    cmd.Parameters.AddWithValue("@creditLimit", client.CreditLimit);
                    cmd.Parameters.AddWithValue("@paymentTerms", client.PaymentTermsDays);
                    cmd.Parameters.AddWithValue("@notes", client.Notes ?? "");

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void UpdateClient(Client client)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    UPDATE Clients SET 
                        FirstName = @firstName, 
                        LastName = @lastName, 
                        BusinessName = @businessName, 
                        VATNumber = @vatNumber, 
                        Email = @email, 
                        Phone = @phone, 
                        Address = @address, 
                        Category = @category,
                        Tags = @tags,
                        CreditLimit = @creditLimit, 
                        PaymentTermsDays = @paymentTerms,
                        Notes = @notes
                    WHERE Id = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", client.Id);
                    cmd.Parameters.AddWithValue("@firstName", client.FirstName);
                    cmd.Parameters.AddWithValue("@lastName", client.LastName);
                    cmd.Parameters.AddWithValue("@businessName", client.BusinessName ?? "");
                    cmd.Parameters.AddWithValue("@vatNumber", client.VATNumber ?? "");
                    cmd.Parameters.AddWithValue("@email", client.Email ?? "");
                    cmd.Parameters.AddWithValue("@phone", client.Phone ?? "");
                    cmd.Parameters.AddWithValue("@address", client.Address ?? "");
                    cmd.Parameters.AddWithValue("@category", client.Category ?? "Regular");
                    cmd.Parameters.AddWithValue("@tags", client.Tags ?? "");
                    cmd.Parameters.AddWithValue("@creditLimit", client.CreditLimit);
                    cmd.Parameters.AddWithValue("@paymentTerms", client.PaymentTermsDays);
                    cmd.Parameters.AddWithValue("@notes", client.Notes ?? "");

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteClient(int clientId)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Check for unpaid payments
                        using (var checkCmd = new SQLiteCommand(
                            "SELECT COUNT(*) FROM Payments WHERE ClientId = @id AND IsPaid = 0", connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", clientId);
                            var unpaidPayments = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (unpaidPayments > 0)
                            {
                                throw new InvalidOperationException($"Cannot delete client with {unpaidPayments} unpaid payments.");
                            }
                        }

                        // Soft delete
                        using (var cmd = new SQLiteCommand("UPDATE Clients SET IsActive = 0 WHERE Id = @id", connection))
                        {
                            cmd.Parameters.AddWithValue("@id", clientId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<Client> SearchClients(string searchTerm, string searchField = "all")
        {
            var clients = new List<Client>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                string query;

                // Build parameterized query based on search field
                switch (searchField.ToLower())
                {
                    case "firstname":
                        query = @"SELECT c.*, 
                                 COALESCE((SELECT SUM(p.Amount) 
                                          FROM Payments p 
                                          WHERE p.ClientId = c.Id AND p.IsPaid = 0), 0) as Balance
                                 FROM Clients c 
                                 WHERE c.IsActive = 1 AND c.FirstName LIKE @search
                                 ORDER BY c.FirstName, c.LastName";
                        break;
                    case "lastname":
                        query = @"SELECT c.*, 
                                 COALESCE((SELECT SUM(p.Amount) 
                                          FROM Payments p 
                                          WHERE p.ClientId = c.Id AND p.IsPaid = 0), 0) as Balance
                                 FROM Clients c 
                                 WHERE c.IsActive = 1 AND c.LastName LIKE @search
                                 ORDER BY c.FirstName, c.LastName";
                        break;
                    case "businessname":
                        query = @"SELECT c.*, 
                                 COALESCE((SELECT SUM(p.Amount) 
                                          FROM Payments p 
                                          WHERE p.ClientId = c.Id AND p.IsPaid = 0), 0) as Balance
                                 FROM Clients c 
                                 WHERE c.IsActive = 1 AND c.BusinessName LIKE @search
                                 ORDER BY c.FirstName, c.LastName";
                        break;
                    case "vatnumber":
                        query = @"SELECT c.*, 
                                 COALESCE((SELECT SUM(p.Amount) 
                                          FROM Payments p 
                                          WHERE p.ClientId = c.Id AND p.IsPaid = 0), 0) as Balance
                                 FROM Clients c 
                                 WHERE c.IsActive = 1 AND c.VATNumber LIKE @search
                                 ORDER BY c.FirstName, c.LastName";
                        break;
                    default:
                        query = @"SELECT c.*, 
                                 COALESCE((SELECT SUM(p.Amount) 
                                          FROM Payments p 
                                          WHERE p.ClientId = c.Id AND p.IsPaid = 0), 0) as Balance
                                 FROM Clients c 
                                 WHERE c.IsActive = 1 AND (c.FirstName LIKE @search OR c.LastName LIKE @search 
                                                  OR c.BusinessName LIKE @search OR c.VATNumber LIKE @search 
                                                  OR c.Email LIKE @search OR c.Phone LIKE @search)
                                 ORDER BY c.FirstName, c.LastName";
                        break;
                }

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%");
                    }
                    else
                    {
                        // If no search term, get all active clients
                        cmd.CommandText = @"SELECT c.*, 
                                           COALESCE((SELECT SUM(p.Amount) 
                                                    FROM Payments p 
                                                    WHERE p.ClientId = c.Id AND p.IsPaid = 0), 0) as Balance
                                           FROM Clients c 
                                           WHERE c.IsActive = 1 
                                           ORDER BY c.FirstName, c.LastName";
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            clients.Add(MapClientFromReader(reader));
                        }
                    }
                }
            }
            return clients;
        }

        private Client MapClientFromReader(SQLiteDataReader reader)
        {
            return new Client
            {
                Id = Convert.ToInt32(reader["Id"]),
                FirstName = reader["FirstName"].ToString(),
                LastName = reader["LastName"].ToString(),
                BusinessName = reader["BusinessName"].ToString(),
                VATNumber = reader["VATNumber"].ToString(),
                Email = reader["Email"].ToString(),
                Phone = reader["Phone"].ToString(),
                Address = reader["Address"].ToString(),
                Category = reader["Category"].ToString(),
                Tags = reader["Tags"].ToString(),
                CreditLimit = Convert.ToDecimal(reader["CreditLimit"]),
                PaymentTermsDays = Convert.ToInt32(reader["PaymentTermsDays"]),
                Notes = reader["Notes"].ToString(),
                Balance = Convert.ToDecimal(reader["Balance"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                IsActive = Convert.ToBoolean(reader["IsActive"])
            };
        }
    }

    // ===================== SERVICE MANAGER =====================
    public class ServiceManager
    {
        private DatabaseManager dbManager;

        public ServiceManager(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
        }

        public List<Service> GetAllServices()
        {
            var services = new List<Service>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM Services WHERE IsActive = 1 ORDER BY Name", connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            services.Add(MapServiceFromReader(reader));
                        }
                    }
                }
            }
            return services;
        }

        public Service GetService(int id)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM Services WHERE Id = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapServiceFromReader(reader);
                        }
                    }
                }
            }
            return null;
        }

        public int CreateService(Service service)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    INSERT INTO Services (Name, Description, BasePrice)
                    VALUES (@name, @description, @basePrice);
                    SELECT last_insert_rowid();", connection))
                {
                    cmd.Parameters.AddWithValue("@name", service.Name);
                    cmd.Parameters.AddWithValue("@description", service.Description ?? "");
                    cmd.Parameters.AddWithValue("@basePrice", service.BasePrice);

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void UpdateService(Service service)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    UPDATE Services SET 
                        Name = @name, 
                        Description = @description, 
                        BasePrice = @basePrice
                    WHERE Id = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", service.Id);
                    cmd.Parameters.AddWithValue("@name", service.Name);
                    cmd.Parameters.AddWithValue("@description", service.Description ?? "");
                    cmd.Parameters.AddWithValue("@basePrice", service.BasePrice);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteService(int serviceId)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Check for active client services
                        using (var checkCmd = new SQLiteCommand(
                            "SELECT COUNT(*) FROM ClientServices WHERE ServiceId = @id AND IsActive = 1", connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", serviceId);
                            var activeServices = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (activeServices > 0)
                            {
                                throw new InvalidOperationException($"Cannot delete service with {activeServices} active client subscriptions.");
                            }
                        }

                        // Soft delete
                        using (var cmd = new SQLiteCommand("UPDATE Services SET IsActive = 0 WHERE Id = @id", connection))
                        {
                            cmd.Parameters.AddWithValue("@id", serviceId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<Service> SearchServices(string searchTerm)
        {
            var services = new List<Service>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT * FROM Services 
                    WHERE IsActive = 1 
                      AND (Name LIKE @search OR Description LIKE @search)
                    ORDER BY Name", connection))
                {
                    cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%");

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            services.Add(MapServiceFromReader(reader));
                        }
                    }
                }
            }
            return services;
        }

        private Service MapServiceFromReader(SQLiteDataReader reader)
        {
            return new Service
            {
                Id = Convert.ToInt32(reader["Id"]),
                Name = reader["Name"].ToString(),
                Description = reader["Description"].ToString(),
                BasePrice = Convert.ToDecimal(reader["BasePrice"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                IsActive = Convert.ToBoolean(reader["IsActive"])
            };
        }
    }

    // ===================== PAYMENT MANAGER =====================
    public class PaymentManager
    {
        private DatabaseManager dbManager;

        public PaymentManager(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
        }

        public List<Payment> GetUpcomingPayments(DateTime fromDate, DateTime toDate)
        {
            var payments = new List<Payment>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT p.*, c.FirstName || ' ' || c.LastName as ClientName, s.Name as ServiceName
                    FROM Payments p
                    INNER JOIN Clients c ON p.ClientId = c.Id
                    LEFT JOIN Services s ON p.ServiceId = s.Id
                    WHERE p.IsPaid = 0 
                      AND p.DueDate BETWEEN @fromDate AND @toDate
                      AND p.DueDate >= date('now')
                    ORDER BY p.DueDate", connection))
                {
                    cmd.Parameters.AddWithValue("@fromDate", fromDate.Date);
                    cmd.Parameters.AddWithValue("@toDate", toDate.Date);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            payments.Add(MapPaymentFromReader(reader));
                        }
                    }
                }
            }
            return payments;
        }

        public List<Payment> GetOverduePayments()
        {
            var payments = new List<Payment>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();

                // First update overdue status
                using (var updateCmd = new SQLiteCommand(@"
                    UPDATE Payments 
                    SET IsOverdue = 1 
                    WHERE IsPaid = 0 AND DueDate < date('now')", connection))
                {
                    updateCmd.ExecuteNonQuery();
                }

                // Then get overdue payments
                using (var cmd = new SQLiteCommand(@"
                    SELECT p.*, c.FirstName || ' ' || c.LastName as ClientName, 
                           s.Name as ServiceName,
                           c.Phone, c.Email,
                           CAST(julianday('now') - julianday(p.DueDate) as INTEGER) as DaysOverdue
                    FROM Payments p
                    INNER JOIN Clients c ON p.ClientId = c.Id
                    LEFT JOIN Services s ON p.ServiceId = s.Id
                    WHERE p.IsPaid = 0 AND p.IsOverdue = 1
                    ORDER BY p.DueDate", connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var payment = MapPaymentFromReader(reader);
                            // Add extra properties for delayed payments view
                            payment.Notes = $"Phone: {reader["Phone"]}, Email: {reader["Email"]}";
                            payments.Add(payment);
                        }
                    }
                }
            }
            return payments;
        }

        public int GetOverduePaymentCount()
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT COUNT(*) FROM Payments WHERE IsPaid = 0 AND DueDate < date('now')",
                    connection))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public decimal GetTotalRevenue()
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT COALESCE(SUM(Amount), 0) FROM Payments WHERE IsPaid = 1",
                    connection))
                {
                    return Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
        }

        public decimal GetTotalOutstanding()
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT COALESCE(SUM(Amount), 0) FROM Payments WHERE IsPaid = 0",
                    connection))
                {
                    return Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
        }

        public List<Payment> GetRecentPayments(int count)
        {
            var payments = new List<Payment>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT p.*, c.FirstName || ' ' || c.LastName as ClientName, s.Name as ServiceName
                    FROM Payments p
                    INNER JOIN Clients c ON p.ClientId = c.Id
                    LEFT JOIN Services s ON p.ServiceId = s.Id
                    ORDER BY CASE WHEN p.PaidDate IS NOT NULL THEN p.PaidDate ELSE p.CreatedDate END DESC
                    LIMIT @count", connection))
                {
                    cmd.Parameters.AddWithValue("@count", count);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            payments.Add(MapPaymentFromReader(reader));
                        }
                    }
                }
            }
            return payments;
        }

        public void MarkPaymentAsPaid(int paymentId, string paymentMethod, string reference, int monthsPaid = 1)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Get payment details
                        Payment payment = null;
                        using (var getCmd = new SQLiteCommand(
                            "SELECT * FROM Payments WHERE Id = @id", connection))
                        {
                            getCmd.Parameters.AddWithValue("@id", paymentId);
                            using (var reader = getCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    payment = MapPaymentFromReader(reader);
                                }
                            }
                        }

                        if (payment == null)
                            throw new Exception("Payment not found");

                        // Update payment as paid
                        using (var updateCmd = new SQLiteCommand(@"
                            UPDATE Payments 
                            SET IsPaid = 1, 
                                PaidDate = @paidDate, 
                                PaymentMethod = @method,
                                Reference = @reference,
                                IsOverdue = 0
                            WHERE Id = @id", connection))
                        {
                            updateCmd.Parameters.AddWithValue("@id", paymentId);
                            updateCmd.Parameters.AddWithValue("@paidDate", DateTime.Now.Date);
                            updateCmd.Parameters.AddWithValue("@method", paymentMethod);
                            updateCmd.Parameters.AddWithValue("@reference", reference ?? "");
                            updateCmd.ExecuteNonQuery();
                        }

                        // If periodic payment and multiple months paid, update ClientService
                        if (payment.ClientServiceId.HasValue && monthsPaid > 1)
                        {
                            using (var csCmd = new SQLiteCommand(
                                "SELECT * FROM ClientServices WHERE Id = @id", connection))
                            {
                                csCmd.Parameters.AddWithValue("@id", payment.ClientServiceId.Value);
                                using (var reader = csCmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        var nextPaymentDate = reader["NextPaymentDate"] != DBNull.Value
                                            ? Convert.ToDateTime(reader["NextPaymentDate"])
                                            : payment.DueDate;
                                        var period = reader["Period"].ToString();

                                        // Calculate new next payment date
                                        DateTime newNextDate = nextPaymentDate;
                                        for (int i = 0; i < monthsPaid; i++)
                                        {
                                            newNextDate = CalculateNextPaymentDate(newNextDate, period,
                                                reader["ChargeDay"] != DBNull.Value ? Convert.ToInt32(reader["ChargeDay"]) : (int?)null);
                                        }

                                        // Update ClientService
                                        using (var updateCsCmd = new SQLiteCommand(@"
                                            UPDATE ClientServices 
                                            SET LastPaidDate = @lastPaid,
                                                NextPaymentDate = @nextDate
                                            WHERE Id = @id", connection))
                                        {
                                            updateCsCmd.Parameters.AddWithValue("@id", payment.ClientServiceId.Value);
                                            updateCsCmd.Parameters.AddWithValue("@lastPaid", DateTime.Now.Date);
                                            updateCsCmd.Parameters.AddWithValue("@nextDate", newNextDate);
                                            updateCsCmd.ExecuteNonQuery();
                                        }

                                        // Delete any existing future payments for the paid period
                                        using (var deleteCmd = new SQLiteCommand(@"
                                            DELETE FROM Payments 
                                            WHERE ClientServiceId = @csId 
                                              AND IsPaid = 0 
                                              AND DueDate > @currentDue
                                              AND DueDate < @nextDate", connection))
                                        {
                                            deleteCmd.Parameters.AddWithValue("@csId", payment.ClientServiceId.Value);
                                            deleteCmd.Parameters.AddWithValue("@currentDue", payment.DueDate);
                                            deleteCmd.Parameters.AddWithValue("@nextDate", newNextDate);
                                            deleteCmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        }

                        transaction.Commit();
                        LogManager.LogInfo($"Payment marked as paid: ID {paymentId}, Method: {paymentMethod}");
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void CreatePaymentForClientService(int clientServiceId)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();

                // Get client service details
                using (var cmd = new SQLiteCommand(@"
                    SELECT cs.*, s.Name as ServiceName, 
                           COALESCE(cs.CustomPrice, s.BasePrice) as Amount
                    FROM ClientServices cs
                    INNER JOIN Services s ON cs.ServiceId = s.Id
                    WHERE cs.Id = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", clientServiceId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var payment = new Payment
                            {
                                ClientId = Convert.ToInt32(reader["ClientId"]),
                                ServiceId = Convert.ToInt32(reader["ServiceId"]),
                                ClientServiceId = clientServiceId,
                                PaymentType = reader["ServiceType"].ToString() == "Periodic" ? "Periodic" : "OneOff",
                                DueDate = reader["NextPaymentDate"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["NextPaymentDate"])
                                    : DateTime.Now.Date,
                                Amount = Convert.ToDecimal(reader["Amount"]),
                                CreatedBy = SessionManager.CurrentUser
                            };

                            CreatePayment(payment);
                        }
                    }
                }
            }
        }

        public void CreatePayment(Payment payment, SQLiteConnection existingConnection = null, SQLiteTransaction existingTransaction = null)
        {
            if (existingConnection != null)
            {
                // Use existing connection and transaction
                using (var cmd = new SQLiteCommand(@"
                    INSERT INTO Payments (ClientId, ServiceId, ClientServiceId, PaymentType, DueDate, Amount, IsPaid, IsOverdue, CreatedBy)
                    VALUES (@clientId, @serviceId, @clientServiceId, @paymentType, @dueDate, @amount, 0, 0, @createdBy)", 
                    existingConnection, existingTransaction))
                {
                    cmd.Parameters.AddWithValue("@clientId", payment.ClientId);
                    cmd.Parameters.AddWithValue("@serviceId", (object)payment.ServiceId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@clientServiceId", (object)payment.ClientServiceId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@paymentType", payment.PaymentType);
                    cmd.Parameters.AddWithValue("@dueDate", payment.DueDate);
                    cmd.Parameters.AddWithValue("@amount", payment.Amount);
                    cmd.Parameters.AddWithValue("@createdBy", payment.CreatedBy);

                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                // Create new connection as before
                using (var connection = dbManager.GetConnection())
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand(@"
                        INSERT INTO Payments (ClientId, ServiceId, ClientServiceId, PaymentType, DueDate, Amount, IsPaid, IsOverdue, CreatedBy)
                        VALUES (@clientId, @serviceId, @clientServiceId, @paymentType, @dueDate, @amount, 0, 0, @createdBy)", connection))
                    {
                        cmd.Parameters.AddWithValue("@clientId", payment.ClientId);
                        cmd.Parameters.AddWithValue("@serviceId", (object)payment.ServiceId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@clientServiceId", (object)payment.ClientServiceId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@paymentType", payment.PaymentType);
                        cmd.Parameters.AddWithValue("@dueDate", payment.DueDate);
                        cmd.Parameters.AddWithValue("@amount", payment.Amount);
                        cmd.Parameters.AddWithValue("@createdBy", payment.CreatedBy);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private DateTime CalculateNextPaymentDate(DateTime currentDate, string period, int? chargeDay)
        {
            switch (period?.ToLower())
            {
                case "weekly":
                    return currentDate.AddDays(7);

                case "monthly":
                    var nextMonth = currentDate.AddMonths(1);
                    if (chargeDay.HasValue)
                    {
                        var day = chargeDay.Value;
                        var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                        if (day > daysInMonth)
                            day = daysInMonth;
                        return new DateTime(nextMonth.Year, nextMonth.Month, day);
                    }
                    return nextMonth;

                case "quarterly":
                    var nextQuarter = currentDate.AddMonths(3);
                    if (chargeDay.HasValue)
                    {
                        var day = chargeDay.Value;
                        var daysInMonth = DateTime.DaysInMonth(nextQuarter.Year, nextQuarter.Month);
                        if (day > daysInMonth)
                            day = daysInMonth;
                        return new DateTime(nextQuarter.Year, nextQuarter.Month, day);
                    }
                    return nextQuarter;

                case "yearly":
                    var nextYear = currentDate.AddYears(1);
                    if (chargeDay.HasValue && chargeDay.Value <= 365)
                    {
                        return new DateTime(nextYear.Year, 1, 1).AddDays(chargeDay.Value - 1);
                    }
                    return nextYear;

                default:
                    return currentDate.AddMonths(1);
            }
        }

        private Payment MapPaymentFromReader(SQLiteDataReader reader)
        {
            return new Payment
            {
                Id = Convert.ToInt32(reader["Id"]),
                ClientId = Convert.ToInt32(reader["ClientId"]),
                ServiceId = reader["ServiceId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["ServiceId"]),
                ClientServiceId = reader["ClientServiceId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["ClientServiceId"]),
                PaymentType = reader["PaymentType"].ToString(),
                DueDate = Convert.ToDateTime(reader["DueDate"]),
                PaidDate = reader["PaidDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["PaidDate"]),
                Amount = Convert.ToDecimal(reader["Amount"]),
                IsPaid = Convert.ToBoolean(reader["IsPaid"]),
                IsOverdue = Convert.ToBoolean(reader["IsOverdue"]),
                PaymentMethod = reader["PaymentMethod"]?.ToString(),
                Reference = reader["Reference"]?.ToString(),
                Notes = reader["Notes"]?.ToString(),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                CreatedBy = reader["CreatedBy"]?.ToString(),
                ClientName = reader.GetOrdinal("ClientName") >= 0 ? reader["ClientName"].ToString() : "",
                ServiceName = reader.GetOrdinal("ServiceName") >= 0 ? reader["ServiceName"]?.ToString() : ""
            };
        }
    }

    // ===================== CLIENT SERVICE MANAGER =====================
    public class ClientServiceManager
    {
        private DatabaseManager dbManager;

        public ClientServiceManager(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
        }

        public List<ClientService> GetClientServices(int clientId)
        {
            var services = new List<ClientService>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT cs.*, s.Name as ServiceName, 
                           COALESCE(cs.CustomPrice, s.BasePrice) as Price
                    FROM ClientServices cs
                    INNER JOIN Services s ON cs.ServiceId = s.Id
                    WHERE cs.ClientId = @clientId
                    ORDER BY s.Name", connection))
                {
                    cmd.Parameters.AddWithValue("@clientId", clientId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            services.Add(MapClientServiceFromReader(reader));
                        }
                    }
                }
            }
            return services;
        }

        public void CreateClientService(ClientService clientService)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Calculate next payment date
                        var nextPaymentDate = CalculateInitialPaymentDate(clientService);

                        // Insert client service
                        using (var cmd = new SQLiteCommand(@"
                            INSERT INTO ClientServices (ClientId, ServiceId, ServiceType, CustomPrice, Period, ChargeDay, StartDate, NextPaymentDate, IsActive)
                            VALUES (@clientId, @serviceId, @serviceType, @customPrice, @period, @chargeDay, @startDate, @nextPaymentDate, @isActive);
                            SELECT last_insert_rowid();", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@clientId", clientService.ClientId);
                            cmd.Parameters.AddWithValue("@serviceId", clientService.ServiceId);
                            cmd.Parameters.AddWithValue("@serviceType", clientService.ServiceType);
                            cmd.Parameters.AddWithValue("@customPrice", (object)clientService.CustomPrice ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@period", (object)clientService.Period ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@chargeDay", (object)clientService.ChargeDay ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@startDate", clientService.StartDate);
                            cmd.Parameters.AddWithValue("@nextPaymentDate", nextPaymentDate);
                            cmd.Parameters.AddWithValue("@isActive", clientService.IsActive);

                            var clientServiceId = Convert.ToInt32(cmd.ExecuteScalar());

                            // Create initial payment
                            if (clientService.IsActive)
                            {
                                var paymentManager = new PaymentManager(dbManager);
                                var payment = new Payment
                                {
                                    ClientId = clientService.ClientId,
                                    ServiceId = clientService.ServiceId,
                                    ClientServiceId = clientServiceId,
                                    PaymentType = clientService.ServiceType,
                                    DueDate = nextPaymentDate,
                                    Amount = clientService.CustomPrice ?? GetServicePrice(clientService.ServiceId, connection),
                                    CreatedBy = SessionManager.CurrentUser
                                };
                                paymentManager.CreatePayment(payment, connection, transaction);
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void UpdateClientService(ClientService clientService)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();

                DateTime nextPaymentDate;
                if (clientService.NextPaymentDate == null || clientService.NextPaymentDate.Value.Year == 1)
                {
                    nextPaymentDate = CalculateInitialPaymentDate(clientService);
                }
                else
                {
                    nextPaymentDate = clientService.NextPaymentDate.Value;
                }

                using (var cmd = new SQLiteCommand(@"
                    UPDATE ClientServices SET 
                        ServiceId = @serviceId,
                        ServiceType = @serviceType,
                        CustomPrice = @customPrice,
                        Period = @period,
                        ChargeDay = @chargeDay,
                        StartDate = @startDate,
                        NextPaymentDate = @nextPaymentDate,
                        IsActive = @isActive
                    WHERE Id = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", clientService.Id);
                    cmd.Parameters.AddWithValue("@serviceId", clientService.ServiceId);
                    cmd.Parameters.AddWithValue("@serviceType", clientService.ServiceType);
                    cmd.Parameters.AddWithValue("@customPrice", (object)clientService.CustomPrice ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@period", (object)clientService.Period ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@chargeDay", (object)clientService.ChargeDay ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@startDate", clientService.StartDate);
                    cmd.Parameters.AddWithValue("@nextPaymentDate", nextPaymentDate);
                    cmd.Parameters.AddWithValue("@isActive", clientService.IsActive);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteClientService(int id)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Delete unpaid payments for this service
                        using (var delPayCmd = new SQLiteCommand(
                            "DELETE FROM Payments WHERE ClientServiceId = @id AND IsPaid = 0", connection))
                        {
                            delPayCmd.Parameters.AddWithValue("@id", id);
                            delPayCmd.ExecuteNonQuery();
                        }

                        // Delete client service
                        using (var cmd = new SQLiteCommand("DELETE FROM ClientServices WHERE Id = @id", connection))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void CheckAndCreateUpcomingPayments()
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();

                // Get all active periodic services that need payment creation
                using (var cmd = new SQLiteCommand(@"
                    SELECT cs.*, s.BasePrice
                    FROM ClientServices cs
                    INNER JOIN Services s ON cs.ServiceId = s.Id
                    WHERE cs.IsActive = 1 
                      AND cs.ServiceType = 'Periodic'
                      AND cs.NextPaymentDate <= date('now', '+30 days')
                      AND NOT EXISTS (
                          SELECT 1 FROM Payments p 
                          WHERE p.ClientServiceId = cs.Id 
                            AND p.DueDate = cs.NextPaymentDate
                      )", connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        var servicesToProcess = new List<(int Id, int ClientId, int ServiceId, DateTime NextDate, decimal Amount, string Period, int? ChargeDay)>();

                        while (reader.Read())
                        {
                            servicesToProcess.Add((
                                Convert.ToInt32(reader["Id"]),
                                Convert.ToInt32(reader["ClientId"]),
                                Convert.ToInt32(reader["ServiceId"]),
                                Convert.ToDateTime(reader["NextPaymentDate"]),
                                reader["CustomPrice"] != DBNull.Value ? Convert.ToDecimal(reader["CustomPrice"]) : Convert.ToDecimal(reader["BasePrice"]),
                                reader["Period"].ToString(),
                                reader["ChargeDay"] != DBNull.Value ? Convert.ToInt32(reader["ChargeDay"]) : (int?)null
                            ));
                        }

                        // Process each service
                        var paymentManager = new PaymentManager(dbManager);
                        foreach (var service in servicesToProcess)
                        {
                            var payment = new Payment
                            {
                                ClientId = service.ClientId,
                                ServiceId = service.ServiceId,
                                ClientServiceId = service.Id,
                                PaymentType = "Periodic",
                                DueDate = service.NextDate,
                                Amount = service.Amount,
                                CreatedBy = "System"
                            };
                            paymentManager.CreatePayment(payment);
                        }
                    }
                }
            }
        }

        private DateTime CalculateInitialPaymentDate(ClientService clientService)
        {
            var baseDate = clientService.StartDate;

            if (clientService.ServiceType != "Periodic" || string.IsNullOrEmpty(clientService.Period))
                return baseDate;

            // For periodic services, calculate the first payment date
            if (clientService.ChargeDay.HasValue)
            {
                switch (clientService.Period.ToLower())
                {
                    case "monthly":
                        var day = clientService.ChargeDay.Value;
                        if (day > DateTime.DaysInMonth(baseDate.Year, baseDate.Month))
                            day = DateTime.DaysInMonth(baseDate.Year, baseDate.Month);

                        var firstDate = new DateTime(baseDate.Year, baseDate.Month, day);
                        if (firstDate < baseDate)
                            firstDate = firstDate.AddMonths(1);
                        return firstDate;

                    case "yearly":
                        var yearDate = new DateTime(baseDate.Year, 1, 1).AddDays(clientService.ChargeDay.Value - 1);
                        if (yearDate < baseDate)
                            yearDate = yearDate.AddYears(1);
                        return yearDate;
                }
            }

            return baseDate;
        }

        private decimal GetServicePrice(int serviceId, SQLiteConnection existingConnection = null)
        {
            if (existingConnection != null)
            {
                using (var cmd = new SQLiteCommand("SELECT BasePrice FROM Services WHERE Id = @id", existingConnection))
                {
                    cmd.Parameters.AddWithValue("@id", serviceId);
                    return Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
            else
            {
                using (var connection = dbManager.GetConnection())
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("SELECT BasePrice FROM Services WHERE Id = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", serviceId);
                        return Convert.ToDecimal(cmd.ExecuteScalar());
                    }
                }
            }
        }

        private ClientService MapClientServiceFromReader(SQLiteDataReader reader)
        {
            return new ClientService
            {
                Id = Convert.ToInt32(reader["Id"]),
                ClientId = Convert.ToInt32(reader["ClientId"]),
                ServiceId = Convert.ToInt32(reader["ServiceId"]),
                ServiceType = reader["ServiceType"].ToString(),
                CustomPrice = reader["CustomPrice"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["CustomPrice"]),
                Period = reader["Period"]?.ToString(),
                ChargeDay = reader["ChargeDay"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["ChargeDay"]),
                StartDate = Convert.ToDateTime(reader["StartDate"]),
                EndDate = reader["EndDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["EndDate"]),
                LastPaidDate = reader["LastPaidDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["LastPaidDate"]),
                NextPaymentDate = reader["NextPaymentDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["NextPaymentDate"]),
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                ServiceName = reader["ServiceName"].ToString(),
            };
        }
    }

    // ===================== CONTACT HISTORY MANAGER =====================
    public class ContactHistoryManager
    {
        private DatabaseManager dbManager;

        public ContactHistoryManager(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
        }

        public List<ContactHistory> GetClientContacts(int clientId)
        {
            var contacts = new List<ContactHistory>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT * FROM ContactHistory WHERE ClientId = @clientId ORDER BY ContactDate DESC",
                    connection))
                {
                    cmd.Parameters.AddWithValue("@clientId", clientId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            contacts.Add(new ContactHistory
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                ClientId = Convert.ToInt32(reader["ClientId"]),
                                ContactDate = Convert.ToDateTime(reader["ContactDate"]),
                                ContactType = reader["ContactType"].ToString(),
                                Notes = reader["Notes"].ToString(),
                                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                CreatedBy = reader["CreatedBy"].ToString()
                            });
                        }
                    }
                }
            }
            return contacts;
        }

        public void AddContact(ContactHistory contact)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    INSERT INTO ContactHistory (ClientId, ContactDate, ContactType, Notes, CreatedBy)
                    VALUES (@clientId, @contactDate, @contactType, @notes, @createdBy)", connection))
                {
                    cmd.Parameters.AddWithValue("@clientId", contact.ClientId);
                    cmd.Parameters.AddWithValue("@contactDate", contact.ContactDate);
                    cmd.Parameters.AddWithValue("@contactType", contact.ContactType);
                    cmd.Parameters.AddWithValue("@notes", contact.Notes);
                    cmd.Parameters.AddWithValue("@createdBy", contact.CreatedBy);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    // ===================== USER MANAGER =====================
    public class UserManager
    {
        private DatabaseManager dbManager;

        public UserManager(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
        }

        public bool ValidateLogin(string username, string password, out bool isFirstLogin)
        {
            isFirstLogin = false;

            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT PasswordHash, Salt, IsFirstLogin FROM Users WHERE Username = @username AND IsActive = 1",
                    connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash = reader["PasswordHash"].ToString();
                            string salt = reader["Salt"].ToString();
                            isFirstLogin = Convert.ToBoolean(reader["IsFirstLogin"]);

                            return CryptoHelper.VerifyPassword(password, salt, storedHash);
                        }
                    }
                }
            }
            return false;
        }

        public bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();

                // Verify old password
                using (var cmd = new SQLiteCommand(
                    "SELECT PasswordHash, Salt FROM Users WHERE Username = @username",
                    connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash = reader["PasswordHash"].ToString();
                            string salt = reader["Salt"].ToString();

                            if (CryptoHelper.VerifyPassword(oldPassword, salt, storedHash))
                            {
                                // Update with new password
                                var newSalt = CryptoHelper.GenerateSalt();
                                var newHash = CryptoHelper.HashPassword(newPassword, newSalt);

                                using (var updateCmd = new SQLiteCommand(
                                    "UPDATE Users SET PasswordHash = @hash, Salt = @salt, IsFirstLogin = 0 WHERE Username = @username",
                                    connection))
                                {
                                    updateCmd.Parameters.AddWithValue("@hash", newHash);
                                    updateCmd.Parameters.AddWithValue("@salt", newSalt);
                                    updateCmd.Parameters.AddWithValue("@username", username);

                                    updateCmd.ExecuteNonQuery();
                                }
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }

    // ===================== REPORT MANAGER =====================
    public class ReportManager
    {
        private DatabaseManager dbManager;

        public ReportManager(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
        }

        public List<RevenueReportItem> GetRevenueReport(DateTime fromDate, DateTime toDate)
        {
            var items = new List<RevenueReportItem>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT p.*, c.FirstName || ' ' || c.LastName as ClientName, s.Name as ServiceName
                    FROM Payments p
                    INNER JOIN Clients c ON p.ClientId = c.Id
                    LEFT JOIN Services s ON p.ServiceId = s.Id
                    WHERE p.IsPaid = 1 AND p.PaidDate BETWEEN @fromDate AND @toDate
                    ORDER BY p.PaidDate DESC", connection))
                {
                    cmd.Parameters.AddWithValue("@fromDate", fromDate.Date);
                    cmd.Parameters.AddWithValue("@toDate", toDate.Date);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new RevenueReportItem
                            {
                                ClientName = reader["ClientName"].ToString(),
                                ServiceName = reader["ServiceName"]?.ToString() ?? "General Payment",
                                PaymentDate = Convert.ToDateTime(reader["PaidDate"]),
                                Amount = Convert.ToDecimal(reader["Amount"]),
                                PaymentMethod = reader["PaymentMethod"]?.ToString() ?? "",
                                Reference = reader["Reference"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }
            return items;
        }

        public List<OutstandingReportItem> GetOutstandingReport()
        {
            var items = new List<OutstandingReportItem>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT p.*, c.FirstName || ' ' || c.LastName as ClientName, s.Name as ServiceName,
                           CAST(julianday('now') - julianday(p.DueDate) as INTEGER) as DaysOverdue
                    FROM Payments p
                    INNER JOIN Clients c ON p.ClientId = c.Id
                    LEFT JOIN Services s ON p.ServiceId = s.Id
                    WHERE p.IsPaid = 0
                    ORDER BY p.DueDate", connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new OutstandingReportItem
                            {
                                ClientName = reader["ClientName"].ToString(),
                                ServiceName = reader["ServiceName"]?.ToString() ?? "Payment",
                                DueDate = Convert.ToDateTime(reader["DueDate"]),
                                Amount = Convert.ToDecimal(reader["Amount"]),
                                DaysOverdue = Convert.ToInt32(reader["DaysOverdue"])
                            });
                        }
                    }
                }
            }
            return items;
        }

        public List<ServicePerformanceItem> GetServicePerformanceReport()
        {
            var items = new List<ServicePerformanceItem>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT s.Name as ServiceName,
                           COUNT(DISTINCT cs.ClientId) as ClientCount,
                           COUNT(p.Id) as TotalPayments,
                           COALESCE(SUM(CASE WHEN p.IsPaid = 1 THEN p.Amount ELSE 0 END), 0) as TotalRevenue,
                           COALESCE(AVG(cs.CustomPrice), s.BasePrice) as AveragePrice,
                           COALESCE(SUM(CASE WHEN p.IsPaid = 0 THEN p.Amount ELSE 0 END), 0) as OutstandingAmount
                    FROM Services s
                    LEFT JOIN ClientServices cs ON s.Id = cs.ServiceId
                    LEFT JOIN Payments p ON s.Id = p.ServiceId
                    WHERE s.IsActive = 1
                    GROUP BY s.Id, s.Name, s.BasePrice
                    ORDER BY TotalRevenue DESC", connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new ServicePerformanceItem
                            {
                                ServiceName = reader["ServiceName"].ToString(),
                                ClientCount = Convert.ToInt32(reader["ClientCount"]),
                                TotalPayments = Convert.ToInt32(reader["TotalPayments"]),
                                TotalRevenue = Convert.ToDecimal(reader["TotalRevenue"]),
                                AveragePrice = Convert.ToDecimal(reader["AveragePrice"]),
                                OutstandingAmount = Convert.ToDecimal(reader["OutstandingAmount"])
                            });
                        }
                    }
                }
            }
            return items;
        }

        public List<ClientProfitabilityItem> GetClientProfitabilityReport()
        {
            var items = new List<ClientProfitabilityItem>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT c.Id, c.FirstName || ' ' || c.LastName as ClientName,
                           COUNT(DISTINCT cs.ServiceId) as ServiceCount,
                           COALESCE(SUM(CASE WHEN p.IsPaid = 1 THEN p.Amount ELSE 0 END), 0) as TotalPaid,
                           COALESCE(SUM(CASE WHEN p.IsPaid = 0 THEN p.Amount ELSE 0 END), 0) as TotalOutstanding
                    FROM Clients c
                    LEFT JOIN ClientServices cs ON c.Id = cs.ClientId
                    LEFT JOIN Payments p ON c.Id = p.ClientId
                    WHERE c.IsActive = 1
                    GROUP BY c.Id, ClientName
                    ORDER BY TotalPaid DESC", connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new ClientProfitabilityItem
                            {
                                ClientName = reader["ClientName"].ToString(),
                                ServiceCount = Convert.ToInt32(reader["ServiceCount"]),
                                TotalPaid = Convert.ToDecimal(reader["TotalPaid"]),
                                TotalOutstanding = Convert.ToDecimal(reader["TotalOutstanding"])
                            });
                        }
                    }
                }
            }
            return items;
        }
    }

    // Report Models
    public class RevenueReportItem
    {
        public string ClientName { get; set; }
        public string ServiceName { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Reference { get; set; }
    }

    public class OutstandingReportItem
    {
        public string ClientName { get; set; }
        public string ServiceName { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public int DaysOverdue { get; set; }
    }

    public class ServicePerformanceItem
    {
        public string ServiceName { get; set; }
        public int ClientCount { get; set; }
        public int TotalPayments { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal OutstandingAmount { get; set; }
    }

    public class ClientProfitabilityItem
    {
        public string ClientName { get; set; }
        public int ServiceCount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
    }
}