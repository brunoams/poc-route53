using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Amazon.Route53;
using Amazon.Route53.Model;
using ConsoleDominioSmart.Auxiliar;
using ConsoleDominioSmart.Modelos;
using ConsoleDominioSmart.Configuracoes.AppSettings;

namespace ConsoleDominioSmart.Servicos
{
    public class ServicoDominio
    {
        private static readonly int[] RetryDelaysMs = { 2000, 4000, 8000 };

        private readonly IAmazonRoute53 _awsRoute53;
        private readonly Config _config;
        private readonly FilaDominioConsultar _fila;

        public ServicoDominio(IAmazonRoute53 awsRoute53, Config config, FilaDominioConsultar fila)
        {
            _awsRoute53 = awsRoute53;
            _config = config;
            _fila = fila;
        }

        public async Task<ChangeStatus> ConsultarStatusAsync(DominioConsulta dominio)
        {
            return await ExecutarComRetryAsync(async () =>
            {
                var status = await _awsRoute53.GetChangeAsync(new GetChangeRequest(dominio.InformacoesDominio.Id));
                return status.ChangeInfo.Status;
            });
        }

        public async Task RegistrarAsync(Dominio dominio)
        {
            await ProcessarAsync(dominio, ChangeAction.CREATE);
        }

        public async Task ExcluirAsync(Dominio dominio)
        {
            await ProcessarAsync(dominio, ChangeAction.DELETE);
        }

        private async Task ProcessarAsync(Dominio dominio, ChangeAction acao)
        {
            await ExecutarComRetryAsync(async () =>
            {
                var recordSet = new ResourceRecordSet(dominio.Url, RRType.A)
                {
                    AliasTarget = new AliasTarget(_config.AWS.HostedZoneSaEast1Id, _config.UrlAplicacaoBeanstalk)
                    {
                        EvaluateTargetHealth = false
                    }
                };

                var change = new Change(acao, recordSet);
                var changeBatch = new ChangeBatch(new List<Change> { change });
                var recordSetRequest = new ChangeResourceRecordSetsRequest(_config.AWS.HostedZoneSmartOnlineAppId, changeBatch);

                var recordSetResponse = await _awsRoute53.ChangeResourceRecordSetsAsync(recordSetRequest);

                dominio.Lido = true;
                _fila.Enfileirar(new DominioConsulta(dominio, recordSetResponse.ChangeInfo));
            });
        }

        private static async Task<T> ExecutarComRetryAsync<T>(Func<Task<T>> operacao)
        {
            Exception? ultima = null;
            for (int i = 0; i < RetryDelaysMs.Length; i++)
            {
                try { return await operacao(); }
                catch (Exception ex)
                {
                    ultima = ex;
                    if (i < RetryDelaysMs.Length - 1)
                        await Task.Delay(RetryDelaysMs[i]);
                }
            }
            throw ultima!;
        }

        private static async Task ExecutarComRetryAsync(Func<Task> operacao)
        {
            Exception? ultima = null;
            for (int i = 0; i < RetryDelaysMs.Length; i++)
            {
                try { await operacao(); return; }
                catch (Exception ex)
                {
                    ultima = ex;
                    if (i < RetryDelaysMs.Length - 1)
                        await Task.Delay(RetryDelaysMs[i]);
                }
            }
            throw ultima!;
        }
    }
}
