using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Assistants;
using Serilog;

namespace Demo3;

public class SwapiShipApiFunction : IGptFunction
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://swapi.dev/api/";
    public const string FunctionName = "call_starwars_api";

    public SwapiShipApiFunction()
    {
        _httpClient = new HttpClient() { BaseAddress = new Uri(BaseUrl) };
    }

    public FunctionToolDefinition GetFunctionDefinition()
    {
        return new FunctionToolDefinition(FunctionName, "Gets Star Wars starship information.", BinaryData.FromObjectAsJson(
            new
            {
                Type = "object",
                Properties = new
                {
                    ship_name = new
                    {
                        Type = "string",
                        Description = "The name of the ship, e.g. CR90 corvette",
                    }
                },
                Required = new[] { "ship_name" },
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    public string GetFunctionName() => FunctionName;

    public async Task<string> GetShipInformation(SwapiShipApiFunctionParameters parameters)
    {
        Log.Verbose("Searching for starship with name {ShipName}", parameters.ShipName);
        var response = await _httpClient.GetFromJsonAsync<SwapiResponse>($"starships?search={UrlEncoder.Default.Encode(parameters.ShipName)}");
        var ship = response.count == 0 ? "No starship found with that name." : ToGptReadable(response.results[0]);
        Log.Verbose("Returning ship information: {Ship}", ship);
        return ship;
    }

    private static string ToGptReadable(StarShip starShip)
    {
        return $"Name: {starShip.name}, Model: {starShip.model}, Manufacturer: {starShip.manufacturer}, Cost in credits: {starShip.cost_in_credits}, Length: {starShip.length}, Max atmosphering speed: {starShip.max_atmosphering_speed}, " +
               $"Crew: {starShip.crew}, Passengers: {starShip.passengers}, Cargo capacity: {starShip.cargo_capacity}, Consumables: {starShip.consumables}, Hyperdrive rating: {starShip.hyperdrive_rating}, MGLT: {starShip.MGLT}, " +
               $"Starship class: {starShip.starship_class}, Pilots: {string.Join(", ", starShip.pilots)}, Films: {string.Join(", ", starShip.films)}";
    }

    public class SwapiShipApiFunctionParameters
    {
        [JsonPropertyName("ship_name")] 
        public string ShipName { get; set; }
    }

    private record SwapiResponse(
        int count,
        string next,
        string previous,
        List<StarShip> results);

    private record StarShip(
        string name,
        string model,
        string manufacturer,
        string cost_in_credits,
        string length,
        string max_atmosphering_speed,
        string crew,
        string passengers,
        string cargo_capacity,
        string consumables,
        string hyperdrive_rating,
        string MGLT,
        string starship_class,
        List<string> pilots,
        List<string> films,
        string created,
        string edited,
        string url
    );
}