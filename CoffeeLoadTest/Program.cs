using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Diagnostics;

var client = new HttpClient();
var totalOrders = 100;

Console.WriteLine("🎟️ Visiting the Ticket Booth to get a VIP Token...");

// 1. Get the Token from the login booth
var loginResponse = await client.GetAsync("http://localhost:5298/login");
var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
var token = loginData!.token;

Console.WriteLine("✅ Token acquired! Starting the Morning Rush...");

// 2. Add the token to EVERY request automatically
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

var tasks = Enumerable.Range(1, totalOrders).Select(async i => {
    var order = new { Customer = $"VIP-Customer-{i}", Details = $"Drink:Latte-{i}" };
    
    var response = await client.PostAsJsonAsync("http://localhost:5298/order", order);
    
    if (response.IsSuccessStatusCode)
        Console.Write(".");
    else
        Console.Write($"X({response.StatusCode})"); 
});

var watch = Stopwatch.StartNew();
await Task.WhenAll(tasks);
watch.Stop();

Console.WriteLine($"\n\n✅ Done! 100 VIP orders processed in {watch.ElapsedMilliseconds}ms");

// Small helper class
public record LoginResult(string token);
