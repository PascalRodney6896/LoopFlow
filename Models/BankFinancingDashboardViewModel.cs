using System;
using System.Collections.Generic;

namespace LoopFlow.Models
{
    public class BankFinancingDashboardViewModel
    {
        // Top-Level Portfolio KPIs
        public decimal TotalApprovedLimit { get; set; }
        public decimal TotalUtilised { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal PendingFinancingAmount { get; set; }
        public int PendingFinancingCount { get; set; }
        public decimal PendingSupplierPaymentsAmount { get; set; }
        public int PendingSupplierPaymentsCount { get; set; }
        public decimal OverdueFinancingAmount { get; set; }
        public int OverdueFinancingCount { get; set; }

        // Financing Pipeline Stages (12 stages)
        public List<PipelineStageItem> PipelineStages { get; set; } = new List<PipelineStageItem>();

        // Detailed Datasets
        public List<PurchaseOrder> FinancingTransactions { get; set; } = new List<PurchaseOrder>();
        public List<Buyer> Facilities { get; set; } = new List<Buyer>();
        public List<SupplierInvoice> Invoices { get; set; } = new List<SupplierInvoice>();
        public List<SupplierSplit> SupplierPayments { get; set; } = new List<SupplierSplit>();
        public List<LoanTransaction> Repayments { get; set; } = new List<LoanTransaction>();
        public List<DashboardExceptionAlert> Exceptions { get; set; } = new List<DashboardExceptionAlert>();
    }

    public class PipelineStageItem
    {
        public int StageNumber { get; set; }
        public string StageName { get; set; }
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
        public string IconClass { get; set; }
        public string BadgeColor { get; set; }
    }

    public class DashboardExceptionAlert
    {
        public string Category { get; set; }
        public string Severity { get; set; } // Critical, High, Warning, Info
        public string Title { get; set; }
        public string Description { get; set; }
        public string EntityRef { get; set; }
        public DateTime DetectedAt { get; set; }
        public string ActionRequired { get; set; }
    }
}
