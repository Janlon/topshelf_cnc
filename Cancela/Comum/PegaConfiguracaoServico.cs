// Decompiled with JetBrains decompiler
// Type: Comum.PegaConfiguracaoServico
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
  public class PegaConfiguracaoServico : AcessoDados, IDisposable
  {
    private bool disposedValue = false;

    public int ID_EQUIPAMENTO_TIPO { get; set; }

        [Obsolete]
        public PegaConfiguracaoServico PegarConfiguracaoServico(
      string v_s_Aplicacao,
      string ip,
      int v_Id_Equipamento)
    {
      PegaConfiguracaoServico configuracaoServico1 = new PegaConfiguracaoServico();
      try
      {
        DynamicParameters dynamicParameters = new DynamicParameters();
        dynamicParameters.Add("vip", (object) ip, new DbType?(), new ParameterDirection?(), new int?(), new byte?(), new byte?());
        IEnumerable<PegaConfiguracaoServico> configuracaoServicos = this.Pesquisar<PegaConfiguracaoServico>("BANCO", "SP_EQUIP_CONFIG_UDPRECEIVE", "PegaConfiguracaoServico.PegaConfiguracaoServico", (object) dynamicParameters, CommandType.StoredProcedure, true);
        if (configuracaoServicos == null)
          return (PegaConfiguracaoServico) null;
        foreach (PegaConfiguracaoServico configuracaoServico2 in configuracaoServicos)
          configuracaoServico1.ID_EQUIPAMENTO_TIPO = configuracaoServico2.ID_EQUIPAMENTO_TIPO;
        return configuracaoServico1;
      }
      catch (Exception ex)
      {
        new GravaEventLog().GravarEventLog(v_s_Aplicacao, "Erro PegarConfiguracaoServico(). Controladora: " + v_Id_Equipamento.ToString() + " - " + v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
        return (PegaConfiguracaoServico) null;
      }
      finally
      {
        configuracaoServico1.Terminate();
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

    ~PegaConfiguracaoServico()
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
