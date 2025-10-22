using System;
using Microsoft.EntityFrameworkCore;

namespace Demo9.Agents.WareHouseAgent
{
    public class WarehouseInventory
    {
        public string PartNumber { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int QuantityReserved { get; set; }
        public int QuantityAvailable { get; set; }
        public int MinimumStock { get; set; }
        public int MaximumStock { get; set; }
        public string WarehouseId { get; set; } = string.Empty;
        public string BinLocation { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public int ReorderPoint { get; set; }
        public int ReorderQuantity { get; set; }
    }

    public class Warehouse
    {
        public string WarehouseId { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
    }

    public class WarehouseDbContext : DbContext
    {
        public DbSet<WarehouseInventory> WarehouseInventory { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }

        public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WarehouseInventory>().HasKey(w => w.PartNumber);
            modelBuilder.Entity<Warehouse>().HasKey(w => w.WarehouseId);
        }
    }
}

