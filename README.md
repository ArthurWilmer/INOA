# Stock Alarm – Monitor de Cotação com Alertas por E-mail

Aplicação de linha de comando em **C# (.NET)** que monitora continuamente a cotação de um ativo da **B3** utilizando a **API da BRAPI** e envia **alertas por e-mail** quando o preço atinge limites de **compra** ou **venda** definidos pelo usuário.

O programa roda enquanto estiver em execução e pode ser encerrado a qualquer momento com `Ctrl + C`.

Este projeto foi desenvolvido como parte de um desafio técnico da INOA

## Tecnologias Utilizadas

* **C# (.NET)**
* **System.Net.Http** – requisições HTTP
* **System.Text.Json** – processamento de JSON
* **System.Net.Mail** – envio de e-mails via SMTP
* **System.Globalization** – padronização de valores decimais
* **API BRAPI** – cotações da B3

---

## Pré-requisitos

* .NET SDK instalado (versão 9 ou superior recomendada)
* Token válido da BRAPI
   [https://brapi.dev](https://brapi.dev)
* Conta de e-mail com acesso SMTP (ex: Gmail, Outlook, etc.)
* Ambiente Windows para execução do `.exe`

---

##  Configuração

### 1️. Criar o arquivo `config.json`

Crie um arquivo chamado `config.json` na raiz do projeto (ele será copiado automaticamente para o diretório de execução no build).

Exemplo:

```json
{
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "SmtpEnableSsl": true,
  "SmtpUser": "seuemail@gmail.com",
  "SmtpPassword": "SENHA_OU_SENHA_DE_APP",
  "EmailFrom": "seuemail@gmail.com",
  "EmailTo": "destino@exemplo.com",
  "BrapiToken": "SEU_TOKEN_BRAPI",
  "PollIntervalMs": 300000
}
```

### 2. Descrição dos campos

* **SmtpHost**: servidor SMTP (ex: `smtp.gmail.com`)
* **SmtpPort**: porta SMTP (geralmente `587`)
* **SmtpEnableSsl**: habilita TLS/SSL (`true` recomendado)
* **SmtpUser / SmtpPassword**: credenciais SMTP
* **EmailFrom**: remetente do e-mail
* **EmailTo**: destinatário dos alertas
* **BrapiToken**: token da API BRAPI
* **PollIntervalMs**: intervalo entre consultas (em ms)

> O arquivo `config.json` está no `.gitignore` e não deve ser versionado, pois contém credenciais.

---

## Execução

### Rodando via `dotnet run`

```bash
dotnet run --project StockAlarm -- PETR4 22.67 22.59
```

### Rodando o executável publicado

```bash
StockAlarm.exe PETR4 32.80 29.59
```

### Parâmetros (ordem obrigatória)

1. TICKER – Código do ativo (ex: PETR4, VALE3)
2. PRECO_VENDA – Preço que dispara alerta de venda
3. PRECO_COMPRA – Preço que dispara alerta de compra

---

## Funcionamento do Sistema

1. O programa valida os argumentos da linha de comando
2. Carrega as configurações do `config.json`
3. Consulta a cotação do ativo na BRAPI
4. Compara o preço atual com os limites definidos
5. Envia e-mail quando:

   * Preço ≤ limite de compra
   * Preço ≥ limite de venda
6. O processo se repete no intervalo configurado
7. O programa pode ser encerrado com `Ctrl + C`

---

## Prevenção de Spam de Alertas

O sistema possui uma lógica simples de controle para evitar envio repetido de e-mails:

* Um alerta só é enviado uma vez enquanto o preço permanecer na condição
* Um novo alerta só é permitido quando o preço sai da condição e retorna novamente

---

## Organização do Código

```
StockAlarm/
│
├── Models/
│   ├── AppConfig.cs              # Representa o config.json
│   └── BrapiQuoteResponse.cs     # DTO da resposta da BRAPI
│
├── Services/
│   ├── ConfigLoader.cs           # Leitura e validação do config.json
│   ├── EmailService.cs           # Envio de e-mails via SMTP
│   └── QuoteService.cs           # Consulta de preços na BRAPI
│
├── Program.cs                    # Ponto de entrada e loop principal
└── StockAlarm.csproj
```

* **Models**: classes que representam dados
* **Services**: classes responsáveis por ações e regras

---

## Exemplo de saída
<img width="841" height="212" alt="image" src="https://github.com/user-attachments/assets/52383c5b-5123-48dd-8e80-98dd4f1defd9" />


---

## Rodar com executável

Para gerar o executável:

```bash
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true -o publish
```

Após o build:

* Certifique-se que `config.json` está na pasta `publish`. Se não, copie-o da raiz. 
* Execute o `.exe` normalmente
