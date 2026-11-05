# Warehouse Agent

The Warehouse Agent provides real-time inventory information and stock quantities for vehicle parts using SQL Server database connectivity.

## Features

### Function Calls Available

1. **get_part_inventory** - Gets current inventory levels for specific parts
   - Searches by part number or part name
   - Returns quantity on hand, reserved quantities, available stock
   - Includes warehouse location and bin information
   - Shows reorder points and quantities

2. **get_low_stock_alerts** - Gets all parts below minimum stock thresholds
   - Identifies parts that need reordering
   - Shows current quantities vs. minimum stock requirements
   - Includes warehouse location information
   - Calculates days since last update

3. **get_warehouse_locations** - Gets warehouse location information for parts
   - Shows which warehouses have specific parts
   - Includes bin locations and contact information
   - Displays current quantities at each location

## Database Integration

The agent connects to SQL Server database using the connection string from configuration:
- **Configuration Key**: `ConnectionStrings:WarehouseDatabase`
- **Default**: Uses LocalDB with `WarehouseDemo` database
- **Fallback**: Simulated data if database is not available

### Database Schema

The agent expects the following database structure:

#### Warehouses Table
- WarehouseId (Primary Key)
- WarehouseName
- Location
- ContactPhone
- IsActive

#### WarehouseInventory Table
- PartNumber
- PartName
- QuantityOnHand
- QuantityReserved
- QuantityAvailable (computed)
- MinimumStock
- MaximumStock
- WarehouseId (Foreign Key)
- BinLocation
- LastUpdated
- ReorderPoint
- ReorderQuantity

### Setting Up the Database

1. Use the provided `database_schema.sql` script to create the database structure
2. Run the script on your SQL Server instance
3. Configure the connection string in your `appsettings.local.json`

```json
{
  "ConnectionStrings": {
    "WarehouseDatabase": "Server=your-server;Database=WarehouseDemo;Trusted_Connection=true;"
  }
}
```

## Simulated Data Mode

When no database is available, the agent uses simulated warehouse data including:
- Sample parts from different warehouses (WH001, WH002, WH003)
- Realistic inventory levels and stock statuses
- Low stock alerts for demonstration
- Warehouse location information

## Example Queries

The agent can answer questions like:
- "How many V6 engines do we have in stock?"
- "Which parts are below reorder point?"
- "Where is part ENG001 located in the warehouse?"
- "Show me all inventory at warehouse WH001"
- "What parts need to be reordered?"
- "How much brake system inventory do we have available?"

## Integration with Multi-Agent System

The Warehouse Agent works seamlessly with other agents in the system:
- **Bill of Materials Agent**: Provides part specifications that warehouse agent can check inventory for
- **Part Supplier Agent**: Supplier information can be cross-referenced with current stock levels
- **Vehicle Production Agent**: Production schedules can be matched against available inventory

This enables complex queries like:
- "Do we have enough parts to build 10 Hauler 500 trucks?"
- "Which parts for the January production run are below minimum stock?"
- "Show me inventory levels for all brake system parts we get from AutoParts Co."