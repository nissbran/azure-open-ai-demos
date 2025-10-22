using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Serilog;

namespace Demo9.Agents.WareHouseAgent;

public class WarehouseSqlPlugin
{
    //private readonly WarehouseDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly SqliteConnection _connection;

    public WarehouseSqlPlugin(IConfiguration configuration)
    {
        _connection = new SqliteConnection("Filename=:memory:");
        //_dbContext = dbContext;
        _configuration = configuration;
        Log.Information("Warehouse SQL Plugin initialized for SQLite/EF Core");
    }

    public async Task SeedData()
    {
        _connection.Open();
        var warehouseInventories = new List<WarehouseInventory>()
        {
            new WarehouseInventory()
            {
                PartNumber = "BAT001", PartName = "Battery Model-1", QuantityOnHand = 6, QuantityReserved = 5, QuantityAvailable = 1,
                MinimumStock = 10, MaximumStock = 50, WarehouseId = "WH002", BinLocation = "A1-A1",
                LastUpdated = DateTime.Now.AddDays(-2), ReorderPoint = 15, ReorderQuantity = 30
            },
            new WarehouseInventory()
            {
                PartNumber = "ENG001", PartName = "V6 Intercooled Diesel Model-1", QuantityOnHand = 45, QuantityReserved = 10, QuantityAvailable = 35,
                MinimumStock = 20, MaximumStock = 100, WarehouseId = "WH001", BinLocation = "A1-B3",
                LastUpdated = DateTime.Now.AddHours(-2), ReorderPoint = 25, ReorderQuantity = 50
            },
            new WarehouseInventory()
            {
                PartNumber = "ECU001", PartName = "Electronic Control Hub Model-1", QuantityOnHand = 8, QuantityReserved = 3, QuantityAvailable = 5,
                MinimumStock = 15, MaximumStock = 50, WarehouseId = "WH001", BinLocation = "B2-C1",
                LastUpdated = DateTime.Now.AddHours(-1), ReorderPoint = 10, ReorderQuantity = 25
            },
            new WarehouseInventory()
            {
                PartNumber = "RAD001", PartName = "Radiator Model-1", QuantityOnHand = 0, QuantityReserved = 0, QuantityAvailable = 0,
                MinimumStock = 10, MaximumStock = 30, WarehouseId = "WH002", BinLocation = "C3-A2",
                LastUpdated = DateTime.Now.AddDays(-1), ReorderPoint = 5, ReorderQuantity = 20
            },
            new WarehouseInventory()
            {
                PartNumber = "SUS001", PartName = "Hydraulic Suspension Module Model-1", QuantityOnHand = 12, QuantityReserved = 2, QuantityAvailable = 10,
                MinimumStock = 8, MaximumStock = 40, WarehouseId = "WH001", BinLocation = "A2-B1",
                LastUpdated = DateTime.Now.AddHours(-3), ReorderPoint = 8, ReorderQuantity = 15
            },
            new WarehouseInventory()
            {
                PartNumber = "TRN001", PartName = "High-Torque Transmission Box Model-1", QuantityOnHand = 28, QuantityReserved = 5, QuantityAvailable = 23,
                MinimumStock = 15, MaximumStock = 60, WarehouseId = "WH003", BinLocation = "D1-A4",
                LastUpdated = DateTime.Now.AddMinutes(-30), ReorderPoint = 18, ReorderQuantity = 30
            },
            new WarehouseInventory()
            {
                PartNumber = "BRK001", PartName = "Dual-Circuit Air Brake Model-1", QuantityOnHand = 150, QuantityReserved = 25, QuantityAvailable = 125,
                MinimumStock = 50, MaximumStock = 200, WarehouseId = "WH002", BinLocation = "B1-C4",
                LastUpdated = DateTime.Now.AddHours(-4), ReorderPoint = 60, ReorderQuantity = 100
            },
            new WarehouseInventory()
            {
                PartNumber = "STG001", PartName = "Steering System Model-1", QuantityOnHand = 18, QuantityReserved = 3, QuantityAvailable = 15,
                MinimumStock = 12, MaximumStock = 45, WarehouseId = "WH001", BinLocation = "C1-D2",
                LastUpdated = DateTime.Now.AddHours(-1), ReorderPoint = 15, ReorderQuantity = 25
            },
            new WarehouseInventory()
            {
                PartNumber = "TRN002", PartName = "Manual Transmission Model-2", QuantityOnHand = 22, QuantityReserved = 8, QuantityAvailable = 14,
                MinimumStock = 10, MaximumStock = 50, WarehouseId = "WH003", BinLocation = "D2-A1",
                LastUpdated = DateTime.Now.AddHours(-2), ReorderPoint = 12, ReorderQuantity = 25
            },
            new WarehouseInventory()
            {
                PartNumber = "ENG002", PartName = "Heavy-Duty Inline-4 Engine Model-1", QuantityOnHand = 32, QuantityReserved = 7, QuantityAvailable = 25,
                MinimumStock = 20, MaximumStock = 80, WarehouseId = "WH002", BinLocation = "A3-B2",
                LastUpdated = DateTime.Now.AddHours(-5), ReorderPoint = 25, ReorderQuantity = 40
            },
            new WarehouseInventory()
            {
                PartNumber = "ECU002", PartName = "Advanced Vehicle Control Module Model-1", QuantityOnHand = 15, QuantityReserved = 4, QuantityAvailable = 11,
                MinimumStock = 12, MaximumStock = 45, WarehouseId = "WH003", BinLocation = "B1-C3",
                LastUpdated = DateTime.Now.AddHours(-1), ReorderPoint = 15, ReorderQuantity = 30
            },
            new WarehouseInventory()
            {
                PartNumber = "ENG003", PartName = "V8 Turbo Diesel Model-3", QuantityOnHand = 38, QuantityReserved = 12, QuantityAvailable = 26,
                MinimumStock = 25, MaximumStock = 90, WarehouseId = "WH001", BinLocation = "A4-B1",
                LastUpdated = DateTime.Now.AddHours(-3), ReorderPoint = 30, ReorderQuantity = 45
            }
        };


        var warehouses = new List<Warehouse>()
        {
            new() { WarehouseId = "WH001", WarehouseName = "Main Distribution Center", Location = "Detroit, MI", ContactPhone = "555-0101" },
            new() { WarehouseId = "WH002", WarehouseName = "West Coast Facility", Location = "Los Angeles, CA", ContactPhone = "555-0102" },
            new() { WarehouseId = "WH003", WarehouseName = "Central Warehouse", Location = "Chicago, IL", ContactPhone = "555-0103" }
        };

        var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
        if (!await context.WarehouseInventory.AnyAsync())
        {
            Log.Information("Seeding warehouse data");
            await context.Warehouses.AddRangeAsync(warehouses);
            await context.WarehouseInventory.AddRangeAsync(warehouseInventories);
            await context.SaveChangesAsync();
        }
    }

    [KernelFunction("get_part_inventory")]
    [Description("Gets current inventory levels for specific parts from the warehouse database")]
    [return: Description("List of warehouse inventory records showing current stock levels, locations, and availability")]
    public async Task<List<WarehouseInventoryResult>> GetPartInventory(InventorySearchParameters parameters)
    {
        try
        {
            var context = CreateDbContext();
            
            Log.Verbose("Searching warehouse inventory for: {SearchCriteria}", parameters.PartNumberOrName);
            
            var inventory = await context.WarehouseInventory.Where(
                    inventory => inventory.PartName.Contains(parameters.PartNumberOrName) || 
                                 inventory.PartNumber.Contains(parameters.PartNumberOrName)) 
                .ToListAsync();
            Log.Verbose("Found {Count} inventory records", inventory.Count);
            return inventory.Select(item => new WarehouseInventoryResult(
                item.PartNumber,
                item.PartName,
                item.QuantityOnHand,
                item.QuantityReserved,
                item.QuantityAvailable,
                item.MinimumStock,
                item.MaximumStock,
                item.WarehouseId,
                item.BinLocation,
                item.LastUpdated,
                item.ReorderPoint,
                item.ReorderQuantity)).ToList();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to query warehouse inventory");
            throw;
        }
    }

    // [KernelFunction("get_low_stock_alerts")]
    // [Description("Gets all parts that are currently below minimum stock thresholds")]
    // [return: Description("List of parts with low stock levels that may need reordering")]
    // public async Task<List<LowStockAlert>> GetLowStockAlerts()
    // {
    //     try
    //     {
    //         Log.Verbose("Checking for low stock alerts");
    //         List<LowStockAlert> alerts;
    //         if (await IsActualDatabaseAvailable())
    //         {
    //             alerts = await QueryLowStockFromDatabase();
    //         }
    //         else
    //         {
    //             alerts = await SimulateLowStockAlerts();
    //         }
    //         Log.Verbose("Found {Count} low stock alerts", alerts.Count);
    //         return alerts;
    //     }
    //     catch (Exception e)
    //     {
    //         Log.Error(e, "Failed to query low stock alerts");
    //         throw;
    //     }
    // }

    [KernelFunction("get_warehouse_locations")]
    [Description("Gets warehouse location information for specific parts")]
    [return: Description("List of warehouse locations showing where parts are stored")]
    public async Task<List<WarehouseLocationResult>> GetWarehouseLocations(LocationSearchParameters parameters)
    {
        try
        {
            var context = CreateDbContext();
            
            Log.Verbose("Searching warehouse locations for: {SearchCriteria}", parameters.PartNumberOrWarehouse);
            
            var query = from w in context.WarehouseInventory
                join wh in context.Warehouses on w.WarehouseId equals wh.WarehouseId
                where EF.Functions.Like(w.PartNumber, $"%{parameters.PartNumberOrWarehouse}%") ||
                      EF.Functions.Like(w.PartName, $"%{parameters.PartNumberOrWarehouse}%") ||
                      EF.Functions.Like(w.WarehouseId, $"%{parameters.PartNumberOrWarehouse}%")
                orderby w.WarehouseId, w.PartNumber
                select new WarehouseLocationResult(
                    w.PartNumber,
                    w.PartName,
                    w.WarehouseId,
                    w.BinLocation,
                    w.QuantityOnHand,
                    wh.WarehouseName,
                    wh.Location,
                    wh.ContactPhone
                );
            
            var locations = await query.ToListAsync();
            
            Log.Verbose("Found {Count} warehouse locations", locations.Count);

            return locations;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to query warehouse locations");
            throw;
        }
    }

    private WarehouseDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<WarehouseDbContext>();
        //var connectionString = _configuration.GetConnectionString("WarehouseDatabase");
        optionsBuilder.UseSqlite(_connection);
        return new WarehouseDbContext(optionsBuilder.Options);
    }
    public class InventorySearchParameters
    {
        [JsonPropertyName("part_number_or_name")]
        [Description("Part number or part name to search for in warehouse inventory")]
        public string PartNumberOrName { get; set; } = string.Empty;
    }

    public class LocationSearchParameters
    {
        [JsonPropertyName("part_number_or_warehouse")]
        [Description("Part number, part name, or warehouse ID to search for location information")]
        public string PartNumberOrWarehouse { get; set; } = string.Empty;
    }

    public record WarehouseInventoryResult(
        string PartNumber,
        string PartName,
        int QuantityOnHand,
        int QuantityReserved,
        int QuantityAvailable,
        int MinimumStock,
        int MaximumStock,
        string WarehouseId,
        string BinLocation,
        DateTime LastUpdated,
        int ReorderPoint,
        int ReorderQuantity
    );

    public record LowStockAlert(
        string PartNumber,
        string PartName,
        int CurrentQuantity,
        int MinimumStock,
        int ReorderPoint,
        int ReorderQuantity,
        string WarehouseId,
        int DaysSinceLastUpdate
    );

    public record WarehouseLocationResult(
        string PartNumber,
        string PartName,
        string WarehouseId,
        string BinLocation,
        int QuantityOnHand,
        string WarehouseName,
        string WarehouseLocation,
        string ContactPhone
    );
}