using Dapper;
using MySqlConnector;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;



var builder = WebApplication.CreateBuilder(args);

var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? "localhost:6379";
var redis = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 10000;
        opt.Window = TimeSpan.FromSeconds(10);
       
    });
    


});
var app = builder.Build();






string connString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Database=shortener_db;User=root;Password=Arya@2005;Max Pool Size=200;Connection Timeout=120;";




app.UseRateLimiter();


app.MapGet("/popular", async()=>{
    using var db = new MySqlConnection(connString);
    var popularUrls = await db.QueryAsync("SELECT ShortCode, OriginalUrl, Clicks FROM urls ORDER BY Clicks DESC LIMIT 5");

    return Results.Ok(popularUrls);


});







using (var db = new MySqlConnection(connString))
{
    db.Execute("CREATE TABLE IF NOT EXISTS urls (ShortCode VARCHAR(10) PRIMARY KEY, OriginalUrl TEXT NOT NULL, Clicks INT DEFAULT 0);");
}

app.MapGet("/stats/{code}",async  (string code) =>

{
    using var db = new MySqlConnection(connString);
     var clicks = await db.QueryFirstOrDefaultAsync<int?>($"SELECT Clicks FROM urls WHERE ShortCode = @code", new { code });

    if(clicks is null )
    {
        return Results.NotFound("Short Code Not Found");
    }

    return Results.Ok(new { shortCode = code, clicks = clicks });

});
app.MapPost("/shorten", async (string url) =>
{

    
    using var db = new MySqlConnection(connString);
    
    while (true)
    {
        var code = Guid.NewGuid().ToString("N")[..10];
        try
        {
            await db.ExecuteAsync("INSERT INTO urls (ShortCode, OriginalUrl) VALUES (@code, @url)", new { code, url });
            return Results.Ok(new { shortCode = code, originalUrl = url });
        }
        catch (MySqlException ex) when (ex.Number == 1062) 
        {
           
            continue; 
        }
    }
}).RequireRateLimiting("fixed");

app.MapGet("/{code}", async (string code, IConnectionMultiplexer redisConn) =>
{
    var cache = redisConn.GetDatabase();

    string? cachedthing = await cache.StringGetAsync(code);
    if (cachedthing is not null)
    {
        
        return Results.Redirect(cachedthing);
    }
    using var db = new MySqlConnection(connString);
    var OriginalUrl  = await db.QueryFirstOrDefaultAsync<string>($"SELECT OriginalUrl FROM urls WHERE ShortCode = @code", new { code });
    if(OriginalUrl is not null){

        await db.ExecuteAsync($"UPDATE urls SET clicks = clicks + 1 WHERE ShortCode = @code", new { code });
        await cache.StringSetAsync(code, OriginalUrl, TimeSpan.FromHours(1));

        return Results.Redirect(OriginalUrl);
    }
    else
    {
         return Results.NotFound("Short Code Not Found");
        

    }

    

   

    


});

app.Run();