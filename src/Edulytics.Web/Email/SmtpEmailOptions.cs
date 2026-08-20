namespace Edulytics.Web.Email;

public sealed class SmtpEmailOptions
{
    public const string SectionName =
        "Email:Smtp";

    public bool Enabled { get; set; }

    public string Host { get; set; } =
        string.Empty;

    public int Port { get; set; } = 587;

    public string Security { get; set; } =
        "StartTls";

    public string Username { get; set; } =
        string.Empty;

    public string Password { get; set; } =
        string.Empty;

    public string FromAddress { get; set; } =
        string.Empty;

    public string FromName { get; set; } =
        "Edulytics";

    public int TimeoutSeconds { get; set; } =
        10;

    public int CircuitFailureThreshold { get; set; } =
        3;

    public int CircuitBreakSeconds { get; set; } =
        60;
}
