using Dapper;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();



string connString = "Server=localhost;Database=shortener_db;User=root;Password=Arya@2005;";



int count = 0;
DateTime resetTime = DateTime.Now.AddMinutes(1);

app.Use(async (context, next) =>
{
    if(context.Request.Path == "/shorten" && context.Request.Method == "POST")
    {

        if(DateTime.Now>= resetTime)
        {
            count = 0;
            resetTime = DateTime.Now.AddMinutes(1);
        }
        if(count >= 3)
        {

            context.Response.StatusCode = 429;
            

            return;

            

        }
        count++;
        
    }

    await next();
});

app.MapGet("/popular", async()=>{
    using var db = new MySqlConnection(connString);
    var popularUrls = await db.QueryFirstAsync("SELECT ShortCode, OriginalUrl, Clicks FROM urls ORDER BY Clicks DESC LIMIT 5");

    return Results.Ok(popularUrls);


});







using (var db = new MySqlConnection(connString))
{
    db.Execute("CREATE TABLE IF NOT EXISTS urls (ShortCode VARCHAR(10) PRIMARY KEY, OriginalUrl TEXT NOT NULL, Clicks INT DEFAULT 0);");
}

app.MapGet("/stats/{code}",async  (string code) =>

{
    using var db = new MySqlConnection(connString);
     var clicks = await db.QueryFirstOrDefaultAsync<int?>($"SELECT Clicks FROM urls WHERE ShortCode = '{code}'");

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
    await db.ExecuteAsync($"INSERT INTO urls (ShortCode, OriginalUrl) VALUES ('{code}', '{url}')");

    return Results.Ok(new{shortCode = code, originalUrl = url});
});

app.MapGet("/{code}", async (string code) =>
{
    using var db = new MySqlConnection(connString);
    var OriginalUrl  = await db.QueryFirstOrDefaultAsync<string>($"SELECT OriginalUrl FROM urls WHERE ShortCode = '{code}'");
    if(OriginalUrl is not null){

        await db.ExecuteAsync($"UPDATE urls SET clicks = clicks + 1 WHERE ShortCode = '{code}'");

        return Results.Redirect(OriginalUrl);
    }

    return Results.NotFound("Short Code Not Found");


});

app.Run();