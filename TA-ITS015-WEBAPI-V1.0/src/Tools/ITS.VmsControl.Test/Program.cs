using Furion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unity;

namespace ITS.VmsControl.Test
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            // Manually create a Host Builder for DI
            var host = Host.CreateDefaultBuilder()
                .Inject()
                .ConfigureServices((context, services) =>
                {
                    services.AddFriendlyException();
                    services.AddLocalization();
                    // Register your WinForms MainForm for DI
                    services.AddScoped<Form1>();
                    
                    // Register Furion services (optional)
                    //services.AddLocalization();
                })
                .Build();
            
            // Resolve MainForm using Furion’s DI
            var mainForm = host.Services.GetRequiredService<Form1>();
            Application.Run(mainForm);
        }

     
    }
}