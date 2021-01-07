using Comum;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Cancela
{
    public class Regras : Base
    {
        private static readonly string v_s_Aplicacao = ConfigurationManager.AppSettings["APLICACAO"].ToString();
        private static readonly int v_Id_Equipamento = int.Parse(ConfigurationManager.AppSettings["ID_EQUIPAMENTO"].ToString());
        private static readonly string v_s_Sentido_Controladora = ConfigurationManager.AppSettings["SENTIDO_ACESSO"].ToString();
        private static readonly string v_Alias_Gate = ConfigurationManager.AppSettings["GATE"].ToString();
        private static readonly int v_TimeOut = int.Parse(ConfigurationManager.AppSettings["TIMEOUT"].ToString());
        private static readonly string v_WebAPI_LBF = ConfigurationManager.AppSettings["WEBAPI_LBF"].ToString();
        private static readonly string v_WebAPI_LAP = ConfigurationManager.AppSettings["WEBAPI_LAP"].ToString();
        private static readonly int v_Fator = int.Parse(ConfigurationManager.AppSettings["FATOR"].ToString());
        public bool v_DoneLBF = false;
        public bool v_DoneDisparaJurar = false;
        public bool v_DoneDisparaJurarSaida = false;
        public bool v_BuscaResultadoLAP = false;
        public bool v_BuscaResultadoLBF = false;
        public bool v_b_Disparo_LAP = false;
        public bool v_b_Disparo_LBF = false;
        public bool v_b_Timeout = false;
        public bool v_b_DuploAcesso = false;
        public bool v_b_Timeout_Acionado = false;
        public bool v_b_Encerrado_LAP = false;
        public bool v_b_Encerrado_LBF = false;
        public bool v_b_VCO_Veiculo = false;
        public bool v_b_VCO_Pessoa = false;
        public bool v_b_VCO_Veiculo_Saida = false;
        public bool v_b_VCO_Pessoa_Saida = false;
        public bool v_b_Jugar = false;
        public bool v_b_Jugar_Saida = false;
        public bool v_b_guarda = false;
        public bool v_b_Acesso_Especial = false;
        public bool v_b_Nivel_99 = false;
        public string v_s_Sentido = "";
        public string v_s_VCO_Veiculo = "";
        public string v_s_VCO_Pessoa = "";
        public string v_Secao = "";
        public string v_Placa = "";
        public string v_PlacaLaco = "";
        public string v_IdMotivacao = "";
        public string v_Credencial_Veiculo = "";
        public string v_Credencial_Pessoa = "";
        public string v_Credencial = "";
        public float v_i_Score_LAP = 0.0f;
        public float v_i_Score_LBF = 0.0f;
        public int v_Liberado_LAP = 0;
        public int v_Liberado_LBF = 0;
        public float v_Id_Requisicao_Problema_LBF = 0.0f;
        public float v_Id_Requisicao_Problema_LAP = 0.0f;
        public int piorTemplate = 0;
        public string v_s_Trata_M = "";
        public int v_i_Trata_M = 1;
        public int FlagMotorista = 0;
        public Thread v_t_lap;
        public Thread v_t_lbf;
        public Thread t_Dispara_Jugar;
        public Thread t_Dispara_Jugar_Saida;
        public Thread t_JugarEntrada;
        public Thread t_JugarSaida;
        public Thread t_LAP;
        public Thread t_LBF;
        public Thread v_t_LIU;
        public int LiuOk;
        public Thread t_Timeout;
        public Thread t_TimeoutErroAplicacaoGuarda;
        private float v_i_Nota_Corte_LAP;
        private float v_i_Nota_Corte_LBF;
        public IPAddress v_s_IP;
        public int v_s_Porta_Envio;
        public bool ignora;

        public string CD_EXECUTA_LBF { get; set; }

        public long ID_SECAO { get; set; }

        public string CD_CREDENCIAL { get; set; }

        public int VL_TEMPO_EXECUCAO { get; set; }

        public int VL_SCORE { get; set; }

        public byte[] IMAGEM { get; set; }

        public void TimeOut()
        {
            try
            {
                string vSecao = this.v_Secao;
                int num = 0;
                while (this.v_Secao != "" && this.v_Secao != null)
                {
                    if (num == Regras.v_TimeOut)
                    {
                        new Enviar().IEnviar("$DSMTIME-OUT        ACESSO NEGADO   006#", this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                        this.v_b_Timeout_Acionado = true;
                        this.EncerraSecao(this.v_s_IP.ToString(), "SECAO ENCERRADA POR TIME-OUT", this.v_s_Porta_Envio, this.v_s_IP);
                        break;
                    }
                    if ((this.v_Credencial_Veiculo == "" || this.v_Credencial_Pessoa == "") && vSecao == this.v_Secao)
                    {
                        ++num;
                        Thread.Sleep(1000);
                    }
                    else
                        break;
                }
                this.v_b_Timeout = false;
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro TimeOut() ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
            }
        }

        public async void IRegras_MultiIO(string v_Comando, IPAddress v_IP, int v_Porta_Envio)
        {
            Console.WriteLine("IRegras_MultiIO");
            try
            {
                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", v_Comando, (string)null, "E", nameof(IRegras_MultiIO));
                this.v_s_IP = v_IP;
                this.v_s_Porta_Envio = v_Porta_Envio;
                this.v_PlacaLaco = "";
                string StatusMessage = "";
                string[] v_ComandoCompleto = v_Comando.Split('@');
                if (((IEnumerable<string>)v_ComandoCompleto).Count<string>() > 2)
                {
                    this.v_PlacaLaco = v_Comando.Split('@')[2].ToString().Replace("#", "");
                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "PLACA RECEBIDA: " + this.v_PlacaLaco, (string)null, "E", nameof(IRegras_MultiIO));
                }
                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", v_Comando, (string)null, "E", nameof(IRegras_MultiIO));
                v_Comando = "@" + v_Comando.Split('@')[1].ToString();
                string v_ComandoSaida;
                if (v_Comando.Substring(1, 3) == "NTR" && !this.v_b_Disparo_LAP)
                {
                    if (this.v_Secao == "" || this.v_Secao == null)
                    {
                        this.v_Secao = new NovaSecao().Secao(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                        if (!this.v_b_Timeout)
                        {
                            this.t_Timeout = new Thread(new ThreadStart(this.TimeOut));
                            this.t_Timeout.Start();
                        }
                    }
                    if (ConfigurationManager.AppSettings["CONTROLE_VAGAS"] == "1")
                    {
                        VerificaVagas vVerificaVagas = new VerificaVagas().VerificarVagas(Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, Convert.ToInt32(ConfigurationManager.AppSettings["GATE"].Replace("G", "")));
                        if (vVerificaVagas.FLAG_OK == 1)
                        {
                            v_ComandoSaida = "$DSMACESSO NEGADO   PATIO LOTADO     8#";
                            new Enviar().IEnviar(v_ComandoSaida, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 34, this.v_Credencial, v_ComandoSaida, (string)null, "L", "LAP");
                            new GravaAcesso().GravarAcesso("00000000", Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "31", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                            this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                            goto label_50;
                        }
                        else
                        {
                            vVerificaVagas = (VerificaVagas)null;
                            vVerificaVagas = (VerificaVagas)null;
                        }
                    }
                    if (this.v_PlacaLaco == "")
                    {
                        Console.WriteLine("v_PlacaLaco", v_PlacaLaco);

                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "LAÇO", (string)null, "E", nameof(IRegras_MultiIO));
                        this.v_s_Sentido = ConfigurationManager.AppSettings["SENTIDO_ACESSO"].ToString();
                        Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["TEMPO_ESPERA_LAP"]));
                        v_ComandoSaida = "$DSMCAPTURANDO      PLACA . . .     005#";
                        new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", v_ComandoSaida, (string)null, "E", nameof(IRegras_MultiIO));
                        this.v_IdMotivacao = "";
                        double v_IdOcr = 0.0;
                        HttpClient client = new HttpClient();
                        try
                        {
                            string v_LAPRequisicao = "{\"ambiente\":\"" + ConfigurationManager.AppSettings["AMBIENTE"] + "\",\"local\":\"" + Regras.v_s_Aplicacao + "\",\"pesagem\":\"\",\"idsecao\":\"" + this.v_Secao + "\"}";
                            string uri = ConfigurationManager.AppSettings["WEBAPI_OCR"].ToString();
                            string data = v_LAPRequisicao;
                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", uri + "-" + v_LAPRequisicao, (string)null, "E", nameof(IRegras_MultiIO));
                            StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
                            HttpResponseMessage response = (HttpResponseMessage)null;
                            response = await client.PostAsync(uri, (HttpContent)content);
                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "response:" + response.IsSuccessStatusCode.ToString(), (string)null, "E", nameof(IRegras_MultiIO));
                            if (response.IsSuccessStatusCode)
                            {
                                JToken token = (JToken)JObject.Parse(response.Content.ReadAsStringAsync().Result);
                                object resultado = (object)token.SelectToken("Result");
                                Regras.retOcr retoc = new Regras.retOcr();
                                retoc = JsonConvert.DeserializeObject<Regras.retOcr>(resultado.ToString());
                                this.v_PlacaLaco = retoc.Placa;
                                v_IdOcr = (double)retoc.IdOcr;
                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "placa:" + this.v_PlacaLaco, (string)null, "E", nameof(IRegras_MultiIO));
                                token = (JToken)null;
                                resultado = (object)null;
                                retoc = (Regras.retOcr)null;
                                token = (JToken)null;
                                resultado = (object)null;
                                retoc = (Regras.retOcr)null;
                            }
                            v_LAPRequisicao = (string)null;
                            uri = (string)null;
                            data = (string)null;
                            content = (StringContent)null;
                            response = (HttpResponseMessage)null;
                            v_LAPRequisicao = (string)null;
                            uri = (string)null;
                            data = (string)null;
                            content = (StringContent)null;
                            response = (HttpResponseMessage)null;
                        }
                        catch (Exception ex1)
                        {
                            Exception ex = ex1;
                            this.v_PlacaLaco = "";
                            ex = (Exception)null;
                        }
                        client = (HttpClient)null;
                        client = (HttpClient)null;
                    }
                    if (this.v_PlacaLaco == "")
                    {
                        Console.WriteLine("v_PlacaLaco 2", v_PlacaLaco);

                        Thread.Sleep(2000);
                        v_ComandoSaida = "$DSMPLACA NAO       RECONHECIDA     010#";
                        new Enviar().IEnviar(v_ComandoSaida, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 34, this.v_Credencial, v_ComandoSaida, (string)null, "E", "LAP");
                        new GravaAcesso().GravarAcesso("00000000", Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "820", "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                        if (Regras.v_s_Aplicacao != "G07CNC01")
                            new InserirOcorrenciaGuarda().GravarOcorrencia("0", "0", "", "", this.v_PlacaLaco, this.v_Secao, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, this.v_s_Sentido, "909", "0");
                        this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                    }
                    else
                    {
                        Console.WriteLine("v_PlacaLaco n ta vazio", v_PlacaLaco);

                        LocalizaVeiculo vLocalizaVeiculo = new LocalizaVeiculo().LocalizarVeiculo(this.v_PlacaLaco, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                        if (vLocalizaVeiculo != null)
                        {
                            Thread.Sleep(2000);
                            v_ComandoSaida = "$DSMAPRESENTE       CREDENCIAL . . .060#";
                            new Enviar().IEnviar(v_ComandoSaida, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 34, this.v_Credencial, v_ComandoSaida, (string)null, "E", "LAP");
                            goto label_50;
                        }
                        else
                        {
                            long id;
                            if (Regras.v_s_Aplicacao == "G03CNC01")
                            {
                                ValidaUltimoAcesso vLocalizaUltimoAcesso = new ValidaUltimoAcesso().ValidarUltimoAcesso(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento, this.v_PlacaLaco);
                                if (vLocalizaUltimoAcesso.RET == null)
                                {
                                    Thread.Sleep(2000);
                                    v_ComandoSaida = "$DSMACESSO NEGADO!                  10#";
                                    new Enviar().IEnviar(v_ComandoSaida, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 34, this.v_Credencial, v_ComandoSaida, (string)null, "E", "LAP");
                                    new GravaAcesso().GravarAcesso("00000000", Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "30", "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", this.v_PlacaLaco, "Ultrapassou o limite de 30 minutos");
                                    new InserirOcorrenciaGuarda().GravarOcorrencia("0", "0", "", "", this.v_PlacaLaco, this.v_Secao, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, this.v_s_Sentido, "30", "0");
                                    this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                                }
                                else
                                {
                                    Thread.Sleep(2000);
                                    GravaAcesso gravaAcesso = new GravaAcesso();
                                    string credencial = vLocalizaUltimoAcesso.CREDENCIAL;
                                    int vIdEquipamento = Regras.v_Id_Equipamento;
                                    string vSSentido = this.v_s_Sentido;
                                    string vSAplicacao = Regras.v_s_Aplicacao;
                                    long V_ID_SECAO = long.Parse(this.v_Secao);
                                    string vl_Score = (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000";
                                    int vFator = Regras.v_Fator;
                                    id = vLocalizaUltimoAcesso.ID;
                                    string vIdMotivacao = id.ToString();
                                    gravaAcesso.GravarAcesso(credencial, vIdEquipamento, vSSentido, vSAplicacao, V_ID_SECAO, "P", "81", "N", vl_Score, vFator, vIdMotivacao, "0", "", "", "");
                                    new InserirDuploAcesso().Gravar(this.v_Credencial_Pessoa, Regras.v_s_Aplicacao, "E", Regras.v_Id_Equipamento);
                                    v_Comando = "$DSMBEM VINDO!      ACESSO LIBERADO 010#";
                                    new Enviar().IEnviar(v_Comando, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                    Thread.Sleep(5000);
                                    this.Dispara_Liu();
                                    this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                                    gravaAcesso = (GravaAcesso)null;
                                    credencial = (string)null;
                                    vSSentido = (string)null;
                                    vSAplicacao = (string)null;
                                    vl_Score = (string)null;
                                    vIdMotivacao = (string)null;
                                }
                                vLocalizaUltimoAcesso = (ValidaUltimoAcesso)null;
                                vLocalizaUltimoAcesso = (ValidaUltimoAcesso)null;
                            }
                            if (Regras.v_s_Aplicacao == "G07CNC01")
                            {
                                Regras.retValidaPatioHeader.retValidaPatio retPatio = new Regras.retValidaPatioHeader.retValidaPatio();
                                string vplacapatio = "";
                                string vNumeroOs = "";
                                HttpClient cliente = new HttpClient();
                                try
                                {
                                    string v_Requisicao = "?placa=" + this.v_PlacaLaco + "&sessao=" + this.v_Secao;
                                    string uri = ConfigurationManager.AppSettings["WEBAPI_VALIDA_PATIO"].ToString() + v_Requisicao;
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", uri, (string)null, "E", nameof(IRegras_MultiIO));
                                    StringContent content = new StringContent("", Encoding.UTF8, "application/json");
                                    HttpResponseMessage response = (HttpResponseMessage)null;
                                    response = await cliente.PostAsync(uri, (HttpContent)content);
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "response:" + response.IsSuccessStatusCode.ToString(), (string)null, "E", nameof(IRegras_MultiIO));
                                    if (response.IsSuccessStatusCode)
                                    {
                                        JToken token = (JToken)JObject.Parse(response.Content.ReadAsStringAsync().Result);
                                        Regras.retMotivar retMotiv = new Regras.retMotivar();
                                        retMotiv = JsonConvert.DeserializeObject<Regras.retMotivar>(token.ToString());
                                        StatusMessage = retMotiv.StatusMessage;
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", response.Content.ReadAsStringAsync().Result, (string)null, "E", nameof(IRegras_MultiIO));
                                        object resultado = (object)token.SelectToken("result");
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", resultado.ToString(), (string)null, "E", nameof(IRegras_MultiIO));
                                        retPatio = JsonConvert.DeserializeObject<Regras.retValidaPatioHeader.retValidaPatio>(resultado.ToString());
                                        vplacapatio = retPatio.placaCavalo;
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, vplacapatio, (string)null, "E", "JugarEntrada");
                                        vNumeroOs = retPatio.numOs;
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, vNumeroOs, (string)null, "E", "JugarEntrada");
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "placa:" + vplacapatio, (string)null, "E", nameof(IRegras_MultiIO));
                                        token = (JToken)null;
                                        retMotiv = (Regras.retMotivar)null;
                                        resultado = (object)null;
                                        token = (JToken)null;
                                        retMotiv = (Regras.retMotivar)null;
                                        resultado = (object)null;
                                    }
                                    v_Requisicao = (string)null;
                                    uri = (string)null;
                                    content = (StringContent)null;
                                    response = (HttpResponseMessage)null;
                                    v_Requisicao = (string)null;
                                    uri = (string)null;
                                    content = (StringContent)null;
                                    response = (HttpResponseMessage)null;
                                }
                                catch (Exception ex)
                                {
                                    vplacapatio = "";
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", ex.Message, (string)null, "E", nameof(IRegras_MultiIO));
                                }
                                if (vplacapatio != "")
                                {
                                    new GravaAcesso().GravarAcesso("00000000", Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "81", "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, "0", "0", vNumeroOs, vplacapatio, "");
                                    new InserirDuploAcesso().Gravar(this.v_Credencial_Pessoa, Regras.v_s_Aplicacao, "E", Regras.v_Id_Equipamento);
                                    v_Comando = "$DSMBEM VINDO!      ACESSO LIBERADO 010#";
                                    new Enviar().IEnviar(v_Comando, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, v_Comando, (string)null, "E", "JugarEntrada");
                                    Thread.Sleep(2000);
                                    this.Dispara_Liu();
                                    if (ConfigurationManager.AppSettings["CONTROLE_VAGAS"] == "1")
                                    {
                                        new GravaAcessoPatio().GravarAcessoPatio("E", 1, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, retPatio.numOs, 0L, retPatio.placaCavalo, retPatio.nomeMotorista, retPatio.dtEmissao.ToString(), retPatio.placaCarreta1, retPatio.placaCarreta2, retPatio.cnpjCliente, retPatio.cnpjTransportadora);
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, "Gravando Acesso Patio", (string)null, "E", "JugarEntrada");
                                    }
                                    this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                                }
                                else
                                {
                                    v_ComandoSaida = "$DSMACESSO NEGADO!                  10#";
                                    new Enviar().IEnviar(v_ComandoSaida, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 34, this.v_Credencial, v_ComandoSaida, (string)null, "E", "LAP");
                                    new GravaAcesso().GravarAcesso("00000000", Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "30", "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, "0", "0", "", this.v_PlacaLaco, StatusMessage);
                                    this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                                    this.v_PlacaLaco = "";
                                }
                                retPatio = (Regras.retValidaPatioHeader.retValidaPatio)null;
                                vplacapatio = (string)null;
                                vNumeroOs = (string)null;
                                cliente = (HttpClient)null;
                                retPatio = (Regras.retValidaPatioHeader.retValidaPatio)null;
                                vplacapatio = (string)null;
                                vNumeroOs = (string)null;
                                cliente = (HttpClient)null;
                            }
                            else
                            {
                                ValidaUltimoAcesso vLocalizaUltimoAcesso = new ValidaUltimoAcesso().ValidarUltimoAcesso(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento, this.v_PlacaLaco);
                                if (vLocalizaUltimoAcesso.CREDENCIAL == null)
                                {
                                    Thread.Sleep(2000);
                                    v_ComandoSaida = "$DSMACESSO NEGADO!                  10#";
                                    new Enviar().IEnviar(v_ComandoSaida, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 34, this.v_Credencial, v_ComandoSaida, (string)null, "E", "LAP");
                                    new GravaAcesso().GravarAcesso("00000000", Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "30", "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                                    new InserirOcorrenciaGuarda().GravarOcorrencia("0", "0", "", "", this.v_PlacaLaco, this.v_Secao, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, this.v_s_Sentido, "30", "0");
                                    this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                                }
                                else
                                {
                                    Thread.Sleep(2000);
                                    GravaAcesso gravaAcesso = new GravaAcesso();
                                    string credencial = vLocalizaUltimoAcesso.CREDENCIAL;
                                    int vIdEquipamento = Regras.v_Id_Equipamento;
                                    string vSSentido = this.v_s_Sentido;
                                    string vSAplicacao = Regras.v_s_Aplicacao;
                                    long V_ID_SECAO = long.Parse(this.v_Secao);
                                    string vl_Score = (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000";
                                    int vFator = Regras.v_Fator;
                                    id = vLocalizaUltimoAcesso.ID;
                                    string vIdMotivacao = id.ToString();
                                    gravaAcesso.GravarAcesso(credencial, vIdEquipamento, vSSentido, vSAplicacao, V_ID_SECAO, "P", "81", "N", vl_Score, vFator, vIdMotivacao, "0", "", "", "");
                                    new InserirDuploAcesso().Gravar(this.v_Credencial_Pessoa, Regras.v_s_Aplicacao, "E", Regras.v_Id_Equipamento);
                                    v_Comando = "$DSMBEM VINDO!      ACESSO LIBERADO 010#";
                                    new Enviar().IEnviar(v_Comando, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                    Thread.Sleep(5000);
                                    this.Dispara_Liu();
                                    this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                                    gravaAcesso = (GravaAcesso)null;
                                    credencial = (string)null;
                                    vSSentido = (string)null;
                                    vSAplicacao = (string)null;
                                    vl_Score = (string)null;
                                    vIdMotivacao = (string)null;
                                }
                                vLocalizaUltimoAcesso = (ValidaUltimoAcesso)null;
                                vLocalizaUltimoAcesso = (ValidaUltimoAcesso)null;
                            }
                            vLocalizaVeiculo = (LocalizaVeiculo)null;
                            vLocalizaVeiculo = (LocalizaVeiculo)null;
                        }
                    }
                }
            label_50:
                StatusMessage = (string)null;
                v_ComandoCompleto = (string[])null;
                v_ComandoSaida = (string)null;
                StatusMessage = (string)null;
                v_ComandoCompleto = (string[])null;
                v_ComandoSaida = (string)null;
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro IRegras_MultiIO() ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
            }
        }

        public async void IRegras(string v_Comando, IPAddress v_IP, int v_Porta_Envio)
        {
            try
            {
                string v_idManobra = "0";
                string v_ComandoSaida = "";
                this.v_s_IP = v_IP;
                this.v_s_Porta_Envio = v_Porta_Envio;
                string[] v_ComandoCompleto = v_Comando.Split('@');
                if (v_Comando.Substring(0, 4) == "@SLC" && ((IEnumerable<string>)v_ComandoCompleto).Count<string>() > 1)
                    v_idManobra = v_Comando.Split('@')[2].ToString().Replace("#", "");
                v_Comando = "@" + v_Comando.Split('@')[1].ToString();
                if (v_Comando == "@SOK#")
                    v_Comando += " ";
                if (v_Comando.Length >= 5)
                {
                    if (v_Comando.Length == 14 && (v_Comando.Substring(0, 3) == "@LI" && v_Comando.Substring(11, 2) == "OK"))
                        Base.v_UltimoComandoLista = "@LIOK#";
                    if (v_Comando.Substring(0, 5) == "@LDOK" || v_Comando.Substring(0, 5) == "@LBOK" || v_Comando.Substring(0, 5) == "@LEOK")
                        Base.v_UltimoComandoLista = v_Comando;
                    string str1 = v_Comando.Substring(1, 5);
                    if (!(str1 == "RESET"))
                    {
                        if (str1 == "_VIVO")
                        {
                            Base.v_ComandoPing = v_Comando;
                        }
                        else
                        {
                            string flagDiretor;
                            LocalizaCrachaMaster vLocalizaCrachaMaster;
                            ColaboradorConsulta v_ColaboradorConsultavco;
                            string v_CD_CREDENCIAL;
                            string v_CD_SENTIDO;
                            string v_s_Aplicacao;
                            string SV_ID_SECAO;
                            string V_CD_TIPO_CREDENCIAL;
                            string V_CD_VCO;
                            string CD_TIPO_EQUIPAMENTO;
                            string vl_Score;
                            string vIdMotivacao;
                            string vIdManobra;
                            string vNumeroOS;
                            string vPlaca;
                            string vObs;
                            string placa;
                            string CNC;
                            string datahora_captura;
                            string nome_arquivo;
                            string str2;
                            switch (v_Comando.Substring(1, 3))
                            {
                                case "ALC":
                                    v_CD_CREDENCIAL = v_Comando.Substring(4, 8);
                                    int v_ID_EQUIPAMENTO = Regras.v_Id_Equipamento;
                                    v_CD_SENTIDO = ConfigurationManager.AppSettings["SENTIDO_ACESSO"].ToString();
                                    v_s_Aplicacao = Regras.v_s_Aplicacao;
                                    SV_ID_SECAO = new NovaSecao().Secao(v_s_Aplicacao, v_ID_EQUIPAMENTO);
                                    long LV_ID_SECAO = (long)Convert.ToUInt32(SV_ID_SECAO);
                                    V_CD_TIPO_CREDENCIAL = "P";
                                    V_CD_VCO = "81";
                                    CD_TIPO_EQUIPAMENTO = "N";
                                    double __db_v_i_Score_LBF = 1234.0;
                                    vl_Score = "00000";
                                    int fator = Regras.v_Fator;
                                    vIdMotivacao = "0";
                                    vIdManobra = "0";
                                    vNumeroOS = "";
                                    vPlaca = "ABC1234";
                                    vObs = "Testes";
                                    Base.ExecutaTestePing = false;
                                    this.v_b_VCO_Pessoa = true;
                                    this.ignora = false;
                                    if (!this.v_b_Timeout)
                                    {
                                        this.t_Timeout = new Thread(new ThreadStart(this.TimeOut));
                                        this.t_Timeout.Start();
                                    }
                                    new Logar().ILogar(v_s_Aplicacao.ToString(), SV_ID_SECAO, v_ID_EQUIPAMENTO, 2, v_CD_CREDENCIAL, v_Comando, (string)null, "R", nameof(IRegras));
                                    this.v_Placa = "";
                                    this.v_IdMotivacao = "";
                                    new GravaAcesso().GravarAcesso(v_CD_CREDENCIAL, v_ID_EQUIPAMENTO, v_CD_SENTIDO, v_s_Aplicacao, LV_ID_SECAO, V_CD_TIPO_CREDENCIAL, V_CD_VCO, CD_TIPO_EQUIPAMENTO, vl_Score, fator, this.v_IdMotivacao, vIdManobra, vNumeroOS, vPlaca, vObs);
                                    this.Dispara_Liu();
                                    this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                                    break;
                                case "ALP":
                                    new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "192", "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, this.v_IdMotivacao, v_idManobra, "", "", "");
                                    int min_length_command = 26;
                                    min_length_command += 3;
                                    placa = "";
                                    CNC = "";
                                    datahora_captura = "";
                                    nome_arquivo = "";
                                    break;
                                case "AOL":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 11, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "CEC":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 15, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "CES":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 67, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "CLO":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 17, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "CVD":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 59, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "CVL":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 69, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "CVO":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 65, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "CVT":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 63, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "CVV":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 61, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "DSM":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 19, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "HKS":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 73, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "HLD":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 6, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "ISM":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 71, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "LIN":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 57, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "LIU":
                                    this.LiuOk = 1;
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 13, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    Thread.Sleep(100);
                                    this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO", v_Porta_Envio, v_IP);
                                    v_ComandoSaida = "$DSM                                000#";
                                    new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                    break;
                                case "MDO":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 84, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "SLC":
                                    Base.ExecutaTestePing = false;
                                    this.v_b_VCO_Pessoa = true;
                                    if (this.v_Secao == "" || this.v_Secao == null)
                                    {
                                        this.ignora = false;
                                        this.v_Secao = new NovaSecao().Secao(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                        if (!this.v_b_Timeout)
                                        {
                                            this.t_Timeout = new Thread(new ThreadStart(this.TimeOut));
                                            this.t_Timeout.Start();
                                        }
                                    }
                                    else
                                        this.ignora = true;
                                    if (this.v_Credencial_Pessoa == "")
                                        this.v_Credencial_Pessoa = v_Comando.Substring(4, 8);
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 2, this.v_Credencial_Pessoa, v_Comando, (string)null, "R", nameof(IRegras));
                                    if (this.v_Credencial_Pessoa == "00000000")
                                    {
                                        this.v_s_Sentido = ConfigurationManager.AppSettings["SENTIDO_ACESSO"].ToString();
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "MANOBRA", (string)null, this.v_s_Sentido, nameof(IRegras));
                                        Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["TEMPO_ESPERA_LAP"]));
                                        v_ComandoSaida = "$DSMCAPTURANDO      PLACA . . .     010#";
                                        new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", v_ComandoSaida, (string)null, this.v_s_Sentido, nameof(IRegras));
                                        this.v_Placa = "";
                                        this.v_IdMotivacao = "";
                                        double v_IdOcr = 0.0;
                                        HttpClient client = new HttpClient();
                                        try
                                        {
                                            string v_LAPRequisicao = "{\"ambiente\":\"" + ConfigurationManager.AppSettings["AMBIENTE"] + "\",\"local\":\"" + Regras.v_s_Aplicacao + "\",\"pesagem\":\"\",\"idsecao\":\"" + this.v_Secao + "\"}";
                                            string uri = ConfigurationManager.AppSettings["WEBAPI_OCR"].ToString();
                                            string data = v_LAPRequisicao;
                                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", uri + "-" + v_LAPRequisicao, (string)null, "E", nameof(IRegras));
                                            StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
                                            HttpResponseMessage response = (HttpResponseMessage)null;
                                            response = await client.PostAsync(uri, (HttpContent)content);
                                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "response:" + response.IsSuccessStatusCode.ToString(), (string)null, "E", nameof(IRegras));
                                            if (response.IsSuccessStatusCode)
                                            {
                                                JToken token = (JToken)JObject.Parse(response.Content.ReadAsStringAsync().Result);
                                                object resultado = (object)token.SelectToken("Result");
                                                Regras.retOcr retoc = new Regras.retOcr();
                                                retoc = JsonConvert.DeserializeObject<Regras.retOcr>(resultado.ToString());
                                                this.v_Placa = retoc.Placa;
                                                v_IdOcr = (double)retoc.IdOcr;
                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "PLACA CAPTURADA:" + this.v_Placa, (string)null, "E", nameof(IRegras));
                                                token = (JToken)null;
                                                resultado = (object)null;
                                                retoc = (Regras.retOcr)null;
                                                token = (JToken)null;
                                                resultado = (object)null;
                                                retoc = (Regras.retOcr)null;
                                            }
                                            v_LAPRequisicao = (string)null;
                                            uri = (string)null;
                                            data = (string)null;
                                            content = (StringContent)null;
                                            response = (HttpResponseMessage)null;
                                            v_LAPRequisicao = (string)null;
                                            uri = (string)null;
                                            data = (string)null;
                                            content = (StringContent)null;
                                            response = (HttpResponseMessage)null;
                                        }
                                        catch (Exception ex1)
                                        {
                                            Exception ex = ex1;
                                            this.v_Placa = "";
                                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "PLACA NÃO CAPTURADA", (string)null, "E", nameof(IRegras));
                                            ex = (Exception)null;
                                        }
                                        if (this.v_s_Sentido == "E")
                                            new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "191", "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, this.v_IdMotivacao, v_idManobra, "", "", "");
                                        else
                                            new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "192", "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, this.v_IdMotivacao, v_idManobra, "", "", "");
                                        this.Dispara_Liu();
                                        if (ConfigurationManager.AppSettings["CONTROLE_VAGAS"] == "1")
                                        {
                                            new GravaAcessoPatio().GravarAcessoPatio("E", 0, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, "", 0L, "", "", "", "", "", "", "");
                                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, "Atualizando Vagas", (string)null, "E", "Entrada");
                                        }
                                        this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                                        client = (HttpClient)null;
                                        break;
                                    }
                                    break;
                                case "TST":
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 77, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    break;
                                case "VAF":
                                    if (v_Comando.Length == 14)
                                    {
                                        if (v_Comando.Substring(0, 3) == "@LI" && v_Comando.Substring(11, 2) == "OK")
                                        {
                                            Base.v_UltimoComandoLista = "@LIOK#";
                                            break;
                                        }
                                        break;
                                    }
                                    Base.v_UltimoComandoLista = v_Comando;
                                    break;
                                case "VCO":
                                    flagDiretor = "";
                                    this.v_Credencial = v_Comando.Substring(4, 8);
                                    vLocalizaCrachaMaster = new LocalizaCrachaMaster().LocalizarCrachaMaster(this.v_Credencial, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                    if (vLocalizaCrachaMaster != null)
                                    {
                                        v_Comando = "$DSMBEM VINDO!      ACESSO LIBERADO 010#";
                                        new Enviar().IEnviar(v_Comando, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial, v_Comando, (string)null, "E", "JugarEntrada");
                                        this.Dispara_Liu();
                                        this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                                        break;
                                    }
                                    if (this.ignora && this.v_PlacaLaco == "")
                                    {
                                        v_ComandoSaida = "$AOL504" + this.v_Credencial + "AGUARDE RESPOSTA#";
                                        new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "IGNORANDO SLC", (string)null, this.v_s_Sentido, nameof(IRegras));
                                        break;
                                    }
                                    if (ConfigurationManager.AppSettings["ACEITA_CREDENCIAL"] == "0" && this.v_PlacaLaco == "")
                                    {
                                        v_ComandoSaida = "$AOL504" + this.v_Credencial + "*NAO AUTORIZADO*#";
                                        new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "IGNORANDO CARTÃO", (string)null, this.v_s_Sentido, nameof(IRegras));
                                        this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                                        break;
                                    }
                                    if (this.v_PlacaLaco != "")
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "PLACA + CARTAO " + this.v_PlacaLaco + " " + this.v_Credencial, (string)null, this.v_s_Sentido, nameof(IRegras));
                                    v_ColaboradorConsultavco = new ColaboradorConsulta().ColaboradorConsultar(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento, this.v_Credencial_Pessoa);
                                    if (v_ColaboradorConsultavco.NOME == null)
                                    {
                                        v_ComandoSaida = "$AOL701" + this.v_Credencial + "*NAO CADASTRADO*#";
                                        new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", v_ComandoSaida + "CARTÃO NÃO EXISTE!", (string)null, this.v_s_Sentido, nameof(IRegras));
                                        this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                                        break;
                                    }
                                    flagDiretor = v_ColaboradorConsultavco.FLAGDIRETOR;
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 4, this.v_Credencial, "CARRO DIRETORIA: " + flagDiretor, (string)null, "L", nameof(IRegras));
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 4, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    if (v_Comando.Substring(12, 3) == "501" || v_Comando.Substring(12, 3) == "502")
                                    {
                                        v_ComandoSaida = "$AOL" + v_Comando.Substring(12, 3) + this.v_Credencial_Pessoa + "#";
                                        new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                        break;
                                    }
                                    if (v_Comando.Substring(12, 5) == "905E#")
                                    {
                                        v_Comando = v_Comando.Replace("905E#", "001E#");
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 4, this.v_Credencial, "TROCANDO 905E# PARA 001E#" + v_Comando, (string)null, "L", nameof(IRegras));
                                    }
                                    if (v_Comando.Substring(12, 5) == "905S#")
                                    {
                                        v_Comando = v_Comando.Replace("905S#", "002S#");
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 4, this.v_Credencial, "TROCANDO 905S# PARA 002S#" + v_Comando, (string)null, "L", nameof(IRegras));
                                    }
                                    if (v_Comando.Substring(12, 5) == "801E#")
                                    {
                                        v_Comando = v_Comando.Replace("801E#", "001E#");
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 4, this.v_Credencial, "TROCANDO 801E# PARA 001E#" + v_Comando, (string)null, "L", nameof(IRegras));
                                    }
                                    if (v_Comando.Substring(12, 5) == "801S#")
                                    {
                                        v_Comando = v_Comando.Replace("801S#", "002S#");
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 4, this.v_Credencial, "TROCANDO 801S# PARA 002S#" + v_Comando, (string)null, "L", nameof(IRegras));
                                    }
                                    if (v_Comando.Substring(12, 5) == "806E#")
                                    {
                                        v_Comando = v_Comando.Replace("806E#", "001E#");
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 4, this.v_Credencial, "TROCANDO 801E# PARA 001E#" + v_Comando, (string)null, "L", nameof(IRegras));
                                    }
                                    if (v_Comando.Substring(12, 5) == "806S#")
                                    {
                                        v_Comando = v_Comando.Replace("806S#", "002S#");
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 4, this.v_Credencial, "TROCANDO 801S# PARA 002S#" + v_Comando, (string)null, "L", nameof(IRegras));
                                    }
                                    this.v_s_VCO_Pessoa = v_Comando.Substring(12, 3);
                                    int v_AcessoConvencional = 0;
                                    if (v_Comando.Substring(15, 1) == "E")
                                    {
                                        if (flagDiretor == "1")
                                        {
                                            this.Libera_Acesso_Registra_Lap(v_IP, v_Porta_Envio);
                                            break;
                                        }
                                        this.v_s_Sentido = v_Comando.Substring(15, 1);
                                        if (ConfigurationManager.AppSettings["CONTROLE_VAGAS"] == "1")
                                        {
                                            VerificaVagas vVerificaVagas = new VerificaVagas().VerificarVagas(Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, Convert.ToInt32(ConfigurationManager.AppSettings["GATE"].Replace("G", "")));
                                            if (vVerificaVagas.FLAG_OK == 1)
                                            {
                                                v_ComandoSaida = "$DSMACESSO NEGADO   PATIO LOTADO     8#";
                                                new Enviar().IEnviar(v_ComandoSaida, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 34, this.v_Credencial, v_ComandoSaida, (string)null, "L", "LAP");
                                                new GravaAcesso().GravarAcesso(this.v_Credencial, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "31", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "Pátio Lotado");
                                                this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                                                break;
                                            }
                                            vVerificaVagas = (VerificaVagas)null;
                                            vVerificaVagas = (VerificaVagas)null;
                                        }
                                        if (this.v_s_VCO_Pessoa == "001")
                                        {
                                            if (new VerificaDuploAcesso().VerificarDuploAcesso(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento) && new VerificandoDuploAccesso().VerificandoDuploAcesso(this.v_Credencial, "E", Regras.v_Id_Equipamento, Regras.v_s_Aplicacao))
                                            {
                                                this.v_b_DuploAcesso = true;
                                                v_ComandoSaida = "$AOL701" + this.v_Credencial + "DUPLO ACESSO    #";
                                                new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 10, this.v_Credencial, v_ComandoSaida, (string)null, "E", nameof(IRegras));
                                                v_ComandoSaida = "";
                                                ValidarMotivacao v_ValidaMotivo = new ValidarMotivacao().ValidaMotivacao(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento, this.v_Credencial_Pessoa);
                                                this.v_IdMotivacao = string.IsNullOrEmpty(v_ValidaMotivo.PLACA) ? "0" : v_ValidaMotivo.ID;
                                                new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "701", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                                                this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO - DUPLO ACESSO - PESSOA", v_Porta_Envio, v_IP);
                                                v_ComandoSaida = "";
                                                v_AcessoConvencional = 0;
                                                break;
                                            }
                                            if (this.v_PlacaLaco != "")
                                            {
                                                v_ComandoSaida = "$AOL504" + this.v_Credencial + "AGUARDE         #";
                                                new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                Thread.Sleep(3000);
                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, v_ComandoSaida, (string)null, "E", "JugarEntrada");
                                                LocalizaCracha vLocalizaCrachaLaco = new LocalizaCracha().LocalizarCracha(this.v_Credencial_Pessoa, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                if (vLocalizaCrachaLaco != null)
                                                {
                                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, v_Comando, (string)null, "E", "JugarEntrada");
                                                    new Enviar().IEnviar(v_Comando, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                    v_Comando = "$DSMBEM VINDO!      ACESSO LIBERADO 010#";
                                                    new Enviar().IEnviar(v_Comando, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, v_Comando + ". Placa:" + this.v_PlacaLaco, (string)null, "E", "JugarEntrada");
                                                    this.Dispara_Liu();
                                                    v_AcessoConvencional = 0;
                                                    if (ConfigurationManager.AppSettings["CONTROLE_VAGAS"] == "1")
                                                    {
                                                        new GravaAcessoPatio().GravarAcessoPatio("E", 0, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, "", 0L, "", "", "", "", "", "", "");
                                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, "Atualizando Vagas", (string)null, "E", "Entrada");
                                                        break;
                                                    }
                                                    break;
                                                }
                                                new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "30", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                                                v_ComandoSaida = "$DSM*NAO AUTORIZADO*                005#";
                                                new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, v_ComandoSaida, (string)null, "E", "Entrada");
                                                this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO - NÃO MOTIVADO", v_Porta_Envio, v_IP);
                                                v_ComandoSaida = "";
                                                v_AcessoConvencional = 0;
                                                break;
                                            }
                                            this.v_IdMotivacao = "0";
                                            ValidarMotivacao v_ValidaMotivacao = new ValidarMotivacao().ValidaMotivacao(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento, this.v_Credencial_Pessoa);
                                            if (!string.IsNullOrEmpty(v_ValidaMotivacao.PLACA))
                                            {
                                                this.v_IdMotivacao = v_ValidaMotivacao.ID;
                                                this.v_Placa = v_ValidaMotivacao.PLACA.ToUpper().Replace("-", "");
                                                v_ComandoSaida = "$AOL504" + this.v_Credencial + "AGUARDE         #";
                                                new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 10, this.v_Credencial_Pessoa, v_ComandoSaida, (string)null, "E", nameof(IRegras));
                                                v_ComandoSaida = "";
                                                v_AcessoConvencional = 1;
                                                this.v_s_Sentido = "E";
                                                this.v_b_DuploAcesso = false;
                                                v_ValidaMotivacao = (ValidarMotivacao)null;
                                                v_ValidaMotivacao = (ValidarMotivacao)null;
                                            }
                                            else
                                            {
                                                int StatusCode = 0;
                                                string StatusMessage = "";
                                                LocalizaCracha vLocalizaCracha = new LocalizaCracha().LocalizarCracha(this.v_Credencial_Pessoa, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, "Localizou: " + (object)vLocalizaCracha, (string)null, "E", "JugarEntrada");
                                                if (vLocalizaCracha != null)
                                                {
                                                    if (vLocalizaCracha.CdAtivo == "True")
                                                    {
                                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, "Ativo: " + vLocalizaCracha.CdAtivo, (string)null, "E", "JugarEntrada");
                                                        if (vLocalizaCracha.IdTipoCracha == 4)
                                                        {
                                                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, "Ativo: " + (object)vLocalizaCracha.IdTipoCracha, (string)null, "E", "JugarEntrada");
                                                            v_ComandoSaida = "$AOL504" + this.v_Credencial + "AGUARDE         #";
                                                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, v_ComandoSaida, (string)null, "E", "JugarEntrada");
                                                            new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                            HttpClient client = new HttpClient();
                                                            try
                                                            {
                                                                string v_LAPRequisicao = "?sessao=" + this.v_Secao + "&idcracha=" + (object)vLocalizaCracha.IdCracha + "&local=" + Regras.v_s_Aplicacao + "&ambiente=" + ConfigurationManager.AppSettings["AMBIENTE"] + "&sentido=" + this.v_s_Sentido;
                                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 10, this.v_Credencial_Pessoa, v_LAPRequisicao, (string)null, "E", nameof(IRegras));
                                                                string uri = ConfigurationManager.AppSettings["WEBAPI_MOTIVAR"].ToString() + v_LAPRequisicao;
                                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 10, this.v_Credencial_Pessoa, uri, (string)null, "E", nameof(IRegras));
                                                                string data = "";
                                                                StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
                                                                HttpResponseMessage response = (HttpResponseMessage)null;
                                                                response = await client.PostAsync(uri, (HttpContent)content);
                                                                if (response.IsSuccessStatusCode)
                                                                {
                                                                    JToken token = (JToken)JObject.Parse(response.Content.ReadAsStringAsync().Result);
                                                                    Regras.retMotivar retMotiv = new Regras.retMotivar();
                                                                    retMotiv = JsonConvert.DeserializeObject<Regras.retMotivar>(token.ToString());
                                                                    StatusCode = retMotiv.StatusCode;
                                                                    StatusMessage = retMotiv.StatusMessage;
                                                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 10, this.v_Credencial_Pessoa, retMotiv.StatusMessage, (string)null, "E", nameof(IRegras));
                                                                    this.v_IdMotivacao = retMotiv.IdMotivacaoTemporaria;
                                                                    token = (JToken)null;
                                                                    retMotiv = (Regras.retMotivar)null;
                                                                    token = (JToken)null;
                                                                    retMotiv = (Regras.retMotivar)null;
                                                                }
                                                                v_LAPRequisicao = (string)null;
                                                                uri = (string)null;
                                                                data = (string)null;
                                                                content = (StringContent)null;
                                                                response = (HttpResponseMessage)null;
                                                                v_LAPRequisicao = (string)null;
                                                                uri = (string)null;
                                                                data = (string)null;
                                                                content = (StringContent)null;
                                                                response = (HttpResponseMessage)null;
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                StatusCode = 1;
                                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 10, this.v_Credencial_Pessoa, ex.Message, (string)null, "E", nameof(IRegras));
                                                            }
                                                            if (StatusCode == 1)
                                                            {
                                                                this.v_s_Sentido = "E";
                                                                if (StatusMessage == "BLOQUEIO DE LAP")
                                                                {
                                                                    new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "820", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "Bloqueio de LAP");
                                                                    v_ComandoSaida = "$DSM    *PLACA NAO RECONHECIDA*     005#";
                                                                }
                                                                else
                                                                {
                                                                    new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "30", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", StatusMessage);
                                                                    v_ComandoSaida = "$DSM        *NAO AUTORIZADO*        005#";
                                                                }
                                                                new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 10, this.v_Credencial_Pessoa, v_ComandoSaida, (string)null, "E", nameof(IRegras));
                                                                this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO - NÃO MOTIVADO", v_Porta_Envio, v_IP);
                                                                v_ComandoSaida = "";
                                                                v_AcessoConvencional = 0;
                                                                StatusMessage = "";
                                                                break;
                                                            }
                                                            VerificaLBF v_VerificaLBF = new VerificaLBF().VerificarLBF(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                            if (v_VerificaLBF != null)
                                                            {
                                                                this.v_i_Nota_Corte_LBF = (float)v_VerificaLBF.VLNOTACORTELBF;
                                                                if (v_VerificaLBF.CDEXECUTALBF == "S")
                                                                {
                                                                    this.v_b_Disparo_LAP = true;
                                                                    this.t_LBF = new Thread(new ThreadStart(this.disparaLbf));
                                                                    this.t_LBF.Start();
                                                                    this.v_b_Disparo_LBF = true;
                                                                }
                                                                else
                                                                {
                                                                    this.v_i_Score_LBF = 999f;
                                                                    this.v_b_Encerrado_LBF = true;
                                                                    this.v_Liberado_LBF = 1;
                                                                }
                                                            }
                                                            v_VerificaLBF.Terminate();
                                                            Thread.Sleep(3000);
                                                            new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", this.v_s_VCO_Pessoa, "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                                                            new InserirDuploAcesso().Gravar(this.v_Credencial_Pessoa, Regras.v_s_Aplicacao, "E", Regras.v_Id_Equipamento);
                                                            v_Comando = "$DSMBEM VINDO!      ACESSO LIBERADO 010#";
                                                            new Enviar().IEnviar(v_Comando, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                            this.Dispara_Liu();
                                                            v_AcessoConvencional = 0;
                                                            if (ConfigurationManager.AppSettings["CONTROLE_VAGAS"] == "1")
                                                            {
                                                                new GravaAcessoPatio().GravarAcessoPatio("E", 0, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, "", 0L, "", "", "", "", "", "", "");
                                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, "Atualizando Vagas", (string)null, "E", "Entrada");
                                                                break;
                                                            }
                                                            break;
                                                        }
                                                        v_ComandoSaida = "$AOL504" + this.v_Credencial + "CAPTURANDO PLACA #";
                                                        new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, v_ComandoSaida, (string)null, "E", "Entrada");
                                                        this.v_Placa = "";
                                                        HttpClient client1 = new HttpClient();
                                                        try
                                                        {
                                                            string v_LAPRequisicao = "{\"ambiente\":\"" + ConfigurationManager.AppSettings["AMBIENTE"] + "\",\"local\":\"" + Regras.v_s_Aplicacao + "\",\"pesagem\":\"\",\"idsecao\":\"" + this.v_Secao + "\"}";
                                                            string uri = ConfigurationManager.AppSettings["WEBAPI_OCR"].ToString();
                                                            string data = v_LAPRequisicao;
                                                            StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
                                                            HttpResponseMessage response = (HttpResponseMessage)null;
                                                            response = await client1.PostAsync(uri, (HttpContent)content);
                                                            if (response.IsSuccessStatusCode)
                                                            {
                                                                JToken token = (JToken)JObject.Parse(response.Content.ReadAsStringAsync().Result);
                                                                object resultado = (object)token.SelectToken("Result");
                                                                Regras.retOcr retoc = new Regras.retOcr();
                                                                retoc = JsonConvert.DeserializeObject<Regras.retOcr>(resultado.ToString());
                                                                this.v_Placa = retoc.Placa;
                                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, this.v_Placa, (string)null, "E", "Entrada");
                                                                token = (JToken)null;
                                                                resultado = (object)null;
                                                                retoc = (Regras.retOcr)null;
                                                                token = (JToken)null;
                                                                resultado = (object)null;
                                                                retoc = (Regras.retOcr)null;
                                                            }
                                                            v_LAPRequisicao = (string)null;
                                                            uri = (string)null;
                                                            data = (string)null;
                                                            content = (StringContent)null;
                                                            response = (HttpResponseMessage)null;
                                                            v_LAPRequisicao = (string)null;
                                                            uri = (string)null;
                                                            data = (string)null;
                                                            content = (StringContent)null;
                                                            response = (HttpResponseMessage)null;
                                                        }
                                                        catch (Exception ex1)
                                                        {
                                                            Exception ex = ex1;
                                                            this.v_Placa = "";
                                                            ex = (Exception)null;
                                                        }
                                                        if (this.v_Placa != "")
                                                        {
                                                            LocalizaVeiculo vLocalizaVeiculo = new LocalizaVeiculo().LocalizarVeiculo(this.v_Placa, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                            if (vLocalizaVeiculo != null)
                                                            {
                                                                VerificaLBF v_VerificaLBF = new VerificaLBF().VerificarLBF(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                                if (v_VerificaLBF != null)
                                                                {
                                                                    this.v_i_Nota_Corte_LBF = (float)v_VerificaLBF.VLNOTACORTELBF;
                                                                    if (v_VerificaLBF.CDEXECUTALBF == "S")
                                                                    {
                                                                        this.v_b_Disparo_LAP = true;
                                                                        this.t_LBF = new Thread(new ThreadStart(this.disparaLbf));
                                                                        this.t_LBF.Start();
                                                                        this.v_b_Disparo_LBF = true;
                                                                    }
                                                                    else
                                                                    {
                                                                        this.v_i_Score_LBF = 999f;
                                                                        this.v_b_Encerrado_LBF = true;
                                                                        this.v_Liberado_LBF = 1;
                                                                    }
                                                                }
                                                                v_VerificaLBF.Terminate();
                                                                Thread.Sleep(3000);
                                                                new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", this.v_s_VCO_Pessoa, "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", this.v_Placa, "");
                                                                new InserirDuploAcesso().Gravar(this.v_Credencial_Pessoa, Regras.v_s_Aplicacao, "E", Regras.v_Id_Equipamento);
                                                                v_Comando = "$DSMBEM VINDO!      ACESSO LIBERADO 010#";
                                                                new Enviar().IEnviar(v_Comando, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                                this.Dispara_Liu();
                                                                v_AcessoConvencional = 0;
                                                                if (ConfigurationManager.AppSettings["CONTROLE_VAGAS"] == "1")
                                                                {
                                                                    new GravaAcessoPatio().GravarAcessoPatio("E", 0, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, "", 0L, "", "", "", "", "", "", "");
                                                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, "Atualizando Vagas", (string)null, "E", "Entrada");
                                                                    break;
                                                                }
                                                                break;
                                                            }
                                                            this.v_s_Sentido = "E";
                                                            new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "30", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                                                            v_ComandoSaida = "$DSMACESSO NEGADO   SEM MOTIVACAO   005#";
                                                            new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, v_ComandoSaida, (string)null, "E", "Entrada");
                                                            this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO - NÃO MOTIVADO", v_Porta_Envio, v_IP);
                                                            v_ComandoSaida = "";
                                                            v_AcessoConvencional = 0;
                                                            break;
                                                        }
                                                        this.v_s_Sentido = "E";
                                                        new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "30", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "Bloqueio de LAP");
                                                        v_ComandoSaida = "$DSMACESSO NEGADO   SEM PLACA       005#";
                                                        new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, v_ComandoSaida, (string)null, "E", "Entrada");
                                                        this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO - NÃO MOTIVADO", v_Porta_Envio, v_IP);
                                                        v_ComandoSaida = "";
                                                        v_AcessoConvencional = 0;
                                                        break;
                                                    }
                                                    v_ComandoSaida = "$AOL701" + this.v_Credencial + "NAO MOTIVADO    #";
                                                    new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 10, this.v_Credencial, v_ComandoSaida, (string)null, "E", nameof(IRegras));
                                                    v_ComandoSaida = "";
                                                    this.v_s_Sentido = "E";
                                                    new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "30", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "Crachá Inválido");
                                                    this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO - NÃO MOTIVADO", v_Porta_Envio, v_IP);
                                                    v_ComandoSaida = "";
                                                    v_AcessoConvencional = 0;
                                                    break;
                                                }
                                                v_ComandoSaida = "$AOL701" + this.v_Credencial + "NAO MOTIVADO    #";
                                                new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 10, this.v_Credencial, v_ComandoSaida, (string)null, "E", nameof(IRegras));
                                                v_ComandoSaida = "";
                                                this.v_s_Sentido = "E";
                                                new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "30", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "Crachá Inválido");
                                                this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO - NÃO MOTIVADO", v_Porta_Envio, v_IP);
                                                v_ComandoSaida = "";
                                                v_AcessoConvencional = 0;
                                                break;
                                            }
                                        }
                                    }
                                    else if (this.v_s_VCO_Pessoa == "002")
                                    {
                                        if (flagDiretor == "1")
                                        {
                                            this.Libera_Acesso_Registra_Lap(v_IP, v_Porta_Envio);
                                            break;
                                        }
                                        this.v_s_Sentido = "S";
                                        this.v_IdMotivacao = "0";
                                        if (new VerificaDuploAcesso().VerificarDuploAcesso(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento) && new VerificandoDuploAccesso().VerificandoDuploAcesso(this.v_Credencial, "S", Regras.v_Id_Equipamento, Regras.v_s_Aplicacao))
                                        {
                                            this.v_b_DuploAcesso = true;
                                            v_ComandoSaida = "$AOL702" + this.v_Credencial + "DUPLA SAIDA     #";
                                            new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 10, this.v_Credencial, v_ComandoSaida, (string)null, "E", nameof(IRegras));
                                            v_ComandoSaida = "";
                                            ValidarMotivacao v_ValidaMotivo = new ValidarMotivacao().ValidaMotivacao(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento, this.v_Credencial_Pessoa);
                                            this.v_IdMotivacao = string.IsNullOrEmpty(v_ValidaMotivo.PLACA) ? "0" : v_ValidaMotivo.ID;
                                            new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "702", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                                            this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO - DUPLO ACESSO - PESSOA", v_Porta_Envio, v_IP);
                                            v_ComandoSaida = "";
                                            v_AcessoConvencional = 0;
                                            break;
                                        }
                                        if (ConfigurationManager.AppSettings["VALIDA_MOTIVACAO_SAIDA"] == "0")
                                        {
                                            this.Libera_Acesso_Registra_Lap(v_IP, v_Porta_Envio);
                                            break;
                                        }
                                        ValidarMotivacaoSaida v_ValidaMotivacaoSaida = new ValidarMotivacaoSaida().ValidaMotivacaoSaida(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento, this.v_Credencial_Pessoa);
                                        if (!string.IsNullOrEmpty(v_ValidaMotivacaoSaida.PLACA))
                                        {
                                            this.v_IdMotivacao = v_ValidaMotivacaoSaida.ID;
                                            this.v_Placa = v_ValidaMotivacaoSaida.PLACA.ToUpper().Replace("-", "");
                                            this.FlagMotorista = 1;
                                            v_ComandoSaida = "$AOL504" + this.v_Credencial + "AGUARDE         #";
                                            new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 10, this.v_Credencial_Pessoa, v_ComandoSaida, (string)null, "E", nameof(IRegras));
                                            v_ComandoSaida = "";
                                            v_ValidaMotivacaoSaida = (ValidarMotivacaoSaida)null;
                                            v_AcessoConvencional = 1;
                                            this.v_s_Sentido = "S";
                                            this.v_b_DuploAcesso = false;
                                            v_ValidaMotivacaoSaida = (ValidarMotivacaoSaida)null;
                                        }
                                        else
                                        {
                                            int StatusCode = 0;
                                            LocalizaCracha vLocalizaCracha = new LocalizaCracha().LocalizarCracha(this.v_Credencial_Pessoa, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                            if (vLocalizaCracha != null)
                                            {
                                                if (vLocalizaCracha.CdAtivo == "True")
                                                {
                                                    if (vLocalizaCracha.IdTipoCracha != 4)
                                                    {
                                                        v_ComandoSaida = "$AOL504" + this.v_Credencial + "CAPTURANDO PLACA #";
                                                        new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                        this.v_Placa = "";
                                                        HttpClient client = new HttpClient();
                                                        try
                                                        {
                                                            string v_LAPRequisicao = "{\"ambiente\":\"" + ConfigurationManager.AppSettings["AMBIENTE"] + "\",\"local\":\"" + Regras.v_s_Aplicacao + "\",\"pesagem\":\"\",\"idsecao\":\"" + this.v_Secao + "\"}";
                                                            string uri = ConfigurationManager.AppSettings["WEBAPI_OCR"].ToString();
                                                            string data = v_LAPRequisicao;
                                                            StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
                                                            HttpResponseMessage response = (HttpResponseMessage)null;
                                                            response = await client.PostAsync(uri, (HttpContent)content);
                                                            if (response.IsSuccessStatusCode)
                                                            {
                                                                JToken token = (JToken)JObject.Parse(response.Content.ReadAsStringAsync().Result);
                                                                object resultado = (object)token.SelectToken("Result");
                                                                Regras.retOcr retoc = new Regras.retOcr();
                                                                retoc = JsonConvert.DeserializeObject<Regras.retOcr>(resultado.ToString());
                                                                this.v_Placa = retoc.Placa;
                                                                token = (JToken)null;
                                                                resultado = (object)null;
                                                                retoc = (Regras.retOcr)null;
                                                                token = (JToken)null;
                                                                resultado = (object)null;
                                                                retoc = (Regras.retOcr)null;
                                                            }
                                                            v_LAPRequisicao = (string)null;
                                                            uri = (string)null;
                                                            data = (string)null;
                                                            content = (StringContent)null;
                                                            response = (HttpResponseMessage)null;
                                                            v_LAPRequisicao = (string)null;
                                                            uri = (string)null;
                                                            data = (string)null;
                                                            content = (StringContent)null;
                                                            response = (HttpResponseMessage)null;
                                                        }
                                                        catch (Exception ex1)
                                                        {
                                                            Exception ex = ex1;
                                                            this.v_Placa = "";
                                                            ex = (Exception)null;
                                                        }
                                                        if (this.v_Placa != "")
                                                        {
                                                            LocalizaVeiculo vLocalizaVeiculo = new LocalizaVeiculo().LocalizarVeiculo(this.v_Placa, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                            if (vLocalizaVeiculo != null)
                                                            {
                                                                new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", this.v_s_VCO_Pessoa, "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                                                                new InserirDuploAcesso().Gravar(this.v_Credencial_Pessoa, Regras.v_s_Aplicacao, "E", Regras.v_Id_Equipamento);
                                                                v_Comando = "$DSMATE LOGO !      SAIDA LIBERADA  010#";
                                                                new Enviar().IEnviar(v_Comando, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, v_Comando, (string)null, "E", "Entrada");
                                                                this.Dispara_Liu();
                                                                v_AcessoConvencional = 0;
                                                                if (ConfigurationManager.AppSettings["CONTROLE_VAGAS"] == "1")
                                                                {
                                                                    new GravaAcessoPatio().GravarAcessoPatio("S", 0, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, "", 0L, "", "", "", "", "", "", "");
                                                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, "Atualizando Vagas", (string)null, "E", "Entrada");
                                                                    break;
                                                                }
                                                                break;
                                                            }
                                                            this.v_s_Sentido = "S";
                                                            new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "30", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                                                            v_ComandoSaida = "$DSMSAIDA NEGADA    SEM MOTIVACAO   005#";
                                                            new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, v_ComandoSaida, (string)null, "E", "Entrada");
                                                            this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO - NÃO MOTIVADO", v_Porta_Envio, v_IP);
                                                            v_ComandoSaida = "";
                                                            v_AcessoConvencional = 0;
                                                            break;
                                                        }
                                                        this.v_s_Sentido = "S";
                                                        new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "30", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                                                        v_ComandoSaida = "$DSMSAIDA NEGADA    SEM PLACA       005#";
                                                        new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, v_ComandoSaida, (string)null, "E", "Entrada");
                                                        this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO - NÃO MOTIVADO", v_Porta_Envio, v_IP);
                                                        v_ComandoSaida = "";
                                                        v_AcessoConvencional = 0;
                                                        break;
                                                    }
                                                    v_ComandoSaida = "$AOL701" + this.v_Credencial + "NAO MOTIVADO    #";
                                                    new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 10, this.v_Credencial, v_ComandoSaida, (string)null, "E", nameof(IRegras));
                                                    v_ComandoSaida = "";
                                                    this.v_s_Sentido = "S";
                                                    new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "30", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                                                    this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO - NÃO MOTIVADO", v_Porta_Envio, v_IP);
                                                    v_ComandoSaida = "";
                                                    v_AcessoConvencional = 0;
                                                    break;
                                                }
                                                v_ComandoSaida = "$AOL701" + this.v_Credencial + "NAO MOTIVADO    #";
                                                new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 10, this.v_Credencial, v_ComandoSaida, (string)null, "E", nameof(IRegras));
                                                v_ComandoSaida = "";
                                                this.v_s_Sentido = "S";
                                                new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "30", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                                                this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO - NÃO MOTIVADO", v_Porta_Envio, v_IP);
                                                v_ComandoSaida = "";
                                                v_AcessoConvencional = 0;
                                                break;
                                            }
                                            v_ComandoSaida = "$AOL701" + this.v_Credencial + "NAO MOTIVADO    #";
                                            new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 10, this.v_Credencial, v_ComandoSaida, (string)null, "E", nameof(IRegras));
                                            v_ComandoSaida = "";
                                            this.v_s_Sentido = "S";
                                            new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "30", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                                            this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO - NÃO MOTIVADO", v_Porta_Envio, v_IP);
                                            v_ComandoSaida = "";
                                            v_AcessoConvencional = 0;
                                            break;
                                        }
                                    }
                                    this.v_b_VCO_Pessoa = false;
                                    if (v_Comando.Substring(15, 1) == "E" && (!this.v_b_Disparo_LBF && v_AcessoConvencional == 1))
                                    {
                                        VerificaLBF v_VerificaLBF = new VerificaLBF().VerificarLBF(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                        if (v_VerificaLBF != null)
                                        {
                                            this.v_i_Nota_Corte_LBF = (float)v_VerificaLBF.VLNOTACORTELBF;
                                            if (v_VerificaLBF.CDEXECUTALBF == "S")
                                            {
                                                this.t_LBF = new Thread(new ThreadStart(this.disparaLbf));
                                                this.t_LBF.Start();
                                                this.v_b_Disparo_LBF = true;
                                            }
                                            else
                                            {
                                                this.v_i_Score_LBF = 999f;
                                                this.v_b_Encerrado_LBF = true;
                                                this.v_Liberado_LBF = 1;
                                            }
                                        }
                                        v_VerificaLBF.Terminate();
                                        v_VerificaLBF = (VerificaLBF)null;
                                        v_VerificaLBF = (VerificaLBF)null;
                                    }
                                    if (!this.v_b_Disparo_LAP && v_AcessoConvencional == 1)
                                    {
                                        VerificaLAP v_VerificaLAP = new VerificaLAP().VerificarLAP(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                        if (v_VerificaLAP != null)
                                        {
                                            this.v_i_Nota_Corte_LAP = (float)v_VerificaLAP.VlNotaCorteLap;
                                            if (v_VerificaLAP.CdExecutaLap == "S")
                                            {
                                                this.t_LAP = new Thread(new ThreadStart(this.disparaLap));
                                                this.t_LAP.Start();
                                                this.v_b_Disparo_LAP = true;
                                            }
                                            else
                                            {
                                                this.v_i_Score_LAP = 999f;
                                                this.v_b_Disparo_LAP = true;
                                                this.v_b_Encerrado_LAP = true;
                                                this.v_b_VCO_Veiculo_Saida = true;
                                                this.v_Liberado_LAP = 1;
                                            }
                                        }
                                        v_VerificaLAP.Terminate();
                                        v_VerificaLAP = (VerificaLAP)null;
                                        v_VerificaLAP = (VerificaLAP)null;
                                    }
                                    if (v_AcessoConvencional == 1)
                                    {
                                        if (v_Comando.Substring(15, 1) == "E")
                                        {
                                            if (!this.v_b_VCO_Pessoa && !this.v_b_Jugar && !this.v_b_DuploAcesso && v_AcessoConvencional == 1)
                                            {
                                                this.t_Dispara_Jugar = new Thread(new ThreadStart(this.dispara_Jugar));
                                                this.t_Dispara_Jugar.Start();
                                                this.v_b_Jugar = true;
                                            }
                                        }
                                        else
                                        {
                                            if (!this.v_b_VCO_Pessoa && !this.v_b_Jugar_Saida && !this.v_b_DuploAcesso && v_AcessoConvencional == 1)
                                            {
                                                this.t_Dispara_Jugar_Saida = new Thread(new ThreadStart(this.dispara_Jugar_Saida));
                                                this.t_Dispara_Jugar_Saida.Start();
                                                this.v_b_Jugar_Saida = true;
                                            }
                                            this.v_b_VCO_Veiculo_Saida = true;
                                        }
                                        this.v_b_VCO_Veiculo = false;
                                        break;
                                    }
                                    break;
                                default:
                                    str2 = v_Comando.Substring(1, 2);
                                    if (!(str2 == "LD"))
                                    {
                                        if (!(str2 == "LB"))
                                        {
                                            if (!(str2 == "LI"))
                                            {
                                                if (!(str2 == "LE"))
                                                {
                                                    if (str2 == "NP")
                                                    {
                                                        Base.v_UltimoComandoLista = v_Comando;
                                                        break;
                                                    }
                                                    string str3 = v_Comando.Substring(1, 1);
                                                    if (!(str3 == "S"))
                                                    {
                                                        if (str3 == "M")
                                                        {
                                                            if (this.v_s_Trata_M == v_Comando)
                                                            {
                                                                ++this.v_i_Trata_M;
                                                                if (this.v_i_Trata_M >= 4)
                                                                {
                                                                    v_ComandoSaida = "$MDK#";
                                                                    new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 83, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                                                }
                                                            }
                                                            else
                                                                this.v_i_Trata_M = 1;
                                                            this.v_s_Trata_M = v_Comando;
                                                            if (v_Comando.Substring(4, 8) != "--------" || v_Comando.Substring(1, 3) != "MOR" || v_Comando.Substring(4, 8) != "00000000")
                                                            {
                                                                string v_DataHora_Controladora = v_Comando.Substring(15, 2).ToString() + "/" + v_Comando.Substring(17, 2).ToString() + "/20" + v_Comando.Substring(19, 2).ToString() + " " + v_Comando.Substring(21, 2).ToString() + ":" + v_Comando.Substring(23, 2).ToString() + ":" + v_Comando.Substring(25, 2).ToString();
                                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 8, this.v_Credencial, v_Comando, v_DataHora_Controladora, "R", nameof(IRegras));
                                                                v_ComandoSaida = "$MOK#";
                                                                new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 9, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                                                v_ComandoSaida = "";
                                                                v_DataHora_Controladora = (string)null;
                                                                break;
                                                            }
                                                            break;
                                                        }
                                                        break;
                                                    }
                                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 75, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                                    break;
                                                }
                                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 88, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                                Base.v_UltimoComandoLista = v_Comando;
                                                break;
                                            }
                                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 87, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                            if (v_Comando.Length == 14)
                                            {
                                                if (v_Comando.Substring(0, 3) == "@LI" && v_Comando.Substring(11, 2) == "OK")
                                                {
                                                    Base.v_UltimoComandoLista = "@LIOK#";
                                                    break;
                                                }
                                                break;
                                            }
                                            Base.v_UltimoComandoLista = v_Comando;
                                            break;
                                        }
                                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 86, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                        Base.v_UltimoComandoLista = v_Comando;
                                        break;
                                    }
                                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 85, this.v_Credencial, v_Comando, (string)null, "R", nameof(IRegras));
                                    Base.v_UltimoComandoLista = v_Comando;
                                    break;
                            }
                            v_CD_CREDENCIAL = (string)null;
                            v_CD_SENTIDO = (string)null;
                            v_s_Aplicacao = (string)null;
                            SV_ID_SECAO = (string)null;
                            V_CD_TIPO_CREDENCIAL = (string)null;
                            V_CD_VCO = (string)null;
                            CD_TIPO_EQUIPAMENTO = (string)null;
                            vl_Score = (string)null;
                            vIdMotivacao = (string)null;
                            vIdManobra = (string)null;
                            vNumeroOS = (string)null;
                            vPlaca = (string)null;
                            vObs = (string)null;
                            placa = (string)null;
                            CNC = (string)null;
                            datahora_captura = (string)null;
                            nome_arquivo = (string)null;
                            str2 = (string)null;
                            flagDiretor = (string)null;
                            vLocalizaCrachaMaster = (LocalizaCrachaMaster)null;
                            v_ColaboradorConsultavco = (ColaboradorConsulta)null;
                            flagDiretor = (string)null;
                            vLocalizaCrachaMaster = (LocalizaCrachaMaster)null;
                            v_ColaboradorConsultavco = (ColaboradorConsulta)null;
                        }
                    }
                    else
                    {
                        this.EncerraSecao(v_IP.ToString(), "TERMINO DA SECAO - RESET", v_Porta_Envio, v_IP);
                        v_ComandoSaida = "$DSM                                000#";
                        new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), "0", Regras.v_Id_Equipamento, 20, "", "ENCERRANDO A SECAO PELO COMANDO RESET", (string)null, "E", nameof(IRegras));
                    }
                    str1 = (string)null;
                }
                v_idManobra = (string)null;
                v_ComandoSaida = (string)null;
                v_ComandoCompleto = (string[])null;
                v_idManobra = (string)null;
                v_ComandoSaida = (string)null;
                v_ComandoCompleto = (string[])null;
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro IRegras() Serviço Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message + " - Comando: " + v_Comando, EventLogEntryType.Error, ex);
                new Enviar().IEnviar("$RESET#", v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
            }
        }

        public void EncerraSecao(string v_IP_C, string s_Mensagem, int v_Porta_Envio, IPAddress v_IP)
        {
            string vSecao = this.v_Secao;
            string credencialPessoa = this.v_Credencial_Pessoa;
            Base.ExecutaTestePing = true;
            this.v_DoneLBF = true;
            this.v_DoneDisparaJurarSaida = true;
            this.v_DoneDisparaJurar = true;
            this.v_BuscaResultadoLAP = true;
            this.v_BuscaResultadoLBF = true;
            this.v_b_VCO_Veiculo = false;
            this.v_b_VCO_Pessoa = false;
            this.v_b_Disparo_LAP = false;
            this.v_b_Disparo_LBF = false;
            this.v_b_Timeout = false;
            this.v_b_Timeout_Acionado = false;
            this.v_b_Jugar = false;
            this.v_b_Jugar_Saida = false;
            this.v_b_guarda = false;
            this.v_b_Acesso_Especial = false;
            this.v_s_VCO_Veiculo = "";
            this.v_s_VCO_Pessoa = "";
            this.v_Secao = "";
            this.v_Credencial = "";
            this.v_i_Score_LAP = 0.0f;
            this.v_i_Nota_Corte_LAP = 0.0f;
            this.v_i_Score_LBF = 0.0f;
            this.v_Liberado_LAP = 0;
            this.v_Liberado_LBF = 0;
            this.v_Credencial_Pessoa = "";
            this.v_Credencial_Veiculo = "";
            this.v_Placa = "";
            this.v_b_Encerrado_LAP = false;
            this.v_b_Encerrado_LBF = false;
            this.v_b_VCO_Pessoa_Saida = false;
            this.v_b_VCO_Veiculo_Saida = false;
            this.v_s_Sentido = "";
            this.piorTemplate = 0;
            this.v_Id_Requisicao_Problema_LBF = 0.0f;
            this.v_Id_Requisicao_Problema_LAP = 0.0f;
            this.v_IdMotivacao = "0";
            this.FlagMotorista = 0;
            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), vSecao, Regras.v_Id_Equipamento, 2, credencialPessoa, s_Mensagem, (string)null, "E", "IRegras");
        }

        public void disparaLap()
        {
            Console.WriteLine("DisparaLap");
            try
            {
                this.v_t_lap = new Thread(new ThreadStart(this.LAP));
                if ((uint)this.v_t_lap.ThreadState <= 0U)
                    return;
                this.v_t_lap.Start();
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro disparaLap() ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
            }
        }

        public void LAP()
        {
            try
            {
                int.Parse(this.v_Secao);
                float num1 = 0.0f;
                string[] strArray1 = new string[1];
                string v_s_Parametros = "";
                List<PegaCamerasLAP> source1 = new PegaCamerasLAP().PegarCamerasLAP(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                int num2;
                if (source1 != null)
                {
                    strArray1 = new string[source1.Count<PegaCamerasLAP>()];
                    for (int index1 = 0; index1 < source1.Count<PegaCamerasLAP>(); ++index1)
                    {
                        string[] strArray2 = strArray1;
                        int index2 = index1;
                        string[] strArray3 = new string[21];
                        strArray3[0] = "{\"Posicao\":\"";
                        num2 = source1[index1].NRPOSICAOCAMERATOTEM;
                        strArray3[1] = num2.ToString();
                        strArray3[2] = "\",\"Servico\":\"";
                        strArray3[3] = source1[index1].DSLINK.ToString();
                        strArray3[4] = "\",\"DeteccaoFacial\":\"";
                        num2 = source1[index1].CDDETECCAOFACIAL;
                        strArray3[5] = num2.ToString();
                        strArray3[6] = "\",\"ReconhecimentoFacial\":\"";
                        num2 = source1[index1].CDRECONHECIMENTOFACIAL;
                        strArray3[7] = num2.ToString();
                        strArray3[8] = "\",\"IP\":\"";
                        strArray3[9] = source1[index1].IPEQUIPAMENTO.ToString();
                        strArray3[10] = "\",\"Conta\":\"";
                        strArray3[11] = source1[index1].NMUSERCAMERA != null ? source1[index1].NMUSERCAMERA.ToString() : "";
                        strArray3[12] = "\",\"Senha\":\"";
                        strArray3[13] = source1[index1].CDPASSWORDCAMERA != null ? source1[index1].CDPASSWORDCAMERA.ToString() : "";
                        strArray3[14] = "\",\"Porta\":\"";
                        strArray3[15] = source1[index1].NRPORTAENTRADAEQUIPAMENTO.ToString();
                        strArray3[16] = "\",\"Rotacao\":\"";
                        num2 = source1[index1].VLROTACAOCAMERA;
                        strArray3[17] = num2.ToString();
                        strArray3[18] = "\",\"Fabricante\":\"";
                        num2 = source1[index1].CDFABRICANTE;
                        strArray3[19] = num2.ToString();
                        strArray3[20] = "\"}";
                        string str = string.Concat(strArray3);
                        strArray2[index2] = str;
                    }
                }
                else
                    new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " Não existe câmera cadastrada para esta controladora efetuar o teste de LAP", EventLogEntryType.Error, (Exception)null);
                PegaConfiguracaoLAP pegaConfiguracaoLap = new PegaConfiguracaoLAP().PegarConfiguracaoLAP(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                if (pegaConfiguracaoLap != null)
                {
                    string[] strArray2 = new string[11];
                    strArray2[0] = "{\"SectionID\":\"";
                    strArray2[1] = this.v_Secao;
                    strArray2[2] = "\",\"DeviceID\":\"";
                    num2 = Regras.v_Id_Equipamento;
                    strArray2[3] = num2.ToString();
                    strArray2[4] = "\",\"Credencial\":\"";
                    strArray2[5] = this.v_Credencial_Pessoa.ToString();
                    strArray2[6] = "\",\"Placa\":\"";
                    strArray2[7] = this.v_Placa;
                    strArray2[8] = "\",\"Score\":\"";
                    num2 = pegaConfiguracaoLap.VLNOTACORTELAP;
                    strArray2[9] = num2.ToString();
                    strArray2[10] = "\",\"Cameras\":[";
                    v_s_Parametros = string.Concat(strArray2);
                    pegaConfiguracaoLap.Terminate();
                    for (int index = 0; index < ((IEnumerable<string>)strArray1).Count<string>(); ++index)
                        v_s_Parametros = v_s_Parametros + strArray1[index] + (index < ((IEnumerable<string>)strArray1).Count<string>() - 1 ? "," : "]}");
                }
                else
                {
                    GravaEventLog gravaEventLog = new GravaEventLog();
                    string vSAplicacao = Regras.v_s_Aplicacao;
                    num2 = Regras.v_Id_Equipamento;
                    string v_s_Mensagem = "ID Controladora: " + num2.ToString() + " Não Existe configuração para LAP";
                    gravaEventLog.GravarEventLog(vSAplicacao, v_s_Mensagem, EventLogEntryType.Error, (Exception)null);
                }
                int num3 = 0;
                if (v_s_Parametros.Length > 0)
                {
                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 34, this.v_Credencial_Veiculo, "DISPARA WEBAPILAP : SESSÃO : " + this.v_Secao, (string)null, "E", nameof(LAP));
                    this.DisparaWebAPI(v_s_Parametros, Regras.v_WebAPI_LAP);
                    this.v_BuscaResultadoLAP = false;
                    bool flag = true;
                    int num4 = 0;
                    num1 = -1f;
                    while (!this.v_BuscaResultadoLAP)
                    {
                        if (this.v_Secao != "")
                        {
                            IEnumerable<RetornoTesteLAP> source2 = new RetornoTesteLAP().RetornarTesteLAP(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento, int.Parse(this.v_Secao));
                            if (source2 != null && source2.Count<RetornoTesteLAP>() > 0)
                            {
                                using (IEnumerator<RetornoTesteLAP> enumerator = source2.GetEnumerator())
                                {
                                    if (enumerator.MoveNext())
                                    {
                                        RetornoTesteLAP current = enumerator.Current;
                                        num1 = (float)current.VL_SCORE;
                                        this.v_Id_Requisicao_Problema_LAP = (float)current.ID_REQUISICAO_PROBLEMA;
                                        num3 = current.ST_LIBERADO;
                                        this.v_BuscaResultadoLAP = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            this.v_BuscaResultadoLAP = true;
                            num1 = -1f;
                        }
                        Thread.Sleep(100);
                        if (this.v_b_Encerrado_LBF & flag)
                        {
                            flag = false;
                            new Enviar().IEnviar("$DSMVERIFICANDO     PLACA           059#", this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                        }
                        ++num4;
                        if (num4 == 40)
                        {
                            num1 = -1f;
                            this.v_BuscaResultadoLAP = true;
                            break;
                        }
                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 34, this.v_Credencial_Veiculo, "BUSCOU LAP : SESSÃO : " + this.v_Secao, (string)null, "E", nameof(LAP));
                    }
                }
                if ((double)num1 == -1.0)
                    num1 = 0.0f;
                this.v_i_Score_LAP = num1;
                this.v_Liberado_LAP = num3;
                if (!(this.v_Secao != "") || this.v_Secao == null)
                    return;
                string str1;
                if ((double)this.v_i_Score_LAP >= (double)this.v_i_Nota_Corte_LAP && (double)this.v_i_Score_LAP > 1.0 && (double)this.v_i_Nota_Corte_LAP != 0.0 && this.v_Liberado_LAP == 1)
                {
                    str1 = "$DSMPLACA           RECONHECIDA     005#";
                }
                else
                {
                    str1 = "$DSMPLACA NAO       RECONHECIDA     010#";
                    Thread.Sleep(2048);
                }
                new Enviar().IEnviar(str1, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 34, this.v_Credencial, str1, (string)null, "E", nameof(LAP));
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro LAP() ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
            }
            finally
            {
                this.v_b_Encerrado_LAP = true;
            }
        }

        public void disparaLbf()
        {
            try
            {
                this.v_DoneLBF = false;
                while (!this.v_DoneLBF)
                {
                    if (this.v_b_Timeout_Acionado)
                    {
                        this.v_b_Timeout_Acionado = false;
                        this.v_DoneLBF = true;
                    }
                    if (this.v_b_Disparo_LAP)
                    {
                        this.v_t_lbf = new Thread(new ThreadStart(this.LBF));
                        if ((uint)this.v_t_lbf.ThreadState > 0U)
                            this.v_t_lbf.Start();
                        this.v_DoneLBF = true;
                    }
                }
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro disparaLbf() ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
            }
        }

        public void LBF()
        {
            try
            {
                string[] strArray1 = new string[1];
                string v_s_Parametros = "";
                if (!(this.v_Credencial_Pessoa.ToString() != ""))
                    return;
                try
                {
                    List<PegaCamerasLBF> source1 = new PegaCamerasLBF().PegarCamerasLBF(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                    int num1;
                    if (source1 != null)
                    {
                        strArray1 = new string[source1.Count<PegaCamerasLBF>()];
                        for (int index1 = 0; index1 < source1.Count<PegaCamerasLBF>(); ++index1)
                        {
                            string[] strArray2 = strArray1;
                            int index2 = index1;
                            string[] strArray3 = new string[21];
                            strArray3[0] = "{\"Posicao\":\"";
                            num1 = source1[index1].NRPOSICAOCAMERATOTEM;
                            strArray3[1] = num1.ToString();
                            strArray3[2] = "\",\"Servico\":\"";
                            strArray3[3] = source1[index1].DSLINK.ToString();
                            strArray3[4] = "\",\"DeteccaoFacial\":\"";
                            num1 = source1[index1].CDDETECCAOFACIAL;
                            strArray3[5] = num1.ToString();
                            strArray3[6] = "\",\"ReconhecimentoFacial\":\"";
                            num1 = source1[index1].CDRECONHECIMENTOFACIAL;
                            strArray3[7] = num1.ToString();
                            strArray3[8] = "\",\"IP\":\"";
                            strArray3[9] = source1[index1].IPEQUIPAMENTO.ToString();
                            strArray3[10] = "\",\"Conta\":\"";
                            strArray3[11] = source1[index1].NMUSERCAMERA != null ? source1[index1].NMUSERCAMERA.ToString() : "";
                            strArray3[12] = "\",\"Senha\":\"";
                            strArray3[13] = source1[index1].CDPASSWORDCAMERA != null ? source1[index1].CDPASSWORDCAMERA.ToString() : "";
                            strArray3[14] = "\",\"Porta\":\"";
                            strArray3[15] = source1[index1].NRPORTAENTRADAEQUIPAMENTO.ToString();
                            strArray3[16] = "\",\"Rotacao\":\"";
                            num1 = source1[index1].VLROTACAOCAMERA;
                            strArray3[17] = num1.ToString();
                            strArray3[18] = "\",\"Fabricante\":\"";
                            num1 = source1[index1].CDFABRICANTE;
                            strArray3[19] = num1.ToString();
                            strArray3[20] = "\"}";
                            string str = string.Concat(strArray3);
                            strArray2[index2] = str;
                        }
                    }
                    else
                        new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " Não existe câmera cadastrada para esta controladora efetuar o teste de LBF", EventLogEntryType.Error, (Exception)null);
                    PegaConfiguracaoLBF pegaConfiguracaoLbf = new PegaConfiguracaoLBF().PegarConfiguracaoLBF(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                    if (pegaConfiguracaoLbf != null)
                    {
                        string[] strArray2 = new string[11];
                        strArray2[0] = "{\"SectionID\":\"";
                        strArray2[1] = this.v_Secao;
                        strArray2[2] = "\",\"DeviceID\":\"";
                        num1 = Regras.v_Id_Equipamento;
                        strArray2[3] = num1.ToString();
                        strArray2[4] = "\",\"Credencial\":\"";
                        strArray2[5] = this.v_Credencial_Pessoa.ToString();
                        strArray2[6] = "\",\"Score\":\"";
                        num1 = pegaConfiguracaoLbf.VLNOTACORTELBF;
                        strArray2[7] = num1.ToString().Replace(",", ".");
                        strArray2[8] = "\",\"Reconhecer\":\"";
                        num1 = pegaConfiguracaoLbf.QTIMAGENSLBF;
                        strArray2[9] = num1.ToString();
                        strArray2[10] = "\",\"Cameras\":[";
                        v_s_Parametros = string.Concat(strArray2);
                        pegaConfiguracaoLbf.Terminate();
                        for (int index = 0; index < ((IEnumerable<string>)strArray1).Count<string>(); ++index)
                            v_s_Parametros = v_s_Parametros + strArray1[index] + (index < ((IEnumerable<string>)strArray1).Count<string>() - 1 ? "," : "]}");
                    }
                    else
                    {
                        GravaEventLog gravaEventLog = new GravaEventLog();
                        string vSAplicacao = Regras.v_s_Aplicacao;
                        num1 = Regras.v_Id_Equipamento;
                        string v_s_Mensagem = "ID Controladora: " + num1.ToString() + " Não Existe configuração para LBF";
                        gravaEventLog.GravarEventLog(vSAplicacao, v_s_Mensagem, EventLogEntryType.Error, (Exception)null);
                    }
                    string str1 = "$DSMAGUARDE PARA    REGISTRAR FACE  059#";
                    new Enviar().IEnviar(str1, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 24, this.v_Credencial, str1, (string)null, "E", nameof(LBF));
                    Thread.Sleep(500);
                    new Enviar().IEnviar("$DSMOLHE DE FRENTE  PARA O ESPELHO  059#", this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                    Thread.Sleep(2000);
                    string str2 = "$DSMREGISTRANDO FACE                059#";
                    new Enviar().IEnviar(str2, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 24, this.v_Credencial, str2, (string)null, "E", nameof(LBF));
                    this.DisparaWebAPI(v_s_Parametros, Regras.v_WebAPI_LBF);
                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 0, this.v_Credencial, "DISPARAWEBAPILBF : SESSÃO : " + this.v_Secao, (string)null, "E", nameof(LBF));
                    this.v_BuscaResultadoLBF = false;
                    bool flag = true;
                    float num2 = -1f;
                    int num3 = 0;
                    int num4 = 0;
                    while (!this.v_BuscaResultadoLBF)
                    {
                        if (this.v_Secao != "")
                        {
                            IEnumerable<RetornoTesteLBF> source2 = new RetornoTesteLBF().RetornarTesteLBF(Regras.v_s_Aplicacao, Regras.v_Id_Equipamento, int.Parse(this.v_Secao));
                            if (source2 != null && source2.Count<RetornoTesteLBF>() > 0)
                            {
                                using (IEnumerator<RetornoTesteLBF> enumerator = source2.GetEnumerator())
                                {
                                    if (enumerator.MoveNext())
                                    {
                                        RetornoTesteLBF current = enumerator.Current;
                                        num2 = (float)current.VL_SCORE;
                                        this.v_Id_Requisicao_Problema_LBF = (float)current.ID_REQUISICAO_PROBLEMA;
                                        num3 = current.ST_LIBERADO;
                                        this.v_BuscaResultadoLBF = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            this.v_BuscaResultadoLBF = true;
                            this.v_i_Score_LBF = -1f;
                        }
                        if (this.v_b_Encerrado_LAP & flag)
                        {
                            flag = false;
                            new Enviar().IEnviar("$DSMREGISTRANDO FACE                059#", this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                        }
                        ++num4;
                        if (num4 < 10)
                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 24, this.v_Credencial, "BUSCOU LBF : SESSÃO : " + this.v_Secao + "-" + (object)num4, (string)null, "E", nameof(LBF));
                        else
                            break;
                    }
                    if ((double)num2 == -1.0)
                        num2 = 0.0f;
                    this.v_i_Score_LBF = num2;
                    this.v_Liberado_LBF = num3;
                    if (!(this.v_Secao != "") || this.v_Secao == null)
                        return;
                    string str3;
                    if ((double)this.v_i_Score_LBF >= (double)this.v_i_Nota_Corte_LBF && (double)this.v_i_Score_LBF > 1.0 && (double)this.v_i_Nota_Corte_LBF != 0.0 && this.v_Liberado_LBF == 1)
                    {
                        str3 = "$DSMFACE            RECONHECIDA     010#";
                    }
                    else
                    {
                        str3 = "$DSMFACE            REGISTRADA      010#";
                        Thread.Sleep(2048);
                    }
                    new Enviar().IEnviar(str3, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 33, this.v_Credencial, str3, (string)null, "E", nameof(LBF));
                }
                catch (Exception ex)
                {
                    new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro LBF ao tentar verificar a face no método LBF(). ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
                }
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro LBF() ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
            }
            finally
            {
                this.v_b_Encerrado_LBF = true;
            }
        }

        public void dispara_Jugar()
        {
            try
            {
                this.v_DoneDisparaJurar = false;
                while (!this.v_DoneDisparaJurar)
                {
                    if (this.v_b_Encerrado_LAP && this.v_b_Encerrado_LBF)
                    {
                        if (this.t_JugarEntrada != null)
                        {
                            if (this.t_JugarEntrada.ThreadState == System.Threading.ThreadState.Stopped || this.t_JugarEntrada.ThreadState == System.Threading.ThreadState.Aborted || this.t_JugarEntrada.ThreadState == System.Threading.ThreadState.AbortRequested)
                            {
                                this.t_JugarEntrada = new Thread(new ThreadStart(this.JugarEntrada));
                                this.t_JugarEntrada.Start();
                            }
                        }
                        else
                        {
                            this.t_JugarEntrada = new Thread(new ThreadStart(this.JugarEntrada));
                            this.t_JugarEntrada.Start();
                        }
                        this.v_DoneDisparaJurar = true;
                    }
                }
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro dispara_Jugar() ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
            }
        }

        public void dispara_Jugar_Saida()
        {
            try
            {
                this.v_DoneDisparaJurarSaida = false;
                while (!this.v_DoneDisparaJurarSaida)
                {
                    if (this.v_b_Encerrado_LAP)
                    {
                        if (this.t_JugarSaida != null)
                        {
                            if (this.t_JugarSaida.ThreadState == System.Threading.ThreadState.Stopped || this.t_JugarSaida.ThreadState == System.Threading.ThreadState.Aborted || this.t_JugarSaida.ThreadState == System.Threading.ThreadState.AbortRequested)
                            {
                                this.t_JugarSaida = new Thread(new ThreadStart(this.JugarSaida));
                                this.t_JugarSaida.Start();
                            }
                        }
                        else
                        {
                            this.t_JugarSaida = new Thread(new ThreadStart(this.JugarSaida));
                            this.t_JugarSaida.Start();
                        }
                        this.v_DoneDisparaJurarSaida = true;
                    }
                }
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro dispara_Jugar() ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
            }
        }

        public void JugarEntrada()
        {
            string str1 = "";
            string v_VCO_Pessoa = "";
            try
            {
                if (this.v_s_VCO_Pessoa != "" && this.v_Secao != "")
                {
                    this.v_Liberado_LBF = 1;
                    if ((double)this.v_i_Score_LAP >= (double)this.v_i_Nota_Corte_LAP)
                        this.v_Liberado_LAP = 1;
                    if ((double)this.v_i_Score_LBF >= (double)this.v_i_Nota_Corte_LBF)
                        this.v_Liberado_LBF = 1;
                    if ((double)this.v_i_Score_LAP >= (double)this.v_i_Nota_Corte_LAP && (double)this.v_i_Score_LBF >= (double)this.v_i_Nota_Corte_LBF && this.v_Liberado_LBF == 1 && this.v_Liberado_LAP == 1)
                    {
                        new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", this.v_s_VCO_Pessoa, "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                        new InserirDuploAcesso().Gravar(this.v_Credencial_Pessoa, Regras.v_s_Aplicacao, "E", Regras.v_Id_Equipamento);
                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 27, this.v_Credencial_Pessoa, "LAP: " + (object)this.v_i_Score_LAP, (string)null, "E", nameof(JugarEntrada));
                        string str2 = "$DSMBEM VINDO!      ACESSO LIBERADO 010#";
                        new Enviar().IEnviar(str2, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, str2, (string)null, "E", nameof(JugarEntrada));
                        Thread.Sleep(2000);
                        this.Dispara_Liu();
                        if (ConfigurationManager.AppSettings["CONTROLE_VAGAS"] == "1")
                        {
                            new GravaAcessoPatio().GravarAcessoPatio("E", 0, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, "", 0L, "", "", "", "", "", "", "");
                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, "Atualizando Vagas", (string)null, "E", "Entrada");
                        }
                        str1 = "";
                        this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                    }
                    else
                    {
                        int float_as_int = 0;
                        float fx = -this.v_Id_Requisicao_Problema_LBF;
                        if ((fx >= (9999f - 0.4f)) && (fx <= (9999f + 0.4f))) float_as_int = -9999;
                        if ((fx >= (9998f - 0.4f)) && (fx <= (9998f + 0.4f))) float_as_int = -9998;
                        if ((fx >= (9997f - 0.4f)) && (fx <= (9997f + 0.4f))) float_as_int = -9997;
                        if ((fx >= (9996f - 0.4f)) && (fx <= (9996f + 0.4f))) float_as_int = -9996;
                        if ((fx >= (9995f - 0.4f)) && (fx <= (9995f + 0.4f))) float_as_int = -9995;

                        switch (float_as_int)
                        {
                            case -9999: // -9999f:
                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 89, this.v_Credencial_Pessoa, " - LBF: " + (object)this.v_i_Score_LBF, (string)null, "E", nameof(JugarEntrada));
                                break;
                            case -9998:// -9998f:
                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 90, this.v_Credencial_Pessoa, " - LBF: " + (object)this.v_i_Score_LBF, (string)null, "E", nameof(JugarEntrada));
                                break;
                            case -9997: // -9997f:
                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 91, this.v_Credencial_Pessoa, " - LBF: " + (object)this.v_i_Score_LBF, (string)null, "E", nameof(JugarEntrada));
                                break;
                            case -9996: // -9996f:
                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 92, this.v_Credencial_Pessoa, " - LBF: " + (object)this.v_i_Score_LBF, (string)null, "E", nameof(JugarEntrada));
                                break;
                            case -9995: // -9995f:
                                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 93, this.v_Credencial_Pessoa, " - LBF: " + (object)this.v_i_Score_LBF, (string)null, "E", nameof(JugarEntrada));
                                break;
                        }


                        if ((double)this.v_i_Score_LBF < (double)this.v_i_Nota_Corte_LBF && (double)this.v_i_Score_LAP < (double)this.v_i_Nota_Corte_LAP)
                            new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "822", "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                        else if ((double)this.v_i_Score_LAP < (double)this.v_i_Nota_Corte_LAP)
                        {
                            v_VCO_Pessoa = "820";
                            new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "820", "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                        }
                        else
                        {
                            v_VCO_Pessoa = "822";
                            new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "822", "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                        }
                        if (new VerificaRetornoScore().VerificarRetornoScore(long.Parse(this.v_Secao), Regras.v_s_Aplicacao, Regras.v_Id_Equipamento.ToString()))
                        {
                            if ((double)this.v_i_Score_LAP < (double)this.v_i_Nota_Corte_LAP || (double)this.v_i_Score_LBF < (double)this.v_i_Nota_Corte_LBF || this.v_Liberado_LAP == 0 || this.v_Liberado_LBF == 0)
                            {
                                new Enviar().IEnviar("$DSMPROBLEMA        AGUARDE O GUARDA059#", this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                                new InserirOcorrenciaGuarda().GravarOcorrencia(this.v_i_Score_LAP.ToString(), "0", "", this.v_Credencial_Pessoa, this.v_Placa, this.v_Secao, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, this.v_s_Sentido, v_VCO_Pessoa, "");
                                this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                            }
                        }
                        else
                        {
                            new Enviar().IEnviar("$DSMPROBLEMA        TENTE NOVAMENTE 003#", this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                            new InserirOcorrenciaGuarda().GravarOcorrencia(this.v_i_Score_LAP.ToString(), "0", "", this.v_Credencial_Pessoa, this.v_Placa, this.v_Secao, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, this.v_s_Sentido, v_VCO_Pessoa, "");
                            this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                        }
                        if (this.v_Secao != "" && this.v_Secao != null)
                        {
                            this.t_TimeoutErroAplicacaoGuarda = new Thread(new ThreadStart(this.TimeOutErroAplicacaoGuarda));
                            this.t_TimeoutErroAplicacaoGuarda.Start();
                        }
                    }
                }
                else
                    new Enviar().IEnviar("$RESET#", this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro JugarEntrada(). ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
            }
            finally
            {
            }
        }

        public void JugarSaida()
        {
            try
            {
                string v_Mensagem_Livre = "";
                if (!this.v_b_Disparo_LAP)
                {
                    new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", this.v_s_VCO_Pessoa, "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                    new InserirDuploAcesso().Gravar(this.v_Credencial_Pessoa, Regras.v_s_Aplicacao, "S", Regras.v_Id_Equipamento);
                    string str2 = "$DSMATE LOGO!       SAIDA LIBERADA  010#";
                    new Enviar().IEnviar(str2, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, str2, (string)null, "E", "JugarEntrada");
                    Thread.Sleep(2000);
                    this.Dispara_Liu();
                    if (ConfigurationManager.AppSettings["CONTROLE_VAGAS"] == "1")
                    {
                        new GravaAcessoPatio().GravarAcessoPatio("S", this.FlagMotorista, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, "", 0L, "", "", "", "", "", "", "");
                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, "Atualizando Vagas", (string)null, "E", "Entrada");
                    }
                    this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
                }
                else
                {
                    if ((double)this.v_i_Score_LAP >= (double)this.v_i_Nota_Corte_LAP)
                        this.v_Liberado_LAP = 1;
                    if ((double)this.v_i_Score_LAP >= (double)this.v_i_Nota_Corte_LAP && this.v_Liberado_LAP == 1)
                    {
                        new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", this.v_s_VCO_Pessoa, "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                        new InserirDuploAcesso().Gravar(this.v_Credencial_Pessoa, Regras.v_s_Aplicacao, "S", Regras.v_Id_Equipamento);
                        string str2 = "$DSMATE LOGO!       SAIDA LIBERADA  010#";
                        new Enviar().IEnviar(str2, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                        new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, str2, (string)null, "E", "JugarEntrada");
                        Thread.Sleep(2000);
                        this.Dispara_Liu();
                        if (ConfigurationManager.AppSettings["CONTROLE_VAGAS"] == "1")
                        {
                            new GravaAcessoPatio().GravarAcessoPatio("S", this.FlagMotorista, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, "", 0L, "", "", "", "", "", "", "");
                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, "Atualizando Vagas", (string)null, "E", "Entrada");
                        }
                    }
                    else
                    {
                        string v_VCO_Pessoa = "821";
                        string v_VCO_Veiculo = "821";
                        new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "821", "N", "00000", Regras.v_Fator, this.v_IdMotivacao, "0", "", "", "");
                        if (new VerificaRetornoScore().VerificarRetornoScore(long.Parse(this.v_Secao), Regras.v_s_Aplicacao, Regras.v_Id_Equipamento.ToString()))
                        {
                            string v_Comando = "$DSMPROBLEMA        AGUARDE O GUARDA059#";
                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 0, this.v_Credencial_Pessoa, v_Mensagem_Livre, (string)null, "L", nameof(JugarSaida));
                            new Enviar().IEnviar(v_Comando, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                            new InserirOcorrenciaGuarda().GravarOcorrencia(this.v_i_Score_LAP.ToString(), this.v_i_Score_LBF.ToString(), this.v_Credencial_Veiculo, this.v_Credencial_Pessoa, this.v_Placa, this.v_Secao, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, this.v_s_Sentido, v_VCO_Pessoa, v_VCO_Veiculo);
                        }
                        else
                        {
                            string v_Comando = "$DSMPROBLEMA        TENTE NOVAMENTE 003#";
                            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 0, this.v_Credencial_Pessoa, v_Mensagem_Livre, (string)null, "L", nameof(JugarSaida));
                            new Enviar().IEnviar(v_Comando, this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                        }
                        if (this.v_Secao != "" && this.v_Secao != null)
                        {
                            this.t_TimeoutErroAplicacaoGuarda = new Thread(new ThreadStart(this.TimeOutErroAplicacaoGuarda));
                            this.t_TimeoutErroAplicacaoGuarda.Start();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro JugarSaida() ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
            }
            finally
            {
            }
        }

        public void TimeOutErroAplicacaoGuarda()
        {
            try
            {
                int num = 1;
                string vSecao = this.v_Secao;
                while (this.v_Secao != "" && this.v_Secao != null)
                {
                    if (num == 25)
                    {
                        new Enviar().IEnviar("$DSMTIME-OUT        ACESSO NEGADO   006#", this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                        Thread.Sleep(5000);
                        this.EncerraSecao(this.v_s_IP.ToString(), "SECAO ENCERRADA POR TIME-OUT", this.v_s_Porta_Envio, this.v_s_IP);
                        break;
                    }
                    if (!(vSecao == this.v_Secao))
                        break;
                    ++num;
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro TimeOutErroAplicacaoGuarda() ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
            }
        }

        public void DisparaWebAPI(string v_s_Parametros, string v_s_URL)
        {
            try
            {
                HttpClient httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(v_s_URL)
                };
                StringContent stringContent = new StringContent(v_s_Parametros, Encoding.UTF8, "application/json");
                HttpResponseMessage result = httpClient.PostAsync(v_s_URL, (HttpContent)stringContent).Result;
                httpClient.Dispose();
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro DisparaWebAPI() ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
            }
        }

        public async void Libera_Acesso_Registra_Lap(IPAddress v_IP, int v_Porta_Envio)
        {
            string v_ComandoSaida = "";
            this.v_s_Sentido = ConfigurationManager.AppSettings["SENTIDO_ACESSO"].ToString();
            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", nameof(Libera_Acesso_Registra_Lap), (string)null, this.v_s_Sentido, "IRegras");
            Thread.Sleep(Convert.ToInt32(ConfigurationManager.AppSettings["TEMPO_ESPERA_LAP"]));
            v_ComandoSaida = "$DSMCAPTURANDO      PLACA . . .     010#";
            new Enviar().IEnviar(v_ComandoSaida, v_IP, v_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
            new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", v_ComandoSaida, (string)null, this.v_s_Sentido, "IRegras");
            this.v_Placa = "";
            this.v_IdMotivacao = "";
            double v_IdOcr = 0.0;
            HttpClient client = new HttpClient();
            try
            {
                string v_LAPRequisicao = "{\"ambiente\":\"" + ConfigurationManager.AppSettings["AMBIENTE"] + "\",\"local\":\"" + Regras.v_s_Aplicacao + "\",\"pesagem\":\"\",\"idsecao\":\"" + this.v_Secao + "\"}";
                string uri = ConfigurationManager.AppSettings["WEBAPI_OCR"].ToString();
                string data = v_LAPRequisicao;
                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", uri + "-" + v_LAPRequisicao, (string)null, "E", "IRegras");
                StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
                HttpResponseMessage response = (HttpResponseMessage)null;
                response = await client.PostAsync(uri, (HttpContent)content);
                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "response:" + response.IsSuccessStatusCode.ToString(), (string)null, "E", "IRegras");
                if (response.IsSuccessStatusCode)
                {
                    JToken token = (JToken)JObject.Parse(response.Content.ReadAsStringAsync().Result);
                    object resultado = (object)token.SelectToken("Result");
                    Regras.retOcr retoc = new Regras.retOcr();
                    retoc = JsonConvert.DeserializeObject<Regras.retOcr>(resultado.ToString());
                    this.v_Placa = retoc.Placa;
                    v_IdOcr = (double)retoc.IdOcr;
                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "PLACA CAPTURADA:" + this.v_Placa, (string)null, "E", "IRegras");
                    token = (JToken)null;
                    resultado = (object)null;
                    retoc = (Regras.retOcr)null;
                    token = (JToken)null;
                    resultado = (object)null;
                    retoc = (Regras.retOcr)null;
                }
                v_LAPRequisicao = (string)null;
                uri = (string)null;
                data = (string)null;
                content = (StringContent)null;
                response = (HttpResponseMessage)null;
                v_LAPRequisicao = (string)null;
                uri = (string)null;
                data = (string)null;
                content = (StringContent)null;
                response = (HttpResponseMessage)null;
            }
            catch (Exception ex1)
            {
                Exception ex = ex1;
                this.v_Placa = "";
                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 30, "", "PLACA NÃO CAPTURADA", (string)null, "E", "IRegras");
                ex = (Exception)null;
            }
            new GravaAcesso().GravarAcesso(this.v_Credencial_Pessoa, Regras.v_Id_Equipamento, this.v_s_Sentido, Regras.v_s_Aplicacao, long.Parse(this.v_Secao), "P", "81", "N", (double)this.v_i_Score_LBF != 999.0 ? this.v_i_Score_LBF.ToString().PadLeft(5, '0') : "00000", Regras.v_Fator, this.v_IdMotivacao, "", "", "", "");
            this.Dispara_Liu();
            if (ConfigurationManager.AppSettings["CONTROLE_VAGAS"] == "1")
            {
                new GravaAcessoPatio().GravarAcessoPatio("E", 0, Regras.v_Id_Equipamento, Regras.v_s_Aplicacao, "", 0L, "", "", "", "", "", "", "");
                new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, "Atualizando Vagas", (string)null, "E", "Entrada");
            }
            this.EncerraSecao(this.v_s_IP.ToString(), "TERMINO DA SECAO", this.v_s_Porta_Envio, this.v_s_IP);
        }

        public void Dispara_Liu()
        {
            try
            {
                this.v_t_LIU = new Thread(new ThreadStart(this.LIU));
                if ((uint)this.v_t_LIU.ThreadState <= 0U)
                    return;
                this.v_t_LIU.Start();
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro dispara LIU() ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
            }
        }

        public void LIU()
        {
            try
            {
                this.LiuOk = 0;
                for (int index = 0; index <= 4 && this.LiuOk == 0; ++index)
                {
                    new Logar().ILogar(Regras.v_s_Aplicacao.ToString(), this.v_Secao, Regras.v_Id_Equipamento, 12, this.v_Credencial_Pessoa, "$LIU1#(" + (object)index + ")", (string)null, "E", "Liu");
                    new Enviar().IEnviar("$LIU1#", this.v_s_IP, this.v_s_Porta_Envio, Regras.v_s_Aplicacao, Regras.v_Id_Equipamento);
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                new GravaEventLog().GravarEventLog(Regras.v_s_Aplicacao, "Erro LIU() ID Controladora: " + Regras.v_Id_Equipamento.ToString() + " - " + Regras.v_s_Aplicacao + " - Erro: " + ex.Message, EventLogEntryType.Error, ex);
            }
            finally
            {
            }
        }

        public class retOcr
        {
            public string Placa { get; set; }

            public int Score { get; set; }

            public string DataHora { get; set; }

            public int IdOcr { get; set; }

            public string Imagem { get; set; }
        }

        public class retValidaPatioHeader
        {
            public Regras.retValidaPatioHeader.retValidaPatio Origem = new Regras.retValidaPatioHeader.retValidaPatio();

            private int statusCode { get; set; }

            private string statusMessage { get; set; }

            public class retValidaPatio
            {
                public string numOs { get; set; }

                public DateTime dtEmissao { get; set; }

                public string cnpjCliente { get; set; }

                public string nomeCliente { get; set; }

                public DateTime dtPrevisaoChegada { get; set; }

                public string placaCavalo { get; set; }

                public string placaCarreta1 { get; set; }

                public string placaCarreta2 { get; set; }

                public string tipoDocMotorista { get; set; }

                public string numDocMotorista { get; set; }

                public string nomeMotorista { get; set; }

                public string cnpjTransportadora { get; set; }

                public string nomeTransportadora { get; set; }

                public int codSituacaoVeiculo { get; set; }

                public string descrSituacaoVeiculo { get; set; }
            }
        }

        public class retMotivar
        {
            public int StatusCode { get; set; }

            public string IdMotivacaoTemporaria { get; set; }

            public string StatusMessage { get; set; }
        }
    }
}
