// Decompiled with JetBrains decompiler
// Type: Comum.Enviar
// Assembly: Comum, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 99DD0288-5D71-4E3E-8396-2C9790D917B3
// Assembly location: C:\Users\janlo\Desktop\G04CNC01\DLL_Acesso_Cancela\DLL_Acesso_Cancela\lib\Comum.dll

using System;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Comum
{
  public class Enviar : IDisposable
  {
    private static readonly string v_s_Aplicacao = ConfigurationManager.AppSettings["APLICACAO"].ToString();
    private bool disposedValue = false;

    public void IEnviar(
      string v_Comando,
      IPAddress v_IP,
      int v_Porta_Envio,
      string v_s_Aplicacao,
      int v_Id_Equipamento)
    {
      Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      try
      {
        IPEndPoint ipEndPoint = new IPEndPoint(v_IP, v_Porta_Envio);
        byte[] bytes = Encoding.ASCII.GetBytes(v_Comando);
        socket.SendTo(bytes, (EndPoint) ipEndPoint);
      }
      catch (Exception ex)
      {
        new GravaEventLog().GravarEventLog(v_s_Aplicacao, "Erro IEnviar() Serviço Controladora: " + v_Id_Equipamento.ToString() + " - " + v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
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

    ~Enviar()
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
