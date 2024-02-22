using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SwapiIndexer;

public class SwapiVehicleReader
{
    private readonly HttpClient _client = new HttpClient();

    public async Task<List<Vehicle>> GetVehicles()
    {
        var vehicles = new List<Vehicle>();
        var response = await _client.GetFromJsonAsync<SwapiResponse>("https://swapi.dev/api/vehicles");
        vehicles.AddRange(response.Results);
        while (!string.IsNullOrEmpty(response.Next))
        {
            response = await _client.GetFromJsonAsync<SwapiResponse>(response.Next);
            vehicles.AddRange(response.Results);
        }

        return vehicles;
    }
}

public class SwapiResponse
{
    public int Count { get; set; }
    public string Next { get; set; }
    public string Previous { get; set; }
    public IEnumerable<Vehicle> Results { get; set; }
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