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
                           COALESCE((SELECT SUM(ch.Amount - ch.PaidAmount) 
                                    FROM Charges ch 
                                    WHERE ch.ClientId = c.Id AND ch.IsPaid = 0), 0) as Balance
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
                           COALESCE((SELECT SUM(ch.Amount - ch.PaidAmount) 
                                    FROM Charges ch 
                                    WHERE ch.ClientId = c.Id AND ch.IsPaid = 0), 0) as Balance
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
                        // Check for active charges
                        using (var checkCmd = new SQLiteCommand(
                            "SELECT COUNT(*) FROM Charges WHERE ClientId = @id AND IsPaid = 0", connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", clientId);
                            var unpaidCharges = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (unpaidCharges > 0)
                            {
                                throw new InvalidOperationException($"Cannot delete client with {unpaidCharges} unpaid charges.");
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
                string whereClause = "c.IsActive = 1";

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchField.ToLower())
                    {
                        case "firstname":
                            whereClause += " AND c.FirstName LIKE @search";
                            break;
                        case "lastname":
                            whereClause += " AND c.LastName LIKE @search";
                            break;
                        case "businessname":
                            whereClause += " AND c.BusinessName LIKE @search";
                            break;
                        case "vatnumber":
                            whereClause += " AND c.VATNumber LIKE @search";
                            break;
                        default:
                            whereClause += @" AND (c.FirstName LIKE @search OR c.LastName LIKE @search 
                                            OR c.BusinessName LIKE @search OR c.VATNumber LIKE @search 
                                            OR c.Email LIKE @search OR c.Phone LIKE @search)";
                            break;
                    }
                }

                using (var cmd = new SQLiteCommand($@"
                    SELECT c.*, 
                           COALESCE((SELECT SUM(ch.Amount - ch.PaidAmount) 
                                    FROM Charges ch 
                                    WHERE ch.ClientId = c.Id AND ch.IsPaid = 0), 0) as Balance
                    FROM Clients c 
                    WHERE {whereClause}
                    ORDER BY c.FirstName, c.LastName", connection))
                {
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%");
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
                    INSERT INTO Services (Name, Description, BasePrice, Category)
                    VALUES (@name, @description, @basePrice, @category);
                    SELECT last_insert_rowid();", connection))
                {
                    cmd.Parameters.AddWithValue("@name", service.Name);
                    cmd.Parameters.AddWithValue("@description", service.Description ?? "");
                    cmd.Parameters.AddWithValue("@basePrice", service.BasePrice);
                    cmd.Parameters.AddWithValue("@category", service.Category ?? "");

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
                        BasePrice = @basePrice, 
                        Category = @category
                    WHERE Id = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", service.Id);
                    cmd.Parameters.AddWithValue("@name", service.Name);
                    cmd.Parameters.AddWithValue("@description", service.Description ?? "");
                    cmd.Parameters.AddWithValue("@basePrice", service.BasePrice);
                    cmd.Parameters.AddWithValue("@category", service.Category ?? "");

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
                      AND (Name LIKE @search OR Description LIKE @search OR Category LIKE @search)
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
                Category = reader["Category"].ToString(),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                IsActive = Convert.ToBoolean(reader["IsActive"])
            };
        }
    }

    // ===================== CHARGE MANAGER =====================
    public class ChargeManager
    {
        private DatabaseManager dbManager;

        public ChargeManager(DatabaseManager dbManager)
        {
            this.dbManager = dbManager;
        }

        public List<Charge> GetRecentCharges(int count)
        {
            var charges = new List<Charge>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT ch.*, c.FirstName || ' ' || c.LastName as ClientName, s.Name as ServiceName
                    FROM Charges ch
                    INNER JOIN Clients c ON ch.ClientId = c.Id
                    LEFT JOIN Services s ON ch.ServiceId = s.Id
                    ORDER BY ch.CreatedDate DESC
                    LIMIT @count", connection))
                {
                    cmd.Parameters.AddWithValue("@count", count);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            charges.Add(MapChargeFromReader(reader));
                        }
                    }
                }
            }
            return charges;
        }

        public decimal GetTotalOutstanding()
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT COALESCE(SUM(Amount - PaidAmount), 0) FROM Charges WHERE IsPaid = 0",
                    connection))
                {
                    return Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
        }

        public List<UpcomingCharge> GetUpcomingAutoCharges(DateTime fromDate, DateTime toDate)
        {
            var charges = new List<UpcomingCharge>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT cs.*, c.FirstName || ' ' || c.LastName as ClientName, s.Name as ServiceName,
                           COALESCE(cs.CustomPrice, s.BasePrice) as Amount
                    FROM ClientServices cs
                    INNER JOIN Clients c ON cs.ClientId = c.Id
                    INNER JOIN Services s ON cs.ServiceId = s.Id
                    WHERE cs.IsActive = 1 
                      AND cs.ServiceType = 'Periodic'
                      AND cs.NextChargeDate BETWEEN @fromDate AND @toDate
                    ORDER BY cs.NextChargeDate", connection))
                {
                    cmd.Parameters.AddWithValue("@fromDate", fromDate.Date);
                    cmd.Parameters.AddWithValue("@toDate", toDate.Date);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            charges.Add(new UpcomingCharge
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                ClientName = reader["ClientName"].ToString(),
                                ServiceName = reader["ServiceName"].ToString(),
                                Amount = Convert.ToDecimal(reader["Amount"]),
                                NextChargeDate = reader["NextChargeDate"] != DBNull.Value ?
                                    Convert.ToDateTime(reader["NextChargeDate"]) : DateTime.Now.AddDays(30),
                                Period = reader["Period"].ToString()
                            });
                        }
                    }
                }
            }
            return charges;
        }

        public List<Charge> GetAllOutstandingCharges(DateTime fromDate, DateTime toDate)
        {
            var charges = new List<Charge>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT ch.*, c.FirstName || ' ' || c.LastName as ClientName, s.Name as ServiceName
                    FROM Charges ch
                    INNER JOIN Clients c ON ch.ClientId = c.Id
                    LEFT JOIN Services s ON ch.ServiceId = s.Id
                    WHERE ch.IsPaid = 0 
                      AND ch.ChargeDate BETWEEN @fromDate AND @toDate
                    ORDER BY ch.DueDate", connection))
                {
                    cmd.Parameters.AddWithValue("@fromDate", fromDate.Date);
                    cmd.Parameters.AddWithValue("@toDate", toDate.Date);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            charges.Add(MapChargeFromReader(reader));
                        }
                    }
                }
            }
            return charges;
        }

        public List<DelayedPayment> GetOverdueCharges()
        {
            var delayed = new List<DelayedPayment>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT c.Id as ClientId, c.FirstName || ' ' || c.LastName as ClientName,
                           c.Phone, c.Email,
                           SUM(ch.Amount - ch.PaidAmount) as TotalDue,
                           MIN(ch.DueDate) as OldestDueDate,
                           CAST(julianday('now') - julianday(MIN(ch.DueDate)) as INTEGER) as DaysOverdue
                    FROM Charges ch
                    INNER JOIN Clients c ON ch.ClientId = c.Id
                    WHERE ch.IsPaid = 0 AND ch.DueDate < date('now')
                    GROUP BY c.Id, ClientName, c.Phone, c.Email
                    ORDER BY DaysOverdue DESC", connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            delayed.Add(new DelayedPayment
                            {
                                ClientId = Convert.ToInt32(reader["ClientId"]),
                                ClientName = reader["ClientName"].ToString(),
                                TotalDue = Convert.ToDecimal(reader["TotalDue"]),
                                OldestDueDate = Convert.ToDateTime(reader["OldestDueDate"]),
                                DaysOverdue = Convert.ToInt32(reader["DaysOverdue"]),
                                Phone = reader["Phone"].ToString(),
                                Email = reader["Email"].ToString()
                            });
                        }
                    }
                }
            }
            return delayed;
        }

        public List<Charge> GetClientOutstandingCharges(int clientId)
        {
            var charges = new List<Charge>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT ch.*, s.Name as ServiceName
                    FROM Charges ch
                    LEFT JOIN Services s ON ch.ServiceId = s.Id
                    WHERE ch.ClientId = @clientId AND ch.IsPaid = 0
                    ORDER BY ch.DueDate", connection))
                {
                    cmd.Parameters.AddWithValue("@clientId", clientId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            charges.Add(MapChargeFromReader(reader));
                        }
                    }
                }
            }
            return charges;
        }

        public void CreateCharge(Charge charge)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SQLiteCommand(@"
                            INSERT INTO Charges (ClientId, ServiceId, ChargeType, Description, Amount, ChargeDate, DueDate, CreatedBy)
                            VALUES (@clientId, @serviceId, @chargeType, @description, @amount, @chargeDate, @dueDate, @createdBy)", connection))
                        {
                            cmd.Parameters.AddWithValue("@clientId", charge.ClientId);
                            cmd.Parameters.AddWithValue("@serviceId", (object)charge.ServiceId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@chargeType", charge.ChargeType);
                            cmd.Parameters.AddWithValue("@description", charge.Description ?? "");
                            cmd.Parameters.AddWithValue("@amount", charge.Amount);
                            cmd.Parameters.AddWithValue("@chargeDate", charge.ChargeDate);
                            cmd.Parameters.AddWithValue("@dueDate", charge.DueDate);
                            cmd.Parameters.AddWithValue("@createdBy", charge.CreatedBy);

                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        LogManager.LogInfo($"Charge created for client {charge.ClientId}, amount: {charge.Amount}");
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private Charge MapChargeFromReader(SQLiteDataReader reader)
        {
            return new Charge
            {
                Id = Convert.ToInt32(reader["Id"]),
                ClientId = Convert.ToInt32(reader["ClientId"]),
                ServiceId = reader["ServiceId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["ServiceId"]),
                ChargeType = reader["ChargeType"].ToString(),
                Description = reader["Description"].ToString(),
                Amount = Convert.ToDecimal(reader["Amount"]),
                ChargeDate = Convert.ToDateTime(reader["ChargeDate"]),
                DueDate = Convert.ToDateTime(reader["DueDate"]),
                IsPaid = Convert.ToBoolean(reader["IsPaid"]),
                PaidAmount = Convert.ToDecimal(reader["PaidAmount"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                CreatedBy = reader["CreatedBy"].ToString(),
                ClientName = reader.GetOrdinal("ClientName") >= 0 ? reader["ClientName"].ToString() : "",
                ServiceName = reader.GetOrdinal("ServiceName") >= 0 ? reader["ServiceName"]?.ToString() : ""
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

        public List<Payment> GetRecentPayments(int count)
        {
            var payments = new List<Payment>();
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(@"
                    SELECT p.*, c.FirstName || ' ' || c.LastName as ClientName
                    FROM Payments p
                    INNER JOIN Clients c ON p.ClientId = c.Id
                    ORDER BY p.CreatedDate DESC
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

        public decimal GetTotalRevenue()
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT COALESCE(SUM(Amount), 0) FROM Payments",
                    connection))
                {
                    return Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
        }

        public void CreatePayment(Payment payment)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insert payment
                        using (var cmd = new SQLiteCommand(@"
                            INSERT INTO Payments (ClientId, ChargeId, Amount, PaymentDate, PaymentMethod, Reference, Notes, CreatedBy)
                            VALUES (@clientId, @chargeId, @amount, @paymentDate, @paymentMethod, @reference, @notes, @createdBy)", connection))
                        {
                            cmd.Parameters.AddWithValue("@clientId", payment.ClientId);
                            cmd.Parameters.AddWithValue("@chargeId", (object)payment.ChargeId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@amount", payment.Amount);
                            cmd.Parameters.AddWithValue("@paymentDate", payment.PaymentDate);
                            cmd.Parameters.AddWithValue("@paymentMethod", payment.PaymentMethod ?? "");
                            cmd.Parameters.AddWithValue("@reference", payment.Reference ?? "");
                            cmd.Parameters.AddWithValue("@notes", payment.Notes ?? "");
                            cmd.Parameters.AddWithValue("@createdBy", payment.CreatedBy);

                            cmd.ExecuteNonQuery();
                        }

                        // Update charge if linked
                        if (payment.ChargeId.HasValue)
                        {
                            using (var updateCmd = new SQLiteCommand(@"
                                UPDATE Charges 
                                SET PaidAmount = PaidAmount + @amount,
                                    IsPaid = CASE WHEN PaidAmount + @amount >= Amount THEN 1 ELSE 0 END
                                WHERE Id = @chargeId", connection))
                            {
                                updateCmd.Parameters.AddWithValue("@amount", payment.Amount);
                                updateCmd.Parameters.AddWithValue("@chargeId", payment.ChargeId.Value);
                                updateCmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        LogManager.LogInfo($"Payment recorded for client {payment.ClientId}, amount: {payment.Amount}");
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private Payment MapPaymentFromReader(SQLiteDataReader reader)
        {
            return new Payment
            {
                Id = Convert.ToInt32(reader["Id"]),
                ClientId = Convert.ToInt32(reader["ClientId"]),
                ChargeId = reader["ChargeId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["ChargeId"]),
                Amount = Convert.ToDecimal(reader["Amount"]),
                PaymentDate = Convert.ToDateTime(reader["PaymentDate"]),
                PaymentMethod = reader["PaymentMethod"]?.ToString(),
                Reference = reader["Reference"]?.ToString(),
                Notes = reader["Notes"]?.ToString(),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                CreatedBy = reader["CreatedBy"]?.ToString(),
                ClientName = reader["ClientName"].ToString()
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

                // Calculate next charge date
                var nextChargeDate = CalculateNextChargeDate(clientService);

                using (var cmd = new SQLiteCommand(@"
                    INSERT INTO ClientServices (ClientId, ServiceId, ServiceType, CustomPrice, Period, ChargeDay, StartDate, NextChargeDate, IsActive)
                    VALUES (@clientId, @serviceId, @serviceType, @customPrice, @period, @chargeDay, @startDate, @nextChargeDate, @isActive)", connection))
                {
                    cmd.Parameters.AddWithValue("@clientId", clientService.ClientId);
                    cmd.Parameters.AddWithValue("@serviceId", clientService.ServiceId);
                    cmd.Parameters.AddWithValue("@serviceType", clientService.ServiceType);
                    cmd.Parameters.AddWithValue("@customPrice", (object)clientService.CustomPrice ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@period", (object)clientService.Period ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@chargeDay", (object)clientService.ChargeDay ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@startDate", clientService.StartDate);
                    cmd.Parameters.AddWithValue("@nextChargeDate", nextChargeDate);
                    cmd.Parameters.AddWithValue("@isActive", clientService.IsActive);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateClientService(ClientService clientService)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();

                // Calculate next charge date only if it hasn't been set already
                DateTime nextChargeDate;
                if (clientService.NextChargeDate == null || clientService.NextChargeDate.Value.Year == 1)
                {
                    nextChargeDate = CalculateNextChargeDate(clientService);
                }
                else
                {
                    nextChargeDate = clientService.NextChargeDate.Value;
                }

                using (var cmd = new SQLiteCommand(@"
                    UPDATE ClientServices SET 
                        ServiceId = @serviceId,
                        ServiceType = @serviceType,
                        CustomPrice = @customPrice,
                        Period = @period,
                        ChargeDay = @chargeDay,
                        StartDate = @startDate,
                        NextChargeDate = @nextChargeDate,
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
                    cmd.Parameters.AddWithValue("@nextChargeDate", nextChargeDate);
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
                using (var cmd = new SQLiteCommand("DELETE FROM ClientServices WHERE Id = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ProcessAutoCharge(int clientServiceId)
        {
            using (var connection = dbManager.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Get client service details
                        ClientService clientService = null;
                        Client client = null;
                        decimal amount = 0;

                        using (var cmd = new SQLiteCommand(@"
                            SELECT cs.*, s.Name as ServiceName, c.PaymentTermsDays,
                                   COALESCE(cs.CustomPrice, s.BasePrice) as Amount
                            FROM ClientServices cs
                            INNER JOIN Services s ON cs.ServiceId = s.Id
                            INNER JOIN Clients c ON cs.ClientId = c.Id
                            WHERE cs.Id = @id", connection))
                        {
                            cmd.Parameters.AddWithValue("@id", clientServiceId);

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    clientService = new ClientService
                                    {
                                        Id = Convert.ToInt32(reader["Id"]),
                                        ClientId = Convert.ToInt32(reader["ClientId"]),
                                        ServiceId = Convert.ToInt32(reader["ServiceId"]),
                                        ServiceType = reader["ServiceType"].ToString(),
                                        Period = reader["Period"]?.ToString(),
                                        ChargeDay = reader["ChargeDay"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["ChargeDay"]),
                                        ServiceName = reader["ServiceName"].ToString(),
                                        LastChargeDate = reader["LastChargeDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["LastChargeDate"])
                                    };
                                    amount = Convert.ToDecimal(reader["Amount"]);
                                    var paymentTerms = Convert.ToInt32(reader["PaymentTermsDays"]);

                                    client = new Client { PaymentTermsDays = paymentTerms };
                                }
                            }
                        }

                        if (clientService != null)
                        {
                            // Create charge
                            var charge = new Charge
                            {
                                ClientId = clientService.ClientId,
                                ServiceId = clientService.ServiceId,
                                ChargeType = "Auto",
                                Description = $"Auto charge for {clientService.ServiceName} ({clientService.Period})",
                                Amount = amount,
                                ChargeDate = DateTime.Now.Date,
                                DueDate = DateTime.Now.Date.AddDays(client.PaymentTermsDays),
                                CreatedBy = "System"
                            };

                            // Insert charge
                            using (var cmd = new SQLiteCommand(@"
                                INSERT INTO Charges (ClientId, ServiceId, ChargeType, Description, Amount, ChargeDate, DueDate, CreatedBy)
                                VALUES (@clientId, @serviceId, @chargeType, @description, @amount, @chargeDate, @dueDate, @createdBy)", connection))
                            {
                                cmd.Parameters.AddWithValue("@clientId", charge.ClientId);
                                cmd.Parameters.AddWithValue("@serviceId", charge.ServiceId);
                                cmd.Parameters.AddWithValue("@chargeType", charge.ChargeType);
                                cmd.Parameters.AddWithValue("@description", charge.Description);
                                cmd.Parameters.AddWithValue("@amount", charge.Amount);
                                cmd.Parameters.AddWithValue("@chargeDate", charge.ChargeDate);
                                cmd.Parameters.AddWithValue("@dueDate", charge.DueDate);
                                cmd.Parameters.AddWithValue("@createdBy", charge.CreatedBy);

                                cmd.ExecuteNonQuery();
                            }

                            // Update client service with proper next charge date calculation
                            clientService.LastChargeDate = DateTime.Now.Date;
                            var nextChargeDate = CalculateNextChargeDate(clientService);

                            using (var updateCmd = new SQLiteCommand(@"
                                UPDATE ClientServices 
                                SET LastChargeDate = @lastChargeDate,
                                    NextChargeDate = @nextChargeDate
                                WHERE Id = @id", connection))
                            {
                                updateCmd.Parameters.AddWithValue("@lastChargeDate", DateTime.Now.Date);
                                updateCmd.Parameters.AddWithValue("@nextChargeDate", nextChargeDate);
                                updateCmd.Parameters.AddWithValue("@id", clientServiceId);
                                updateCmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        LogManager.LogInfo($"Auto charge processed for client service {clientServiceId}");
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private DateTime CalculateNextChargeDate(ClientService clientService)
        {
            var baseDate = clientService.LastChargeDate ?? clientService.StartDate;

            if (clientService.ServiceType != "Periodic" || string.IsNullOrEmpty(clientService.Period))
                return baseDate.AddDays(30); // Default 30 days

            switch (clientService.Period.ToLower())
            {
                case "weekly":
                    return baseDate.AddDays(7);
                case "monthly":
                    var nextMonth = baseDate.AddMonths(1);
                    if (clientService.ChargeDay.HasValue)
                    {
                        var day = clientService.ChargeDay.Value;
                        // Handle day overflow (e.g., 31st in a month with 30 days)
                        var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                        if (day > daysInMonth)
                            day = daysInMonth;
                        return new DateTime(nextMonth.Year, nextMonth.Month, day);
                    }
                    return nextMonth;
                case "quarterly":
                    var nextQuarter = baseDate.AddMonths(3);
                    if (clientService.ChargeDay.HasValue)
                    {
                        var day = clientService.ChargeDay.Value;
                        var daysInMonth = DateTime.DaysInMonth(nextQuarter.Year, nextQuarter.Month);
                        if (day > daysInMonth)
                            day = daysInMonth;
                        return new DateTime(nextQuarter.Year, nextQuarter.Month, day);
                    }
                    return nextQuarter;
                case "yearly":
                    var nextYear = baseDate.AddYears(1);
                    if (clientService.ChargeDay.HasValue && clientService.ChargeDay.Value <= 365)
                    {
                        return new DateTime(nextYear.Year, 1, 1).AddDays(clientService.ChargeDay.Value - 1);
                    }
                    return nextYear;
                default:
                    return baseDate.AddDays(30);
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
                LastChargeDate = reader["LastChargeDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["LastChargeDate"]),
                NextChargeDate = reader["NextChargeDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["NextChargeDate"]),
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                ServiceName = reader["ServiceName"].ToString()
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
                    LEFT JOIN Charges ch ON p.ChargeId = ch.Id
                    LEFT JOIN Services s ON ch.ServiceId = s.Id
                    WHERE p.PaymentDate BETWEEN @fromDate AND @toDate
                    ORDER BY p.PaymentDate DESC", connection))
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
                                PaymentDate = Convert.ToDateTime(reader["PaymentDate"]),
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
                    SELECT ch.*, c.FirstName || ' ' || c.LastName as ClientName, s.Name as ServiceName
                    FROM Charges ch
                    INNER JOIN Clients c ON ch.ClientId = c.Id
                    LEFT JOIN Services s ON ch.ServiceId = s.Id
                    WHERE ch.IsPaid = 0
                    ORDER BY ch.DueDate", connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new OutstandingReportItem
                            {
                                ClientName = reader["ClientName"].ToString(),
                                ServiceName = reader["ServiceName"]?.ToString() ?? reader["Description"].ToString(),
                                ChargeDate = Convert.ToDateTime(reader["ChargeDate"]),
                                DueDate = Convert.ToDateTime(reader["DueDate"]),
                                Amount = Convert.ToDecimal(reader["Amount"]),
                                PaidAmount = Convert.ToDecimal(reader["PaidAmount"])
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
                           COUNT(ch.Id) as TotalCharges,
                           COALESCE(SUM(p.Amount), 0) as TotalRevenue,
                           COALESCE(AVG(cs.CustomPrice), s.BasePrice) as AveragePrice,
                           COALESCE(SUM(CASE WHEN ch.IsPaid = 0 THEN ch.Amount - ch.PaidAmount ELSE 0 END), 0) as OutstandingAmount
                    FROM Services s
                    LEFT JOIN ClientServices cs ON s.Id = cs.ServiceId
                    LEFT JOIN Charges ch ON s.Id = ch.ServiceId
                    LEFT JOIN Payments p ON ch.Id = p.ChargeId
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
                                TotalCharges = Convert.ToInt32(reader["TotalCharges"]),
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
                           COALESCE(SUM(ch.Amount), 0) as TotalCharged,
                           COALESCE(SUM(p.Amount), 0) as TotalPaid
                    FROM Clients c
                    LEFT JOIN ClientServices cs ON c.Id = cs.ClientId
                    LEFT JOIN Charges ch ON c.Id = ch.ClientId
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
                                TotalCharged = Convert.ToDecimal(reader["TotalCharged"]),
                                TotalPaid = Convert.ToDecimal(reader["TotalPaid"])
                            });
                        }
                    }
                }
            }
            return items;
        }
    }

    // Report Models
    public class UpcomingCharge
    {
        public int Id { get; set; }
        public string ClientName { get; set; }
        public string ServiceName { get; set; }
        public decimal Amount { get; set; }
        public DateTime NextChargeDate { get; set; }
        public string Period { get; set; }
    }

    public class DelayedPayment
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public decimal TotalDue { get; set; }
        public DateTime OldestDueDate { get; set; }
        public int DaysOverdue { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }

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
        public DateTime ChargeDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Outstanding => Amount - PaidAmount;
        public int DaysOverdue => (DateTime.Now.Date - DueDate).Days;
    }

    public class ServicePerformanceItem
    {
        public string ServiceName { get; set; }
        public int ClientCount { get; set; }
        public int TotalCharges { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal OutstandingAmount { get; set; }
    }

    public class ClientProfitabilityItem
    {
        public string ClientName { get; set; }
        public int ServiceCount { get; set; }
        public decimal TotalCharged { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Outstanding => TotalCharged - TotalPaid;
        public decimal ProfitMargin { get; set; }
    }
}