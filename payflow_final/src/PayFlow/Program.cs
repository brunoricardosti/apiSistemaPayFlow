using PayFlow.Providers;
using PayFlow.Services;
using Serilog;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using System.Reflection; // Necessário para Reflection do XML
using Swashbuckle.AspNetCore.SwaggerGen; // Adiciona o namespace necessário
using Swashbuckle.AspNetCore.Swagger;    // Adiciona o namespace necessário

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do Serilog
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Serviços da API e Swagger unificado
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();                 
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PayFlow API", Version = "v1" });

    // Configuração correta para ler o XML de comentários
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    // Verifica se o arquivo existe para evitar crash se o XML não for gerado
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var config = builder.Configuration;

// 3. Política de Resiliência (Polly)
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() => // Adiciona static conforme sugestão do IDE0062
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt)));

// 4. Configuração dos HttpClients Tipados
// Nota: AddHttpClient registra os serviços como TRANSIENT por padrão.
builder.Services.AddHttpClient<IFastPayProvider, FastPayProvider>((sp, client) => {
    var baseUrl = config["Providers:FastPay:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl)) client.BaseAddress = new Uri(baseUrl);

    var key = config["Providers:FastPay:ApiKey"];
    if (!string.IsNullOrWhiteSpace(key)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
}).AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHttpClient<ISecurePayProvider, SecurePayProvider>((sp, client) => {
    var baseUrl = config["Providers:SecurePay:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl)) client.BaseAddress = new Uri(baseUrl);

    var token = config["Providers:SecurePay:Token"];
    if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Add("X-API-TOKEN", token);
}).AddPolicyHandler(GetRetryPolicy());

// 5. Registro de Polimorfismo (Correção Crítica de Lifetime)
// Mudamos de AddSingleton para AddTransient para alinhar com o ciclo de vida do HttpClient.
// Isso evita problemas de DNS e conexões presas.
builder.Services.AddTransient<IPaymentProvider>(sp => sp.GetRequiredService<IFastPayProvider>());
builder.Services.AddTransient<IPaymentProvider>(sp => sp.GetRequiredService<ISecurePayProvider>());

// 6. Registro do Serviço Principal
// Mudamos para Scoped (padrão para serviços web) ou Transient. Nunca Singleton se ele depende de HttpClient.
builder.Services.AddScoped<PaymentService>();

var app = builder.Build();

// Configuração do Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

// É recomendável colocar o log de requisição após o Routing para ter dados do Endpoint, 
// mas antes da execução dos controllers.
app.UseSerilogRequestLogging();

app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Iniciando Web Host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host encerrado inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}