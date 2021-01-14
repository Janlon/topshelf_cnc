﻿// Decompiled with JetBrains decompiler
// Type: Comum.VerificaAcessoDiretoSphera
// Assembly: Comum, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 99DD0288-5D71-4E3E-8396-2C9790D917B3
// Assembly location: C:\Users\janlo\Desktop\G04CNC01\DLL_Acesso_Cancela\DLL_Acesso_Cancela\lib\Comum.dll

using Cancela;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Diagnostics;
using System.Linq;

namespace Comum
{
  public class VerificaAcessoDiretoSphera : AcessoDados, IDisposable
  {
    private bool disposedValue = false;

    public string ID_CREDENCIAL_PESSOA { get; set; }

        [Obsolete]
        public bool VerificarAcessoDiretoSphera(
      string v_Credencial_Pessoa,
      string v_Id_Equipamento,
      string v_s_Aplicacao,
      string v_IP_Controladora)
    {
      try
      {
        OracleDynamicParameters dynamicParameters = new OracleDynamicParameters();
        dynamicParameters.Add("V_CREDENCIAL", (object) v_Credencial_Pessoa, new OracleType?(), new ParameterDirection?(), new int?());
        dynamicParameters.Add("V_ID_EQUIPAMENTO", (object) v_Id_Equipamento, new OracleType?(), new ParameterDirection?(), new int?());
        dynamicParameters.Add("V_INTEGRA_SPHERA", (object) null, new OracleType?(OracleType.Cursor), new ParameterDirection?(ParameterDirection.Output), new int?());
        IEnumerable<VerificaAcessoDiretoSphera> source = this.Pesquisar<VerificaAcessoDiretoSphera>("BANCO", "INTEGRA_SPHERA.SP_INTEGRA_SPHERA", "VerificaAcessoDiretoSphera.VerificarAcessoDiretoSphera()", (object) dynamicParameters, CommandType.StoredProcedure, true);
        if (source != null && source.Count<VerificaAcessoDiretoSphera>() > 0)
        {
          using (IEnumerator<VerificaAcessoDiretoSphera> enumerator = source.GetEnumerator())
          {
            if (enumerator.MoveNext())
            {
              VerificaAcessoDiretoSphera current = enumerator.Current;
              return true;
            }
          }
        }
        return false;
      }
      catch (Exception ex)
      {
        new GravaEventLog().GravarEventLog(v_s_Aplicacao, "Erro VerificarAcessoDiretoSphera(). Serviço Controladora: " + v_Id_Equipamento.ToString() + " - " + v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
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

    ~VerificaAcessoDiretoSphera()
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
