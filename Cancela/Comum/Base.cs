// Decompiled with JetBrains decompiler
// Type: Comum.Base
// Assembly: Comum, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 99DD0288-5D71-4E3E-8396-2C9790D917B3
// Assembly location: C:\Users\janlo\Desktop\G04CNC01\DLL_Acesso_Cancela\DLL_Acesso_Cancela\lib\Comum.dll

using Cancela;

namespace Comum
{
  public class Base : AcessoDados
  {
    public static bool Envio_Lista_Iniciado = false;

    public static string v_UltimoComandoLista { get; set; }

    public static string v_ComandoPing { get; set; }

    public static int ContaTestePing { get; set; }

    public static bool ExecutaTestePing { get; set; }
  }
}
