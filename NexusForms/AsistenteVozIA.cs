using System;
using System.Globalization;
using System.Linq;
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

                    reconocedor.BabbleTimeout = TimeSpan.FromSeconds(2.0);
                    reconocedor.InitialSilenceTimeout = TimeSpan.FromSeconds(4.5);
                    reconocedor.EndSilenceTimeout = TimeSpan.FromSeconds(0.9);
                    reconocedor.EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(1.1);

                    Choices nombres = new Choices(new string[]
                    {
                        "Julio", "Diego", "Carlos", "Luis", "Juan", "Pedro", "Miguel", "José", "Andrés",
                        "Jorge", "Fernando", "Ricardo", "Alejandro", "David", "Daniel", "Kevin", "Cristian",
                        "Brayan", "Sebastián", "Mateo", "Santiago", "Nicolás", "Gabriel", "Ángel", "Marco",
                        "María", "Ana", "Sofía", "Camila", "Valeria", "Lucía", "Andrea", "Paula", "Elena",
                        "Comandante", "Capitán", "Explorador", "Nexus", "Valkyrie", "Nova", "Kaelen"
                    });

                    Choices apellidosORangos = new Choices(new string[]
                    {
                        "Lara", "García", "Rodríguez", "Martínez", "López", "González", "Pérez", "Sánchez",
                        "Ramírez", "Torres", "Flores", "Rivera", "Gómez", "Díaz", "Reyes", "Morales",
                        "Cruz", "Ortiz", "Gutiérrez", "Chávez", "Ramos", "Vargas", "Castillo", "Mendoza",
                        "Alvarez", "Fernández", "Romero", "Herrera", "Medina", "Aguilar", "Rojas", "Silva",
                        "Alfa", "Omega", "Prime", "Cero", "Uno", "Élite"
                    });

                    // 1. Gramática de 1 palabra (ej: "Julio" o "Comandante")
                    GrammarBuilder gbNombreSolo = new GrammarBuilder(nombres) { Culture = cultura };
                    reconocedor.LoadGrammar(new Grammar(gbNombreSolo) { Weight = 1.0f });

                    // 2. Gramática de 2 palabras (ej: "Julio Lara" o "Comandante Julio")
                    GrammarBuilder gbNombreCompleto = new GrammarBuilder { Culture = cultura };
                    gbNombreCompleto.Append(nombres);
                    gbNombreCompleto.Append(apellidosORangos);
                    reconocedor.LoadGrammar(new Grammar(gbNombreCompleto) { Weight = 0.95f });

                    // 3. Dictado libre de respaldo con bajo peso (sin Priority, ya que Windows no lo permite en DictationGrammar)
                    reconocedor.LoadGrammar(new DictationGrammar() { Weight = 0.25f });

                    RecognitionResult? resultado = reconocedor.Recognize(TimeSpan.FromSeconds(5));

                    if (resultado != null && !string.IsNullOrWhiteSpace(resultado.Text) && resultado.Confidence >= 0.42f)
                    {
                        string texto = CapitalizarPalabras(resultado.Text.Trim().TrimEnd('.', ',', ';', '!', '?'));

                        alRecibirTexto(texto);
                        ResponderConVoz($"Bienvenido a Nexus, {texto}.");
                    }
                    else
                    {
                        ResponderConVoz("No escuché con claridad. Acércate al micrófono e intenta de nuevo.");
                    }
                }
                catch (Exception ex)
                {
                    alError?.Invoke($"Detalle técnico del sensor: {ex.Message}");
                }
                finally
                {
                    escuchando = false;
                    alCambiarEstado?.Invoke("🎤 VOZ", false);
                }
            });
        }

        private static string CapitalizarPalabras(string entrada)
        {
            if (string.IsNullOrWhiteSpace(entrada)) return entrada;
            var palabras = entrada.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => char.ToUpper(p[0]) + (p.Length > 1 ? p.Substring(1).ToLower() : ""));
            return string.Join(" ", palabras);
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
                    reconocedor.LoadGrammar(new Grammar(gbComandos) { Weight = 0.9f });
                    reconocedor.LoadGrammar(new DictationGrammar() { Weight = 0.3f });

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