// Decompiled with JetBrains decompiler
// Type: Comum.VerificaGuarda
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
  public class VerificaGuarda : AcessoDados, IDisposable
  {
    private bool disposedValue = false;

    public string RETORNO { get; set; }

        [Obsolete]
        public bool VerificarGuarda(string v_Credencial, string v_s_Aplicacao, int v_Id_Equipamento)
    {
      try
      {
        OracleDynamicParameters dynamicParameters = new OracleDynamicParameters();
        dynamicParameters.Add("V_CREDENCIAL", (object) v_Credencial, new OracleType?(), new ParameterDirection?(), new int?());
        dynamicParameters.Add("V_CONSULTA_GUARDA", (object) null, new OracleType?(OracleType.Cursor), new ParameterDirection?(ParameterDirection.Output), new int?());
        IEnumerable<VerificaGuarda> verificaGuardas = this.Pesquisar<VerificaGuarda>("BANCO", "GUARDA.SP_CONSULTA_GUARDA", "VerificaGuarda.VerificarGuarda", (object) dynamicParameters, CommandType.StoredProcedure, true);
        if (verificaGuardas != null)
        {
          using (IEnumerator<VerificaGuarda> enumerator = verificaGuardas.GetEnumerator())
          {
            if (enumerator.MoveNext())
            {
              VerificaGuarda current = enumerator.Current;
              return current.RETORNO != null && current.RETORNO == "63";
            }
          }
        }
        return false;
      }
      catch (Exception ex)
      {
        new GravaEventLog().GravarEventLog(v_s_Aplicacao, "Erro VerificarGuarda(). ID Controladora: " + v_Id_Equipamento.ToString() + " - " + v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
        return false;
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

    ~VerificaGuarda()
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
