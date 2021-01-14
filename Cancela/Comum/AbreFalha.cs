// Decompiled with JetBrains decompiler
// Type: Comum.AbreFalha
// Assembly: Comum, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 99DD0288-5D71-4E3E-8396-2C9790D917B3
// Assembly location: C:\Users\janlo\Desktop\G04CNC01\DLL_Acesso_Cancela\DLL_Acesso_Cancela\lib\Comum.dll

using Cancela;
using Dapper;
using System;
using System.Data;
using System.Diagnostics;

namespace Comum
{
  public class AbreFalha : AcessoDados, IDisposable
  {
    private bool disposedValue = false;

        [Obsolete]
        public void AbrirFalha(string v_s_Aplicacao, int v_Id_Equipamento)
    {
      try
      {
        DynamicParameters dynamicParameters = new DynamicParameters();
        dynamicParameters.Add("V_ID_EQUIPAMENTO", (object) v_Id_Equipamento, new DbType?(), new ParameterDirection?(), new int?(), new byte?(), new byte?());
        this.Executar("BANCO", "FALHA.SP_FALHA_INICIO", (object) dynamicParameters, "AbreFalha.AbrirFalha", CommandType.StoredProcedure);
      }
      catch (Exception ex)
      {
        new GravaEventLog().GravarEventLog(v_s_Aplicacao, "Erro AbrirFalha(). ID Controladora: " + v_Id_Equipamento.ToString() + " - " + v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
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

    ~AbreFalha()
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
