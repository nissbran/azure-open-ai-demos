using System.ComponentModel;
using System.Text.Encodings.Web;
using ModelContextProtocol.Server;

namespace McpToolServer.Tools;

[McpServerToolType]
public sealed class ShipTool
{
    [McpServerTool, Description("Gets Star Wars starship information")]
    public static async Task<string> GetStarship(
        IHttpClientFactory httpClientFactory,
        ILogger<ShipTool> logger,
        [Description("The name of the ship, e.g. CR90 corvette")]
        string shipName)
    {
        logger.LogInformation("Searching for starship with name {ShipName}", shipName);
        
        var httpClient = httpClientFactory.CreateClient("SwapiClient");
        
        var response = await httpClient.GetFromJsonAsync<SwapiResponse>($"starships?search={UrlEncoder.Default.Encode(shipName)}");
        var ship = response?.count == 0 ? "No starship found with that name." : ToGptReadable(response!.results[0]);
        
        logger.LogInformation("Returning ship information: {Ship}", ship);
        return ship;
    }
    
    private static string ToGptReadable(StarShip starShip)
    {
        return $"Name: {starShip.name}, Model: {starShip.model}, Manufacturer: {starShip.manufacturer}, Cost in credits: {starShip.cost_in_credits}, Length: {starShip.length}, Max atmosphering speed: {starShip.max_atmosphering_speed}, " +
               $"Crew: {starShip.crew}, Passengers: {starShip.passengers}, Cargo capacity: {starShip.cargo_capacity}, Consumables: {starShip.consumables}, Hyperdrive rating: {starShip.hyperdrive_rating}, MGLT: {starShip.MGLT}, " +
               $"Starship class: {starShip.starship_class}, Pilots: {string.Join(", ", starShip.pilots)}, Films: {string.Join(", ", starShip.films)}";
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