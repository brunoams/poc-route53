using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using ConsoleDominioSmart.Configuracoes;
using ConsoleDominioSmart.Configuracoes.AppSettings;

namespace ConsoleDominioSmart
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder().Configurar().ConfigureServices((contexto, servico) =>
            {
                var config = contexto.Configuration.GetSection("Config").Get<Config>();

                servico.ConfigurarNLog();
                servico.ConfigurarTempoLimiteDesligamento(30);
                servico.ConfigurarAmbienteAmazon(config, contexto.Configuration);
                servico.ConfigurarAmazonRoute53();
                servico.InjetarDependencias(config);
            });

            await host.UseConsoleLifetime().Build().RunAsync();
        }
    }
}