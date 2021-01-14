// Decompiled with JetBrains decompiler
// Type: Comum.VerificaAcesso
// Assembly: Comum, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 99DD0288-5D71-4E3E-8396-2C9790D917B3
// Assembly location: C:\Users\janlo\Desktop\G04CNC01\DLL_Acesso_Cancela\DLL_Acesso_Cancela\lib\Comum.dll

using Cancela;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace Comum
{
  public class VerificaAcesso : AcessoDados, IDisposable
  {
    private bool disposedValue = false;

    public string CREDENCIAL { get; set; }

    public string CDACAO { get; set; }

        [Obsolete]
        public bool VerificandoAcesso(
      string v_Credencial,
      string v_Alias_Gate,
      int v_id_equipamento,
      string v_s_Aplicacao)
    {
      try
      {
        DynamicParameters dynamicParameters = new DynamicParameters();
        dynamicParameters.Add("vCredencial", (object) v_Credencial, new DbType?(), new ParameterDirection?(), new int?(), new byte?(), new byte?());
        dynamicParameters.Add("vIdEquipamento", (object) v_id_equipamento, new DbType?(), new ParameterDirection?(), new int?(), new byte?(), new byte?());
        IEnumerable<VerificaAcesso> verificaAcessos = this.Pesquisar<VerificaAcesso>("BANCO", "SP_ACESSO_VERIFICA", "VerificaAcesso.VerificaAcesso", (object) dynamicParameters, CommandType.StoredProcedure, true);
        if (verificaAcessos != null && verificaAcessos != null)
        {
          using (IEnumerator<VerificaAcesso> enumerator = verificaAcessos.GetEnumerator())
          {
            if (enumerator.MoveNext())
            {
              VerificaAcesso current = enumerator.Current;
              if (current.CDACAO == "I")
                return current.CREDENCIAL != null;
              return false;
            }
          }
        }
        return false;
      }
      catch (Exception ex)
      {
        new GravaEventLog().GravarEventLog(v_s_Aplicacao, "Erro VerificaAcesso() ID Controladora: " + v_id_equipamento.ToString() + " - " + v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
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

    ~VerificaAcesso()
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
