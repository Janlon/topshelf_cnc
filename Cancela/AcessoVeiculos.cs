using Comum;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace Cancela
{
    public class AcessoVeiculos : ServiceBase
    {
        private int v_Id_Equipamento = int.Parse(ConfigurationManager.AppSettings["ID_EQUIPAMENTO"].ToString());
        private string v_s_Aplicacao = ConfigurationManager.AppSettings["APLICACAO"].ToString();
        private Logar myLogar = new Logar();
        private bool done = false;
        private bool done_ping = false;
        private IContainer components = (IContainer)null;
        private int v_Porta_Escuta;
        private int v_Porta_Envio;
        private IPAddress v_IP;
        private Thread v_t_Receber;
        private Thread v_t_AtualizaLista;
        private Thread v_t_TestaPing;

        public AcessoVeiculos()
        {
            new GravaEventLog().GravarEventLog(this.v_s_Aplicacao, "Ok", EventLogEntryType.Information, (Exception)null);
            Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(int.Parse(ConfigurationManager.AppSettings["CPU"].ToString()));
           // this.InitializeComponent();
        }

        //protected override void OnStart(string[] args)
        //{
        //    this.Inicio();
        //}

        public void Inicio()
        {
            try
            {
                new CriaChaveRegistroWindows().CriarChaveRegistroWindows(this.v_s_Aplicacao);
                new GravaEventLog().GravarEventLog(this.v_s_Aplicacao, "Serviço Controladora: " + this.v_Id_Equipamento.ToString() + " - Foi Iniciado", EventLogEntryType.Information, (Exception)null);
                Base.ExecutaTestePing = true;
                PegaControladora pegaControladora = new PegaControladora().PegarControladora(this.v_Id_Equipamento, this.v_s_Aplicacao);
                this.v_IP = IPAddress.Parse(pegaControladora.Ip);
                this.v_Porta_Envio = int.Parse(pegaControladora.NrPorta);
                this.v_Porta_Escuta = int.Parse(pegaControladora.NrPortaSaida);
                pegaControladora.Terminate();
                if ((uint)this.v_Porta_Escuta > 0U)
                {
                    if (this.v_t_Receber != null)
                    {
                        if (this.v_t_Receber.ThreadState == System.Threading.ThreadState.Stopped || this.v_t_Receber.ThreadState == System.Threading.ThreadState.Aborted || this.v_t_Receber.ThreadState == System.Threading.ThreadState.AbortRequested)
                        {
                            this.v_t_Receber = new Thread(new ThreadStart(this.Receber));
                            this.v_t_Receber.Start();
                        }
                    }
                    else
                    {
                        this.v_t_Receber = new Thread(new ThreadStart(this.Receber));
                        this.v_t_Receber.Start();
                    }
                    if (this.v_t_AtualizaLista != null)
                    {
                        if (this.v_t_AtualizaLista.ThreadState == System.Threading.ThreadState.Stopped || this.v_t_AtualizaLista.ThreadState == System.Threading.ThreadState.Aborted || this.v_t_AtualizaLista.ThreadState == System.Threading.ThreadState.AbortRequested)
                        {
                            this.v_t_AtualizaLista = new Thread(new ThreadStart(this.AtualizaLista));
                            this.v_t_AtualizaLista.Start();
                        }
                    }
                    else
                    {
                        this.v_t_AtualizaLista = new Thread(new ThreadStart(this.AtualizaLista));
                        this.v_t_AtualizaLista.Start();
                    }
                    if (this.v_t_TestaPing != null)
                    {
                        if (this.v_t_TestaPing.ThreadState == System.Threading.ThreadState.Stopped || this.v_t_TestaPing.ThreadState == System.Threading.ThreadState.Aborted || this.v_t_TestaPing.ThreadState == System.Threading.ThreadState.AbortRequested)
                        {
                            this.v_t_TestaPing = new Thread(new ThreadStart(this.TestePing));
                            this.v_t_TestaPing.Start();
                        }
                    }
                    else
                    {
                        this.v_t_TestaPing = new Thread(new ThreadStart(this.TestePing));
                        this.v_t_TestaPing.Start();
                    }
                    new FechaFalha().FecharFalha(this.v_s_Aplicacao, this.v_Id_Equipamento);
                }
                else
                    new GravaEventLog().GravarEventLog(this.v_s_Aplicacao, "Erro OnStart ID Controladora: " + this.v_Id_Equipamento.ToString() + " - Aplicação não carregou a porta que ela deve escutar, verifique o ID do equipamento e coloque o ID correto no appconfig", EventLogEntryType.Error, (Exception)null);
                new InicializaControladora().Inicializa_Controladora(this.v_IP, this.v_Porta_Envio, this.v_s_Aplicacao, this.v_Id_Equipamento);
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(this.v_s_Aplicacao, "Erro Inicio() Serviço Controladora: " + this.v_Id_Equipamento.ToString() + " - " + ex.Message, EventLogEntryType.Error, ex);
            }
        }

        protected override void OnStop()
        {
            new GravaEventLog().GravarEventLog(this.v_s_Aplicacao, "Serviço da Controladora: " + this.v_Id_Equipamento.ToString() + " - " + this.v_s_Aplicacao + " - Foi Encerrado", EventLogEntryType.Warning, (Exception)null);
            this.done = true;
            Thread.Sleep(3000);
            this.v_t_Receber.Abort();
            this.v_t_AtualizaLista.Abort();
            this.v_t_TestaPing.Abort();
            this.Stop();
        }

        public void Receber()
        {
            Console.WriteLine("PORTA", v_Porta_Escuta);
            Console.WriteLine("IP", v_IP);

            UdpClient udpClient = new UdpClient(this.v_Porta_Escuta);
            IPEndPoint remoteEP = new IPEndPoint(this.v_IP, this.v_Porta_Escuta);
            Regras regras = new Regras();
            try
            {
                while (!this.done)
                {
                    try
                    {
                        byte[] bytes = udpClient.Receive(ref remoteEP);
                        string ip = remoteEP.Address.ToString();
                        string v_Comando = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                        if (new PegaConfiguracaoServico().PegarConfiguracaoServico(this.v_s_Aplicacao, ip, this.v_Id_Equipamento).ID_EQUIPAMENTO_TIPO != 0 || v_Comando.Substring(1, 3) == "NTR")
                            regras.IRegras_MultiIO(v_Comando, this.v_IP, this.v_Porta_Envio);
                        else
                            regras.IRegras(v_Comando, this.v_IP, this.v_Porta_Envio);
                    }
                    catch (Exception ex)
                    {
                        new GravaEventLog().GravarEventLog(this.v_s_Aplicacao, "Erro Receber() - Serviço Controladora: " + this.v_Id_Equipamento.ToString() + " - " + ex.Message, EventLogEntryType.Error, ex);
                        if (this.v_t_Receber.ThreadState == System.Threading.ThreadState.Stopped || this.v_t_Receber.ThreadState == System.Threading.ThreadState.Aborted || this.v_t_Receber.ThreadState == System.Threading.ThreadState.AbortRequested)
                        {
                            this.v_t_Receber = new Thread(new ThreadStart(this.Receber));
                            this.v_t_Receber.Start();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(this.v_s_Aplicacao, "Erro Receber() - Serviço Controladora: " + this.v_Id_Equipamento.ToString() + " - " + ex.Message, EventLogEntryType.Error, ex);
            }
            finally
            {
                udpClient.Close();
            }
        }

        public void AtualizaLista()
        {
            try
            {
                Thread.Sleep(20000);
                while (!this.done)
                {
                    if (Base.ContaTestePing == 0)
                        new AtualizaListaControladora().AtualizarListaControladora(this.v_s_Aplicacao, this.v_Id_Equipamento, this.v_IP, this.v_Porta_Envio);
                    Thread.Sleep(60000);
                }
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(this.v_s_Aplicacao, "Erro AtualizaLista() - Serviço Controladora: " + this.v_Id_Equipamento.ToString() + " - " + ex.Message, EventLogEntryType.Error, ex);
            }
        }

        public void TestePing()
        {
            try
            {
                Base.ContaTestePing = 0;
                DateTime now = DateTime.Now;
                int num = 1;
                while (!this.done_ping)
                {
                    try
                    {
                        Thread.Sleep(5000);
                        if ((now - DateTime.Now).Hours == 1)
                        {
                            now = DateTime.Now;
                            new AjustaDataHora().AjustarDataHora(this.v_s_Aplicacao, this.v_Id_Equipamento, this.v_IP, this.v_Porta_Envio);
                        }
                        if (Base.ExecutaTestePing)
                        {
                            string[] strArray = new TestePing().TestarPing(this.v_IP, this.v_Porta_Envio, this.v_s_Aplicacao, this.v_Id_Equipamento).Split('|');
                            if (strArray[0] == "NOKP" || strArray[1] == "NOKC" || strArray[1] == "NOKE")
                            {
                                if (Base.ContaTestePing == 12)
                                {
                                    new AbreFalha().AbrirFalha(this.v_s_Aplicacao, this.v_Id_Equipamento);
                                    new GravaEventLog().GravarEventLog(this.v_s_Aplicacao, "FALHA DE COMUNICAÇÃO 4IP, Teste Ping = " + strArray[0] + " - Teste Retorno de Comando = " + strArray[1] + " - Serviço Controladora: " + this.v_Id_Equipamento.ToString() + " - " + this.v_s_Aplicacao, EventLogEntryType.Error, (Exception)null);
                                }
                                ++Base.ContaTestePing;
                            }
                            else
                            {
                                if (Base.ContaTestePing > 6)
                                {
                                    new InicializaControladora().Inicializa_Controladora(this.v_IP, this.v_Porta_Envio, this.v_s_Aplicacao, this.v_Id_Equipamento);
                                    new Enviar().IEnviar("$RESET#", this.v_IP, this.v_Porta_Envio, this.v_s_Aplicacao, this.v_Id_Equipamento);
                                    new GravaEventLog().GravarEventLog(this.v_s_Aplicacao, "Comunicação restabelecida - 4IP OK - Serviço Controladora: " + this.v_Id_Equipamento.ToString() + " - " + this.v_s_Aplicacao, EventLogEntryType.Information, (Exception)null);
                                }
                                if (Base.ContaTestePing > 12)
                                    new FechaFalha().FecharFalha(this.v_s_Aplicacao, this.v_Id_Equipamento);
                                Base.ContaTestePing = 0;
                                num = 1;
                            }
                        }
                        else
                        {
                            ++num;
                            if (num > 25)
                            {
                                new InicializaControladora().Inicializa_Controladora(this.v_IP, this.v_Porta_Envio, this.v_s_Aplicacao, this.v_Id_Equipamento);
                                new Enviar().IEnviar("$RESET#", this.v_IP, this.v_Porta_Envio, this.v_s_Aplicacao, this.v_Id_Equipamento);
                                new GravaEventLog().GravarEventLog(this.v_s_Aplicacao, "Teste de Ping Parado, tempo parado excedido, seção encerrada e teste reiniciado  - Serviço Controladora: " + this.v_Id_Equipamento.ToString() + " - " + this.v_s_Aplicacao, EventLogEntryType.Warning, (Exception)null);
                                num = 1;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        new GravaEventLog().GravarEventLog(this.v_s_Aplicacao, "Erro TestePing() - Serviço Controladora: " + this.v_Id_Equipamento.ToString() + " - " + this.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, (Exception)null);
                    }
                }
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(this.v_s_Aplicacao, "Erro TestePing() - Serviço Controladora: " + this.v_Id_Equipamento.ToString() + " - " + this.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, (Exception)null);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
                this.components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = (IContainer)new Container();
            this.ServiceName = ConfigurationManager.AppSettings["APLICACAO"].ToString();
        }
    }
}
