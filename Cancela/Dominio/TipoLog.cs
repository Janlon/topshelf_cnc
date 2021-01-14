using System.Collections.Generic;

namespace Dominio
{
    public class TipoLog
    {
        public int ID_TIPO_LOG { get; set; }

        public string DESCRICAO { get; set; }

        public IList<Log> Logs { get; set; }

        public TipoLog _ID_TIPO_LOG(int v_ID_TIPO_LOG)
        {
            this.ID_TIPO_LOG = v_ID_TIPO_LOG;
            return this;
        }

        public TipoLog _DESCRICAO(string v_DESCRICAO)
        {
            this.DESCRICAO = v_DESCRICAO;
            return this;
        }

        public TipoLog _Logs(IList<Log> v_Logs)
        {
            this.Logs = v_Logs;
            return this;
        }
    }
}
