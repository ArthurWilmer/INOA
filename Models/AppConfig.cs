namespace StockAlarm.Models;

public sealed class AppConfig
{
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public bool SmtpEnableSsl { get; set; } = true;

    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";

    public string EmailFrom { get; set; } = "";
    public string EmailTo { get; set; } = "";

    public string BrapiToken { get; set; } = "";

    public int PollIntervalMs { get; set; } = 10000;
}
