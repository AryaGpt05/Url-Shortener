using Dapper;
using MySqlConnector;
using Microsoft.AspNetCore.RateLimiting;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 10000;
        opt.Window = TimeSpan.FromSeconds(10);
       
    });
    


});
var app = builder.Build();






string connString = "Server=localhost;Database=shortener_db;User=root;Password=Arya@2005;";




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
app.MapPost("/shorten",async(string url) =>
{

    var code = Guid.NewGuid().ToString()[..6];
    using var db = new MySqlConnection(connString);
    await db.ExecuteAsync($"INSERT INTO urls (ShortCode, OriginalUrl) VALUES (@code, @url)", new { code, url });

    return Results.Ok(new{shortCode = code, originalUrl = url});
}).RequireRateLimiting("fixed");

app.MapGet("/{code}", async (string code) =>
{
    using var db = new MySqlConnection(connString);
    var OriginalUrl  = await db.QueryFirstOrDefaultAsync<string>($"SELECT OriginalUrl FROM urls WHERE ShortCode = @code", new { code });
    if(OriginalUrl is not null){

        await db.ExecuteAsync($"UPDATE urls SET clicks = clicks + 1 WHERE ShortCode = @code", new { code });

        return Results.Redirect(OriginalUrl);
    }

    return Results.NotFound("Short Code Not Found");


});

app.Run();