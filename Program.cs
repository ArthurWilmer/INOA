using System.Globalization;
using StockAlarm.Services;

// Função auxiliar que mostra o uso correto do programa;
// É chamada quando há algum argumento inválido na linha de comando
static int PrintUsage()
{
    Console.WriteLine("Uso:");
    Console.WriteLine("  StockAlarm.exe <TICKER> <PRECO_VENDA> <PRECO_COMPRA>");
    Console.WriteLine("Exemplo:");
    Console.WriteLine("  StockAlarm.exe PETR4 22.67 22.59");
    return 1;
}

// Validação dos 3 argumentos da linha de comando <TICKER> <PRECO_VENDA> <PRECO_COMPRA>
if (args.Length != 3)
    return PrintUsage();

var ticker = args[0].Trim().ToUpperInvariant();

// Garante que o separador decimal seja sempre ponto (.), usando InvariantCulture
if (!decimal.TryParse(args[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var sellPrice))
{
    Console.WriteLine("PRECO_VENDA inválido. Use ponto como separador decimal (ex: 22.67).");
    return PrintUsage();
}
// Ide o preço de venda acima, mas agora para o de compra.
if (!decimal.TryParse(args[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var buyPrice))
{
    Console.WriteLine("PRECO_COMPRA inválido. Use ponto como separador decimal (ex: 22.59).");
    return PrintUsage();
}

Console.WriteLine($"Monitorando {ticker} | venda: {sellPrice} | compra: {buyPrice}");

// ------------------------------------------------------------
// Leitura do arquivo config.json
// O arquivo é procurado no diretório onde o executável está rodando
// (AppContext.BaseDirectory)
// ------------------------------------------------------------
var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");

StockAlarm.Models.AppConfig config;
try
{
    // Carrega e valida o arquivo de configuração
    config = ConfigLoader.Load(configPath);
    Console.WriteLine($"Config carregado. Alertas serão enviados para: {config.EmailTo}");
    Console.WriteLine($"Intervalo de consulta: {config.PollIntervalMs} ms");
}
catch (Exception ex)
{
    Console.WriteLine($"Erro ao ler config.json: {ex.Message}");
    return 1;
}

// Serviços

// HttpClient reutilizado durante toda a execução
// Evita criação excessiva de conexões
using var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(10)
};

// Serviço responsável por consultar a cotação na BRAPI
var quoteService = new QuoteService(httpClient);

// Serviço responsável por enviar e-mails via SMTP
var emailService = new EmailService(config);

// Tratamento de encerramento por Cntrl+c
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // impede o encerramento “bruto”
    cts.Cancel();
    Console.WriteLine("\nEncerrando... (Ctrl+C)");
};

// Variáveis de controle
// buyAlertSent / sellAlertSent evitam envio repetido de alertas
// attempt -> contador iterações.
bool buyAlertSent = false;
bool sellAlertSent = false;
int attempt = 0;

Console.WriteLine("Iniciando monitoramento. Pressione Ctrl+C para sair.");

while (!cts.IsCancellationRequested)
{
    attempt++;

    try
    {
        // Consulta o preço atual do ativo via BRAPI
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
            // Marca que o alerta de compra já foi enviado.
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
            // Marca que o alerta de venda já foi enviado
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
        // erros de API, rede ou SMTP
        Console.WriteLine($"Erro: {ex.Message}");
    }
        // Aguarda o intervalo configurado antes da próxima consulta
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
