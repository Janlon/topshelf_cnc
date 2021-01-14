using Cancela;
using System.Configuration;

namespace Infra
{
    public class BaseInfra : AcessoDados
    {
        public string v_Aplicacao = ConfigurationManager.AppSettings["APLICACAO"].ToString();
    }
}

