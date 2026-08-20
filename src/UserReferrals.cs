using Dapper;
using MySqlConnector;

public class UserReferral
{
    public string ReferrerCode { get; set; }
    public string ReferrerEmail { get; set; }
    public DateTime Timestamp { get; set; }
}

public class UserReferralTracker
{
    private readonly MySqlConnection _db;
    public UserReferralTracker(MySqlConnection db)
    {
        _db = db;
    }

    public async Task<bool> TrackReferralAsync(string referrerCode, string referrerEmail)
    {
        var referral = new UserReferral
        {
            ReferrerCode = referrerCode,
            ReferrerEmail = referrerEmail,
            Timestamp = DateTime.Now
        };

        var result = await _db.ExecuteAsync($"INSERT INTO user_referrals (ReferrerCode, ReferrerEmail, Timestamp) VALUES ('{referrerCode}', '{referrerEmail}', '{DateTime.Now}')");

        return result > 0;
    }
}