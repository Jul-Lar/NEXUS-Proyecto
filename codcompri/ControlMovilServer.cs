using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NexusForms
{
    public static class ControlMovilServer
    {
        private static HttpListener? listener;
        private static bool activo = false;

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYUP = 0x0002;

        private const byte VK_UP = 0x26;
        private const byte VK_DOWN = 0x28;
        private const byte VK_LEFT = 0x25;
        private const byte VK_RIGHT = 0x27;
        private const byte VK_RETURN = 0x0D;
        private const byte VK_SPACE = 0x20;
        private const byte VK_ESCAPE = 0x1B;
        private const byte VK_TAB = 0x09;
        private const byte VK_W = 0x57;
        private const byte VK_A = 0x41;
        private const byte VK_S = 0x53;
        private const byte VK_D = 0x44;

        public static void Iniciar(int puerto = 8085)
        {
            if (activo) return;
            activo = true;

            Task.Run(async () =>
            {
                try
                {
                    listener = new HttpListener();
                    listener.Prefixes.Add($"http://+:{puerto}/");
                    listener.Start();
                }
                catch
                {
                    listener = new HttpListener();
                    listener.Prefixes.Add($"http://localhost:{puerto}/");
                    listener.Start();
                }

                while (activo && listener != null && listener.IsListening)
                {
                    try
                    {
                        var ctx = await listener.GetContextAsync();
                        _ = Task.Run(() => ProcesarPeticion(ctx));
                    }
                    catch { }
                }
            });

            string ipLocal = ObtenerIpLocal();
            MessageBox.Show(
                $"Abre Chrome en tu celular (conectado al mismo Wi-Fi) y entra a:\n\nhttp://{ipLocal}:{puerto}",
                "NEXUS // Mando Móvil Listo"
            );
        }

        private static string ObtenerIpLocal()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }
            return "localhost";
        }

        private static void ProcesarPeticion(HttpListenerContext ctx)
        {
            string ruta = ctx.Request.Url?.AbsolutePath.ToLower() ?? "/";
            string accion = ctx.Request.QueryString["btn"] ?? "";
            string estado = ctx.Request.QueryString["state"] ?? "press";

            if (ruta == "/cmd" && !string.IsNullOrEmpty(accion))
            {
                EjecutarComando(accion, estado);
                byte[] ok = Encoding.UTF8.GetBytes("OK");
                ctx.Response.ContentType = "text/plain";
                ctx.Response.OutputStream.Write(ok, 0, ok.Length);
                ctx.Response.Close();
                return;
            }

            byte[] html = Encoding.UTF8.GetBytes(ObtenerHtmlMando());
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.OutputStream.Write(html, 0, html.Length);
            ctx.Response.Close();
        }

        private static void EjecutarComando(string btn, string state)
        {
            switch (btn)
            {
                case "up":
                    EnviarTecla(VK_UP, state);
                    EnviarTecla(VK_W, state);
                    break;
                case "down":
                    EnviarTecla(VK_DOWN, state);
                    EnviarTecla(VK_S, state);
                    break;
                case "left":
                    EnviarTecla(VK_LEFT, state);
                    EnviarTecla(VK_A, state);
                    break;
                case "right":
                    EnviarTecla(VK_RIGHT, state);
                    EnviarTecla(VK_D, state);
                    break;
                case "a":
                    EnviarTecla(VK_RETURN, state);
                    EnviarTecla(VK_SPACE, state);
                    break;
                case "b":
                    EnviarTecla(VK_ESCAPE, state);
                    break;
                case "tab":
                    EnviarTecla(VK_TAB, state);
                    break;
            }
        }

        private static void EnviarTecla(byte vk, string state)
        {
            if (state == "down")
                keybd_event(vk, 0, 0, UIntPtr.Zero);
            else if (state == "up")
                keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            else
            {
                keybd_event(vk, 0, 0, UIntPtr.Zero);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        private static string ObtenerHtmlMando()
        {
            return @"<!DOCTYPE html>
<html lang='es'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
<title>NEXUS // Mando Táctil</title>
<style>
  * { box-sizing: border-box; user-select: none; -webkit-user-select: none; touch-action: none; }
  body { margin: 0; background: #070b14; color: #00e5ff; font-family: monospace; display: flex; flex-direction: column; height: 100vh; overflow: hidden; }
  header { text-align: center; padding: 8px; background: #0d1626; border-bottom: 1px solid #00e5ff44; font-size: 14px; letter-spacing: 2px; }
  .pad-container { flex: 1; display: flex; justify-content: space-between; align-items: center; padding: 20px; }
  .dpad { display: grid; grid-template-columns: repeat(3, 68px); grid-template-rows: repeat(3, 68px); gap: 6px; }
  .btn { background: #102038; border: 2px solid #00e5ff; border-radius: 14px; color: #fff; font-size: 20px; font-weight: bold; display: flex; align-items: center; justify-content: center; box-shadow: 0 0 12px #00e5ff33; }
  .btn:active, .btn.active { background: #00e5ff; color: #050a14; transform: scale(0.94); }
  .actions { display: flex; gap: 18px; align-items: center; }
  .btn-round { width: 82px; height: 82px; border-radius: 50%; font-size: 18px; }
  .btn-a { border-color: #00ff88; box-shadow: 0 0 15px #00ff8844; }
  .btn-b { border-color: #ff3b5c; box-shadow: 0 0 15px #ff3b5c44; }
  .btn-tab { width: 68px; height: 68px; border-radius: 10px; font-size: 13px; border-color: #ffd700; color: #ffd700; }
</style>
</head>
<body>
  <header>NEXUS // CONTROL MÓVIL</header>
  <div class='pad-container'>
    <div class='dpad'>
      <div></div>
      <div class='btn' data-btn='up'>▲</div>
      <div></div>
      <div class='btn' data-btn='left'>◀</div>
      <div class='btn btn-tab' data-btn='tab'>TAB</div>
      <div class='btn' data-btn='right'>▶</div>
      <div></div>
      <div class='btn' data-btn='down'>▼</div>
      <div></div>
    </div>
    <div class='actions'>
      <div class='btn btn-round btn-b' data-btn='b'>ATRÁS</div>
      <div class='btn btn-round btn-a' data-btn='a'>OK / FIRE</div>
    </div>
  </div>
<script>
  function send(btn, state) {
    fetch(`/cmd?btn=${btn}&state=${state}`).catch(()=>{});
  }
  document.querySelectorAll('.btn').forEach(el => {
    const b = el.dataset.btn;
    el.addEventListener('touchstart', e => { e.preventDefault(); el.classList.add('active'); send(b, 'down'); });
    el.addEventListener('touchend', e => { e.preventDefault(); el.classList.remove('active'); send(b, 'up'); });
  });
</script>
</body>
</html>";
        }
    }
}