using System.Configuration;
using Topshelf;

namespace Cancela
{
    internal class ConfigureService
    {
        internal static void Configure()
        {
            HostFactory.Run(configure =>
            {
                configure.Service<Servico>(service =>
                {
                    service.ConstructUsing(s => new Servico());
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });
                configure.RunAsLocalSystem();
                configure.SetServiceName(ConfigurationManager.AppSettings["APLICACAO"].ToString());
                configure.SetDisplayName(ConfigurationManager.AppSettings["APLICACAO"].ToString());
                configure.SetDescription($"{ConfigurationManager.AppSettings["APLICACAO"]} Serviço");
            });
        }
    }
}