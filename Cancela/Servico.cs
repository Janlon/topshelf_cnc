using System;
using System.Configuration;

namespace Cancela
{
    public class Servico
    {
        readonly AcessoVeiculos acessoVeiculos = new AcessoVeiculos();

        public void Start()
        {
            Console.WriteLine($"Iniciando serviço {ConfigurationManager.AppSettings["APLICACAO"]}.");
            acessoVeiculos.Inicio();
        }

        public void Stop()
        {
            Console.WriteLine($"Serviço {ConfigurationManager.AppSettings["APLICACAO"]} encerrou.");
        }
    }
}