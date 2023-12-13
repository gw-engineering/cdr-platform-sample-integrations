
using CdrAzureFunctions.Interfaces;
using CdrAzureFunctions.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CdrAzureFunctions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IBlobClientFactory, BlobClientFactory>();
            builder.Services.AddTransient<IWriteFile, FileBlobWriter>();
            builder.Services.AddTransient<IProtectFile, FileBlobProtectService>();
        }
    }
}
