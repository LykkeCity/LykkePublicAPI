using System;
using System.IO;
using Lykke.Common;
using Microsoft.AspNetCore.Hosting;

namespace LykkePublicAPI
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine($"{AppEnvironment.Name} {AppEnvironment.Version}");
            try
            {
                var hostBuilder = new WebHostBuilder()
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseIISIntegration()
                    .UseStartup<Startup>();
#if !DEBUG
                hostBuilder = hostBuilder.UseApplicationInsights();
#endif
                var host = hostBuilder.Build();

                host.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
