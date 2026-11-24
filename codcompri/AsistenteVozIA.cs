using System;
using System.Globalization;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace NexusForms
{
    public static class AsistenteVozIA
    {
        private static SpeechRecognitionEngine? reconocedor;
        private static SpeechSynthesizer? sintetizador;
        private static bool escuchando = false;

        public static Func<string>? ObtenerContextoJuegoActual { get; set; }
        public static Action<string, int>? MostrarSubtituloEnJuego { get; set; }

        public static bool EstaEscuchando => escuchando;

        public static void Inicializar()
        {
            if (sintetizador != null) return;
            try
            {
                sintetizador = new SpeechSynthesizer();
                sintetizador.SetOutputToDefaultAudioDevice();
                sintetizador.Rate = 1;
            }
            catch { }
        }

        private static CultureInfo ObtenerCulturaEspanol()
        {
            CultureInfo cultura = new CultureInfo("es-ES");
            try
            {
                foreach (var ri in SpeechRecognitionEngine.InstalledRecognizers())
                {
                    if (ri.Culture.TwoLetterISOLanguageName.Equals("es", StringComparison.OrdinalIgnoreCase))
                    {
                        cultura = ri.Culture;
                        break;
                    }
                }
            }
            catch { }
            return cultura;
        }

        public static void DictarTextoParaControl(
            Action<string> alRecibirTexto,
            Action<string, bool>? alCambiarEstado = null,
            Action<string>? alError = null)
        {
            if (escuchando) return;
            Inicializar();
            escuchando = true;
            alCambiarEstado?.Invoke("🔴 HABLA PC", true);

            Task.Run(() =>
            {
                try
                {
                    CultureInfo cultura = ObtenerCulturaEspanol();
                    reconocedor?.Dispose();
                    reconocedor = new SpeechRecognitionEngine(cultura);
                    reconocedor.SetInputToDefaultAudioDevice();

                    Choices nombres = new Choices(new string[]
                    {
                        "Julio", "Diego", "Carlos", "Luis", "Juan", "Pedro", "Miguel", "José", "Andrés",
                        "Jorge", "Fernando", "Ricardo", "Alejandro", "David", "Daniel", "Kevin", "Cristian",
                        "Brayan", "Sebastián", "Mateo", "Santiago", "Nicolás", "Gabriel", "Ángel", "Marco",
                        "María", "Ana", "Sofía", "Camila", "Valeria", "Lucía", "Andrea", "Paula", "Elena",
                        "Comandante", "Capitán", "Explorador", "Nexus", "Valkyrie", "Nova", "Kaelen"
                    });

                    GrammarBuilder gbNombres = new GrammarBuilder(nombres) { Culture = cultura };
                    reconocedor.LoadGrammar(new Grammar(gbNombres) { Weight = 0.8f });
                    reconocedor.LoadGrammar(new DictationGrammar() { Weight = 0.4f });

                    reconocedor.BabbleTimeout = TimeSpan.FromSeconds(1.5);
                    reconocedor.InitialSilenceTimeout = TimeSpan.FromSeconds(4);
                    reconocedor.EndSilenceTimeout = TimeSpan.FromSeconds(1.0);

                    RecognitionResult? resultado = reconocedor.Recognize(TimeSpan.FromSeconds(5));

                    if (resultado != null && !string.IsNullOrWhiteSpace(resultado.Text))
                    {
                        string texto = resultado.Text.Trim().TrimEnd('.', ',');
                        if (texto.Length > 0)
                            texto = char.ToUpper(texto[0]) + texto.Substring(1);

                        alRecibirTexto(texto);
                        ResponderConVoz($"Bienvenido a Nexus, {texto}.");
                    }
                    else
                    {
                        ResponderConVoz("No detecté ningún nombre. Intenta de nuevo.");
                    }
                }
                catch
                {
                    alError?.Invoke("Verifica que el micrófono de tu PC esté conectado y habilitado en Windows.");
                }
                finally
                {
                    escuchando = false;
                    alCambiarEstado?.Invoke("🎤 VOZ", false);
                }
            });
        }

        public static void ActivarEscuchaPorMando()
        {
            if (escuchando) return;
            Inicializar();
            escuchando = true;

            MostrarSubtituloEnJuego?.Invoke("🎙 IA NEXUS ESCUCHANDO... (Pregunta o pide tu objetivo)", 240);

            Task.Run(() =>
            {
                try
                {
                    CultureInfo cultura = ObtenerCulturaEspanol();
                    reconocedor?.Dispose();
                    reconocedor = new SpeechRecognitionEngine(cultura);
                    reconocedor.SetInputToDefaultAudioDevice();

                    Choices comandosRapidos = new Choices(new string[]
                    {
                        "qué tengo que hacer",
                        "cuál es mi objetivo",
                        "dime qué hago",
                        "ayuda",
                        "cómo voy",
                        "estado de la nave",
                        "estado del explorador",
                        "dónde están las gemas",
                        "qué arma uso"
                    });

                    GrammarBuilder gbComandos = new GrammarBuilder(comandosRapidos) { Culture = cultura };
                    reconocedor.LoadGrammar(new Grammar(gbComandos));
                    reconocedor.LoadGrammar(new DictationGrammar());

                    reconocedor.InitialSilenceTimeout = TimeSpan.FromSeconds(4);
                    reconocedor.EndSilenceTimeout = TimeSpan.FromSeconds(1.2);

                    RecognitionResult? resultado = reconocedor.Recognize(TimeSpan.FromSeconds(6));

                    if (resultado != null && !string.IsNullOrWhiteSpace(resultado.Text))
                    {
                        string textoUsuario = resultado.Text;
                        MostrarSubtituloEnJuego?.Invoke($"🗣 TÚ: \"{textoUsuario}\"", 150);
                        string respuesta = ProcesarConsultaIA(textoUsuario);
                        ResponderConVoz(respuesta);
                    }
                    else
                    {
                        string respuestaAuto = ProcesarConsultaIA("qué tengo que hacer");
                        ResponderConVoz(respuestaAuto);
                    }
                }
                catch
                {
                    string fallback = ProcesarConsultaIA("qué tengo que hacer");
                    ResponderConVoz(fallback);
                }
                finally
                {
                    escuchando = false;
                }
            });
        }

        private static string ProcesarConsultaIA(string pregunta)
        {
            string p = pregunta.ToLowerInvariant();
            string contexto = ObtenerContextoJuegoActual?.Invoke()
                ?? "Estás en la cabina principal de NEXUS listo para iniciar una misión.";

            if (p.Contains("hacer") || p.Contains("objetivo") || p.Contains("ayuda") || p.Contains("gema"))
                return contexto;

            if (p.Contains("estado") || p.Contains("cómo voy") || p.Contains("vida"))
                return $"Reporte táctico: {contexto}";

            return $"Entendido. {contexto}";
        }

        public static void ResponderConVoz(string mensaje)
        {
            MostrarSubtituloEnJuego?.Invoke($"🤖 IA NEXUS: {mensaje}", 300);
            Task.Run(() =>
            {
                try
                {
                    Inicializar();
                    sintetizador?.SpeakAsyncCancelAll();
                    sintetizador?.SpeakAsync(mensaje);
                }
                catch { }
            });
        }

        public static void Liberar()
        {
            try
            {
                reconocedor?.Dispose();
                sintetizador?.SpeakAsyncCancelAll();
                sintetizador?.Dispose();
            }
            catch { }
        }
    }
}