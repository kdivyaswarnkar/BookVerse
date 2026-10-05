namespace BookStore.Core.Options;

// Bound to the "Email" section. Provider "Log" only writes emails to the log (development).
// Provider "Smtp" really sends. The Password must come from User Secrets / environment, never from a file in Git.
public class EmailOptions
{
    public string Provider { get; set; } = "Log";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "no-reply@bookstore.local";
    public string FromName { get; set; } = "BookStore";
}
