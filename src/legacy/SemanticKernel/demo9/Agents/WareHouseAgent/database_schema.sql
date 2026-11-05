-- Warehouse Database Schema
-- This script creates the necessary tables for the Warehouse Agent demo
-- Run this script on your SQL Server instance to set up the warehouse database

-- Create the database (uncomment if needed)
-- CREATE DATABASE WarehouseDemo;
-- GO
-- USE WarehouseDemo;
-- GO

-- Create Warehouses table
CREATE TABLE Warehouses (
    WarehouseId NVARCHAR(10) PRIMARY KEY,
    WarehouseName NVARCHAR(100) NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    ContactPhone NVARCHAR(20),
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1
);

-- Create WarehouseInventory table
CREATE TABLE WarehouseInventory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PartNumber NVARCHAR(20) NOT NULL,
    PartName NVARCHAR(200) NOT NULL,
    QuantityOnHand INT NOT NULL DEFAULT 0,
    QuantityReserved INT NOT NULL DEFAULT 0,
    QuantityAvailable AS (QuantityOnHand - QuantityReserved) PERSISTED,
    MinimumStock INT NOT NULL DEFAULT 0,
    MaximumStock INT NOT NULL DEFAULT 100,
    WarehouseId NVARCHAR(10) NOT NULL,
    BinLocation NVARCHAR(20) NOT NULL,
    LastUpdated DATETIME2 DEFAULT GETDATE(),
    ReorderPoint INT NOT NULL DEFAULT 10,
    ReorderQuantity INT NOT NULL DEFAULT 25,
    
    CONSTRAINT FK_WarehouseInventory_Warehouse 
        FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId),
    
    CONSTRAINT CHK_QuantityOnHand CHECK (QuantityOnHand >= 0),
    CONSTRAINT CHK_QuantityReserved CHECK (QuantityReserved >= 0),
    CONSTRAINT CHK_MinimumStock CHECK (MinimumStock >= 0),
    CONSTRAINT CHK_MaximumStock CHECK (MaximumStock >= MinimumStock),
    CONSTRAINT CHK_ReorderPoint CHECK (ReorderPoint >= 0),
    CONSTRAINT CHK_ReorderQuantity CHECK (ReorderQuantity > 0)
);

-- Create indexes for better performance
CREATE INDEX IX_WarehouseInventory_PartNumber ON WarehouseInventory(PartNumber);
CREATE INDEX IX_WarehouseInventory_WarehouseId ON WarehouseInventory(WarehouseId);
CREATE INDEX IX_WarehouseInventory_QuantityAvailable ON WarehouseInventory(QuantityAvailable);
CREATE INDEX IX_WarehouseInventory_ReorderPoint ON WarehouseInventory(ReorderPoint);

-- Insert sample warehouse data
INSERT INTO Warehouses (WarehouseId, WarehouseName, Location, ContactPhone) VALUES
('WH001', 'Main Distribution Center', 'Detroit, MI', '555-0101'),
('WH002', 'West Coast Facility', 'Los Angeles, CA', '555-0102'),
('WH003', 'Central Warehouse', 'Chicago, IL', '555-0103');

-- Insert sample inventory data
INSERT INTO WarehouseInventory (PartNumber, PartName, QuantityOnHand, QuantityReserved, MinimumStock, MaximumStock, WarehouseId, BinLocation, ReorderPoint, ReorderQuantity) VALUES
('ENG001', 'V6 Intercooled Diesel Model-1', 45, 10, 20, 100, 'WH001', 'A1-B3', 25, 50),
('ECU001', 'Electronic Control Hub Model-1', 8, 3, 15, 50, 'WH001', 'B2-C1', 10, 25),
('RAD001', 'Radiator Model-1', 0, 0, 10, 30, 'WH002', 'C3-A2', 5, 20),
('SUS001', 'Hydraulic Suspension Module Model-1', 12, 2, 8, 40, 'WH001', 'A2-B1', 8, 15),
('TRN001', 'High-Torque Transmission Box Model-1', 28, 5, 15, 60, 'WH003', 'D1-A4', 18, 30),
('ENG002', 'Heavy-Duty Inline-4 Engine Model-1', 32, 7, 20, 80, 'WH002', 'A3-B2', 25, 40),
('ECU002', 'Advanced Vehicle Control Module Model-1', 15, 4, 12, 45, 'WH003', 'B1-C3', 15, 30),
('BRK001', 'Dual-Circuit Air Brake Model-1', 150, 25, 50, 200, 'WH002', 'B1-C4', 60, 100),
('STG001', 'Steering System Model-1', 18, 3, 12, 45, 'WH001', 'C1-D2', 15, 25),
('TRN002', 'Manual Transmission Model-2', 22, 8, 10, 50, 'WH003', 'D2-A1', 12, 25),
('ENG003', 'V8 Turbo Diesel Model-3', 38, 12, 25, 90, 'WH001', 'A4-B1', 30, 45);

-- Create a view for low stock alerts
CREATE VIEW vw_LowStockAlerts AS
SELECT 
    wi.PartNumber,
    wi.PartName,
    wi.QuantityOnHand AS CurrentQuantity,
    wi.MinimumStock,
    wi.ReorderPoint,
    wi.ReorderQuantity,
    wi.WarehouseId,
    w.WarehouseName,
    w.Location,
    DATEDIFF(day, wi.LastUpdated, GETDATE()) AS DaysSinceLastUpdate,
    CASE 
        WHEN wi.QuantityOnHand = 0 THEN 'OUT OF STOCK'
        WHEN wi.QuantityOnHand <= wi.ReorderPoint THEN 'REORDER NEEDED'
        WHEN wi.QuantityOnHand <= wi.MinimumStock THEN 'LOW STOCK'
        ELSE 'NORMAL'
    END AS StockStatus
FROM WarehouseInventory wi
INNER JOIN Warehouses w ON wi.WarehouseId = w.WarehouseId
WHERE wi.QuantityOnHand <= wi.ReorderPoint
    AND w.IsActive = 1;

-- Create a stored procedure for updating inventory
CREATE PROCEDURE sp_UpdateInventory
    @PartNumber NVARCHAR(20),
    @WarehouseId NVARCHAR(10),
    @QuantityChange INT,
    @TransactionType NVARCHAR(10) -- 'IN' for receiving, 'OUT' for issuing
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        IF @TransactionType = 'IN'
        BEGIN
            UPDATE WarehouseInventory 
            SET QuantityOnHand = QuantityOnHand + @QuantityChange,
                LastUpdated = GETDATE()
            WHERE PartNumber = @PartNumber AND WarehouseId = @WarehouseId;
        END
        ELSE IF @TransactionType = 'OUT'
        BEGIN
            UPDATE WarehouseInventory 
            SET QuantityOnHand = QuantityOnHand - @QuantityChange,
                LastUpdated = GETDATE()
            WHERE PartNumber = @PartNumber 
                AND WarehouseId = @WarehouseId
                AND QuantityOnHand >= @QuantityChange;
                
            IF @@ROWCOUNT = 0
            BEGIN
                THROW 50000, 'Insufficient inventory for the requested transaction.', 1;
            END
        END
        
        COMMIT TRANSACTION;
        
        SELECT 'SUCCESS' AS Result, 'Inventory updated successfully.' AS Message;
        
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        
        SELECT 'ERROR' AS Result, ERROR_MESSAGE() AS Message;
    END CATCH
END;

-- Sample queries that the Warehouse Agent might use:

-- Query 1: Get inventory for specific parts
/*
SELECT 
    PartNumber,
    PartName,
    QuantityOnHand,
    QuantityReserved,
    QuantityAvailable,
    MinimumStock,
    MaximumStock,
    WarehouseId,
    BinLocation,
    LastUpdated,
    ReorderPoint,
    ReorderQuantity
FROM WarehouseInventory 
WHERE PartNumber LIKE '%ENG%' 
   OR PartName LIKE '%Engine%'
ORDER BY PartNumber;
*/

-- Query 2: Get low stock alerts
/*
SELECT * FROM vw_LowStockAlerts
ORDER BY CurrentQuantity ASC;
*/

-- Query 3: Get warehouse locations for parts
/*
SELECT DISTINCT
    wi.PartNumber,
    wi.PartName,
    wi.WarehouseId,
    wi.BinLocation,
    wi.QuantityOnHand,
    w.WarehouseName,
    w.Location as WarehouseLocation,
    w.ContactPhone
FROM WarehouseInventory wi
INNER JOIN Warehouses w ON wi.WarehouseId = w.WarehouseId
WHERE wi.PartNumber LIKE '%BRK%'
ORDER BY wi.WarehouseId, wi.PartNumber;
*/