
using GManufacturingERP.Models;
using GManufacturingERP.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GManufacturingERP.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ==========================================
        // Sales & Production
        // ==========================================

        public DbSet<SalesOrder>
            SalesOrders
        { get; set; }

        public DbSet<ProductionPlan>
            ProductionPlans
        { get; set; }


        // ==========================================
        // Material Requirement Planning
        // ==========================================

        public DbSet<MaterialRequirementPlan>
            MaterialRequirementPlans
        { get; set; }

        public DbSet<MaterialRequirementItem>
            MaterialRequirementItems
        { get; set; }


        // ==========================================
        // Purchase
        // ==========================================

        public DbSet<PurchaseRequisition>
            PurchaseRequisitions
        { get; set; }

        public DbSet<PurchaseRequisitionItem>
            PurchaseRequisitionItems
        { get; set; }

        public DbSet<PurchaseOrder>
            PurchaseOrders
        { get; set; }

        public DbSet<PurchaseOrderItem>
            PurchaseOrderItems
        { get; set; }


        // ==========================================
        // Goods Receipt
        // ==========================================

        public DbSet<GoodsReceipt>
            GoodsReceipts
        { get; set; }

        public DbSet<GoodsReceiptItem>
            GoodsReceiptItems
        { get; set; }


        // ==========================================
        // Inventory
        // ==========================================

        public DbSet<InventoryStock>
            InventoryStocks
        { get; set; }

        public DbSet<InventoryTransaction>
            InventoryTransactions
        { get; set; }


        // ==========================================
        // Material Issue
        // ==========================================

        public DbSet<MaterialIssue>
            MaterialIssues
        { get; set; }

        public DbSet<MaterialIssueItem>
            MaterialIssueItems
        { get; set; }


        // ==========================================
        // Production Monitoring
        // ==========================================

        public DbSet<ProductionMonitoring>
            ProductionMonitorings
        { get; set; }

        public DbSet<ProductionProgressEntry>
            ProductionProgressEntries
        { get; set; }


        // ==========================================
        // Quality Control
        // ==========================================

        public DbSet<QualityInspection>
            QualityInspections
        { get; set; }


        // ==========================================
        // Finished Goods
        // ==========================================

        public DbSet<FinishedGoodsStock>
            FinishedGoodsStocks
        { get; set; }

        public DbSet<FinishedGoodsTransaction>
            FinishedGoodsTransactions
        { get; set; }

        public DbSet<FinishedGoodsRework>
            FinishedGoodsReworks
        { get; set; }


        // ==========================================
        // Packing
        // ==========================================

        public DbSet<Packing>
            Packings
        { get; set; }

        public DbSet<PackingItem>
            PackingItems
        { get; set; }

        public DbSet<Notification>
            Notifications
        { get; set; }

        public DbSet<PartialShipmentRequest>
            PartialShipmentRequests
        { get; set; }


        // ==========================================
        // Shipment
        // ==========================================

        public DbSet<Shipment>
            Shipments
        { get; set; }

        public DbSet<Dispatch>
            Dispatches
        { get; set; }


        // ==========================================
        // Delivery
        // ==========================================

        public DbSet<Delivery>
            Deliveries
        { get; set; }


        // ==========================================
        // Invoice & Payment
        // ==========================================

        public DbSet<Invoice>
            Invoices
        { get; set; }

        public DbSet<Payment>
            Payments
        { get; set; }


        // ==========================================
        // Quotation
        // ==========================================

        public DbSet<Quotation>
            Quotations
        { get; set; }


        // ==========================================
        // Advance Payment
        // ==========================================

        public DbSet<AdvancePayment>
            AdvancePayments
        { get; set; }


        // ==========================================
        // Model Relationships
        // ==========================================

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // ==========================================
            // MaterialIssue → MaterialRequirementPlan
            // ==========================================

            modelBuilder.Entity<MaterialIssue>()
                .HasOne(x => x.MaterialRequirementPlan)
                .WithMany()
                .HasForeignKey(x => x.MaterialRequirementPlanId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // FinishedGoodsStock → ProductionPlan
            // ==========================================

            modelBuilder.Entity<FinishedGoodsStock>()
                .HasOne(x => x.ProductionPlan)
                .WithMany()
                .HasForeignKey(x => x.ProductionPlanId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // FinishedGoodsStock → SalesOrder
            // ==========================================

            modelBuilder.Entity<FinishedGoodsStock>()
                .HasOne(x => x.SalesOrder)
                .WithMany()
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // FinishedGoodsRework → FinishedGoodsStock
            // ==========================================

            modelBuilder.Entity<FinishedGoodsRework>()
                .HasOne(x => x.FinishedGoodsStock)
                .WithMany()
                .HasForeignKey(x => x.FinishedGoodsStockId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // FinishedGoodsRework → ProductionMonitoring
            // ==========================================

            modelBuilder.Entity<FinishedGoodsRework>()
                .HasOne(x => x.ProductionMonitoring)
                .WithMany()
                .HasForeignKey(x => x.ProductionMonitoringId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Packing → SalesOrder
            // ==========================================

            modelBuilder.Entity<Packing>()
                .HasOne(x => x.SalesOrder)
                .WithMany()
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // PackingItem → Packing
            // ==========================================

            modelBuilder.Entity<PackingItem>()
                .HasOne(x => x.Packing)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.PackingId)
                .OnDelete(DeleteBehavior.Cascade);


            // ==========================================
            // PackingItem → FinishedGoodsStock
            // ==========================================

            modelBuilder.Entity<PackingItem>()
                .HasOne(x => x.FinishedGoodsStock)
                .WithMany()
                .HasForeignKey(x => x.FinishedGoodsStockId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Shipment → Packing
            // ==========================================

            modelBuilder.Entity<Shipment>()
                .HasOne(x => x.Packing)
                .WithMany(x => x.Shipments)
                .HasForeignKey(x => x.PackingId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Delivery → Dispatch
            // One-to-One Relationship
            // ==========================================

            modelBuilder.Entity<Delivery>()
                .HasOne(x => x.Dispatch)
                .WithOne()
                .HasForeignKey<Delivery>(x => x.DispatchId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Invoice → Delivery
            // One-to-One Relationship
            // ==========================================

            modelBuilder.Entity<Invoice>()
                .HasOne(x => x.Delivery)
                .WithOne()
                .HasForeignKey<Invoice>(x => x.DeliveryId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Invoice → SalesOrder
            // One-to-Many Relationship
            // ==========================================

            modelBuilder.Entity<Invoice>()
                .HasOne(x => x.SalesOrder)
                .WithMany()
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Payment → Invoice
            // One-to-Many Relationship
            // ==========================================

            modelBuilder.Entity<Payment>()
                .HasOne(x => x.Invoice)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // QUOTATION → BUYER
            // ==========================================

            modelBuilder.Entity<Quotation>()
                .HasOne(x => x.Buyer)
                .WithMany()
                .HasForeignKey(x => x.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // QUOTATION → SALES ORDER
            // ==========================================

            modelBuilder.Entity<Quotation>()
                .HasOne(x => x.SalesOrder)
                .WithMany()
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // ADVANCE PAYMENT CONFIGURATION
            // ==========================================

            modelBuilder.Entity<AdvancePayment>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.Quotation)
                    .WithMany()
                    .HasForeignKey(x => x.QuotationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.Amount)
                    .HasColumnType("decimal(18,2)");

                entity.HasIndex(x => x.TransactionId)
                    .IsUnique();

                entity.Property(x => x.Status)
                    .HasMaxLength(30)
                    .IsRequired();
            });


            // ==========================================
            // Partial Shipments
            // ==========================================

            modelBuilder.Entity<PartialShipmentRequest>()
                .HasOne(x => x.SalesOrder)
                .WithMany()
                .HasForeignKey(x => x.SalesOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PartialShipmentRequest>()
                .HasOne(x => x.FinishedGoodsStock)
                .WithMany()
                .HasForeignKey(x => x.FinishedGoodsStockId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Notification → PartialShipmentRequest
            // ==========================================

            modelBuilder.Entity<Notification>()
                .HasOne(x => x.PartialShipmentRequest)
                .WithMany()
                .HasForeignKey(x => x.PartialShipmentRequestId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // Packing → PartialShipmentRequest
            // ==========================================

            modelBuilder.Entity<Packing>()
                .HasOne(x => x.PartialShipmentRequest)
                .WithMany()
                .HasForeignKey(x => x.PartialShipmentRequestId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}