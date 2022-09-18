using ImageGramFunctions.Options;
using ImageGramFunctions.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

[assembly: FunctionsStartup(typeof(ImageGramFunctions.Startup))]
namespace ImageGramFunctions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                 .AddEnvironmentVariables()
                 .Build();

            builder.Services.AddSingleton<IConfiguration>(config);
            builder.Services.Configure<AzureStorageConfig>(config.GetSection("AzureStorageConfig"));

            builder.Services.AddScoped<IAzureBlobService, AzureBlobService>();
            builder.Services.AddScoped<IMessageHandlerService, MessageHandlerService>();
        }
    }
}
