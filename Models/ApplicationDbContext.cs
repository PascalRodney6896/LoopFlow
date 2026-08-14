using System.Data.Entity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace LoopFlow.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        static ApplicationDbContext()
        {
            Database.SetInitializer(new DbInitializer());
        }

        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<User> DomainUsers { get; set; }
        public DbSet<LoopAccount> LoopAccounts { get; set; }
        public DbSet<Buyer> Buyers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<CreditLimit> CreditLimits { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<SupplierSplit> SupplierSplits { get; set; }
        public DbSet<SupplierInvoice> SupplierInvoices { get; set; }
        public DbSet<FinancingRequest> FinancingRequests { get; set; }
        public DbSet<LoanTransaction> LoanTransactions { get; set; }
        public DbSet<SettlementBatch> SettlementBatches { get; set; }
        public DbSet<SweepConfiguration> SweepConfigurations { get; set; }
        public DbSet<SweepHistory> SweepHistories { get; set; }
        public DbSet<DelinquencyMonitoring> DelinquencyMonitorings { get; set; }
        public DbSet<TrustChainRecord> TrustChainRecords { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<InvestmentRecommendation> InvestmentRecommendations { get; set; }
        public DbSet<CashFlowForecast> CashFlowForecasts { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<BankSuspenseAccount> BankSuspenseAccounts { get; set; }
        public DbSet<BankSuspenseLedgerEntry> BankSuspenseLedgerEntries { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure explicit foreign key relationships for EF6
            modelBuilder.Entity<CreditLimit>()
                .HasRequired(c => c.Buyer)
                .WithMany()
                .HasForeignKey(c => c.BuyerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SweepConfiguration>()
                .HasRequired(s => s.Buyer)
                .WithMany()
                .HasForeignKey(s => s.BuyerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<LoopAccount>()
                .HasRequired(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Buyer>()
                .HasRequired(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Supplier>()
                .HasRequired(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SupplierSplit>()
                .HasRequired(ss => ss.Order)
                .WithMany(po => po.SupplierSplits)
                .HasForeignKey(ss => ss.OrderId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SupplierSplit>()
                .HasRequired(ss => ss.Supplier)
                .WithMany(s => s.SupplierSplits)
                .HasForeignKey(ss => ss.SupplierId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SupplierInvoice>()
                .HasRequired(si => si.Order)
                .WithMany(po => po.Invoices)
                .HasForeignKey(si => si.OrderId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SupplierInvoice>()
                .HasRequired(si => si.Supplier)
                .WithMany(s => s.SupplierInvoices)
                .HasForeignKey(si => si.SupplierId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<FinancingRequest>()
                .HasRequired(fr => fr.Order)
                .WithMany()
                .HasForeignKey(fr => fr.OrderId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<FinancingRequest>()
                .HasRequired(fr => fr.Buyer)
                .WithMany(b => b.FinancingRequests)
                .HasForeignKey(fr => fr.BuyerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<LoanTransaction>()
                .HasRequired(lt => lt.Buyer)
                .WithMany(b => b.LoanTransactions)
                .HasForeignKey(lt => lt.BuyerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SweepHistory>()
                .HasRequired(sh => sh.Buyer)
                .WithMany()
                .HasForeignKey(sh => sh.BuyerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TrustChainRecord>()
                .HasRequired(tc => tc.Order)
                .WithMany()
                .HasForeignKey(tc => tc.OrderId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CashFlowForecast>()
                .HasRequired(c => c.Buyer)
                .WithMany()
                .HasForeignKey(c => c.BuyerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<InvestmentRecommendation>()
                .HasRequired(i => i.Buyer)
                .WithMany()
                .HasForeignKey(i => i.BuyerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DelinquencyMonitoring>()
                .HasRequired(d => d.Buyer)
                .WithMany()
                .HasForeignKey(d => d.BuyerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<AuditLog>()
                .HasOptional(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Notification>()
                .HasRequired(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .WillCascadeOnDelete(false);
        }
    }
}
