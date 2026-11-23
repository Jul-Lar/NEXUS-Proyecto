using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NexusForms
{
    public static class AudioManager
    {
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern int mciSendString(string command, StringBuilder? buffer, int bufferSize, IntPtr hwndCallback);

        public static bool MusicaActiva { get; private set; } = true;
        private static bool mciCargado = false;
        private static SoundPlayer? wavPlayer;

        public static void IniciarMusica()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] candidatos = Directory.GetFiles(baseDir, "musica*.*");

                if (candidatos.Length == 0)
                {
                    MusicaActiva = false;
                    return;
                }

                DetenerMusica();

                // 1. Intentar primero reproducir como WAV nativo con SoundPlayer
                foreach (string archivo in candidatos)
                {
                    try
                    {
                        wavPlayer = new SoundPlayer(archivo);
                        wavPlayer.Load();
                        wavPlayer.PlayLooping();
                        MusicaActiva = true;
                        return;
                    }
                    catch
                    {
                        wavPlayer?.Dispose();
                        wavPlayer = null;
                    }
                }

                // 2. Si es MP3 (o WAV comprimido), abrir con MCI de Windows
                foreach (string archivo in candidatos)
                {
                    int err = mciSendString($"open \"{archivo}\" type mpegvideo alias NexusBGM", null, 0, IntPtr.Zero);
                    if (err != 0)
                        err = mciSendString($"open \"{archivo}\" alias NexusBGM", null, 0, IntPtr.Zero);

                    if (err == 0)
                    {
                        mciSendString("setaudio NexusBGM volume to 450", null, 0, IntPtr.Zero);
                        mciSendString("play NexusBGM repeat", null, 0, IntPtr.Zero);
                        mciCargado = true;
                        MusicaActiva = true;
                        return;
                    }
                }

                MusicaActiva = false;
            }
            catch
            {
                MusicaActiva = false;
            }
        }

        public static void DetenerMusica()
        {
            try
            {
                wavPlayer?.Stop();
                wavPlayer?.Dispose();
                wavPlayer = null;

                if (mciCargado)
                {
                    mciSendString("stop NexusBGM", null, 0, IntPtr.Zero);
                    mciSendString("close NexusBGM", null, 0, IntPtr.Zero);
                    mciCargado = false;
                }
            }
            catch { }
        }

        public static void AlternarMusica()
        {
            if (MusicaActiva)
            {
                DetenerMusica();
                MusicaActiva = false;
            }
            else
            {
                IniciarMusica();
            }
        }

        public static void CargarFondoMapa(System.Windows.Forms.Panel pnlMapa, string sector)
        {
            if (pnlMapa == null) return;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string nombreArchivo = sector.ToLower().Split(' ')[0] switch
            {
                "iris-07" => "fondo_iris.jpg",
                "nebula-x9" => "fondo_nebula.jpg",
                "void-00" => "fondo_void.jpg",
                "aether-4" => "fondo_aether.jpg",
                _ => ""
            };

            if (string.IsNullOrEmpty(nombreArchivo))
            {
                pnlMapa.BackgroundImage = null;
                return;
            }

            string rutaFondo = Path.Combine(baseDir, nombreArchivo);
            if (File.Exists(rutaFondo))
            {
                try
                {
                    pnlMapa.BackgroundImage?.Dispose();
                    pnlMapa.BackgroundImage = Image.FromFile(rutaFondo);
                    pnlMapa.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
                }
                catch
                {
                    pnlMapa.BackgroundImage = null;
                }
            }
            else
            {
                pnlMapa.BackgroundImage = null;
            }
        }
    }

    public static class ServicioIA
    {
        private static readonly string ApiKey = "TU_API_KEY_AQUI";
        private static readonly HttpClient client = CrearClienteRapido();
        private static readonly Random randIA = new Random();
        private static readonly object lockVoz = new object();

        private static readonly string[] Modelos = new[]
        {
            "gemini-3.8-flash",
            "gemini-3.5-flash-lite"
        };

        private static HttpClient CrearClienteRapido()
        {
            var handler = new SocketsHttpHandler
            {
                ConnectCallback = async (context, cancellationToken) =>
                {
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.NoDelay = true;
                    try
                    {
                        await socket.ConnectAsync(context.DnsEndPoint.Host, context.DnsEndPoint.Port, cancellationToken);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }
            };

            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public static async Task<string> ConsultarGeminiAsync(Jugador j, string pregunta)
        {
            string clave = ApiKey.Trim();
            string contexto = $"Eres IRIS, la IA de a bordo de la nave {j.NaveAsignada} en el juego NEXUS. " +
                              $"Piloto: {j.Nombre}, Sector: {j.RealidadAsignada}, Energía: {j.NivelEnergia}%, " +
                              $"Estabilidad: {j.NivelEstabilidad}%, Peligro: {j.NivelPeligro}%, Fragmentos: {j.FragmentosDatos}. " +
                              $"Responde en español, con tono táctico de ciencia ficción y en máximo 2 oraciones. " +
                              $"Consulta del piloto: {pregunta}";

            foreach (string modelo in Modelos)
            {
                try
                {
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelo}:generateContent";

                    object payload = modelo.Contains("lite")
                        ? new
                        {
                            contents = new[] { new { parts = new[] { new { text = contexto } } } },
                            generationConfig = new { maxOutputTokens = 150 }
                        }
                        : new
                        {
                            contents = new[] { new { parts = new[] { new { text = contexto } } } },
                            generationConfig = new
                            {
                                thinkingConfig = new { thinkingBudget = 0 },
                                maxOutputTokens = 150
                            }
                        };

                    string json = JsonSerializer.Serialize(payload);

                    using var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Add("x-goog-api-key", clave);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    using var response = await client.SendAsync(request);
                    string body = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(body);
                        string? respuesta = doc.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();

                        if (!string.IsNullOrWhiteSpace(respuesta))
                            return respuesta.Trim();
                    }
                }
                catch { }
            }

            return GenerarRespuestaTacticaLocal(j, pregunta);
        }

        private static string GenerarRespuestaTacticaLocal(Jugador j, string pregunta)
        {
            string q = pregunta.ToLower();

            if (q.Contains("donde") || q.Contains("ubicacion") || q.Contains("sector") || q.Contains("estoy"))
                return $"[ENLACE CON INTERFERENCIAS] Comandante {j.Nombre}, orbitas actualmente el sector {j.RealidadAsignada} a bordo de la nave {j.NaveAsignada}.";

            if (q.Contains("energia") || q.Contains("estado") || q.Contains("audita") || q.Contains("reporte"))
                return $"[ENLACE CON INTERFERENCIAS] Telemetría de {j.NaveAsignada}: Energía al {j.NivelEnergia}%, Estabilidad al {j.NivelEstabilidad}% y Peligro en {j.NivelPeligro}%.";

            if (q.Contains("mision") || q.Contains("objetivo") || q.Contains("hacer"))
                return $"[ENLACE CON INTERFERENCIAS] Directivas activas en {j.RealidadAsignada}: explora el perímetro, recolecta fragmentos ({j.FragmentosDatos} actuales) y neutraliza anomalías hostiles.";

            if (q.Contains("hola") || q.Contains("iris"))
                return $"[ENLACE CON INTERFERENCIAS] Sistemas de IRIS operativos con ruido subespacial, Comandante {j.Nombre}. Escudos {(j.EscudosActivos ? "activados" : "en espera")}; espero tus órdenes.";

            string[] respuestas = new[]
            {
                $"[ENLACE CON INTERFERENCIAS] Señal cuántica fluctuante en {j.RealidadAsignada}, Comandante {j.Nombre}. Sensores de la {j.NaveAsignada} en alerta.",
                $"[ENLACE CON INTERFERENCIAS] Canal subespacial degradado, pero la telemetría local confirma energía al {j.NivelEnergia}% y estabilidad al {j.NivelEstabilidad}%.",
                $"[ENLACE CON INTERFERENCIAS] Interferencia electromagnética detectada en el núcleo externo. Recomiendo conservar energía y vigilar el nivel de peligro ({j.NivelPeligro}%)."
            };

            return respuestas[randIA.Next(respuestas.Length)];
        }

        public static void HablarAsync(string texto)
        {
            Task.Run(() =>
            {
                lock (lockVoz)
                {
                    try
                    {
                        string textoLimpio = texto
                            .Replace("[ENLACE CON INTERFERENCIAS]", "")
                            .Replace("[SISTEMA]", "")
                            .Trim();

                        using var synth = new SpeechSynthesizer();
                        synth.SetOutputToDefaultAudioDevice();
                        synth.Volume = 100;
                        synth.Rate = 1;

                        foreach (var voz in synth.GetInstalledVoices())
                        {
                            if (voz.Enabled && voz.VoiceInfo.Culture.TwoLetterISOLanguageName == "es")
                            {
                                synth.SelectVoice(voz.VoiceInfo.Name);
                                break;
                            }
                        }

                        synth.Speak(textoLimpio);
                    }
                    catch { }
                }
            });
        }
    }

    public abstract class Personaje
    {
        public string Nombre { get; set; } = "Explorador";
        public int Salud { get; set; } = 100;
    }

    public class EntidadSistema : Personaje
    {
        public string Tipo { get; set; } = "Anomalía";
        public int NivelAmenaza { get; set; } = 1;
    }

    public class Robot : EntidadSistema
    {
        public Robot()
        {
            Nombre = "Centinela Autónomo KX-4";
            Tipo = "Dron de Defensa";
            Salud = 90;
            NivelAmenaza = 2;
        }
    }

    public class Enemigo : EntidadSistema
    {
        public Enemigo()
        {
            Nombre = "Espectro del Vacío";
            Tipo = "Entidad Hostil";
            Salud = 110;
            NivelAmenaza = 3;
        }
    }

    public class EstadoMision
    {
        public bool ZonaAlcanzada { get; set; } = false;
        public bool NucleoEncontrado { get; set; } = false;
        public bool EntidadSobrevivida { get; set; } = false;
    }

    public class Jugador : Personaje
    {
        public string RealidadAsignada { get; set; } = "IRIS-07";
        public string NaveAsignada { get; set; } = "Caza Estelar V-9";
        public Color ColorTema { get; set; } = Color.Cyan;
        public bool EsElite { get; set; } = true;

        public int NivelEnergia { get; set; } = 100;
        public int NivelEstabilidad { get; set; } = 100;
        public int NivelPeligro { get; set; } = 0;
        public bool EscudosActivos { get; set; } = false;

        public int FragmentosDatos { get; set; } = 0;
        public int XP { get; set; } = 0;

        public EstadoMision MisionActual { get; set; } = new EstadoMision();

        public Dictionary<string, int> Inventario { get; set; } = new Dictionary<string, int>
        {
            { "Células de energía", 3 },
            { "Analizadores", 2 },
            { "Módulos de ataque", 2 },
            { "Escudos", 1 }
        };

        public void LimitarAtributos()
        {
            NivelEnergia = Math.Clamp(NivelEnergia, 0, 100);
            NivelEstabilidad = Math.Clamp(NivelEstabilidad, 0, 100);
            NivelPeligro = Math.Clamp(NivelPeligro, 0, 100);
        }
    }
}