using System;
using System.IO;
using System.Threading;
using System.Media;
using System.Text;
using System.Speech.Synthesis;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace SimuladorNexus
{
    public static class ServicioIA
    {
        private static SpeechSynthesizer voz = new SpeechSynthesizer();
        private static readonly HttpClient clienteHttp = new HttpClient();

        private const string apiKey = "AQ.Ab8RN6JUcOVR7P-_KEoKelF7NaHuu6xUqxNtf2Wk1b_sk8Svvw";
        private const string modeloIA = "gemini-3.8-flash";

        static ServicioIA()
        {
            try
            {
                voz.SetOutputToDefaultAudioDevice();
                voz.Rate = 0;
                voz.Volume = 100;

                voz.SpeakCompleted += (s, e) => {
                    Program.ReproductorMusica?.PlayLooping();
                };
            }
            catch { }
        }

        public static void HablarAsync(string texto)
        {
            try
            {
                Program.ReproductorMusica?.Stop();
                voz.SpeakAsyncCancelAll();
                voz.SpeakAsync(texto);
            }
            catch { }
        }

        public static void HablarSincrono(string texto)
        {
            try
            {
                voz.Speak(texto);
            }
            catch { }
        }

        private static async Task<HttpResponseMessage> EnviarPeticionGeminiAsync(string systemPrompt, string userPrompt)
        {
            var cuerpoSolicitud = new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[] { new { parts = new[] { new { text = userPrompt } } } }
            };

            string jsonString = JsonSerializer.Serialize(cuerpoSolicitud);

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modeloIA}:generateContent?key={apiKey}";

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonString, Encoding.UTF8, "application/json")
            };

            return await clienteHttp.SendAsync(request);
        }

        public static string GenerarRespuestaTactica(Jugador player, string consultaUsuario)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return "Error: Configura tu API Key válida en la variable apiKey.";

            try
            {
                Task<string> tareaIA = Task.Run(() => ConsultarGemini(player, consultaUsuario));
                tareaIA.Wait();
                return tareaIA.Result;
            }
            catch (Exception ex)
            {
                return $"Error de ejecución: {ex.InnerException?.Message ?? ex.Message}";
            }
        }

        private static async Task<string> ConsultarGemini(Jugador player, string consultaUsuario)
        {
            string systemPrompt = $@"Eres IRIS, la IA táctica del traje espacial del explorador {player.Nombre}. 
            Tu realidad asignada es {player.RealidadAsignada}.
            Responde siempre en español, con tono militar, conciso y protector (máximo 2 líneas breves).
            Datos de telemetría actuales: 
            Energía: {player.NivelEnergia}% | Estabilidad: {player.NivelEstabilidad}% | Peligro: {player.NivelPeligro}% | Escudos: {(player.EscudosActivos ? "ACTIVOS" : "DESACTIVADOS")}.
            Adapta tu respuesta a estos niveles.";

            HttpResponseMessage respuesta = await EnviarPeticionGeminiAsync(systemPrompt, consultaUsuario);

            if (respuesta.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return "SISTEMA SATURADO: Se ha alcanzado el límite de peticiones de la IA (Error 429). Por favor espera 1 minuto.";
            }
            if (respuesta.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                return "SISTEMA IRIS: Interferencia cuántica en la señal, intenta más tarde.";
            }

            string jsonRespuesta = await respuesta.Content.ReadAsStringAsync();

            if (respuesta.IsSuccessStatusCode)
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonRespuesta))
                {
                    return doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text").GetString().Trim();
                }
            }
            else
            {
                return $"[Error HTTP {(int)respuesta.StatusCode}]: {jsonRespuesta}";
            }
        }

        public static string CrearMisionDinamicaIA(Jugador player)
        {
            try
            {
                Task<string> tarea = Task.Run(() => GenerarMisionJSON(player));
                tarea.Wait();
                return tarea.Result;
            }
            catch (Exception ex)
            {
                return $"FALLO_IA: {ex.Message}";
            }
        }

        private static async Task<string> GenerarMisionJSON(Jugador player)
        {
            string systemPrompt = @"Eres el motor de creación procedimental de NEXUS. Genera un evento único para el jugador.
            Debes responder ÚNICAMENTE en formato JSON plano con la siguiente estructura estricta:
            {
              ""titulo"": ""Nombre corto de la misión"",
              ""descripcion"": ""Breve narrativa del evento (máximo 2 frases)"",
              ""cambioEnergia"": 5,
              ""cambioEstabilidad"": -10,
              ""cambioPeligro"": 10,
              ""fragmentosGanados"": 2
            }";

            string promptUsuario = $"Genera un escenario adaptado al jugador {player.Nombre} en la realidad {player.RealidadAsignada} con Peligro actual al {player.NivelPeligro}%.";

            HttpResponseMessage respuesta = await EnviarPeticionGeminiAsync(systemPrompt, promptUsuario);

            if (respuesta.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return "{\"titulo\": \"Saturación Cuántica (429)\", \"descripcion\": \"Límite de peticiones de IA alcanzado. Por favor espera 1 minuto antes de solicitar otro evento.\", \"cambioEnergia\": 0, \"cambioEstabilidad\": 0, \"cambioPeligro\": 0, \"fragmentosGanados\": 0}";
            }
            if (respuesta.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                return "{\"titulo\": \"Interferencia (503)\", \"descripcion\": \"SISTEMA IRIS: Interferencia cuántica en la señal, intenta más tarde.\", \"cambioEnergia\": 0, \"cambioEstabilidad\": 0, \"cambioPeligro\": 0, \"fragmentosGanados\": 0}";
            }

            string jsonRespuesta = await respuesta.Content.ReadAsStringAsync();

            if (respuesta.IsSuccessStatusCode)
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonRespuesta))
                {
                    string textoGenerado = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text").GetString().Trim();

                    int inicio = textoGenerado.IndexOf('{');
                    int fin = textoGenerado.LastIndexOf('}');
                    if (inicio != -1 && fin != -1)
                    {
                        return textoGenerado.Substring(inicio, fin - inicio + 1);
                    }
                    return textoGenerado;
                }
            }
            return $"ERROR: {(int)respuesta.StatusCode} - {jsonRespuesta}";
        }

        public static string ValidarYAutocorregirSistemaIA(Jugador player)
        {
            try
            {
                Task<string> tarea = Task.Run(() => AuditarSistemaJSON(player));
                tarea.Wait();
                return tarea.Result;
            }
            catch (Exception ex)
            {
                return $"Error de auditoría: {ex.Message}";
            }
        }

        private static async Task<string> AuditarSistemaJSON(Jugador player)
        {
            string systemPrompt = $@"Eres el Validador de Integridad Cuántica de NEXUS. Analiza el estado del objeto Jugador:
            Nombre: {player.Nombre}
            Energía: {player.NivelEnergia}%
            Estabilidad: {player.NivelEstabilidad}%
            Peligro: {player.NivelPeligro}%
            Escudos: {player.EscudosActivos}
            Rango Élite: {player.EsElite}
            
            Evalúa la integridad del sistema y proporciona un diagnóstico técnico formal (máximo 3 líneas) explicando si el sistema es estable o si requiere corrección de parámetros.";

            HttpResponseMessage respuesta = await EnviarPeticionGeminiAsync(systemPrompt, "Ejecutar auditoría de integridad de código y estado de memoria.");

            if (respuesta.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return "AUDITORÍA RECHAZADA: Límite de cuota API excedido (Error 429). Espera 1 minuto antes de volver a auditar.";
            }
            if (respuesta.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                return "AUDITORÍA RECHAZADA: Interferencia cuántica en la señal (Error 503). Intenta más tarde.";
            }

            string jsonRespuesta = await respuesta.Content.ReadAsStringAsync();

            if (respuesta.IsSuccessStatusCode)
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonRespuesta))
                {
                    return doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text").GetString().Trim();
                }
            }
            return $"Error de diagnóstico: {(int)respuesta.StatusCode} - {jsonRespuesta}";
        }
    }

    public abstract class Personaje
    {
        public string Nombre { get; set; }
    }

    public class EntidadSistema : Personaje
    {
        public string Tipo { get; set; }
        public int Salud { get; set; }
    }

    public class Robot : EntidadSistema
    {
        public Robot() { Nombre = "Centinela Defensivo"; Tipo = "Robot"; Salud = 80; }
    }

    public class Enemigo : EntidadSistema
    {
        public Enemigo() { Nombre = "Sombra Entrópica"; Tipo = "Enemigo"; Salud = 120; }
    }

    public class EstadoMision
    {
        public bool ZonaAlcanzada { get; set; } = false;
        public bool NucleoEncontrado { get; set; } = false;
        public bool EntidadSobrevivida { get; set; } = false;
        public bool RegresoExtraccion { get; set; } = false;
        public bool RecompensasEntregadas { get; set; } = false;

        public bool EsCompletada()
        {
            return ZonaAlcanzada && NucleoEncontrado && EntidadSobrevivida && RegresoExtraccion;
        }
    }

    public class Jugador : Personaje
    {
        public string RealidadAsignada { get; set; } = "Desconocida";
        public int NivelEnergia { get; set; } = 100;
        public int NivelEstabilidad { get; set; } = 100;
        public int NivelPeligro { get; set; } = 0;
        public int FragmentosDatos { get; set; } = 0;
        public bool EscudosActivos { get; set; } = false;
        public bool EsElite { get; set; } = false;
        public bool Conectado { get; set; } = true;

        public List<string> Logros { get; set; } = new List<string>();
        public int AnomaliasHackeadas { get; set; } = 0;
        public int EventosSobrevividos { get; set; } = 0;

        public int XP { get; set; } = 0;
        public Dictionary<string, int> Inventario { get; set; } = new Dictionary<string, int>
        {
            { "Células de energía", 2 },
            { "Escudos", 1 },
            { "Módulos de ataque", 1 },
            { "Analizadores", 1 }
        };
        public EstadoMision MisionActual { get; set; } = new EstadoMision();

        public Jugador()
        {
            Nombre = "Cadete";
        }

        public void LimitarAtributos()
        {
            if (NivelPeligro < 0) NivelPeligro = 0;
            if (NivelPeligro > 100) NivelPeligro = 100;
            if (NivelEnergia < 0) NivelEnergia = 0;
            if (NivelEnergia > 100) NivelEnergia = 100;
            if (NivelEstabilidad < 0) NivelEstabilidad = 0;
            if (NivelEstabilidad > 100) NivelEstabilidad = 100;
        }
    }

    class Program
    {
        static Jugador player = new Jugador();
        static Random rand = new Random();

        public static SoundPlayer ReproductorMusica;
        static ConsoleColor TemaInterfaz = ConsoleColor.Cyan;
        static DateTime ultimaTransmision = DateTime.Now;

        static void SFX_Navegacion() { try { Console.Beep(800, 50); } catch { } }
        static void SFX_Seleccion() { try { Console.Beep(1200, 100); } catch { } }
        static void SFX_Error() { try { Console.Beep(300, 200); Console.Beep(250, 200); } catch { } }
        static void SFX_Alarma() { try { for (int i = 0; i < 3; i++) { Console.Beep(1500, 300); Thread.Sleep(100); } } catch { } }
        static void SFX_Logro() { try { Console.Beep(500, 100); Console.Beep(700, 100); Console.Beep(1000, 200); } catch { } }

        static void EscribirTypewriter(string texto, ConsoleColor color = ConsoleColor.White, int velocidad = 20)
        {
            Console.ForegroundColor = color;
            foreach (char c in texto)
            {
                Console.Write(c);
                Thread.Sleep(velocidad);
            }
            Console.WriteLine();
            Console.ResetColor();
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            CargarMusicaEnMemoria();
            MostrarSplashEstudio();
            MostrarIntroduccion();
            RecopilarDatosUsuario();

            if (EvaluarIngreso())
            {
                BuclePrincipalSimulacion();
            }

            MostrarCierre();
        }

        static void MostrarSplashEstudio()
        {
            Console.Clear();
            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.DarkGray;

            Console.WriteLine("\n\n\n\n\n\n\n\n");
            Console.WriteLine("                                                   N E X U S                 ");

            ServicioIA.HablarSincrono("Bienvenido a Nexus");

            Thread.Sleep(800);
            Console.Clear();
        }

        static void BuclePrincipalSimulacion()
        {
            int indiceSeleccionado = 0;
            Console.CursorVisible = false;

            while (player.Conectado)
            {
                string[] opciones = {
                    "Explorar realidad",
                    "Analizar anomalía",
                    "Alternar Escudos de Defensa",
                    "Recuperar energía",
                    "Consola IRIS",
                    "MAPAS DE REALIDADES",
                    player.EsElite ? "Purgar Peligro y Restaurar Sistema ★" : "[Bloqueado - Requiere Rango Élite]",
                    "Modo Multijugador",
                    "Generar Misión Dinámica",
                    "Validar & Auditar Sistema por IA",
                    "Administrar Inventario",
                    "Estado de Misión: La Anomalía Perdida",
                    "Intentar desconexión",
                    "Ajustes de Perfil y Temas"
                };

                bool opcionElegida = false;
                Console.Clear();

                while (!opcionElegida)
                {
                    Console.SetCursorPosition(0, 0);
                    MostrarInterfazUsuario();

                    for (int i = 0; i < opciones.Length; i++)
                    {
                        if (i == indiceSeleccionado)
                        {
                            Console.BackgroundColor = ConsoleColor.White;
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.WriteLine($" > {opciones[i].PadRight(62)}");
                            Console.ResetColor();
                        }
                        else
                        {
                            if (i == 4) Console.ForegroundColor = ConsoleColor.Magenta;
                            else if (i == 5) Console.ForegroundColor = TemaInterfaz;
                            else if (i == 6) Console.ForegroundColor = player.EsElite ? ConsoleColor.Yellow : ConsoleColor.DarkGray;
                            else if (i == 7) Console.ForegroundColor = ConsoleColor.Green;
                            else if (i == 8 || i == 9) Console.ForegroundColor = ConsoleColor.Cyan;
                            else if (i == 10 || i == 11) Console.ForegroundColor = ConsoleColor.Yellow;
                            else if (i == 13) Console.ForegroundColor = ConsoleColor.DarkYellow;
                            else Console.ForegroundColor = ConsoleColor.White;

                            Console.WriteLine($"   {opciones[i].PadRight(62)}");
                            Console.ResetColor();
                        }
                    }

                    GenerarTransmisionAleatoria();

                    DateTime inicioEspera = DateTime.Now;
                    while (!Console.KeyAvailable && (DateTime.Now - inicioEspera).TotalSeconds < 1)
                    {
                        Thread.Sleep(50);
                    }

                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo tecla = Console.ReadKey(true);

                        if (tecla.Key == ConsoleKey.UpArrow)
                        {
                            SFX_Navegacion();
                            indiceSeleccionado--;
                            if (indiceSeleccionado < 0) indiceSeleccionado = opciones.Length - 1;
                        }
                        else if (tecla.Key == ConsoleKey.DownArrow)
                        {
                            SFX_Navegacion();
                            indiceSeleccionado++;
                            if (indiceSeleccionado >= opciones.Length) indiceSeleccionado = 0;
                        }
                        else if (tecla.Key == ConsoleKey.Enter)
                        {
                            SFX_Seleccion();
                            opcionElegida = true;
                        }
                    }
                }

                Console.WriteLine("\n--------------------------------------------------");
                Console.CursorVisible = true;
                ProcesarOpcion((indiceSeleccionado + 1).ToString());
                Console.CursorVisible = false;

                player.LimitarAtributos();
                VerificarEstadoCritico();
                EvaluarLogros();
                EvaluarMisionObjetivos();

                if (player.Conectado)
                {
                    Console.WriteLine("\nPresione cualquier tecla para continuar...");
                    while (Console.KeyAvailable) Console.ReadKey(true);
                    Console.ReadKey(true);
                }
            }
        }

        static void ProcesarOpcion(string opcion)
        {
            switch (opcion)
            {
                case "1": ExplorarRealidad(); break;
                case "2": AnalizarAnomalia(); break;
                case "3": AlternarEscudos(); break;
                case "4": RecuperarEnergia(); break;
                case "5": ComunicacionIRIS(); break;
                case "6": MostrarMenuCartografia(); break;
                case "7":
                    if (player.EsElite) EjecutarProtocoloElite();
                    else ComandoNoReconocido();
                    break;
                case "8": ConectarMultijugador(); break;
                case "9": EjecutarMisionDinamicaIA(); break;
                case "10": EjecutarAuditoriaSistemaIA(); break;
                case "11": AdministrarInventario(); break;
                case "12": VerEstadoMision(); break;
                case "13": DesconectarSistema(); break;
                case "14": MenuAjustes(); break;
                default: ComandoNoReconocido(); break;
            }
        }

        static void EjecutarMisionDinamicaIA()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================================================");
            Console.WriteLine(" 🧠 GENERANDO MISIÓN DINÁMICA MEDIANTE IA...");
            Console.WriteLine("==================================================");
            Console.ResetColor();

            MostrarCarga("Conectando con el motor cuántico de NEXUS");

            string resultadoJSON = ServicioIA.CrearMisionDinamicaIA(player);

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(resultadoJSON))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n[ NUEVA MISIÓN ]: {doc.RootElement.GetProperty("titulo").GetString()}");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"Detalle: {doc.RootElement.GetProperty("descripcion").GetString()}");

                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("\n[ IMPACTO ESTIMADO ]");
                    Console.WriteLine($"Energía: {doc.RootElement.GetProperty("cambioEnergia").GetInt32()}");
                    Console.WriteLine($"Estabilidad: {doc.RootElement.GetProperty("cambioEstabilidad").GetInt32()}");
                    Console.WriteLine($"Peligro: {doc.RootElement.GetProperty("cambioPeligro").GetInt32()}");
                    Console.WriteLine($"Fragmentos: {doc.RootElement.GetProperty("fragmentosGanados").GetInt32()}");
                    Console.ResetColor();
                }
            }
            catch
            {
                Console.WriteLine("\nResultado del sistema:");
                Console.WriteLine(resultadoJSON);
            }
        }

        static void EjecutarAuditoriaSistemaIA()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================================================");
            Console.WriteLine(" 🛡️ EJECUTANDO AUDITORÍA DE INTEGRIDAD IA...");
            Console.WriteLine("==================================================");
            Console.ResetColor();

            MostrarCarga("Analizando el estado actual de la memoria del jugador");

            string diagnostico = ServicioIA.ValidarYAutocorregirSistemaIA(player);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n[ REPORTE DE AUDITORÍA ]");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(diagnostico);
            Console.ResetColor();
        }

        static void AdministrarInventario()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================================================");
            Console.WriteLine(" 📦 SISTEMA DE INVENTARIO Y SUMINISTROS");
            Console.WriteLine("==================================================");
            Console.ResetColor();

            if (player.Inventario.Count == 0)
            {
                Console.WriteLine("El inventario está vacío.");
                return;
            }

            int index = 1;
            List<string> llaves = player.Inventario.Keys.ToList();
            foreach (var item in llaves)
            {
                Console.WriteLine($"{index}. {item} (Cantidad: {player.Inventario[item]})");
                index++;
            }
            Console.WriteLine($"{index}. Salir");

            Console.Write("\nSelecciona el número del objeto que deseas utilizar: ");
            if (int.TryParse(Console.ReadLine(), out int seleccion) && seleccion > 0)
            {
                if (seleccion == index) return;

                if (seleccion <= llaves.Count)
                {
                    string objetoSeleccionado = llaves[seleccion - 1];

                    if (player.Inventario[objetoSeleccionado] > 0)
                    {
                        UtilizarObjeto(objetoSeleccionado);
                    }
                    else
                    {
                        SFX_Error();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[-] Error: No te quedan más '{objetoSeleccionado}' en el inventario.");
                        Console.ResetColor();
                    }
                }
                else
                {
                    SFX_Error();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[-] Error: Índice de objeto inexistente. Sistema protegido de fallo.");
                    Console.ResetColor();
                }
            }
            else
            {
                SFX_Error();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[-] Comando de entrada inválido.");
                Console.ResetColor();
            }
        }

        static void UtilizarObjeto(string nombreObjeto)
        {
            MostrarCarga($"Aplicando {nombreObjeto}");
            player.Inventario[nombreObjeto]--;

            switch (nombreObjeto)
            {
                case "Células de energía":
                    int energiaAnterior = player.NivelEnergia;
                    player.NivelEnergia += 25;
                    player.LimitarAtributos();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[+] Célula utilizada. Energía: {energiaAnterior}% -> {player.NivelEnergia}%");
                    break;
                case "Escudos":
                    player.EscudosActivos = true;
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("[+] Módulo de escudo consumido. Escudos en línea sin coste de energía.");
                    break;
                case "Módulos de ataque":
                    SFX_Error();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[-] Error Táctico: No hay enemigos presentes. El módulo se ha desperdiciado y quemado.");
                    player.Inventario["Módulos de ataque"]++;
                    Console.WriteLine("[!] El sistema IRIS bloqueó el consumo para evitar desperdicios.");
                    break;
                case "Analizadores":
                    player.NivelEstabilidad += 30;
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"[+] Analizador purga estática de la red. Estabilidad aumentada a {player.NivelEstabilidad}%.");
                    break;
                default:
                    Console.WriteLine("Objeto utilizado sin efectos conocidos.");
                    break;
            }
            Console.ResetColor();
        }

        static void VerEstadoMision()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("==================================================");
            Console.WriteLine(" 📜 MISIÓN ACTIVA: LA ANOMALÍA PERDIDA");
            Console.WriteLine("==================================================");
            Console.ResetColor();

            Console.WriteLine($"[ {(player.MisionActual.ZonaAlcanzada ? "X" : " ")} ] Llegar a la zona de anomalía.");
            Console.WriteLine($"[ {(player.MisionActual.NucleoEncontrado ? "X" : " ")} ] Encontrar el núcleo energético.");
            Console.WriteLine($"[ {(player.MisionActual.EntidadSobrevivida ? "X" : " ")} ] Sobrevivir al encuentro con la entidad.");
            Console.WriteLine($"[ {(player.MisionActual.RegresoExtraccion ? "X" : " ")} ] Regresar al punto de extracción.");

            if (player.MisionActual.EsCompletada())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n>>> MISIÓN COMPLETADA <<<");
            }
            else
            {
                Console.WriteLine("\n[!] Explora la realidad para progresar en los objetivos.");
            }
            Console.ResetColor();
        }

        static void EvaluarMisionObjetivos()
        {
            if (player.MisionActual.EsCompletada() && !player.MisionActual.RecompensasEntregadas)
            {
                player.MisionActual.RecompensasEntregadas = true;
                player.XP += 100;

                if (player.Inventario.ContainsKey("Módulo desconocido"))
                    player.Inventario["Módulo desconocido"]++;
                else
                    player.Inventario.Add("Módulo desconocido", 1);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n==================================================");
                Console.WriteLine(" 🟢 ¡MISIÓN COMPLETADA: LA ANOMALÍA PERDIDA!");
                Console.WriteLine(" RECOMPENSAS OBTENIDAS:");
                Console.WriteLine(" +100 XP");
                Console.WriteLine(" +1 Módulo desconocido añadido al inventario.");
                Console.WriteLine("==================================================");
                Console.ResetColor();
                SFX_Logro();
            }
        }

        static void SistemaEncuentros(EntidadSistema entidad)
        {
            SFX_Alarma();
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("==================================================");
            Console.WriteLine($" 👾 ¡ENCUENTRO DETECTADO! Entidad: {entidad.Tipo} ({entidad.Nombre})");
            Console.WriteLine("==================================================");
            Console.ResetColor();

            bool enEncuentro = true;

            while (enEncuentro && player.Conectado)
            {
                Console.WriteLine($"\nSalud de {entidad.Nombre}: {entidad.Salud}%");
                Console.WriteLine("ACCIONES:");
                Console.WriteLine("1. Atacar");
                Console.WriteLine("2. Escapar");
                Console.WriteLine("3. Utilizar un objeto");
                Console.WriteLine("4. Analizar la entidad");
                Console.Write("\nSelecciona tu acción: ");

                string accion = Console.ReadLine();

                switch (accion)
                {
                    case "1":
                        if (entidad is Robot)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("\n[!] Al atacar al Robot, este activa de inmediato su SISTEMA DEFENSIVO.");
                            player.NivelEstabilidad -= 15;
                            Console.WriteLine("Recibes daño por la sobrecarga defensiva.");
                        }
                        else if (entidad is Enemigo)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("\n[!] Has provocado al Enemigo. INICIA UN COMBATE DIRECTO.");
                            IniciarCombateHackeo();
                            enEncuentro = false;
                            player.MisionActual.EntidadSobrevivida = true;
                            return;
                        }

                        entidad.Salud -= 20;
                        Console.WriteLine($"Infliges 20 de daño. Salud restante: {entidad.Salud}%");
                        if (entidad.Salud <= 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(">>> Entidad neutralizada con éxito.");
                            player.MisionActual.EntidadSobrevivida = true;
                            enEncuentro = false;
                        }
                        break;
                    case "2":
                        Console.WriteLine("\nIntentando escapar de la zona...");
                        if (rand.Next(1, 100) > 40)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[+] Has escapado con éxito.");
                            enEncuentro = false;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[-] Falla al escapar. La entidad te ataca por la espalda.");
                            player.NivelEstabilidad -= 25;
                        }
                        break;
                    case "3":
                        Console.WriteLine("\n[Utilizar un objeto de combate]");
                        if (player.Inventario.ContainsKey("Módulos de ataque") && player.Inventario["Módulos de ataque"] > 0)
                        {
                            player.Inventario["Módulos de ataque"]--;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[+] Disparas un Módulo de ataque destructivo.");
                            entidad.Salud -= 60;
                            if (entidad.Salud <= 0)
                            {
                                Console.WriteLine(">>> Entidad desintegrada con éxito.");
                                player.MisionActual.EntidadSobrevivida = true;
                                enEncuentro = false;
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("[-] No tienes Módulos de ataque en el inventario o has tecleado mal.");
                        }
                        break;
                    case "4":
                        MostrarCarga("Escaneando biometría de la entidad");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"\n[ANÁLISIS]: Es de clase '{entidad.Tipo}'. " +
                            (entidad is Robot ? "Cuidado, tienen armadura de reacción térmica." : "Altamente inestable. Se recomienda pulso EMP."));
                        break;
                    default:
                        SFX_Error();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[-] Error: Acción no reconocida.");
                        Console.ResetColor();
                        break;
                }
                player.LimitarAtributos();
            }
        }

        static void ExplorarRealidad()
        {
            int costoExploracion = player.EsElite ? 10 : 15;

            if (player.NivelEnergia >= costoExploracion)
            {
                MostrarCarga("Escaneando cuadrante y recolectando datos");
                player.NivelEnergia -= costoExploracion;

                int evento = rand.Next(1, 101);

                if (evento <= 20)
                {
                    player.NivelPeligro += 5;
                    Console.WriteLine(">>> Exploración completada sin incidentes. No has encontrado nada de interés.");
                    player.EventosSobrevividos++;
                }
                else if (evento <= 40)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(">>> 🎁 OBJETO ENCONTRADO: Has descubierto suministros abandonados.");
                    if (player.Inventario.ContainsKey("Células de energía"))
                        player.Inventario["Células de energía"]++;
                    Console.ResetColor();
                    player.EventosSobrevividos++;
                }
                else if (evento <= 60)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(">>> 👾 ENTIDAD DETECTADA en la periferia de los sensores.");
                    Console.ResetColor();
                    Thread.Sleep(1000);
                    EntidadSistema entidadEncontrada = rand.Next(0, 2) == 0 ? (EntidadSistema)new Robot() : new Enemigo();
                    SistemaEncuentros(entidadEncontrada);
                }
                else if (evento <= 75)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(">>> ⚡ ANOMALÍA DETECTADA: Registrando firmas cuánticas e información...");

                    if (!player.MisionActual.ZonaAlcanzada)
                    {
                        Console.WriteLine("[MISIÓN]: Has llegado a la zona principal de la anomalía perdida.");
                        player.MisionActual.ZonaAlcanzada = true;
                    }
                    else if (!player.MisionActual.NucleoEncontrado)
                    {
                        Console.WriteLine("[MISIÓN]: Has encontrado el núcleo energético dentro de la anomalía.");
                        player.MisionActual.NucleoEncontrado = true;
                    }
                    else if (!player.MisionActual.RegresoExtraccion && player.MisionActual.EntidadSobrevivida)
                    {
                        Console.WriteLine("[MISIÓN]: Has localizado el punto de extracción.");
                        player.MisionActual.RegresoExtraccion = true;
                    }
                    else
                    {
                        Console.WriteLine("Información residual añadida a la base de datos.");
                    }
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(">>> 🚨 SISTEMA DAÑADO: 🔋 PÉRDIDA DE ENERGÍA por Tormenta entrópica.");
                    player.NivelPeligro += 35;
                    if (!player.EscudosActivos)
                    {
                        Console.WriteLine("[-] Sin escudos, la tormenta daña tu estabilidad gravemente.");
                        player.NivelEstabilidad -= 20;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("[+] Tus escudos absorbieron el impacto de la tormenta.");
                    }
                    Console.ResetColor();

                    Console.WriteLine("\n[!] Entidad agresiva detectada tras la tormenta. Preparando sistemas de combate clásico...");
                    Thread.Sleep(1500);
                    IniciarCombateHackeo();
                }
            }
            else
            {
                SFX_Error();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[-] Energía insuficiente para explorar.");
                Console.ResetColor();
            }
        }

        static void AnalizarAnomalia()
        {
            MostrarCarga("Analizando la estructura de la anomalía");

            if (player.NivelEstabilidad >= 75)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(">>> ÉXITO TOTAL: Análisis perfecto. Datos puros extraídos.");
                player.NivelPeligro -= player.EsElite ? 30 : 20;
                player.FragmentosDatos += 3;
            }
            else if (player.NivelEstabilidad >= 40)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(">>> ÉXITO PARCIAL: Hay estática en la conexión. Los datos están fragmentados.");
                player.NivelEnergia -= player.EsElite ? 5 : 10;
                player.FragmentosDatos += 1;
            }
            else
            {
                SFX_Error();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(">>> FALLO CATASTRÓFICO: La inestabilidad provocó un contragolpe en el sistema.");
                player.NivelEnergia -= player.EsElite ? 15 : 20;
                player.NivelPeligro += player.EsElite ? 20 : 30;
            }
            Console.ResetColor();
        }

        static void AlternarEscudos()
        {
            int costoEscudo = player.EsElite ? 10 : 20;
            if (!player.EscudosActivos)
            {
                if (player.NivelEnergia >= costoEscudo)
                {
                    player.EscudosActivos = true;
                    player.NivelEnergia -= costoEscudo;
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine("[+] ESCUDOS ACTIVADOS. Consumen energía al iniciar, pero te protegen del daño.");
                }
                else
                {
                    SFX_Error();
                    Console.WriteLine($"[-] Energía insuficiente (Requiere {costoEscudo}%) para iniciar escudos.");
                }
            }
            else
            {
                player.EscudosActivos = false;
                Console.WriteLine("[+] ESCUDOS DESACTIVADOS. Ahorrando recursos del sistema.");
            }
            Console.ResetColor();
        }

        static void RecuperarEnergia()
        {
            MostrarCarga("Iniciando protocolo de recuperación");
            player.NivelEnergia += player.EsElite ? 40 : 30;
            Console.WriteLine($"[+] Energía restaurada. Sistema al {player.NivelEnergia}%");
        }

        static void ComunicacionIRIS()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("==================================================");
            Console.WriteLine("        CONSOLA TÁCTICA IRIS // MODO JARVIS       ");
            Console.WriteLine("==================================================");

            string saludoInicial = $"Sistemas integrados. Monitoreando a {player.Nombre}. ¿Cuál es su orden?";
            Console.WriteLine($"\nIRIS: {saludoInicial}");
            ServicioIA.HablarAsync(saludoInicial);

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\n> Escribe tu consulta para IRIS: ");
            Console.CursorVisible = true;
            string mensaje = Console.ReadLine() ?? "";
            Console.CursorVisible = false;

            MostrarCarga("Analizando telemetría y generando diagnóstico");

            string respuestaTactica = ServicioIA.GenerarRespuestaTactica(player, mensaje);

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"\n[IRIS]: {respuestaTactica}\n");

            ServicioIA.HablarAsync(respuestaTactica);

            Console.ResetColor();
        }

        static void EjecutarProtocoloElite()
        {
            MostrarCarga("Ejecutando Protocolo Dios (Sobrecarga Élite)");
            player.NivelPeligro = 0;
            player.NivelEstabilidad = 100;
            player.NivelEnergia -= 15;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n[★] SOBRECARGA ÉLITE COMPLETADA:");
            Console.WriteLine(" - El peligro ha sido totalmente erradicado.");
            Console.WriteLine(" - La estabilidad del sistema está al 100%.");
            Console.ResetColor();
        }

        static void DesconectarSistema()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[+] Guardando progreso en NEXUS. Fragmentos recolectados: {player.FragmentosDatos}");
            Console.WriteLine("[+] Salida controlada iniciada. Cerrando conexiones de realidad y purgando caché...");
            Thread.Sleep(1200);
            player.Conectado = false;
            Console.ResetColor();
        }

        static void ComandoNoReconocido()
        {
            SFX_Error();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[ERROR] Comando no reconocido en este contexto.");
            Console.ResetColor();
        }

        static void VerificarEstadoCritico()
        {
            if (player.NivelPeligro >= 80 && !player.EscudosActivos && player.Conectado)
            {
                SFX_Alarma();
                int danoIris = player.EsElite ? 15 : 25;
                Console.WriteLine("\n==================================================");
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("⚠️ ALERTA: PELIGRO CRÍTICO DETECTADO");
                Console.WriteLine("El entorno es demasiado hostil. Sistemas colapsando.");
                player.NivelEstabilidad -= danoIris;
                Console.ResetColor();
                Console.WriteLine("==================================================");
            }

            if (player.NivelEnergia <= 0 && player.NivelEstabilidad <= 0 && player.NivelPeligro >= 80 && player.Conectado)
            {
                SFX_Alarma();
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("==================================================");
                Console.WriteLine("        ERROR CRÍTICO: DESGARRO DE REALIDAD       ");
                Console.WriteLine("==================================================");
                Console.WriteLine("Condiciones letales alcanzadas:");
                Console.WriteLine("- Energía agotada.");
                Console.WriteLine("- Estabilidad colapsada.");
                Console.WriteLine("- Peligro extremo.");
                Console.WriteLine("\nLa realidad te ha consumido. Conexión perdida...");
                Console.WriteLine("==================================================");
                Console.ResetColor();
                player.Conectado = false;
            }
        }

        static void MostrarMenuCartografia()
        {
            string[] opcionesMapa = {
                "IRIS-07 // Valle de Cristal",
                "IRIS-12 // Sector Abisal",
                "IRIS-21 // Zona de Interferencia",
                "Volver al Menú Principal"
            };
            int indiceMapa = 0;
            bool enMenuCartografia = true;

            Console.Clear();
            Console.CursorVisible = false;

            while (enMenuCartografia)
            {
                Console.SetCursorPosition(0, 0);
                Console.ForegroundColor = TemaInterfaz;
                Console.WriteLine("╔═══════════════════════════════════════════════════╗");
                Console.WriteLine("║            NEXUS // MAPAS DE REALIDADES           ║");
                Console.WriteLine("╠═══════════════════════════════════════════════════╣");
                Console.ResetColor();

                for (int i = 0; i < opcionesMapa.Length; i++)
                {
                    if (i == indiceMapa)
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"║ > [{i + 1}] {opcionesMapa[i].PadRight(42)}║");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = TemaInterfaz;
                        Console.Write("║ ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($"  [{i + 1}] {opcionesMapa[i].PadRight(42)}");
                        Console.ForegroundColor = TemaInterfaz;
                        Console.WriteLine("║");
                    }
                }

                Console.ForegroundColor = TemaInterfaz;
                Console.WriteLine("╚═══════════════════════════════════════════════════╝");
                Console.ResetColor();
                Console.WriteLine("\n[Use las flechas para seleccionar y ENTER para confirmar]      ");

                ConsoleKeyInfo tecla = Console.ReadKey(true);

                if (tecla.Key == ConsoleKey.UpArrow)
                {
                    SFX_Navegacion();
                    indiceMapa--;
                    if (indiceMapa < 0) indiceMapa = opcionesMapa.Length - 1;
                }
                else if (tecla.Key == ConsoleKey.DownArrow)
                {
                    SFX_Navegacion();
                    indiceMapa++;
                    if (indiceMapa >= opcionesMapa.Length) indiceMapa = 0;
                }
                else if (tecla.Key == ConsoleKey.Enter)
                {
                    SFX_Seleccion();
                    if (indiceMapa == 3)
                    {
                        enMenuCartografia = false;
                    }
                    else
                    {
                        RenderizarMapa(indiceMapa + 1);
                        Console.Clear();
                    }
                }
            }
        }

        static void RenderizarMapa(int idRealidad)
        {
            Console.Clear();
            MostrarCarga("Cargando matriz cartográfica");

            string[] nombresRealidad = { "IRIS-07 // Valle de Cristal", "IRIS-12 // Sector Abisal", "IRIS-21 // Zona de Interferencia" };
            string nombreActual = nombresRealidad[idRealidad - 1];

            int filas = 8, columnas = 16;
            char[,] matrizMapa = new char[filas, columnas];

            PoblarMatrizTerritorio(idRealidad, matrizMapa, filas, columnas);

            int totalRecursos = 0, totalAnomalias = 0, zonasDesconocidas = 0, zonasRegistradas = 0;

            Console.Clear();
            Console.ForegroundColor = TemaInterfaz;
            Console.WriteLine("╔═════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║                    NEXUS // CARTOGRAPHY                         ║");
            Console.WriteLine($"║                    REALIDAD: {nombreActual.PadRight(35)}║");
            Console.WriteLine("╠═════════════════════════════════════════════════════════════════╣");
            Console.ResetColor();

            for (int f = 0; f < filas; f++)
            {
                Console.ForegroundColor = TemaInterfaz;
                Console.Write("║  ");
                Console.ResetColor();

                for (int c = 0; c < columnas; c++)
                {
                    char celda = matrizMapa[f, c];
                    switch (celda)
                    {
                        case 'B': Console.ForegroundColor = ConsoleColor.Green; Console.Write(" ⌂ "); zonasRegistradas++; break;
                        case 'R': Console.ForegroundColor = ConsoleColor.Yellow; Console.Write(" ◆ "); totalRecursos++; zonasRegistradas++; break;
                        case 'A': Console.ForegroundColor = ConsoleColor.Red; Console.Write(" ▲ "); totalAnomalias++; zonasRegistradas++; break;
                        case 'W': Console.ForegroundColor = ConsoleColor.Blue; Console.Write(" ≈ "); zonasRegistradas++; break;
                        case 'T': Console.ForegroundColor = ConsoleColor.DarkGray; Console.Write(" █ "); zonasRegistradas++; break;
                        case '?': Console.ForegroundColor = ConsoleColor.Magenta; Console.Write(" ? "); zonasDesconocidas++; break;
                        case 'P': Console.ForegroundColor = TemaInterfaz; Console.Write(" • "); zonasRegistradas++; break;
                        default: Console.Write("   "); break;
                    }
                }
                Console.ForegroundColor = TemaInterfaz;
                Console.WriteLine(" ║");
                Console.ResetColor();
            }

            Console.ForegroundColor = TemaInterfaz;
            Console.WriteLine("╠═════════════════════════════════════════════════════════════════╣");
            Console.ResetColor();
            Console.Write("║ ");
            Console.ForegroundColor = ConsoleColor.Yellow; Console.Write(" ◆ RECURSO   ");
            Console.ForegroundColor = ConsoleColor.Red; Console.Write(" ▲ ANOMALÍA   ");
            Console.ForegroundColor = ConsoleColor.Green; Console.Write(" ⌂ BASE   ");
            Console.ForegroundColor = ConsoleColor.Blue; Console.Write(" ≈ AGUA\n");
            Console.ResetColor();
            Console.Write("║ ");
            Console.ForegroundColor = ConsoleColor.DarkGray; Console.Write(" █ TERRENO   ");
            Console.ForegroundColor = ConsoleColor.Magenta; Console.Write(" ? DESCONOCIDO ");
            Console.ForegroundColor = TemaInterfaz; Console.Write(" • INTERÉS\n");
            Console.ResetColor();
            Console.ForegroundColor = TemaInterfaz;
            Console.WriteLine("╠═════════════════════════════════════════════════════════════════╣");

            int porcentajeCartografia = (zonasRegistradas * 100) / (filas * columnas);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"║  CARTOGRAFÍA: {porcentajeCartografia}%     RECURSOS DETECTADOS: {totalRecursos:D2}                ║");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"║  AMENAZAS: {totalAnomalias:D2}        ZONAS SIN EXPLORAR: {zonasDesconocidas:D2}                 ║");
            Console.ForegroundColor = TemaInterfaz;
            Console.WriteLine("╚═════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            Console.WriteLine("\n[+] Presione cualquier tecla para volver al selector de mapas...");
            Console.WriteLine("    [!] Moverse fuera de las cuadrículas detectadas es letal. Quédate en la zona escaneada.");
            Console.ReadKey();
        }

        static void PoblarMatrizTerritorio(int idRealidad, char[,] matriz, int filas, int columnas)
        {
            for (int f = 0; f < filas; f++)
                for (int c = 0; c < columnas; c++)
                    matriz[f, c] = 'T';

            if (idRealidad == 1)
            {
                matriz[3, 7] = 'B';
                matriz[1, 2] = 'R'; matriz[2, 10] = 'R'; matriz[6, 4] = 'R'; matriz[5, 12] = 'R';
                matriz[0, 14] = 'A'; matriz[7, 1] = 'A';
                matriz[3, 0] = 'W'; matriz[4, 1] = 'W'; matriz[4, 2] = 'W'; matriz[5, 3] = 'W';
                matriz[0, 0] = '?'; matriz[0, 1] = '?'; matriz[7, 14] = '?'; matriz[7, 15] = '?';
                matriz[2, 5] = 'P'; matriz[5, 9] = 'P';
            }
            else if (idRealidad == 2)
            {
                matriz[1, 1] = 'B';
                matriz[4, 8] = 'R'; matriz[6, 13] = 'R';
                matriz[2, 3] = 'A'; matriz[3, 10] = 'A'; matriz[6, 6] = 'A';
                for (int c = 0; c < columnas; c++) { matriz[0, c] = 'W'; matriz[7, c] = 'W'; }
                matriz[3, 4] = 'W'; matriz[4, 4] = 'W'; matriz[3, 5] = 'W';
                matriz[5, 14] = '?'; matriz[5, 15] = '?'; matriz[6, 14] = '?';
            }
            else
            {
                matriz[0, 0] = 'B';
                matriz[7, 15] = 'R'; matriz[1, 8] = 'R';
                matriz[1, 2] = 'A'; matriz[3, 7] = 'A'; matriz[5, 11] = 'A'; matriz[2, 14] = 'A';
                for (int f = 4; f < filas; f++)
                    for (int c = 0; c < 5; c++) matriz[f, c] = '?';
                matriz[2, 2] = 'P';
            }
        }

        static void CargarMusicaEnMemoria()
        {
            try
            {
                string carpetaUsuario = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] posiblesUbicaciones = {
                    "musica.wav",
                    Path.Combine(carpetaUsuario, "Downloads", "musica.wav"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "musica.wav")
                };

                foreach (string ruta in posiblesUbicaciones)
                {
                    if (File.Exists(ruta))
                    {
                        ReproductorMusica = new SoundPlayer(ruta);
                        ReproductorMusica.LoadAsync();
                        break;
                    }
                }
            }
            catch { }
        }

        static void MostrarIntroduccion()
        {
            Console.Clear();
            Console.CursorVisible = false;

            ReproducirSonidoBooteo();

            Program.ReproductorMusica?.PlayLooping();

            string[] lineasLogo = new string[]
            {
                @" ███╗   ██╗███████╗██╗  ██╗██╗   ██╗███████╗",
                @" ████╗  ██║██╔════╝╚██╗██╔╝██║   ██║██╔════╝",
                @" ██╔██╗ ██║█████╗   ╚███╔╝ ██║   ██║███████╗",
                @" ██║╚██╗██║██╔══╝   ██╔██╗ ██║   ██║╚════██║",
                @" ██║ ╚████║███████╗██╔╝ ██╗╚██████╔╝███████║",
                @" ╚═╝  ╚═══╝╚══════╝╚═╝  ╚═╝ ╚═════╝ ╚══════╝"
            };

            string[] degradadoRojo = new string[]
            {
                "\x1b[38;2;255;50;50m",
                "\x1b[38;2;230;30;30m",
                "\x1b[38;2;200;15;15m",
                "\x1b[38;2;170;5;5m",
                "\x1b[38;2;135;0;0m",
                "\x1b[38;2;100;0;0m"
            };

            for (int i = 0; i < lineasLogo.Length; i++)
            {
                Console.Write(degradadoRojo[i]);
                foreach (char c in lineasLogo[i])
                {
                    Console.Write(c);
                    if (c != ' ') Thread.Sleep(2);
                }
                Console.WriteLine();
            }
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(" ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀");

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(" ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄\n");

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write(">> Protocolo de inicio listo. Presione ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("[ENTER]");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(" para ingresar a la simulación...");

            Console.ReadLine();
            Console.CursorVisible = true;
        }

        static void ReproducirSonidoBooteo()
        {
            try
            {
                Console.Beep(300, 80);
                Console.Beep(500, 80);
                Console.Beep(700, 100);
                Console.Beep(1000, 150);
            }
            catch { }
        }

        static int LeerEntero(string mensaje, int min, int max)
        {
            int valor;
            Console.Write($"> {mensaje}: ");
            while (!int.TryParse(Console.ReadLine(), out valor) || valor < min || valor > max)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Ingrese un número válido entre {min} y {max}.");
                Console.ResetColor();
                Console.Write($"> {mensaje}: ");
            }
            return valor;
        }

        static void RecopilarDatosUsuario()
        {
            Console.Write("> Ingrese su Nombre de Explorador: ");
            string inputNombre = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(inputNombre)) player.Nombre = inputNombre;

            int edad = LeerEntero("Ingrese su Edad", 10, 120);

            Console.Write("> Ingrese su Realidad asignada (Ej. Sector-7G): ");
            string inputRealidad = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(inputRealidad)) player.RealidadAsignada = inputRealidad;

            player.NivelEnergia = LeerEntero("Ingrese su Energía inicial", 0, 100);
            player.NivelEstabilidad = LeerEntero("Ingrese su Estabilidad inicial", 0, 100);

            if (player.NivelEnergia >= 80 && player.NivelEstabilidad >= 80) player.EsElite = true;
        }

        static bool EvaluarIngreso()
        {
            Console.Clear();
            Console.CursorVisible = false;
            Console.ForegroundColor = TemaInterfaz;

            string txtEscaneo = "[NEXUS] Evaluando parámetros biométricos y conexión a la red cuántica...";
            for (int i = 0; i < txtEscaneo.Length; i++)
            {
                Console.Write(txtEscaneo[i]);
                Thread.Sleep(20);
            }
            Console.WriteLine("\n");

            Console.Write("  [");
            int barraTop = Console.CursorTop;
            int barraLeft = Console.CursorLeft;
            Console.Write(new string('-', 30) + "] 0%");

            for (int i = 1; i <= 30; i++)
            {
                Console.SetCursorPosition(barraLeft + i - 1, barraTop);
                Console.Write("█");
                Console.SetCursorPosition(barraLeft + 31, barraTop);
                Console.Write($"{Math.Round((i / 30.0) * 100)}%");
                Thread.Sleep(40);
            }
            Console.WriteLine("\n");

            if (player.NivelEnergia >= 40 && player.NivelEstabilidad >= 40)
            {
                Console.ForegroundColor = ConsoleColor.Green;

                for (int i = 0; i < 4; i++)
                {
                    Console.Write("\r>>> INMERSIÓN AUTORIZADA <<<");
                    Thread.Sleep(200);
                    Console.Write("\r                            ");
                    Thread.Sleep(100);
                }
                Console.WriteLine("\r>>> INMERSIÓN AUTORIZADA <<<\n");

                Console.ForegroundColor = TemaInterfaz;
                string[] portalArt = {
                    @"      .  *  .  . *       *    ",
                    @"    *   .  * .    *  .   *    ",
                    @"   .  * [NEXUS-PORTAL] * .    ",
                    @"     *  .   \ O /  .  *       ",
                    @"        *    \|/     *        ",
                    @"   .......   / \   ........   "
                };
                foreach (var linea in portalArt)
                {
                    Console.WriteLine(linea);
                    Thread.Sleep(150);
                }

                if (player.EsElite)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n[★] ¡SISTEMA ÉLITE ACTIVADO! ★");
                    Console.WriteLine("Rendimiento superior detectado. Tienes ventajas tácticas exclusivas.");
                }
                Console.ResetColor();
                Thread.Sleep(2000);
                Console.CursorVisible = true;
                return true;
            }
            else
            {
                SFX_Error();
                Console.ForegroundColor = ConsoleColor.Red;
                for (int i = 0; i < 4; i++)
                {
                    Console.Write("\r>>> INMERSIÓN RECHAZADA <<<");
                    Thread.Sleep(200);
                    Console.Write("\r                           ");
                    Thread.Sleep(100);
                }
                Console.WriteLine("\r>>> INMERSIÓN RECHAZADA <<<");
                Console.WriteLine("Niveles críticos. No es seguro proceder.");
                Console.ResetColor();
                Thread.Sleep(2500);
                Console.CursorVisible = true;
                return false;
            }
        }

        static void MostrarInterfazUsuario()
        {
            Console.ForegroundColor = TemaInterfaz;
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                     NEXUS - TERMINAL                       ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
            Console.ForegroundColor = ConsoleColor.White;

            string rango = player.EsElite ? "[ÉLITE ⭐]" : "[CADETE]";

            Console.WriteLine($"  EXPLORADOR: {player.Nombre,-12} | REALIDAD: {player.RealidadAsignada,-8} | RANGO: {rango}");
            Console.WriteLine($"  FRAGMENTOS: {player.FragmentosDatos,-12} | XP ACUMULADO: {player.XP,-8} | HORA:  {DateTime.Now:HH:mm:ss}");

            Console.ForegroundColor = TemaInterfaz;
            Console.WriteLine("╠════════════════════════════════════════════════════════════╣");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  ENERGÍA:      [");
            ImprimirBarraColor(player.NivelEnergia, false);
            Console.WriteLine($"] {player.NivelEnergia,3}%");

            Console.Write("  ESTABILIDAD:  [");
            ImprimirBarraColor(player.NivelEstabilidad, false);
            Console.WriteLine($"] {player.NivelEstabilidad,3}%");

            Console.ForegroundColor = player.NivelPeligro > 60 ? ConsoleColor.Red : ConsoleColor.White;
            Console.Write("  PELIGRO:      [");
            ImprimirBarraColor(player.NivelPeligro, true);
            Console.WriteLine($"] {player.NivelPeligro,3}%");

            Console.ForegroundColor = player.EscudosActivos ? ConsoleColor.Blue : ConsoleColor.DarkGray;
            Console.WriteLine($"  ESCUDOS:      {(player.EscudosActivos ? "EN LÍNEA" : "DESACTIVADOS").PadRight(15)}");

            Console.ForegroundColor = TemaInterfaz;
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
            Console.ResetColor();
        }

        static void ImprimirBarraColor(int porcentaje, bool esPeligro)
        {
            int p = Math.Max(0, Math.Min(100, porcentaje));
            int longitud = 20;
            int bloques = (int)Math.Round((double)p / 100 * longitud);

            string barraLlena = new string('█', bloques);
            string barraVacia = new string('-', longitud - bloques);

            if (esPeligro)
            {
                if (p > 70) Console.ForegroundColor = ConsoleColor.Red;
                else if (p > 30) Console.ForegroundColor = ConsoleColor.Yellow;
                else Console.ForegroundColor = ConsoleColor.DarkGray;
            }
            else
            {
                if (p > 60) Console.ForegroundColor = ConsoleColor.Green;
                else if (p > 25) Console.ForegroundColor = ConsoleColor.Yellow;
                else Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.Write(barraLlena);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(barraVacia);
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void MostrarCierre()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(".......................................TRANSMISIÓN FINAL\n");
            Console.ResetColor();
            Console.WriteLine("NEXUS v1.1 cerrado correctamente.\n");
            Console.WriteLine("Las decisiones tienen consecuencias. La realidad ha sido alterada.");
            Console.WriteLine("\nPresione cualquier tecla para salir al sistema operativo...");
            Console.ReadKey();
        }

        static void MostrarCarga(string mensaje)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"\n[+] {mensaje} [");
            for (int i = 0; i < 20; i++)
            {
                Console.Write("█");
                Thread.Sleep(20);
            }
            Console.WriteLine("] ¡Listo!");
            Console.ResetColor();
            Thread.Sleep(300);
        }

        static void ConectarMultijugador()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("==================================================");
            Console.WriteLine("       NEXUS // RED MULTIJUGADOR (BETA)         ");
            Console.WriteLine("==================================================");
            MostrarCarga("Buscando servidores de realidad activos");
            Thread.Sleep(800);

            Console.WriteLine("\n[+] Ping a servidor central: 24ms");
            Console.WriteLine($"[+] Sincronizando perfil de {player.Nombre}...");
            Thread.Sleep(1000);

            int jugadores = rand.Next(12, 45);
            Console.WriteLine($"\n>>> CONEXIÓN ESTABLECIDA. Jugadores en este sector: {jugadores} <<<");
            Console.WriteLine("\n[CHAT GLOBAL] Xx_Cadete_xX: ¿Alguien encontró el Valle de Cristal?");
            Console.WriteLine("[CHAT GLOBAL] EliteSniper: Cuidado con la tormenta entrópica en IRIS-12.");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\n> Escribe un mensaje al servidor (o deja en blanco para salir): ");
            string mensaje = Console.ReadLine() ?? "";

            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                Console.WriteLine($"\n[CHAT GLOBAL] {player.Nombre}: {mensaje}");
                Console.ForegroundColor = TemaInterfaz;
                Console.WriteLine("[SERVIDOR] Mensaje transmitido a la red.");
            }

            SFX_Error();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[!] Inestabilidad de red detectada. Desconectando del servidor multijugador...");
            Console.ResetColor();
        }

        static void GenerarTransmisionAleatoria()
        {
            if ((DateTime.Now - ultimaTransmision).TotalSeconds > 15 && rand.Next(0, 100) > 85)
            {
                string[] mensajes = {
                    "ALERTA SECUNDARIA: Señal de auxilio detectada en Sector C-4.",
                    "INFO: Fluctuación cuántica estabilizada en órbita baja.",
                    "RUMOR DE RED: 'No confíen en los espejos de IRIS-07'.",
                    "ADVERTENCIA: Desgaste de matriz de escudos detectado."
                };

                // Guarda la posición actual para no interrumpir la navegación del menú
                int cursorIzquierda = Console.CursorLeft;
                int cursorArriba = Console.CursorTop;

                // Calcula dinámicamente el final de la ventana
                int lineaInferior = Console.WindowHeight - 2;

                if (lineaInferior > 0)
                {
                    Console.SetCursorPosition(0, lineaInferior);
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    // Limpia la línea completa según el ancho actual de la consola
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                    Console.SetCursorPosition(0, lineaInferior);
                    Console.Write($"[TRANSMISIÓN ENTRANTE] {mensajes[rand.Next(mensajes.Length)]}");
                }

                ultimaTransmision = DateTime.Now;
                Console.ResetColor();

                // Regresa el cursor a la opción que el jugador estaba seleccionando
                Console.SetCursorPosition(cursorIzquierda, cursorArriba);
            }
        }

        static void EvaluarLogros()
        {
            void OtorgarLogro(string nombre, string descripcion)
            {
                if (!player.Logros.Contains(nombre))
                {
                    player.Logros.Add(nombre);
                    SFX_Logro();
                    EscribirTypewriter($"\n[★ LOGRO DESBLOQUEADO ★] {nombre}: {descripcion}", ConsoleColor.Yellow, 30);
                }
            }

            if (player.FragmentosDatos >= 10) OtorgarLogro("Coleccionista de Datos", "Reúne 10 fragmentos.");
            if (player.AnomaliasHackeadas >= 3) OtorgarLogro("Fantasma de la Red", "Hackea 3 entidades con éxito.");
            if (player.NivelPeligro == 100 && player.NivelEstabilidad > 0) OtorgarLogro("Sobreviviente Entrópico", "Sobrevive con Peligro Máximo.");
            if (player.EventosSobrevividos >= 5) OtorgarLogro("Explorador Veterano", "Sobrevive a 5 eventos de exploración.");
        }

        static void MenuAjustes()
        {
            Console.Clear();
            Console.WriteLine("=== AJUSTES DE SISTEMA Y PERFIL ===");
            Console.WriteLine("\n[ LOGROS DESBLOQUEADOS ]");
            if (player.Logros.Count == 0) Console.WriteLine("  - Ninguno. Aún tienes mucho que demostrar.");
            foreach (var l in player.Logros) Console.WriteLine($"  ★ {l}");

            Console.WriteLine("\n[ TEMAS VISUALES ]");
            Console.WriteLine("1. Rojo Nexus (Clásico)");
            Console.WriteLine("2. Verde Matrix (Retro)");
            Console.WriteLine("3. Ámbar Sci-Fi");
            Console.WriteLine("4. Azul Ciberespacio (Actual)");
            Console.Write("\nSeleccione un tema: ");

            string t = Console.ReadLine();
            if (t == "1") TemaInterfaz = ConsoleColor.Red;
            else if (t == "2") TemaInterfaz = ConsoleColor.Green;
            else if (t == "3") TemaInterfaz = ConsoleColor.DarkYellow;
            else if (t == "4") TemaInterfaz = ConsoleColor.Cyan;

            Console.WriteLine("\n[+] Tema actualizado. Regresando al menú...");
            Thread.Sleep(1000);
        }

        static void IniciarCombateHackeo()
        {
            SFX_Alarma();
            int saludEntidad = 100;
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("==================================================");
            Console.WriteLine(" ⚠ PROTOCOLO DE DEFENSA INICIADO // COMBATE ACTIVO");
            Console.WriteLine("==================================================");

            while (saludEntidad > 0 && player.NivelEstabilidad > 0 && player.Conectado)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"\n> Entidad Desconocida [Integridad: {saludEntidad}%]");
                Console.WriteLine($"> {player.Nombre} [Estabilidad: {player.NivelEstabilidad}% | Energía: {player.NivelEnergia}%]");
                Console.WriteLine("\nACCIONES TÁCTICAS:");
                Console.WriteLine("1. Ataque de pulso EMP (Cuesta 15% Energía)");
                Console.WriteLine("2. Intentar Hackeo (Requiere Escudos Activos, +Riesgo)");
                Console.WriteLine("3. Desviar energía a Estabilidad");
                Console.Write("\nSeleccione acción: ");

                string accion = Console.ReadLine();
                Console.WriteLine("-------------------------");

                if (accion == "1")
                {
                    if (player.NivelEnergia >= 15)
                    {
                        player.NivelEnergia -= 15;
                        int dano = rand.Next(25, 45);
                        saludEntidad -= dano;
                        EscribirTypewriter($"[+] Pulso EMP impacta causando {dano}% de daño.", ConsoleColor.Yellow);
                    }
                    else { SFX_Error(); EscribirTypewriter("[-] Energía insuficiente para el pulso.", ConsoleColor.Red); continue; }
                }
                else if (accion == "2")
                {
                    if (player.EscudosActivos)
                    {
                        if (rand.Next(0, 100) > 40)
                        {
                            EscribirTypewriter("[+] HACKEO EXITOSO. Cortafuegos enemigo destruido.", ConsoleColor.Green);
                            saludEntidad = 0;
                            player.FragmentosDatos += 2;
                            player.AnomaliasHackeadas++;
                        }
                        else
                        {
                            SFX_Error();
                            EscribirTypewriter("[-] Hackeo fallido. Contramedidas activadas.", ConsoleColor.Red);
                            player.NivelEstabilidad -= 20;
                        }
                    }
                    else { SFX_Error(); EscribirTypewriter("[-] Los escudos deben estar activos para el hackeo.", ConsoleColor.Red); continue; }
                }
                else if (accion == "3")
                {
                    player.NivelEstabilidad += 25;
                    player.NivelEnergia -= 10;
                    EscribirTypewriter("[+] Energía redirigida. Estabilidad recuperada.", TemaInterfaz);
                }
                else { continue; }

                if (saludEntidad > 0)
                {
                    int danoEnemigo = rand.Next(10, 25);
                    if (player.EscudosActivos) { danoEnemigo /= 2; EscribirTypewriter($"[!] El enemigo ataca. Los escudos mitigan el daño a {danoEnemigo}%", ConsoleColor.Magenta); }
                    else { EscribirTypewriter($"[!] El enemigo ataca directamente. Pierdes {danoEnemigo}% de estabilidad.", ConsoleColor.Red); }
                    player.NivelEstabilidad -= danoEnemigo;
                }
            }

            if (saludEntidad <= 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n>>> AMENAZA NEUTRALIZADA. Área asegurada.");
                Console.ResetColor();
            }
        }
    }
}