using System.Globalization;
using StockAlarm.Services;

static int PrintUsage()
{
    Console.WriteLine("Uso:");
    Console.WriteLine("  StockAlarm.exe <TICKER> <PRECO_VENDA> <PRECO_COMPRA>");
    Console.WriteLine("Exemplo:");
    Console.WriteLine("  StockAlarm.exe PETR4 22.67 22.59");
    return 1;
}

if (args.Length != 3)
    return PrintUsage();

var ticker = args[0].Trim().ToUpperInvariant();

if (!decimal.TryParse(args[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var sellPrice))
{
    Console.WriteLine("PRECO_VENDA inválido. Use ponto como separador decimal (ex: 22.67).");
    return PrintUsage();
}

if (!decimal.TryParse(args[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var buyPrice))
{
    Console.WriteLine("PRECO_COMPRA inválido. Use ponto como separador decimal (ex: 22.59).");
    return PrintUsage();
}

Console.WriteLine($"Monitorando {ticker} | venda: {sellPrice} | compra: {buyPrice}");

// ---------------------------------
// 2) Leitura do arquivo config.json
// ---------------------------------
var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");

StockAlarm.Models.AppConfig config;
try
{
    config = ConfigLoader.Load(configPath);
    Console.WriteLine($"Config carregado. Alertas serão enviados para: {config.EmailTo}");
    Console.WriteLine($"Intervalo de consulta: {config.PollIntervalMs} ms");
}
catch (Exception ex)
{
    Console.WriteLine($"Erro ao ler config.json: {ex.Message}");
    return 1;
}

using var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(10)
};

var quoteService = new QuoteService(httpClient);
var emailService = new EmailService(config);

// Ctrl+C para sair
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // impede o encerramento “bruto”
    cts.Cancel();
    Console.WriteLine("\nEncerrando... (Ctrl+C)");
};

bool buyAlertSent = false;
bool sellAlertSent = false;
int attempt = 0;

Console.WriteLine("Iniciando monitoramento. Pressione Ctrl+C para sair.");

while (!cts.IsCancellationRequested)
{
    attempt++;

    try
    {
        var price = await quoteService.GetRegularMarketPriceAsync(ticker, config.BrapiToken, cts.Token);

        Console.WriteLine($"[{attempt}] {DateTime.Now:yyyy-MM-dd HH:mm:ss} | {ticker} = R$ {price.ToString(CultureInfo.InvariantCulture)}");

        // ALERTA DE COMPRA: preço <= limite de compra
        if (price <= buyPrice && !buyAlertSent)
        {
            var subject = $"Alerta de COMPRA - {ticker}";
            var body =
                $"O ativo {ticker} atingiu o preço de COMPRA.\n" +
                $"Preço atual: R$ {price.ToString(CultureInfo.InvariantCulture)}\n" +
                $"Limite compra: R$ {buyPrice.ToString(CultureInfo.InvariantCulture)}\n" +
                $"Data/Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            emailService.Send(subject, body);
            Console.WriteLine(">> Alerta de compra enviado por e-mail.");
            buyAlertSent = true;
        }

        // ALERTA DE VENDA: preço >= limite de venda
        if (price >= sellPrice && !sellAlertSent)
        {
            var subject = $"Alerta de VENDA - {ticker}";
            var body =
                $"O ativo {ticker} atingiu o preço de VENDA.\n" +
                $"Preço atual: R$ {price.ToString(CultureInfo.InvariantCulture)}\n" +
                $"Limite venda: R$ {sellPrice.ToString(CultureInfo.InvariantCulture)}\n" +
                $"Data/Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            emailService.Send(subject, body);
            Console.WriteLine(">> Alerta de venda enviado por e-mail.");
            sellAlertSent = true;
        }

        // Anti-spam: libera novo alerta quando voltar pra "zona neutra"
        // zona neutra = entre buyPrice e sellPrice
        if (price > buyPrice)
            buyAlertSent = false;

        if (price < sellPrice)
            sellAlertSent = false;
    }
    catch (OperationCanceledException)
    {
        // cancelamento via Ctrl+C
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro: {ex.Message}");
    }

    try
    {
        await Task.Delay(config.PollIntervalMs, cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

Console.WriteLine("Programa encerrado.");
return 0;
