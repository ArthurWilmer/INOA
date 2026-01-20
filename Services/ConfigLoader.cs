using System.Text.Json;
using StockAlarm.Models;

namespace StockAlarm.Services;

public static class ConfigLoader
{
    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Arquivo de configuração não encontrado: {path}");

        var json = File.ReadAllText(path);

        var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config is null)
            throw new InvalidOperationException("Não foi possível ler o config.json (JSON inválido).");

        Validate(config);
        return config;
    }

    private static void Validate(AppConfig config)
    {
        // Email
        if (string.IsNullOrWhiteSpace(config.EmailTo))
            throw new InvalidOperationException("Config inválido: EmailTo é obrigatório.");
        if (string.IsNullOrWhiteSpace(config.EmailFrom))
            throw new InvalidOperationException("Config inválido: EmailFrom é obrigatório.");

        // SMTP
        if (string.IsNullOrWhiteSpace(config.SmtpHost))
            throw new InvalidOperationException("Config inválido: SmtpHost é obrigatório.");
        if (config.SmtpPort <= 0)
            throw new InvalidOperationException("Config inválido: SmtpPort deve ser > 0.");

        if (string.IsNullOrWhiteSpace(config.SmtpUser))
            throw new InvalidOperationException("Config inválido: SmtpUser é obrigatório.");
        if (string.IsNullOrWhiteSpace(config.SmtpPassword))
            throw new InvalidOperationException("Config inválido: SmtpPassword é obrigatório.");

        // Polling
        if (config.PollIntervalMs < 1000)
            throw new InvalidOperationException("Config inválido: PollIntervalMs deve ser >= 1000.");
    }
}
