using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace SwapiIndexer;

public class SwapiVehicleReader
{
    private readonly HttpClient _client;

    public SwapiVehicleReader(IConfiguration configuration)
    {
        var swapiInfoApi = configuration["Swapi:BaseUrl"] ?? "https://swapi.info/api/";
        _client = new HttpClient { BaseAddress = new Uri(swapiInfoApi) };
    }

    public async Task<List<Vehicle>> GetVehicles()
    {
        var vehicles = new List<Vehicle>();
        var response = await _client.GetFromJsonAsync<List<Vehicle>>("vehicles");
        if (response != null)
        {
            vehicles.AddRange(response);
        }
        else
        {
            Log.Error("Failed to get vehicles from SWAPI");
        }

        return vehicles;
    }
}

public class Vehicle
{
    public string Name { get; set; }
    public string Model { get; set; }
    public string Manufacturer { get; set; }
    [JsonPropertyName("cost_in_credits")]
    public string CostInCredits { get; set; }
    public string Length { get; set; }
    [JsonPropertyName("max_atmosphering_speed")]
    public string MaxAtmospheringSpeed { get; set; }
    public string Crew { get; set; }
    public string Passengers { get; set; }
    [JsonPropertyName("cargo_capacity")]
    public string CargoCapacity { get; set; }
    public string Consumables { get; set; }
    [JsonPropertyName("vehicle_class")]
    public string VehicleClass { get; set; }
    public List<string> Pilots { get; set; }
    public List<string> Films { get; set; }

    public string Summary =>
        $"{Name} is a vehicle class {VehicleClass} with model {Model} and made by {Manufacturer}. It costs {CostInCredits}. It is {Length} in length and max speed of {MaxAtmospheringSpeed}. It has a crew of {Crew} and can carry {Passengers} passengers. It has a cargo capacity of {CargoCapacity} and can go {Consumables} without refueling.";
    public ReadOnlyMemory<float> VectorizedSummary { get; set; }
}