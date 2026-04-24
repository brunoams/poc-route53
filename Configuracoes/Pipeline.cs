using System;
using System.IO;
using Amazon.Route53;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ConsoleDominioSmart.Auxiliar;
using ConsoleDominioSmart.Servicos;
using ConsoleDominioSmart.Servicos.Hosted;
using ConsoleDominioSmart.Configuracoes.AppSettings;

namespace ConsoleDominioSmart.Configuracoes
{
    public static class Pipeline
    {
        public static IHostBuilder Configurar(this IHostBuilder builder)
        {
            return builder.ConfigureHostConfiguration(config =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
            }).ConfigureAppConfiguration((hostContext, configApp) =>
            {
                configApp.AddJsonFile("appsettings.json", optional: false);
                configApp.AddEnvironmentVariables();
            });
        }

        public static void ConfigurarTempoLimiteDesligamento(this IServiceCollection servico, double segundos)
        {
            servico.Configure<HostOptions>(opcao =>
            {
                opcao.ShutdownTimeout = TimeSpan.FromSeconds(segundos);
            });
        }

        public static void ConfigurarAmbienteAmazon(this IServiceCollection servico, Config config)
        {
            var awsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
            {
                Region = Amazon.RegionEndpoint.GetBySystemName(config.AWS.Region)
            };
            servico.AddDefaultAWSOptions(awsOptions);
        }

        public static void ConfigurarAmazonRoute53(this IServiceCollection servico)
        {
            servico.AddAWSService<IAmazonRoute53>(ServiceLifetime.Singleton);
        }

        public static void ConfigurarNLog(this IServiceCollection servico)
        {
            servico.AddLogging(logger => logger.AddNLog("nlog.config"));
        }

        public static void InjetarDependencias(this IServiceCollection servico, Config config)
        {
            servico.AddSingleton(config);
            servico.AddSingleton<FilaDominioConsultar>();
            servico.AddScoped<ServicoDominio>();

            servico.AddHostedService<HostedServicoRegistraDominio>();
            servico.AddHostedService<HostedServicoExcluiDominio>();
            servico.AddHostedService<HostedServicoConsultaDominio>();
        }
    }
}
