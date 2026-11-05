using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Demo3;

public class SwapiShipApiFunction
{
    private readonly HttpClient _httpClient;
    public const string FunctionName = "call_starwars_api";

    public SwapiShipApiFunction(IConfiguration configuration)
    {
        var baseUrl  = configuration["Swapi:BaseUrl"] ?? "https://swapi.info/api/";
        _httpClient = new HttpClient() { BaseAddress = new Uri(baseUrl) };
    }

    public AITool GetFunctionDefinition()
    {
        return AIFunctionFactory.Create(GetShipInformation, FunctionName);
    }

    [Description("Gets Star Wars starship information")]
    public async Task<string> GetShipInformation(SwapiShipApiFunctionParameters parameters)
    {
        Log.Verbose("Searching for starship with name {ShipName}", parameters.ShipName);
        var response = await _httpClient.GetFromJsonAsync<List<StarShip>>($"starships");
        var ship = response.Find(starShip => starShip.name == parameters.ShipName);
        var result = ship == null ? "No starship found with that name." : ToGptReadable(ship);
        Log.Verbose("Returning ship information: {Ship}", result);
        return result;
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
        [Description("The name of the starship to search for")]
        public string ShipName { get; set; }
    }

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