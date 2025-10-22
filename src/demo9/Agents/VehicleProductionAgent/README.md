# Vehicle Production Agent

The Vehicle Production Agent is designed to answer questions about vehicle manufacturing data, including VIN numbers, build dates, models, production status, and manufacturing details.

## Features

- **VIN Lookup**: Search for specific vehicles using VIN numbers
- **Production Status Tracking**: Check the current status of vehicles in production
- **Model Information**: Get details about different vehicle models and their production
- **Plant Location Data**: Information about which manufacturing plant produced each vehicle
- **Build Date Tracking**: Manufacturing dates and production timeline information
- **Vehicle Specifications**: Engine type, color, and optional features for each vehicle

## Data Structure

The agent works with vehicle production data that includes:

- **VIN**: Vehicle Identification Number (unique identifier)
- **Model**: Vehicle model name (e.g., Contoso Hauler 500, Contoso Ranger X, Contoso Titan Pro)
- **Build Date**: Date when the vehicle was manufactured
- **Production Line**: Manufacturing line where the vehicle was built
- **Status**: Current production status (In Progress, Quality Check, Completed, Shipped)
- **Plant Location**: Manufacturing facility location
- **Engine Type**: Type of engine installed
- **Color**: Vehicle exterior color
- **Options**: Additional features and packages included

## Sample Data

The agent includes realistic demo data with 25 sample vehicles across three different models, manufactured at three different plant locations with various production statuses and configurations.

## Integration

The Vehicle Production Agent is integrated into the multi-agent orchestration system alongside the Bill of Materials Agent and Part Supplier Agent, allowing for comprehensive conversations about vehicle manufacturing, parts requirements, and production tracking.

## Usage Examples

Users can ask questions such as:
- "What is the status of vehicle with VIN 1C4RJFAG8FC123456?"
- "Show me all Contoso Hauler 500 models built in January 2024"
- "Which vehicles are currently in production at the Detroit Plant?"
- "What options are available on VIN 1C4RJFAG8FC123462?"
- "How many vehicles have been shipped this month?"