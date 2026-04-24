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
        private readonly IServiceScopeFactory _escopoFactory;
        private readonly ILogger<HostedServicoRegistraDominio> _logger;
        private readonly Config _config;

        public HostedServicoRegistraDominio(IServiceScopeFactory escopoFactory, Config config, ILogger<HostedServicoRegistraDominio> logger)
        {
            _escopoFactory = escopoFactory;
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
                    using var escopo = _escopoFactory.CriarEscopo();
                    await RegistrarAsync(escopo);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Erro ao registrar domínios");
                }

                try { await Task.Delay(TimeSpan.FromSeconds(_config.FrequenciaSegundos), token); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task RegistrarAsync(IServiceScope escopo)
        {
            var caminho = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dados", "dominios-registrar.json");
            var conteudo = File.ReadAllText(caminho);
            var todosDominios = JsonConvert.DeserializeObject<List<Dominio>>(conteudo)!;
            var dominiosRegistrar = todosDominios.Where(p => !p.Lido).ToList();

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

            EscreverArquivoAtomico(caminho, JsonConvert.SerializeObject(todosDominios));
        }

        private static void EscreverArquivoAtomico(string caminho, string conteudo)
        {
            var caminhoTemp = caminho + ".tmp";
            File.WriteAllText(caminhoTemp, conteudo);
            File.Move(caminhoTemp, caminho, overwrite: true);
        }
    }
}
