namespace BookStore.Core.Options;

// Bound to the "Jwt" section. The Key lives in secrets.json, never in appsettings.json.
public class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}