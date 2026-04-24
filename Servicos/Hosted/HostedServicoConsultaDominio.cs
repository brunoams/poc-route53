using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Route53;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ConsoleDominioSmart.Auxiliar;
using ConsoleDominioSmart.Auxiliar.Extensoes;
using ConsoleDominioSmart.Modelos;
using ConsoleDominioSmart.Configuracoes.AppSettings;

namespace ConsoleDominioSmart.Servicos.Hosted
{
    public class HostedServicoConsultaDominio : BackgroundService, IHostedService
    {
        private readonly IServiceScopeFactory _escopoFactory;
        private readonly ILogger<HostedServicoConsultaDominio> _logger;
        private readonly Config _config;
        private readonly FilaDominioConsultar _fila;

        public HostedServicoConsultaDominio(IServiceScopeFactory escopoFactory, Config config, ILogger<HostedServicoConsultaDominio> logger, FilaDominioConsultar fila)
        {
            _escopoFactory = escopoFactory;
            _logger = logger;
            _config = config;
            _fila = fila;
        }

        public override Task StartAsync(CancellationToken token)
        {
            _logger.LogInformation($"Serviço {nameof(HostedServicoConsultaDominio)} iniciado.");
            token.Register(() => _logger.LogInformation($"Serviço {nameof(HostedServicoConsultaDominio)} está parado."));
            return base.StartAsync(token);
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var escopo = _escopoFactory.CriarEscopo();
                    await ConsultarAsync(escopo);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Erro ao consultar status dos domínios");
                }

                try { await Task.Delay(TimeSpan.FromSeconds(_config.FrequenciaSegundos), token); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task ConsultarAsync(IServiceScope escopo)
        {
            var servicoRota53 = escopo.ObterServico<ServicoDominio>();

            while (_fila.TryDequeue(out DominioConsulta? dominioConsulta))
            {
                try
                {
                    _logger.LogInformation($"Consultando status do domínio {dominioConsulta!.Dominio.Url}");

                    var resultado = await servicoRota53.ConsultarStatusAsync(dominioConsulta);

                    _logger.LogInformation($"Status do domínio {dominioConsulta.Dominio.Url}: {resultado.Value}");

                    if (resultado == ChangeStatus.PENDING)
                        _fila.Enfileirar(dominioConsulta);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao consultar status do domínio {dominioConsulta!.Dominio.Url}");
                }
            }
        }
    }
}
