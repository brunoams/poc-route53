using Microsoft.Extensions.DependencyInjection;

namespace ConsoleDominioSmart.Auxiliar.Extensoes
{
    public static class IServicesExtensoes
    {
        public static IServiceScope CriarEscopo(this IServiceScope escopo)
        {
            return escopo.ServiceProvider.CreateScope();
        }

        public static T ObterServico<T>(this IServiceScope escopo)
        {
            return escopo.ServiceProvider.GetRequiredService<T>();
        }
    }
}
