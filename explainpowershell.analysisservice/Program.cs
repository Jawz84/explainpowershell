using explainpowershell.analysisservice.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.IO;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddEnvironmentVariables();

        if (File.Exists("local.settings.json"))
        {
            builder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        }
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddLogging();
        services.Configure<AiExplanationOptions>(context.Configuration.GetSection(AiExplanationOptions.SectionName));
        
        // Register ChatClient factory
        services.AddSingleton<ChatClient?>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AiExplanationOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<Program>>();
            
            var isConfigured = options.Enabled
                && !string.IsNullOrWhiteSpace(options.ApiKey)
                && !string.IsNullOrWhiteSpace(options.DeploymentName)
                && !string.IsNullOrWhiteSpace(options.Endpoint);

            if (!isConfigured)
            {
                logger.LogWarning("AI explanation ChatClient not configured. AI features will be disabled.");
                return null;
            }

            logger.LogInformation(
                "Registering ChatClient with model '{Model}' at endpoint '{Endpoint}'.",
                options.DeploymentName,
                options.Endpoint);

            var credential = new ApiKeyCredential(options.ApiKey!);
            return new ChatClient(
                credential: credential,
                model: options.DeploymentName!,
                options: new OpenAIClientOptions
                {
                    Endpoint = new Uri(options.Endpoint!)
                });
        });
        
        services.AddSingleton<IAiExplanationService, AiExplanationService>();
    })
    .Build();

host.Run();
