using Amazon.Route53;
using Amazon.Route53.Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using ConsoleDominioSmart.Modelos;
using ConsoleDominioSmart.Auxiliar;
using ConsoleDominioSmart.Configuracoes.AppSettings;

namespace ConsoleDominioSmart.Servicos
{
    public class ServicoDominio
    {
        private readonly IAmazonRoute53 _awsRoute53;
        private readonly Config _config;

        public ServicoDominio(IAmazonRoute53 awsRoute53, Config config)
        {
            _awsRoute53 = awsRoute53;
            _config = config;
        }

        public async Task<ChangeStatus> ConsultarStatusAsync(DominioConsulta dominio)
        {
            var status = await _awsRoute53.GetChangeAsync(new GetChangeRequest(dominio.InformacoesDominio.Id));

            return status.ChangeInfo.Status;
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
            var recordSet = new ResourceRecordSet(dominio.Url, RRType.A);
            recordSet.AliasTarget = new AliasTarget(_config.AWS.HostedZoneSaEast1Id, _config.UrlAplicacaoBeanstalk);
            recordSet.AliasTarget.EvaluateTargetHealth = false;

            var change = new Change(acao, recordSet);
            var changeBatch = new ChangeBatch(new List<Change> { change });
            var recordSetRequest = new ChangeResourceRecordSetsRequest(_config.AWS.HostedZoneSmartOnlineAppId, changeBatch);

            var recordSetResponse = await _awsRoute53.ChangeResourceRecordSetsAsync(recordSetRequest);

            dominio.Lido = true;

            FilaDominioConsultar.Enfileirar(new DominioConsulta(dominio, recordSetResponse.ChangeInfo));
        }
    }
}
