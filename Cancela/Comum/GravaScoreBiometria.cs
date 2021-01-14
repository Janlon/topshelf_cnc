// Decompiled with JetBrains decompiler
// Type: Comum.GravaScoreBiometria
// Assembly: Comum, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 99DD0288-5D71-4E3E-8396-2C9790D917B3
// Assembly location: C:\Users\janlo\Desktop\G04CNC01\DLL_Acesso_Cancela\DLL_Acesso_Cancela\lib\Comum.dll

using Cancela;
using System;
using System.Data;
using System.Data.OracleClient;
using System.Diagnostics;

namespace Comum
{
  public class GravaScoreBiometria : AcessoDados, IDisposable
  {
    private bool disposedValue = false;

        [Obsolete]
        public void GravarScoreBiometria(
      string v_s_Aplicacao,
      int v_ID_EQUIPAMENTO,
      long v_Id_Secao,
      int v_Vl_Score,
      int v_Id_Tipo_Requisicao = 4)
    {
      try
      {
        OracleDynamicParameters dynamicParameters = new OracleDynamicParameters();
        dynamicParameters.Add("V_ID_SECAO", (object) v_Id_Secao, new OracleType?(), new ParameterDirection?(), new int?());
        dynamicParameters.Add("V_VL_SCORE", (object) v_Vl_Score, new OracleType?(), new ParameterDirection?(), new int?());
        dynamicParameters.Add("V_ID_TIPO_REQUISICAO", (object) v_Id_Tipo_Requisicao, new OracleType?(), new ParameterDirection?(), new int?());
        this.Executar("BANCO", "ACESSO.SP_BIOMETRIA_ADICIONA_SCORE", (object) dynamicParameters, "GravaAcesso.GravarAcesso", CommandType.StoredProcedure);
      }
      catch (Exception ex)
      {
        new GravaEventLog().GravarEventLog(v_s_Aplicacao, "Erro GravarAcesso() ID Controladora: " + v_ID_EQUIPAMENTO.ToString() + " - " + v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
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

    ~GravaScoreBiometria()
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
