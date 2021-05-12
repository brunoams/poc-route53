using Amazon.Route53.Model;

namespace ConsoleDominioSmart.Modelos
{
    public class DominioConsulta
    {
        public Dominio Dominio { get; set; }
        public ChangeInfo InformacoesDominio { get; set; }

        public DominioConsulta(Dominio dominio, ChangeInfo informacoesDominio)
        {
            Dominio = dominio;
            InformacoesDominio = informacoesDominio;
        }
    }
}
