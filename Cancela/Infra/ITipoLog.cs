using Dominio;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace Infra
{
    public class ITipoLog : BaseInfra
    {
        [Obsolete]
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
                return Pesquisar<TipoLog>(connectionString, query, v_Cliente, parameters, commandType, comBuffer);
            }
            catch (Exception ex)
            {
                if (!EventLog.SourceExists(this.v_Aplicacao))
                    EventLog.CreateEventSource(this.v_Aplicacao, this.v_Aplicacao);
                EventLog.WriteEntry(this.v_Aplicacao, "Erro ITipoLog.Listar() Log -  - query: " + query + " - erro: " + ex.Message, EventLogEntryType.Error);
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
                EventLog.WriteEntry(this.v_Aplicacao, "Erro ITipoLog.Salvar() Log -  - query: " + query + " - erro: " + ex.Message, EventLogEntryType.Error);
            }
        }
    }
}
