// Decompiled with JetBrains decompiler
// Type: Comum.PegaPortaEscuta
// Assembly: Comum, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 99DD0288-5D71-4E3E-8396-2C9790D917B3
// Assembly location: C:\Users\janlo\Desktop\G04CNC01\DLL_Acesso_Cancela\DLL_Acesso_Cancela\lib\Comum.dll

using Cancela;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Diagnostics;

namespace Comum
{
  public class PegaPortaEscuta : AcessoDados, IDisposable
  {
    private bool disposedValue = false;

    public int NR_PORTA_APLICACAO { get; set; }

        [Obsolete]
        public int PegarPortaEscuta(int v_Id_Equipamento, string v_s_Aplicacao)
    {
      try
      {
        OracleDynamicParameters dynamicParameters = new OracleDynamicParameters();
        dynamicParameters.Add("V_ID_EQUIPAMENTO", (object) v_Id_Equipamento, new OracleType?(), new ParameterDirection?(), new int?());
        dynamicParameters.Add("V_EQUIP_CONFIG_PORTA_ESCUTA", (object) null, new OracleType?(OracleType.Cursor), new ParameterDirection?(ParameterDirection.Output), new int?());
        IEnumerable<PegaPortaEscuta> pegaPortaEscutas = this.Pesquisar<PegaPortaEscuta>("BANCO", "EQUIPAMENTO_CONFIGURACAO.SP_EQUIP_CONFIG_PORTA_ESCUTA", "PegaPortaEscuta.PegarPortaEscuta", (object) dynamicParameters, CommandType.StoredProcedure, true);
        if (pegaPortaEscutas != null)
        {
          using (IEnumerator<PegaPortaEscuta> enumerator = pegaPortaEscutas.GetEnumerator())
          {
            if (enumerator.MoveNext())
              return enumerator.Current.NR_PORTA_APLICACAO;
          }
        }
        return 0;
      }
      catch (Exception ex)
      {
        new GravaEventLog().GravarEventLog(v_s_Aplicacao, "Erro PegarPortaEscuta(). ID Controladora: " + v_Id_Equipamento.ToString() + " - " + v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
        return 0;
      }
      finally
      {
        this.Terminate();
      }
    }

    protected virtual void Dispose(bool disposing)
    {
      if (this.disposedValue)
        return;
      if (!disposing)
        ;
      this.disposedValue = true;
    }

    ~PegaPortaEscuta()
    {
      this.Dispose(false);
    }

    void IDisposable.Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    public void Terminate()
    {
      this.Dispose(true);
    }
  }
}
