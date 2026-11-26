using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Models;

namespace Smart_Xerox_Centre.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<CollectionLog> CollectionLogs { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomerSession> CustomerSessions { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderFile> OrderFiles { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<PrintAgent> PrintAgents { get; set; }

    public virtual DbSet<PrintAgentLog> PrintAgentLogs { get; set; }

    public virtual DbSet<PrintJob> PrintJobs { get; set; }

    public virtual DbSet<PrintJobAttempt> PrintJobAttempts { get; set; }

    public virtual DbSet<PrintJobStatus> PrintJobStatuses { get; set; }

    public virtual DbSet<PrintLog> PrintLogs { get; set; }

    public virtual DbSet<PrintSetting> PrintSettings { get; set; }

    public virtual DbSet<PrintToken> PrintTokens { get; set; }

    public virtual DbSet<Printer> Printers { get; set; }

    public virtual DbSet<PrinterCapability> PrinterCapabilities { get; set; }

    public virtual DbSet<Refund> Refunds { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Shop> Shops { get; set; }

    public virtual DbSet<ShopExpense> ShopExpenses { get; set; }

    public virtual DbSet<ShopPricing> ShopPricings { get; set; }

    public virtual DbSet<ShopQrcode> ShopQrcodes { get; set; }

    public virtual DbSet<ShopUser> ShopUsers { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<Tenant> Tenants { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<WastageLog> WastageLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => new { e.EntityName, e.EntityId }, "IX_AuditLogs_Entity");

            entity.HasIndex(e => new { e.TenantId, e.CreatedAtUtc }, "IX_AuditLogs_TenantDate").IsDescending(false, true);

            entity.Property(e => e.ActionName).HasMaxLength(50);
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);

            entity.HasOne(d => d.Tenant).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.TenantId)
                .HasConstraintName("FK_AuditLogs_Tenants");

            entity.HasOne(d => d.Shop).WithMany(p => p.AuditLogs)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId })
                .HasConstraintName("FK_AuditLogs_Shops");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasPrincipalKey(p => new { p.TenantId, p.UserId })
                .HasForeignKey(d => new { d.TenantId, d.UserId })
                .HasConstraintName("FK_AuditLogs_Users");
        });

        modelBuilder.Entity<CollectionLog>(entity =>
        {
            entity.HasIndex(e => e.PrintTokenId, "UX_CollectionLogs_OneSuccessfulCollection")
                .IsUnique()
                .HasFilter("([CollectionStatus]='Collected')");

            entity.Property(e => e.CollectedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CollectionStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Remarks).HasMaxLength(500);

            entity.HasOne(d => d.PrintToken).WithOne(p => p.CollectionLog)
                .HasForeignKey<CollectionLog>(d => d.PrintTokenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CollectionLogs_Tokens");

            entity.HasOne(d => d.Shop).WithMany(p => p.CollectionLogs)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CollectionLogs_Shops");

            entity.HasOne(d => d.User).WithMany(p => p.CollectionLogs)
                .HasPrincipalKey(p => new { p.TenantId, p.UserId })
                .HasForeignKey(d => new { d.TenantId, d.StaffUserId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CollectionLogs_Users");

            entity.HasOne(d => d.Order).WithMany(p => p.CollectionLogs)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId, p.OrderId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId, d.OrderId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CollectionLogs_Orders");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Phone }, "IX_Customers_TenantPhone").HasFilter("([Phone] IS NOT NULL)");

            entity.HasIndex(e => new { e.TenantId, e.CustomerId }, "UQ_Customers_TenantCustomer").IsUnique();

            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.UpdatedAtUtc).HasPrecision(0);

            entity.HasOne(d => d.Tenant).WithMany(p => p.Customers)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Customers_Tenants");
        });

        modelBuilder.Entity<CustomerSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);

            entity.HasIndex(e => new { e.ShopId, e.ExpiresAtUtc }, "IX_CustomerSessions_ShopExpiry");

            entity.HasIndex(e => new { e.TenantId, e.SessionId }, "UQ_CustomerSessions_TenantSession").IsUnique();

            entity.Property(e => e.SessionId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.EndedAtUtc).HasPrecision(0);
            entity.Property(e => e.ExpiresAtUtc).HasPrecision(0);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.QrcodeId).HasColumnName("QRCodeId");
            entity.Property(e => e.SessionTokenHash).HasMaxLength(256);
            entity.Property(e => e.StartedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasOne(d => d.Qrcode).WithMany(p => p.CustomerSessions)
                .HasForeignKey(d => d.QrcodeId)
                .HasConstraintName("FK_CustomerSessions_QR");

            entity.HasOne(d => d.Customer).WithMany(p => p.CustomerSessions)
                .HasPrincipalKey(p => new { p.TenantId, p.CustomerId })
                .HasForeignKey(d => new { d.TenantId, d.CustomerId })
                .HasConstraintName("FK_CustomerSessions_Customers");

            entity.HasOne(d => d.Shop).WithMany(p => p.CustomerSessions)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CustomerSessions_Shops");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => new { e.NotificationStatus, e.CreatedAtUtc }, "IX_Notifications_StatusDate");

            entity.Property(e => e.Channel)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.NotificationStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.ProviderReference).HasMaxLength(200);
            entity.Property(e => e.Recipient).HasMaxLength(256);
            entity.Property(e => e.SentAtUtc).HasPrecision(0);
            entity.Property(e => e.Subject).HasMaxLength(200);

            entity.HasOne(d => d.Tenant).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_Tenants");

            entity.HasOne(d => d.Customer).WithMany(p => p.Notifications)
                .HasPrincipalKey(p => new { p.TenantId, p.CustomerId })
                .HasForeignKey(d => new { d.TenantId, d.CustomerId })
                .HasConstraintName("FK_Notifications_Customers");

            entity.HasOne(d => d.Shop).WithMany(p => p.Notifications)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId })
                .HasConstraintName("FK_Notifications_Shops");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasPrincipalKey(p => new { p.TenantId, p.UserId })
                .HasForeignKey(d => new { d.TenantId, d.UserId })
                .HasConstraintName("FK_Notifications_Users");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => new { e.ShopId, e.OrderStatus, e.CreatedAtUtc }, "IX_Orders_ShopStatusDate").IsDescending(false, false, true);

            entity.HasIndex(e => new { e.TenantId, e.CustomerId, e.CreatedAtUtc }, "IX_Orders_TenantCustomerDate").IsDescending(false, false, true);

            entity.HasIndex(e => new { e.ShopId, e.OrderNumber }, "UQ_Orders_ShopNumber").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.OrderId }, "UQ_Orders_TenantOrder").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.ShopId, e.OrderId }, "UQ_Orders_TenantShopOrder").IsUnique();

            entity.Property(e => e.AdditionalCharges).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CompletedAtUtc).HasPrecision(0);
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("INR")
                .IsFixedLength();
            entity.Property(e => e.CustomerNote).HasMaxLength(500);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OrderNumber).HasMaxLength(40);
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Created");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAtUtc).HasPrecision(0);

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasPrincipalKey(p => new { p.TenantId, p.CustomerId })
                .HasForeignKey(d => new { d.TenantId, d.CustomerId })
                .HasConstraintName("FK_Orders_Customers");

            entity.HasOne(d => d.CustomerSession).WithMany(p => p.Orders)
                .HasPrincipalKey(p => new { p.TenantId, p.SessionId })
                .HasForeignKey(d => new { d.TenantId, d.SessionId })
                .HasConstraintName("FK_Orders_Sessions");

            entity.HasOne(d => d.Shop).WithMany(p => p.Orders)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Shops");
        });

        modelBuilder.Entity<OrderFile>(entity =>
        {
            entity.HasIndex(e => e.OrderId, "IX_OrderFiles_Order");

            entity.Property(e => e.ContentType).HasMaxLength(150);
            entity.Property(e => e.FileExtension).HasMaxLength(10);
            entity.Property(e => e.OriginalFileName).HasMaxLength(255);
            entity.Property(e => e.StoragePath).HasMaxLength(1000);
            entity.Property(e => e.StoredFileName).HasMaxLength(255);
            entity.Property(e => e.UploadStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Uploaded");
            entity.Property(e => e.UploadedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderFiles)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderFiles_Orders");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => new { e.OrderId, e.CreatedAtUtc }, "IX_Payments_Order").IsDescending(false, true);

            entity.HasIndex(e => new { e.TenantId, e.PaymentId }, "UQ_Payments_TenantPayment").IsUnique();

            entity.HasIndex(e => new { e.GatewayName, e.GatewayPaymentId }, "UX_Payments_GatewayPayment")
                .IsUnique()
                .HasFilter("([GatewayPaymentId] IS NOT NULL)");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("INR")
                .IsFixedLength();
            entity.Property(e => e.GatewayName).HasMaxLength(50);
            entity.Property(e => e.GatewayOrderId).HasMaxLength(150);
            entity.Property(e => e.GatewayPaymentId).HasMaxLength(150);
            entity.Property(e => e.PaidAtUtc).HasPrecision(0);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Created");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId, p.OrderId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId, d.OrderId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Orders");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasIndex(e => new { e.PaymentId, e.ReceivedAtUtc }, "IX_PaymentTransactions_PaymentDate").IsDescending(false, true);

            entity.HasIndex(e => e.GatewayEventId, "UX_PaymentTransactions_Event")
                .IsUnique()
                .HasFilter("([GatewayEventId] IS NOT NULL)");

            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.GatewayEventId).HasMaxLength(200);
            entity.Property(e => e.GatewayStatus).HasMaxLength(50);
            entity.Property(e => e.ProcessedAtUtc).HasPrecision(0);
            entity.Property(e => e.ReceivedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Payment).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentTransactions_Payments");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(e => e.PermissionCode, "UQ_Permissions_Code").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.PermissionCode).HasMaxLength(100);
            entity.Property(e => e.PermissionName).HasMaxLength(150);
        });

        modelBuilder.Entity<PrintAgent>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ShopId, e.PrintAgentId }, "UQ_PrintAgents_TenantShopAgent").IsUnique();

            entity.Property(e => e.AgentKeyHash).HasMaxLength(500);
            entity.Property(e => e.AgentName).HasMaxLength(150);
            entity.Property(e => e.AgentVersion).HasMaxLength(50);
            entity.Property(e => e.LastHeartbeatAtUtc).HasPrecision(0);
            entity.Property(e => e.MachineName).HasMaxLength(150);
            entity.Property(e => e.RegisteredAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Offline");

            entity.HasOne(d => d.Shop).WithMany(p => p.PrintAgents)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrintAgents_Shops");
        });

        modelBuilder.Entity<PrintAgentLog>(entity =>
        {
            entity.HasIndex(e => new { e.PrintAgentId, e.LoggedAtUtc }, "IX_PrintAgentLogs_AgentDate").IsDescending(false, true);

            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.LogLevel)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LoggedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Message).HasMaxLength(2000);

            entity.HasOne(d => d.PrintAgent).WithMany(p => p.PrintAgentLogs)
                .HasForeignKey(d => d.PrintAgentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrintAgentLogs_Agents");
        });

        modelBuilder.Entity<PrintJob>(entity =>
        {
            entity.HasIndex(e => e.OrderId, "IX_PrintJobs_Order");

            entity.HasIndex(e => new { e.ShopId, e.JobStatus, e.Priority, e.QueuedAtUtc }, "IX_PrintJobs_Queue");

            entity.HasIndex(e => e.JobGuid, "UQ_PrintJobs_JobGuid").IsUnique();

            entity.Property(e => e.CompletedAtUtc).HasPrecision(0);
            entity.Property(e => e.JobGuid).HasDefaultValueSql("(newid())");
            entity.Property(e => e.JobStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.LastError).HasMaxLength(2000);
            entity.Property(e => e.Priority).HasDefaultValue(5);
            entity.Property(e => e.QueuedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.StartedAtUtc).HasPrecision(0);

            entity.HasOne(d => d.Order).WithMany(p => p.PrintJobs)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId, p.OrderId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId, d.OrderId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrintJobs_Orders");

            entity.HasOne(d => d.PrintAgent).WithMany(p => p.PrintJobs)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId, p.PrintAgentId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId, d.PrintAgentId })
                .HasConstraintName("FK_PrintJobs_Agents");

            entity.HasOne(d => d.Printer).WithMany(p => p.PrintJobs)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId, p.PrinterId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId, d.PrinterId })
                .HasConstraintName("FK_PrintJobs_Printers");
        });

        modelBuilder.Entity<PrintJobAttempt>(entity =>
        {
            entity.HasIndex(e => new { e.PrintJobId, e.AttemptNumber }, "UQ_PrintJobAttempts_Number").IsUnique();

            entity.Property(e => e.AttemptStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.EndedAtUtc).HasPrecision(0);
            entity.Property(e => e.ErrorCode).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.StartedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.WindowsSpoolerJobId).HasMaxLength(100);

            entity.HasOne(d => d.PrintAgent).WithMany(p => p.PrintJobAttempts)
                .HasForeignKey(d => d.PrintAgentId)
                .HasConstraintName("FK_PrintJobAttempts_Agents");

            entity.HasOne(d => d.PrintJob).WithMany(p => p.PrintJobAttempts)
                .HasForeignKey(d => d.PrintJobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrintJobAttempts_Jobs");
        });

        modelBuilder.Entity<PrintJobStatus>(entity =>
        {
            entity.ToTable("PrintJobStatus");

            entity.HasIndex(e => new { e.PrintJobId, e.ChangedAtUtc }, "IX_PrintJobStatus_JobDate").IsDescending(false, true);

            entity.Property(e => e.ChangedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ChangedByType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NewStatus)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.OldStatus)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Remarks).HasMaxLength(1000);

            entity.HasOne(d => d.PrintJob).WithMany(p => p.PrintJobStatuses)
                .HasForeignKey(d => d.PrintJobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrintJobStatus_Jobs");
        });

        modelBuilder.Entity<PrintLog>(entity =>
        {
            entity.Property(e => e.LogLevel)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LoggedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Message).HasMaxLength(2000);

            entity.HasOne(d => d.PrintJob).WithMany(p => p.PrintLogs)
                .HasForeignKey(d => d.PrintJobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrintLogs_Jobs");
        });

        modelBuilder.Entity<PrintSetting>(entity =>
        {
            entity.Property(e => e.ColorMode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Copies).HasDefaultValue(1);
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LineTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Orientation)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Portrait");
            entity.Property(e => e.PageRange).HasMaxLength(100);
            entity.Property(e => e.PaperSize).HasMaxLength(30);
            entity.Property(e => e.RatePerPage).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Sides)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.OrderFile).WithMany(p => p.PrintSettings)
                .HasForeignKey(d => d.OrderFileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrintSettings_OrderFiles");

            entity.HasOne(d => d.Printer).WithMany(p => p.PrintSettings)
                .HasForeignKey(d => d.PrinterId)
                .HasConstraintName("FK_PrintSettings_Printers");
        });

        modelBuilder.Entity<PrintToken>(entity =>
        {
            entity.HasIndex(e => new { e.ShopId, e.TokenDisplay }, "IX_PrintTokens_ShopDisplay");

            entity.HasIndex(e => e.TokenDisplay, "UQ_PrintTokens_Display").IsUnique();

            entity.HasIndex(e => e.TokenHash, "UQ_PrintTokens_Hash").IsUnique();

            entity.HasIndex(e => e.OrderId, "UQ_PrintTokens_Order").IsUnique();

            entity.Property(e => e.ExpiresAtUtc).HasPrecision(0);
            entity.Property(e => e.GeneratedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TokenDisplay).HasMaxLength(20);
            entity.Property(e => e.TokenHash).HasMaxLength(256);
            entity.Property(e => e.UsedAtUtc).HasPrecision(0);

            entity.HasOne(d => d.Shop).WithMany(p => p.PrintTokens)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrintTokens_Shops");

            entity.HasOne(d => d.User).WithMany(p => p.PrintTokens)
                .HasPrincipalKey(p => new { p.TenantId, p.UserId })
                .HasForeignKey(d => new { d.TenantId, d.UsedByUserId })
                .HasConstraintName("FK_PrintTokens_Users");

            entity.HasOne(d => d.Order).WithMany(p => p.PrintTokens)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId, p.OrderId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId, d.OrderId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrintTokens_Orders");
        });

        modelBuilder.Entity<Printer>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ShopId, e.PrinterId }, "UQ_Printers_TenantShopPrinter").IsUnique();

            entity.HasIndex(e => e.ShopId, "UX_Printers_OneDefaultPerShop")
                .IsUnique()
                .HasFilter("([IsDefault]=(1))");

            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LastStatus).HasMaxLength(250);
            entity.Property(e => e.PrinterModel).HasMaxLength(150);
            entity.Property(e => e.PrinterName).HasMaxLength(150);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAtUtc).HasPrecision(0);
            entity.Property(e => e.WindowsPrinterName).HasMaxLength(250);

            entity.HasOne(d => d.Shop).WithMany(p => p.Printers)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Printers_Shops");
        });

        modelBuilder.Entity<PrinterCapability>(entity =>
        {
            entity.HasIndex(e => new { e.PrinterId, e.PaperSize }, "UQ_PrinterCapabilities").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PaperSize).HasMaxLength(30);
            entity.Property(e => e.SupportsBlackWhite).HasDefaultValue(true);

            entity.HasOne(d => d.Printer).WithMany(p => p.PrinterCapabilities)
                .HasForeignKey(d => d.PrinterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrinterCapabilities_Printers");
        });

        modelBuilder.Entity<Refund>(entity =>
        {
            entity.Property(e => e.CompletedAtUtc).HasPrecision(0);
            entity.Property(e => e.GatewayRefundId).HasMaxLength(150);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.RefundAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RefundStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Requested");
            entity.Property(e => e.RequestedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Payment).WithMany(p => p.Refunds)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Refunds_Payments");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleName, "UX_Roles_SystemName")
                .IsUnique()
                .HasFilter("([TenantId] IS NULL)");

            entity.HasIndex(e => new { e.TenantId, e.RoleName }, "UX_Roles_TenantName")
                .IsUnique()
                .HasFilter("([TenantId] IS NOT NULL)");

            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.RoleName).HasMaxLength(80);

            entity.HasOne(d => d.Tenant).WithMany(p => p.Roles)
                .HasForeignKey(d => d.TenantId)
                .HasConstraintName("FK_Roles_Tenants");

            entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_RolePermissions_Permissions"),
                    l => l.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_RolePermissions_Roles"),
                    j =>
                    {
                        j.HasKey("RoleId", "PermissionId");
                        j.ToTable("RolePermissions");
                    });
        });

        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ShopCode }, "UQ_Shops_TenantCode").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.ShopId }, "UQ_Shops_TenantShop").IsUnique();

            entity.Property(e => e.AddressLine1).HasMaxLength(250);
            entity.Property(e => e.AddressLine2).HasMaxLength(250);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.ContactPerson).HasMaxLength(150);
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasDefaultValue("India");
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.ShopCode).HasMaxLength(50);
            entity.Property(e => e.ShopName).HasMaxLength(200);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TimeZone)
                .HasMaxLength(100)
                .HasDefaultValue("India Standard Time");
            entity.Property(e => e.UpdatedAtUtc).HasPrecision(0);

            entity.HasOne(d => d.Tenant).WithMany(p => p.Shops)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shops_Tenants");
        });

        modelBuilder.Entity<ShopExpense>(entity =>
        {
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ExpenseCategory).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.ShopExpenses)
                .HasPrincipalKey(p => new { p.TenantId, p.UserId })
                .HasForeignKey(d => new { d.TenantId, d.RecordedByUserId })
                .HasConstraintName("FK_ShopExpenses_Users");

            entity.HasOne(d => d.Shop).WithMany(p => p.ShopExpenses)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShopExpenses_Shops");
        });

        modelBuilder.Entity<ShopPricing>(entity =>
        {
            entity.ToTable("ShopPricing");

            entity.HasIndex(e => new { e.ShopId, e.ColorMode, e.Sides, e.PaperSize, e.IsActive, e.EffectiveFromUtc }, "IX_ShopPricing_Lookup");

            entity.Property(e => e.AdditionalCharge).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ColorMode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EffectiveFromUtc).HasPrecision(0);
            entity.Property(e => e.EffectiveToUtc).HasPrecision(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PaperSize).HasMaxLength(30);
            entity.Property(e => e.RatePerPage).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Sides)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TaxRate).HasColumnType("decimal(7, 4)");

            entity.HasOne(d => d.Shop).WithMany(p => p.ShopPricings)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShopPricing_Shops");
        });

        modelBuilder.Entity<ShopQrcode>(entity =>
        {
            entity.HasKey(e => e.QrcodeId);

            entity.ToTable("ShopQRCodes");

            entity.HasIndex(e => e.QrcodeValue, "UQ_ShopQRCodes_Value").IsUnique();

            entity.HasIndex(e => e.ShopId, "UX_ShopQRCodes_OneActivePerShop")
                .IsUnique()
                .HasFilter("([IsActive]=(1))");

            entity.Property(e => e.QrcodeId).HasColumnName("QRCodeId");
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DeactivatedAtUtc).HasPrecision(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.QrcodeValue)
                .HasMaxLength(200)
                .HasColumnName("QRCodeValue");
            entity.Property(e => e.QrimagePath)
                .HasMaxLength(500)
                .HasColumnName("QRImagePath");

            entity.HasOne(d => d.Shop).WithMany(p => p.ShopQrcodes)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShopQRCodes_Shops");
        });

        modelBuilder.Entity<ShopUser>(entity =>
        {
            entity.HasIndex(e => new { e.ShopId, e.UserId }, "UQ_ShopUsers").IsUnique();

            entity.Property(e => e.AssignedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Shop).WithMany(p => p.ShopUsers)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShopUsers_Shops");

            entity.HasOne(d => d.User).WithMany(p => p.ShopUsers)
                .HasPrincipalKey(p => new { p.TenantId, p.UserId })
                .HasForeignKey(d => new { d.TenantId, d.UserId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShopUsers_Users");
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BillingCycle)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasDefaultValue("INR")
                .IsFixedLength();
            entity.Property(e => e.PlanName).HasMaxLength(100);
            entity.Property(e => e.SubscriptionStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Trial");

            entity.HasOne(d => d.Tenant).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subscriptions_Tenants");

            entity.HasOne(d => d.Shop).WithMany(p => p.Subscriptions)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId })
                .HasConstraintName("FK_Subscriptions_Shops");
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(e => e.TenantCode, "UQ_Tenants_Code").IsUnique();

            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.TenantCode).HasMaxLength(50);
            entity.Property(e => e.TenantName).HasMaxLength(200);
            entity.Property(e => e.UpdatedAtUtc).HasPrecision(0);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Phone }, "IX_Users_TenantPhone").HasFilter("([Phone] IS NOT NULL)");

            entity.HasIndex(e => new { e.TenantId, e.UserId }, "UQ_Users_TenantUser").IsUnique();

            entity.HasIndex(e => new { e.TenantId, e.Email }, "UX_Users_TenantEmail")
                .IsUnique()
                .HasFilter("([Email] IS NOT NULL)");

            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.LastLoginAtUtc).HasPrecision(0);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAtUtc).HasPrecision(0);

            entity.HasOne(d => d.Tenant).WithMany(p => p.Users)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Tenants");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });

            entity.Property(e => e.AssignedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Roles");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Users");
        });

        modelBuilder.Entity<WastageLog>(entity =>
        {
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EstimatedCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.WastageType)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.PrintJob).WithMany(p => p.WastageLogs)
                .HasForeignKey(d => d.PrintJobId)
                .HasConstraintName("FK_WastageLogs_Jobs");

            entity.HasOne(d => d.User).WithMany(p => p.WastageLogs)
                .HasPrincipalKey(p => new { p.TenantId, p.UserId })
                .HasForeignKey(d => new { d.TenantId, d.RecordedByUserId })
                .HasConstraintName("FK_WastageLogs_Users");

            entity.HasOne(d => d.Shop).WithMany(p => p.WastageLogs)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WastageLogs_Shops");

            entity.HasOne(d => d.Order).WithMany(p => p.WastageLogs)
                .HasPrincipalKey(p => new { p.TenantId, p.ShopId, p.OrderId })
                .HasForeignKey(d => new { d.TenantId, d.ShopId, d.OrderId })
                .HasConstraintName("FK_WastageLogs_Orders");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
