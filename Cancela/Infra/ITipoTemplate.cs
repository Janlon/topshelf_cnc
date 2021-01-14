using Dominio;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infra
{
    public class ITipoTemplate : BaseInfra
    {
        public IEnumerable<TipoLog> Listar(
          string connectionString,
          string query,
          object parameters,
          CommandType commandType,
          bool comBuffer,
          string v_Cliente = "")
        {
            try
            {
                return this.Pesquisar<TipoLog>(connectionString, query, v_Cliente, parameters, commandType, comBuffer);
            }
            catch (Exception ex)
            {
                if (!EventLog.SourceExists(this.v_Aplicacao))
                    EventLog.CreateEventSource(this.v_Aplicacao, this.v_Aplicacao);
                EventLog.WriteEntry(this.v_Aplicacao, "Erro ITipoTemplate.Listar() Log -  - query: " + query + " - erro: " + ex.Message, EventLogEntryType.Error);
                return (IEnumerable<TipoLog>)null;
            }
        }

        [Obsolete]
        public void Salvar(
          string connectionString,
          string query,
          object parameters,
          CommandType commandType,
          string v_Cliente = "")
        {
            try
            {
                Executar(connectionString, query, parameters, v_Cliente, commandType);
            }
            catch (Exception ex)
            {
                if (!EventLog.SourceExists(this.v_Aplicacao))
                    EventLog.CreateEventSource(this.v_Aplicacao, this.v_Aplicacao);
                EventLog.WriteEntry(this.v_Aplicacao, "Erro ITipoTemplate.Salvar() Log -  - query: " + query + " - erro: " + ex.Message, EventLogEntryType.Error);
            }
        }
    }
}
