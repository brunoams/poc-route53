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
    public class HostedServicoExcluiDominio : BackgroundService, IHostedService
    {
        private readonly IServiceScope _escopo;
        private readonly ILogger<HostedServicoExcluiDominio> _logger;
        private readonly Config _config;

        public HostedServicoExcluiDominio(IServiceScope escopo, Config config, ILogger<HostedServicoExcluiDominio> logger)
        {
            _escopo = escopo;
            _logger = logger;
            _config = config;
        }

        public override Task StartAsync(CancellationToken token)
        {
            _logger.LogInformation($"Serviço {nameof(HostedServicoExcluiDominio)} iniciado.");

            token.Register(() => _logger.LogInformation($"Serviço {nameof(HostedServicoExcluiDominio)} está parado."));

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
                        await ExcluirAsync(escopo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao excluir domínios");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(_config.FrequenciaSegundos));
                }
            }
        }

        private async Task ExcluirAsync(IServiceScope escopo)
        {
            var conteudo = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dados", "dominios-excluir.json"));
            var dominiosExcluir = JsonConvert.DeserializeObject<List<Dominio>>(conteudo)
                                             .Where(p => !p.Lido);

            if (!dominiosExcluir.Any())
                return;

            var servicoRota53 = escopo.ObterServico<ServicoDominio>();

            foreach (var dominio in dominiosExcluir)
            {
                try
                {
                    await servicoRota53.ExcluirAsync(dominio);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao excluir domínio {dominio.Url}");
                }
            }

            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dados", "dominios-excluir.json"), JsonConvert.SerializeObject(dominiosExcluir));
        }
    }
}
