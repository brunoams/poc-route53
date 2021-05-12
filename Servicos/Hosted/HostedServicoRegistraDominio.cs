using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ConsoleDominioSmart.Modelos;
using ConsoleDominioSmart.Auxiliar.Extensoes;
using ConsoleDominioSmart.Configuracoes.AppSettings;

namespace ConsoleDominioSmart.Servicos.Hosted
{
    public class HostedServicoRegistraDominio : BackgroundService, IHostedService
    {
        private readonly IServiceScope _escopo;
        private readonly ILogger<HostedServicoRegistraDominio> _logger;
        private readonly Config _config;

        public HostedServicoRegistraDominio(IServiceScope escopo, Config config, ILogger<HostedServicoRegistraDominio> logger)
        {
            _escopo = escopo;
            _logger = logger;
            _config = config;
        }

        public override Task StartAsync(CancellationToken token)
        {
            _logger.LogInformation($"Serviço {nameof(HostedServicoRegistraDominio)} iniciado.");

            token.Register(() => _logger.LogInformation($"Serviço {nameof(HostedServicoRegistraDominio)} está parado."));

            return base.StartAsync(token);
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using (var escopo = _escopo.CriarEscopo())
                    {
                        await RegistrarAsync(escopo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao registrar domínios");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(_config.FrequenciaSegundos));
                }
            }
        }

        private async Task RegistrarAsync(IServiceScope escopo)
        {
            var conteudo = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dados", "dominios-registrar.json"));
            var dominiosRegistrar = JsonConvert.DeserializeObject<List<Dominio>>(conteudo)
                                               .Where(p => !p.Lido);

            if (!dominiosRegistrar.Any())
                return;

            var servicoRota53 = escopo.ObterServico<ServicoDominio>();

            foreach (var dominio in dominiosRegistrar)
            {
                try
                {
                    await servicoRota53.RegistrarAsync(dominio);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao registrar domínio {dominio.Url}");
                }
            }

            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dados", "dominios-registrar.json"), JsonConvert.SerializeObject(dominiosRegistrar));
        }
    }
}
