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
        private readonly IServiceScopeFactory _escopoFactory;
        private readonly ILogger<HostedServicoExcluiDominio> _logger;
        private readonly Config _config;

        public HostedServicoExcluiDominio(IServiceScopeFactory escopoFactory, Config config, ILogger<HostedServicoExcluiDominio> logger)
        {
            _escopoFactory = escopoFactory;
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
                    using var escopo = _escopoFactory.CriarEscopo();
                    await ExcluirAsync(escopo);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Erro ao excluir domínios");
                }

                try { await Task.Delay(TimeSpan.FromSeconds(_config.FrequenciaSegundos), token); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task ExcluirAsync(IServiceScope escopo)
        {
            var caminho = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dados", "dominios-excluir.json");
            var conteudo = File.ReadAllText(caminho);
            var todosDominios = JsonConvert.DeserializeObject<List<Dominio>>(conteudo)!;
            var dominiosExcluir = todosDominios.Where(p => !p.Lido).ToList();

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
