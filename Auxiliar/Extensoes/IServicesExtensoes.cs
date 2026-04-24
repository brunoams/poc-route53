using Microsoft.Extensions.DependencyInjection;

namespace ConsoleDominioSmart.Auxiliar.Extensoes
{
    public static class IServicesExtensoes
    {
        public static IServiceScope CriarEscopo(this IServiceScopeFactory factory)
        {
            return factory.CreateScope();
        }

        public static T ObterServico<T>(this IServiceScope escopo) where T : notnull
        {
            return escopo.ServiceProvider.GetRequiredService<T>();
        }
    }
}
