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
        private readonly IServiceScope _escopo;
        private readonly ILogger<HostedServicoConsultaDominio> _logger;
        private readonly Config _config;

        public HostedServicoConsultaDominio(IServiceScope escopo, Config config, ILogger<HostedServicoConsultaDominio> logger)
        {
            _escopo = escopo;
            _logger = logger;
            _config = config;
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
                    using (var escopo = _escopo.CriarEscopo())
                    {
                        await ConsultarAsync(escopo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao consultar status dos domínios");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(_config.FrequenciaSegundos));
                }
            }
        }

        private async Task ConsultarAsync(IServiceScope escopo)
        {
            var servicoRota53 = escopo.ObterServico<ServicoDominio>();

            while (FilaDominioConsultar.Fila.TryDequeue(out DominioConsulta dominioConsulta))
            {
                try
                {
                    Console.WriteLine($"Consultando status do Domínio {dominioConsulta.Dominio.Url}");

                    var resultado = await servicoRota53.ConsultarStatusAsync(dominioConsulta);

                    Console.WriteLine($"Status: {resultado.Value}");

                    if (resultado == ChangeStatus.PENDING)
                        FilaDominioConsultar.Enfileirar(dominioConsulta);

                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao consultar status do domínio {dominioConsulta.Dominio.Url}");
                }
            }
        }
    }
}
