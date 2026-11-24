using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Speech.Recognition;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NexusForms
{
    public partial class FormJuego : Form
    {
        #region Variables de Estado y UI
        private Jugador player;
        private int velocidadTickMs;
        private System.Windows.Forms.Timer timerSistema;
        private int ticksTranscurridos = 0;
        private Random rand = new Random();

        // Controles de cabina y estado
        private Label lblPiloto, lblSector, lblNave, lblReloj, lblAdvertencia;
        private Label lblEnergiaVal, lblEstabilidadVal, lblPeligroVal, lblXPVal, lblFragVal, lblEscudoEstado;
        private ProgressBar pbEnergia, pbEstabilidad, pbPeligro;
        private RichTextBox txtBitacora;
        private TextBox txtComandoIA;
        private Panel pnlMenuLat;

        // Paneles secundarios
        private Panel pnlCombate, pnlMapa, pnlInventario, pnlMultijugador;
        private Label lblEnemigoTitulo, lblEnemigoSalud;
        private ProgressBar pbEnemigo;
        private RichTextBox txtCombateLog;
        private EntidadSistema? enemigoActual;
        private ListBox lstInventario;

        // Controles del escáner topográfico
        private Label lblEstadoEscaner;
        private PictureBox picMapaTactico;
        private RichTextBox txtLeyendaCartografica;

        // Misiones dinámicas
        private string tituloMisionDinamica = "Reconocimiento del Perímetro";
        private string descMisionDinamica = "Explora el sector, recolecta fragmentos y sobrevive a las anomalías.";
        private Label lblMisionHUD;

        // Control de mando fluido + luz amarilla indicadora
        private System.Windows.Forms.Timer? timerMandoCabina;
        private List<Button> botonesCabina = new List<Button>();
        private List<Button> botonesSubpanel = new List<Button>();
        private int indiceBotonCabina = 0;
        private int indiceBotonSubpanel = 0;
        private ControlMando.EstadoMando mandoCabinaAnterior;
        private int contadorRepeticionMando = 0;
        private Panel? luzMarcadorAmarilla;

        // Comandos de voz tácticos IRIS manos libres
        private SpeechRecognitionEngine? motorVozCabina;
        private Button? btnVozCabina;
        private bool irisEscuchando = false;
        #endregion

        public FormJuego(Jugador jugadorConfigurado, int velMs)
        {
            InitializeComponent();
            player = jugadorConfigurado;
            velocidadTickMs = velMs;

            ConfigurarJugadorInicial();
            ConstruirCabinaHUD();
            CrearPanelesSecundarios();
            ActualizarHUD();
            IniciarRelojYEventos();
            IniciarMandoCabina();

            this.Shown += (s, e) =>
            {
                this.Activate();
                SeleccionarBotonCabina(0);
            };

            Log("==================================================================", player.ColorTema);
            Log("[SISTEMA] Inmersión completada. Todos los módulos operativos.", Color.LimeGreen);
            Log($"[IRIS] Bienvenido a bordo de {player.NaveAsignada}, Comandante {player.Nombre}. Pulsa 🎙️ VOZ o Triángulo para hablarme.", Color.Cyan);
        }

        #region Configuración Inicial y UI
        private void ConfigurarJugadorInicial()
        {
            if (player.EsElite && player.XP == 0 && player.FragmentosDatos == 0)
            {
                player.NivelEnergia = 100;
                player.NivelEstabilidad = 100;
                player.NivelPeligro = 0;
                player.EscudosActivos = true;
                player.FragmentosDatos = 5;
            }
        }

        private void CrearPanelesSecundarios()
        {
            CrearPanelCombate();
            CrearPanelMapa();
            CrearPanelInventario();
            CrearPanelMultijugador();
        }

        private void ConstruirCabinaHUD()
        {
            this.Text = $"NEXUS // CABINA DE MANDO - {player.NaveAsignada.ToUpper()}";
            this.Size = new Size(1240, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(6, 10, 18);

            Image? imgCabina = BuscarImagen("fondo_cabina");
            if (imgCabina != null)
            {
                this.BackgroundImage = imgCabina;
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }

            // Panel Superior
            Panel pnlTop = new Panel { Bounds = new Rectangle(15, 12, 1195, 68), BackColor = Color.FromArgb(220, 10, 16, 28) };
            lblPiloto = new Label { Text = $"PILOTO: {player.Nombre.ToUpper()}", Font = new Font("Consolas", 11, FontStyle.Bold), ForeColor = player.ColorTema, Location = new Point(15, 10), AutoSize = true };
            lblSector = new Label { Text = $"SECTOR: {player.RealidadAsignada}", Font = new Font("Consolas", 11, FontStyle.Bold), ForeColor = Color.Gold, Location = new Point(280, 10), AutoSize = true };
            lblNave = new Label { Text = $"NAVE: {player.NaveAsignada}", Font = new Font("Consolas", 11, FontStyle.Bold), ForeColor = Color.White, Location = new Point(540, 10), AutoSize = true };
            lblReloj = new Label { Text = $"TIEMPO: {DateTime.Now:HH:mm:ss}", Font = new Font("Consolas", 11, FontStyle.Bold), ForeColor = Color.Cyan, Location = new Point(980, 10), AutoSize = true };
            lblAdvertencia = new Label { Text = "ESTADO DEL NÚCLEO: ESTABLE // AUTOGUARDADO ACTIVO", Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = Color.LimeGreen, Location = new Point(15, 38), AutoSize = true };
            pnlTop.Controls.AddRange(new Control[] { lblPiloto, lblSector, lblNave, lblReloj, lblAdvertencia });
            this.Controls.Add(pnlTop);

            // Panel de Estadísticas
            Panel pnlStats = new Panel { Bounds = new Rectangle(15, 88, 1195, 54), BackColor = Color.FromArgb(215, 12, 18, 32) };
            pnlStats.Controls.Add(new Label { Text = "ENERGÍA:", Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = Color.Cyan, Location = new Point(15, 8), AutoSize = true });
            pbEnergia = new ProgressBar { Location = new Point(15, 28), Size = new Size(190, 16), Maximum = 100 };
            lblEnergiaVal = new Label { Text = "100%", Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(165, 8), AutoSize = true };

            pnlStats.Controls.Add(new Label { Text = "ESTABILIDAD:", Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = Color.LimeGreen, Location = new Point(230, 8), AutoSize = true });
            pbEstabilidad = new ProgressBar { Location = new Point(230, 28), Size = new Size(190, 16), Maximum = 100 };
            lblEstabilidadVal = new Label { Text = "100%", Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(380, 8), AutoSize = true };

            pnlStats.Controls.Add(new Label { Text = "PELIGRO:", Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = Color.OrangeRed, Location = new Point(445, 8), AutoSize = true });
            pbPeligro = new ProgressBar { Location = new Point(445, 28), Size = new Size(190, 16), Maximum = 100 };
            lblPeligroVal = new Label { Text = "0%", Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(595, 8), AutoSize = true };

            lblFragVal = new Label { Text = "FRAGMENTOS: 0", Font = new Font("Consolas", 10, FontStyle.Bold), ForeColor = Color.Gold, Location = new Point(665, 18), AutoSize = true };
            lblXPVal = new Label { Text = "XP: 0", Font = new Font("Consolas", 10, FontStyle.Bold), ForeColor = Color.Magenta, Location = new Point(825, 18), AutoSize = true };
            lblEscudoEstado = new Label { Text = "ESCUDOS: ON", Font = new Font("Consolas", 10, FontStyle.Bold), ForeColor = Color.Cyan, Location = new Point(940, 18), AutoSize = true };
            pnlStats.Controls.AddRange(new Control[] { pbEnergia, lblEnergiaVal, pbEstabilidad, lblEstabilidadVal, pbPeligro, lblPeligroVal, lblFragVal, lblXPVal, lblEscudoEstado });
            this.Controls.Add(pnlStats);

            // Menú Lateral
            pnlMenuLat = new Panel { Bounds = new Rectangle(15, 150, 285, 580), BackColor = Color.FromArgb(220, 10, 16, 28) };
            int y = 14;
            AgregarBotonTactico(pnlMenuLat, "🚀 Explorar realidad", ref y, Color.Cyan, (s, e) => ExplorarRealidad());
            AgregarBotonTactico(pnlMenuLat, "✈️ Combate aéreo (1945)", ref y, Color.OrangeRed, (s, e) => IniciarCombateAereo1945());
            AgregarBotonTactico(pnlMenuLat, "🛰️ Escanear entorno", ref y, Color.DeepSkyBlue, (s, e) => EscanearEntorno());
            AgregarBotonTactico(pnlMenuLat, "🔋 Recargar energía", ref y, Color.LimeGreen, (s, e) => RecargarEnergia());
            AgregarBotonTactico(pnlMenuLat, "🛡️ Activar / Desactivar escudos", ref y, Color.Gold, (s, e) => AlternarEscudos());
            AgregarBotonTactico(pnlMenuLat, "🎯 Nueva misión dinámica (IA)", ref y, Color.Orange, async (s, e) => await GenerarMisionDinamicaIA());
            AgregarBotonTactico(pnlMenuLat, "🤖 Auditar sistemas (IA)", ref y, Color.Magenta, async (s, e) => await AuditarSistemaPorIA());
            AgregarBotonTactico(pnlMenuLat, "🗺️ Mapa estelar", ref y, Color.White, (s, e) => MostrarPanelSecundario(pnlMapa));
            AgregarBotonTactico(pnlMenuLat, "🎒 Inventario y módulos", ref y, Color.White, (s, e) => { ActualizarVistaInventario(); MostrarPanelSecundario(pnlInventario); });
            AgregarBotonTactico(pnlMenuLat, "📡 Red multijugador", ref y, Color.White, (s, e) => MostrarPanelSecundario(pnlMultijugador));
            AgregarBotonTactico(pnlMenuLat, "🖥️ Volver a consola central", ref y, Color.LightGray, (s, e) => OcultarPanelesSecundarios());

            // Luz amarilla indicadora del marcador del mando
            luzMarcadorAmarilla = new Panel
            {
                Size = new Size(6, 38),
                Location = new Point(4, 14),
                BackColor = Color.Yellow,
                Visible = true
            };
            pnlMenuLat.Controls.Add(luzMarcadorAmarilla);
            luzMarcadorAmarilla.BringToFront();

            lblMisionHUD = new Label { Text = $"MISIÓN: {tituloMisionDinamica}", Font = new Font("Consolas", 8.5f, FontStyle.Bold), ForeColor = Color.Gold, Location = new Point(12, y + 6), Size = new Size(260, 55) };
            pnlMenuLat.Controls.Add(lblMisionHUD);
            this.Controls.Add(pnlMenuLat);

            // Bitácora Principal
            txtBitacora = new RichTextBox { Bounds = new Rectangle(312, 150, 898, 530), BackColor = Color.FromArgb(8, 12, 20), ForeColor = Color.White, Font = new Font("Consolas", 10.5f), ReadOnly = true, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(txtBitacora);

            // Panel de Chat IA + Botón VOZ IRIS manos libres
            Panel pnlChat = new Panel { Bounds = new Rectangle(312, 688, 898, 42), BackColor = Color.FromArgb(220, 12, 18, 32) };
            txtComandoIA = new TextBox { Location = new Point(8, 8), Size = new Size(635, 26), Font = new Font("Consolas", 11), BackColor = Color.FromArgb(18, 26, 44), ForeColor = Color.White };
            txtComandoIA.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await EnviarMensajeAIRIS(); } };

            btnVozCabina = new Button
            {
                Text = "🎙️ VOZ",
                Location = new Point(650, 6),
                Size = new Size(98, 30),
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(110, 30, 140),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnVozCabina.Click += (s, e) => ActivarEscuchaTacticaIRIS();

            Button btnEnviar = new Button { Text = "ENVIAR A IRIS", Location = new Point(756, 6), Size = new Size(134, 30), Font = new Font("Consolas", 9.5f, FontStyle.Bold), BackColor = Color.FromArgb(0, 130, 160), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnEnviar.Click += async (s, e) => await EnviarMensajeAIRIS();

            pnlChat.Controls.AddRange(new Control[] { txtComandoIA, btnVozCabina, btnEnviar });
            this.Controls.Add(pnlChat);
        }

        private void AgregarBotonTactico(Panel p, string texto, ref int y, Color colorTexto, EventHandler accion)
        {
            Button btn = new Button
            {
                Text = texto,
                Location = new Point(12, y),
                Size = new Size(260, 38),
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(18, 28, 46),
                ForeColor = colorTexto,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            btn.Click += accion;
            p.Controls.Add(btn);
            botonesCabina.Add(btn);
            y += 43;
        }
        #endregion

        #region Control de Mando Fluido (60 FPS + Luz Amarilla)
        private void IniciarMandoCabina()
        {
            this.KeyPreview = true;
            this.DoubleBuffered = true;

            timerMandoCabina = new System.Windows.Forms.Timer { Interval = 16 };
            timerMandoCabina.Tick += (s, e) =>
            {
                if (!this.Visible || botonesCabina.Count == 0) return;

                var actual = ControlMando.Leer();

                bool pulsaAbajo = actual.Abajo && !mandoCabinaAnterior.Abajo;
                bool pulsaArriba = actual.Arriba && !mandoCabinaAnterior.Arriba;
                bool pulsaDerecha = actual.Derecha && !mandoCabinaAnterior.Derecha;
                bool pulsaIzquierda = actual.Izquierda && !mandoCabinaAnterior.Izquierda;
                bool pulsaX = actual.BotonSaltar && !mandoCabinaAnterior.BotonSaltar;           // X = Entrar / Ejecutar
                bool pulsaCirculo = actual.BotonDisparar && !mandoCabinaAnterior.BotonDisparar;   // Redondo = Volver atrás
                bool pulsaTriangulo = actual.BotonEspecial && !mandoCabinaAnterior.BotonEspecial; // Triángulo = Voz IRIS

                if (actual.Abajo || actual.Arriba || actual.Derecha || actual.Izquierda)
                {
                    contadorRepeticionMando++;
                    if (contadorRepeticionMando > 10 && contadorRepeticionMando % 3 == 0)
                    {
                        if (actual.Abajo) pulsaAbajo = true;
                        if (actual.Arriba) pulsaArriba = true;
                        if (actual.Derecha) pulsaDerecha = true;
                        if (actual.Izquierda) pulsaIzquierda = true;
                    }
                }
                else
                {
                    contadorRepeticionMando = 0;
                }

                mandoCabinaAnterior = actual;

                if (pulsaTriangulo)
                {
                    ActivarEscuchaTacticaIRIS();
                    return;
                }

                bool enSubpanel = (pnlMapa != null && pnlMapa.Visible) ||
                                  (pnlInventario != null && pnlInventario.Visible) ||
                                  (pnlCombate != null && pnlCombate.Visible);

                if (enSubpanel && botonesSubpanel.Count > 0)
                {
                    if (pulsaAbajo || pulsaDerecha)
                    {
                        SeleccionarBotonSubpanel(indiceBotonSubpanel + 1);
                    }
                    else if (pulsaArriba || pulsaIzquierda)
                    {
                        SeleccionarBotonSubpanel(indiceBotonSubpanel - 1);
                    }

                    if (pulsaX)
                    {
                        botonesSubpanel[indiceBotonSubpanel].PerformClick();
                        return;
                    }
                    if (pulsaCirculo)
                    {
                        OcultarPanelesSecundarios();
                        SeleccionarBotonCabina(indiceBotonCabina);
                        return;
                    }
                    return;
                }

                if (pulsaAbajo)
                {
                    SeleccionarBotonCabina(indiceBotonCabina + 1);
                }
                else if (pulsaArriba)
                {
                    SeleccionarBotonCabina(indiceBotonCabina - 1);
                }

                if (pulsaX)
                {
                    botonesCabina[indiceBotonCabina].PerformClick();
                }
                else if (pulsaCirculo)
                {
                    if (pnlMultijugador != null && pnlMultijugador.Visible)
                    {
                        OcultarPanelesSecundarios();
                    }
                    else
                    {
                        GestorGuardado.Guardar(player);
                        this.Close();
                    }
                }
            };
            timerMandoCabina.Start();

            this.FormClosing += (s, e) =>
            {
                GestorGuardado.Guardar(player);
                timerMandoCabina?.Stop();
                motorVozCabina?.Dispose();
            };
        }

        private void SeleccionarBotonCabina(int nuevoIndice)
        {
            if (botonesCabina.Count == 0) return;
            indiceBotonCabina = (nuevoIndice + botonesCabina.Count) % botonesCabina.Count;

            for (int i = 0; i < botonesCabina.Count; i++)
            {
                bool activo = (i == indiceBotonCabina);
                botonesCabina[i].BackColor = activo ? Color.FromArgb(42, 58, 92) : Color.FromArgb(18, 28, 46);
                botonesCabina[i].FlatAppearance.BorderColor = activo ? Color.Yellow : Color.FromArgb(45, 75, 110);
                botonesCabina[i].FlatAppearance.BorderSize = activo ? 2 : 1;
            }

            Button btnActivo = botonesCabina[indiceBotonCabina];
            if (luzMarcadorAmarilla != null)
            {
                luzMarcadorAmarilla.Location = new Point(4, btnActivo.Top);
                luzMarcadorAmarilla.Height = btnActivo.Height;
                luzMarcadorAmarilla.Visible = true;
                luzMarcadorAmarilla.BringToFront();
            }

            btnActivo.Focus();
        }

        private void CargarBotonesSubpanel(Control contenedor)
        {
            botonesSubpanel.Clear();
            BuscarBotonesEn(contenedor);
            if (botonesSubpanel.Count > 0)
            {
                SeleccionarBotonSubpanel(0);
            }
        }

        private void BuscarBotonesEn(Control padre)
        {
            foreach (Control c in padre.Controls)
            {
                if (c is Button btn && btn.Visible && btn.Enabled)
                    botonesSubpanel.Add(btn);
                if (c.HasChildren)
                    BuscarBotonesEn(c);
            }
        }

        private void SeleccionarBotonSubpanel(int nuevoIndice)
        {
            if (botonesSubpanel.Count == 0) return;
            indiceBotonSubpanel = (nuevoIndice + botonesSubpanel.Count) % botonesSubpanel.Count;

            for (int i = 0; i < botonesSubpanel.Count; i++)
            {
                bool activo = (i == indiceBotonSubpanel);
                botonesSubpanel[i].FlatAppearance.BorderColor = activo ? Color.Yellow : Color.FromArgb(60, 90, 130);
                botonesSubpanel[i].FlatAppearance.BorderSize = activo ? 3 : 1;
            }
            botonesSubpanel[indiceBotonSubpanel].Focus();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (txtComandoIA != null && txtComandoIA.Focused && keyData != Keys.Up && keyData != Keys.Down)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            if (keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Left || keyData == Keys.Right)
            {
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        #endregion

        #region Comandos de Voz Tácticos IRIS (Manos Libres)
        private void ActivarEscuchaTacticaIRIS()
        {
            if (irisEscuchando) return;

            try
            {
                irisEscuchando = true;
                if (btnVozCabina != null)
                {
                    btnVozCabina.Text = "🔴 OÍDO...";
                    btnVozCabina.BackColor = Color.FromArgb(180, 30, 30);
                }
                Log("[IRIS VOZ] Canal abierto (habla al micrófono de la PC): 'Recargar energía', 'Activar escudos', 'Escanear entorno', 'Explorar realidad', 'Combate aéreo', 'Mapa estelar', 'Inventario'...", Color.Magenta);

                motorVozCabina?.Dispose();

                RecognizerInfo? infoEs = null;
                foreach (var r in SpeechRecognitionEngine.InstalledRecognizers())
                {
                    if (r.Culture.TwoLetterISOLanguageName.Equals("es", StringComparison.OrdinalIgnoreCase))
                    {
                        infoEs = r;
                        break;
                    }
                }

                motorVozCabina = infoEs != null
                    ? new SpeechRecognitionEngine(infoEs)
                    : new SpeechRecognitionEngine();

                motorVozCabina.SetInputToDefaultAudioDevice();
                motorVozCabina.InitialSilenceTimeout = TimeSpan.FromSeconds(4);

                Choices comandos = new Choices(new string[]
                {
                    "Recargar energía",
                    "Activar escudos",
                    "Desactivar escudos",
                    "Escanear entorno",
                    "Explorar realidad",
                    "Combate aéreo",
                    "Nueva misión",
                    "Auditar sistemas",
                    "Mapa estelar",
                    "Inventario",
                    "Volver a consola"
                });

                motorVozCabina.LoadGrammar(new Grammar(new GrammarBuilder(comandos)));

                motorVozCabina.SpeechRecognized += async (s, args) =>
                {
                    string cmd = args.Result.Text.ToLowerInvariant();
                    Log($"[VOZ RECONOCIDA]: \"{args.Result.Text}\"", Color.Cyan);

                    if (cmd.Contains("recargar")) RecargarEnergia();
                    else if (cmd.Contains("escudos")) AlternarEscudos();
                    else if (cmd.Contains("escanear")) EscanearEntorno();
                    else if (cmd.Contains("explorar")) ExplorarRealidad();
                    else if (cmd.Contains("combate")) IniciarCombateAereo1945();
                    else if (cmd.Contains("misión")) await GenerarMisionDinamicaIA();
                    else if (cmd.Contains("auditar")) await AuditarSistemaPorIA();
                    else if (cmd.Contains("mapa")) MostrarPanelSecundario(pnlMapa);
                    else if (cmd.Contains("inventario")) { ActualizarVistaInventario(); MostrarPanelSecundario(pnlInventario); }
                    else if (cmd.Contains("volver")) OcultarPanelesSecundarios();

                    RestaurarBotonVozCabina();
                };

                motorVozCabina.RecognizeCompleted += (s, args) => RestaurarBotonVozCabina();
                motorVozCabina.RecognizeAsync(RecognizeMode.Single);
            }
            catch
            {
                RestaurarBotonVozCabina();
                Log("[IRIS VOZ] No se detectó micrófono activo en Windows.", Color.OrangeRed);
            }
        }

        private void RestaurarBotonVozCabina()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(RestaurarBotonVozCabina));
                return;
            }
            irisEscuchando = false;
            if (btnVozCabina != null)
            {
                btnVozCabina.Text = "🎙️ VOZ";
                btnVozCabina.BackColor = Color.FromArgb(110, 30, 140);
            }
        }
        #endregion

        #region Utilidades y Eventos del Sistema
        private Image? BuscarImagen(string nombreBase)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] extensiones = new[] { ".jpg", ".jpeg", ".png" };
            foreach (string ext in extensiones)
            {
                string ruta = Path.Combine(baseDir, nombreBase + ext);
                if (File.Exists(ruta))
                {
                    try
                    {
                        using var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read);
                        return Image.FromStream(fs);
                    }
                    catch { }
                }
            }
            return null;
        }

        private void IniciarRelojYEventos()
        {
            timerSistema = new System.Windows.Forms.Timer { Interval = velocidadTickMs };
            timerSistema.Tick += (s, e) =>
            {
                ticksTranscurridos++;
                lblReloj.Text = $"TIEMPO: {DateTime.Now:HH:mm:ss}";
                if (ticksTranscurridos % 30 == 0)
                {
                    GenerarAdvertenciaSistema();
                }
            };
            timerSistema.Start();
        }

        private void GenerarAdvertenciaSistema()
        {
            string[] alertas = new[]
            {
                "TRANSMISIÓN ENTRANTE: Fluctuación cuántica detectada en el perímetro.",
                "SEÑAL CODIFICADA: Explorador aliado detectado en frecuencia 142.8 MHz.",
                "TELEMETRÍA: Matriz de escudos operando dentro de parámetros nominales.",
                "SENSOR LARGO ALCANCE: Firma energética no identificada a 12 parsecs."
            };
            lblAdvertencia.Text = alertas[rand.Next(alertas.Length)];
            lblAdvertencia.ForeColor = Color.Orange;
        }

        private void Log(string mensaje, Color color)
        {
            if (txtBitacora.InvokeRequired)
            {
                txtBitacora.Invoke(new Action(() => Log(mensaje, color)));
                return;
            }
            txtBitacora.SelectionStart = txtBitacora.TextLength;
            txtBitacora.SelectionLength = 0;
            txtBitacora.SelectionColor = Color.Gray;
            txtBitacora.AppendText($"[{DateTime.Now:HH:mm:ss}] ");
            txtBitacora.SelectionColor = color;
            txtBitacora.AppendText(mensaje + Environment.NewLine);
            txtBitacora.ScrollToCaret();
        }
        #endregion

        #region Acciones del Menú Lateral (Juegos Arcade Reales)
        private void ExplorarRealidad()
        {
            OcultarPanelesSecundarios();
            if (player.NivelEnergia < 10)
            {
                Log("[ALERTA] Energía insuficiente para despliegue terrestre. Recarga primero.", Color.OrangeRed);
                return;
            }

            player.NivelEnergia -= 10;
            Log($"[DESPLIEGUE] Iniciando operación terrestre en {player.RealidadAsignada}...", Color.Cyan);

            timerMandoCabina?.Stop();
            using (var juegoTerrestre = new FormExploracionMetalSlug(player))
            {
                juegoTerrestre.ShowDialog(this);
            }
            this.Activate();
            timerMandoCabina?.Start();
            SeleccionarBotonCabina(indiceBotonCabina);

            Log($"[RETORNO] Operación finalizada. Fragmentos: {player.FragmentosDatos} | XP: {player.XP}.", Color.LimeGreen);
            PostAccion();
        }

        private void IniciarCombateAereo1945()
        {
            OcultarPanelesSecundarios();
            if (player.NivelEnergia < 10)
            {
                Log("[ALERTA] Energía insuficiente para despegue de caza. Recarga primero.", Color.OrangeRed);
                return;
            }

            player.NivelEnergia -= 10;
            Log($"[ESCUADRÓN 1945] Despegando en espacio aéreo de {player.RealidadAsignada}...", Color.OrangeRed);

            timerMandoCabina?.Stop();
            using (var juegoAviones = new FormAviones1945(player))
            {
                juegoAviones.ShowDialog(this);
            }
            this.Activate();
            timerMandoCabina?.Start();
            SeleccionarBotonCabina(indiceBotonCabina);

            Log($"[ATERRIZAJE] Misión aérea concluida. Fragmentos: {player.FragmentosDatos} | XP: {player.XP}.", Color.Gold);
            PostAccion();
        }

        private void EscanearEntorno()
        {
            OcultarPanelesSecundarios();
            if (player.NivelEnergia < 5)
            {
                Log("[ALERTA] Energía insuficiente para usar escáner.", Color.OrangeRed);
                return;
            }

            player.NivelEnergia -= 5;
            player.FragmentosDatos += 1;
            player.MisionActual.NucleoEncontrado = true;
            Log($"[ESCÁNER] Barrido completado en {player.RealidadAsignada}. +1 Fragmento localizado.", Color.DeepSkyBlue);
            PostAccion();
        }

        private void RecargarEnergia()
        {
            OcultarPanelesSecundarios();
            player.NivelEnergia += 25;
            player.NivelEstabilidad += 10;
            Log("[SISTEMA] Células solares y núcleo auxiliar sincronizados. Energía aumentada.", Color.LimeGreen);
            PostAccion();
        }

        private void AlternarEscudos()
        {
            OcultarPanelesSecundarios();
            player.EscudosActivos = !player.EscudosActivos;
            Log(player.EscudosActivos ? "[DEFENSA] Escudos deflectores ACTIVADOS." : "[DEFENSA] Escudos deflectores en ESPERA.", Color.Gold);
            PostAccion();
        }
        #endregion

        #region Integración IA (IRIS)
        private async Task EnviarMensajeAIRIS()
        {
            string msg = txtComandoIA.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            txtComandoIA.Clear();
            Log($"[{player.Nombre}]: {msg}", Color.White);

            try
            {
                string respuesta = await ServicioIA.ConsultarGeminiAsync(player, msg);
                Log($"[IRIS]: {respuesta}", Color.Magenta);
                ServicioIA.HablarAsync(respuesta);
            }
            catch (Exception ex)
            {
                Log($"[ERROR IRIS] Conexión de IA interrumpida: {ex.Message}", Color.Red);
            }
        }

        private async Task GenerarMisionDinamicaIA()
        {
            OcultarPanelesSecundarios();
            Log("[🎯] Solicitando nueva directiva dinámica a IRIS...", Color.DeepSkyBlue);

            try
            {
                string prompt = $"Genera un objetivo táctico de 1 línea para una misión espacial en el sector {player.RealidadAsignada}.";
                string resp = await ServicioIA.ConsultarGeminiAsync(player, prompt);

                tituloMisionDinamica = $"Operación {player.RealidadAsignada}-{rand.Next(10, 99)}";
                descMisionDinamica = resp;
                lblMisionHUD.Text = $"MISIÓN: {tituloMisionDinamica}";

                player.MisionActual.ZonaAlcanzada = false;
                player.MisionActual.NucleoEncontrado = false;
                player.MisionActual.EntidadSobrevivida = false;

                Log($"[NUEVA MISIÓN] {tituloMisionDinamica}: {descMisionDinamica}", Color.Gold);
                ServicioIA.HablarAsync($"Nueva misión asignada: {tituloMisionDinamica}");
            }
            catch
            {
                Log("[ERROR IRIS] No se pudo generar misión dinámica de red. Reintente.", Color.Red);
            }
        }

        private async Task AuditarSistemaPorIA()
        {
            OcultarPanelesSecundarios();
            Log("[🤖] Ejecutando auditoría integral de sistemas por IA...", Color.Cyan);

            try
            {
                string prompt = $"Audita mi estado actual: Energía {player.NivelEnergia}%, Estabilidad {player.NivelEstabilidad}%, Peligro {player.NivelPeligro}%, Fragmentos {player.FragmentosDatos}.";
                string resp = await ServicioIA.ConsultarGeminiAsync(player, prompt);
                Log($"[AUDITORÍA IRIS] {resp}", Color.Magenta);
                ServicioIA.HablarAsync(resp);
            }
            catch
            {
                Log("[ERROR IRIS] Imposible auditar sistemas en este momento.", Color.Red);
            }
        }
        #endregion

        #region Sistema de Combate
        private void CrearPanelCombate()
        {
            pnlCombate = new Panel { Bounds = new Rectangle(312, 150, 898, 530), BackColor = Color.Black, Visible = false };
            Image? imgCombate = BuscarImagen("fondo_combate");
            if (imgCombate != null)
            {
                pnlCombate.BackgroundImage = imgCombate;
                pnlCombate.BackgroundImageLayout = ImageLayout.Stretch;
            }

            Panel ov = new Panel { Location = new Point(20, 20), Size = new Size(858, 490), BackColor = Color.FromArgb(115, 8, 10, 18) };
            lblEnemigoTitulo = new Label { Font = new Font("Consolas", 15, FontStyle.Bold), ForeColor = Color.Red, BackColor = Color.FromArgb(190, 10, 10, 18), Location = new Point(20, 15), AutoSize = true };
            lblEnemigoSalud = new Label { Font = new Font("Consolas", 11, FontStyle.Bold), ForeColor = Color.Orange, BackColor = Color.FromArgb(190, 10, 10, 18), Location = new Point(20, 52), AutoSize = true };
            pbEnemigo = new ProgressBar { Location = new Point(260, 50), Size = new Size(570, 24), Maximum = 120 };
            txtCombateLog = new RichTextBox { Location = new Point(20, 230), Size = new Size(818, 140), Font = new Font("Consolas", 10.5f), BackColor = Color.FromArgb(12, 15, 24), ForeColor = Color.White, ReadOnly = true };

            Button b1 = new Button { Text = "⚡ PULSO EMP", Location = new Point(20, 390), Size = new Size(195, 55), Font = new Font("Consolas", 10, FontStyle.Bold), BackColor = Color.FromArgb(20, 25, 40), ForeColor = Color.Cyan, FlatStyle = FlatStyle.Flat };
            b1.Click += (s, e) => Turno("EMP");

            Button b2 = new Button { Text = "💻 HACKEAR", Location = new Point(228, 390), Size = new Size(195, 55), Font = new Font("Consolas", 10, FontStyle.Bold), BackColor = Color.FromArgb(20, 25, 40), ForeColor = Color.Lime, FlatStyle = FlatStyle.Flat };
            b2.Click += (s, e) => Turno("HACK");

            Button b3 = new Button { Text = "🚀 MÓDULO ATAQUE", Location = new Point(436, 390), Size = new Size(195, 55), Font = new Font("Consolas", 10, FontStyle.Bold), BackColor = Color.FromArgb(20, 25, 40), ForeColor = Color.Gold, FlatStyle = FlatStyle.Flat };
            b3.Click += (s, e) => Turno("MOD");

            Button b4 = new Button { Text = "🏃 EVADIR", Location = new Point(644, 390), Size = new Size(195, 55), Font = new Font("Consolas", 10, FontStyle.Bold), BackColor = Color.FromArgb(20, 25, 40), ForeColor = Color.OrangeRed, FlatStyle = FlatStyle.Flat };
            b4.Click += (s, e) => { pnlCombate.Visible = false; PostAccion(); };

            ov.Controls.AddRange(new Control[] { lblEnemigoTitulo, lblEnemigoSalud, pbEnemigo, txtCombateLog, b1, b2, b3, b4 });
            pnlCombate.Controls.Add(ov);
            this.Controls.Add(pnlCombate);
            pnlCombate.BringToFront();
        }

        private void IniciarCombate(EntidadSistema enemigo)
        {
            if (enemigo == null) return;
            enemigoActual = enemigo;

            lblEnemigoTitulo.Text = $"⚠ AMENAZA DETECTADA: {enemigo.Nombre.ToUpper()} ({enemigo.Tipo})";
            lblEnemigoSalud.Text = $"INTEGRIDAD HOSTIL: {enemigo.Salud}%";
            pbEnemigo.Maximum = enemigo.Salud;
            pbEnemigo.Value = enemigo.Salud;
            txtCombateLog.Clear();
            txtCombateLog.AppendText($"[ALERTA ROJA] {enemigo.Nombre} bloquea tu trayectoria en {player.RealidadAsignada}.\n");
            MostrarPanelSecundario(pnlCombate);
        }

        private void Turno(string accion)
        {
            if (enemigoActual == null) return;

            int dano = accion switch
            {
                "EMP" => rand.Next(25, 40),
                "HACK" => rand.Next(20, 35),
                _ => rand.Next(35, 55)
            };

            enemigoActual.Salud = Math.Max(0, enemigoActual.Salud - dano);
            pbEnemigo.Value = enemigoActual.Salud;
            lblEnemigoSalud.Text = $"INTEGRIDAD HOSTIL: {enemigoActual.Salud}%";
            txtCombateLog.AppendText($"> Ejecutas {accion}: -{dano} de integridad al objetivo.\n");

            if (enemigoActual.Salud <= 0)
            {
                player.XP += 50;
                player.FragmentosDatos += 3;
                player.MisionActual.EntidadSobrevivida = true;
                Log($"[VICTORIA] {enemigoActual.Nombre} neutralizado. +50 XP y +3 Fragmentos.", Color.LimeGreen);
                pnlCombate.Visible = false;
                enemigoActual = null;
                PostAccion();
                return;
            }

            int contraataque = player.EscudosActivos ? rand.Next(4, 10) : rand.Next(12, 22);
            player.NivelEstabilidad -= contraataque;
            txtCombateLog.AppendText($"< Contraataque enemigo: -{contraataque}% estabilidad de casco.\n");
            PostAccion();
        }
        #endregion

        #region Mapa Estelar y Topografía
        private void CrearPanelMapa()
        {
            pnlMapa = new Panel { Bounds = new Rectangle(312, 150, 898, 530), BackColor = Color.FromArgb(235, 10, 16, 28), Visible = false };
            AudioManager.CargarFondoMapa(pnlMapa, player.RealidadAsignada);

            pnlMapa.Controls.Add(new Label { Text = "🗺️ CARTAS ESTELARES // ESCÁNER TÁCTICO", Font = new Font("Consolas", 14, FontStyle.Bold), ForeColor = Color.Cyan, BackColor = Color.FromArgb(190, 10, 16, 28), Location = new Point(25, 20), AutoSize = true });

            lblEstadoEscaner = new Label { Font = new Font("Consolas", 12, FontStyle.Bold), ForeColor = Color.Orange, BackColor = Color.FromArgb(210, 10, 10, 18), Location = new Point(340, 220), AutoSize = true, Visible = false };
            pnlMapa.Controls.Add(lblEstadoEscaner);

            picMapaTactico = new PictureBox { Location = new Point(340, 75), Size = new Size(520, 280), BackColor = Color.Black, SizeMode = PictureBoxSizeMode.StretchImage, Visible = false, BorderStyle = BorderStyle.FixedSingle };
            pnlMapa.Controls.Add(picMapaTactico);

            txtLeyendaCartografica = new RichTextBox { Location = new Point(340, 365), Size = new Size(520, 145), Font = new Font("Consolas", 10f), BackColor = Color.FromArgb(12, 15, 24), ForeColor = Color.White, ReadOnly = true, Visible = false };
            pnlMapa.Controls.Add(txtLeyendaCartografica);

            string[] sectores = new[] { "IRIS-07 (Núcleo Central)", "NEBULA-X9 (Cinturón de Asteroides)", "VOID-00 (Singularidad Cuántica)", "AETHER-4 (Estación Orbital)" };
            int y = 75;
            foreach (string sec in sectores)
            {
                Button bSec = new Button
                {
                    Text = $"SALTAR A: {sec}",
                    Location = new Point(25, y),
                    Size = new Size(295, 45),
                    Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                    BackColor = Color.FromArgb(215, 20, 32, 55),
                    ForeColor = Color.Gold,
                    FlatStyle = FlatStyle.Flat
                };

                bSec.Click += async (s, e) => await ProcesarSaltoYCartografia(sec);
                pnlMapa.Controls.Add(bSec);
                y += 60;
            }
            this.Controls.Add(pnlMapa);
        }

        private async Task ProcesarSaltoYCartografia(string sectorCompleto)
        {
            string codigoSector = sectorCompleto.Split(' ')[0];
            player.RealidadAsignada = codigoSector;
            lblSector.Text = $"SECTOR: {player.RealidadAsignada}";
            GestorGuardado.Guardar(player);

            string nombreFondo = codigoSector.ToLower() switch
            {
                "iris-07" => "fondo_iris",
                "nebula-x9" => "fondo_nebula",
                "void-00" => "fondo_void",
                "aether-4" => "fondo_aether",
                _ => ""
            };

            Image? imgSector = BuscarImagen(nombreFondo);
            if (imgSector != null)
            {
                pnlMapa.BackgroundImage = imgSector;
                pnlMapa.BackgroundImageLayout = ImageLayout.Stretch;
                this.BackgroundImage = imgSector;
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }

            Log($"[SALTO HIPERESPACIAL] Llegada a {codigoSector}. Iniciando escaneo topográfico...", Color.Cyan);

            picMapaTactico.Visible = false;
            txtLeyendaCartografica.Visible = false;

            lblEstadoEscaner.Text = $"[ 🛰️ ESCANEANDO TOPOGRAFÍA DE {codigoSector}... ]\nAnalizando rutas transitables, ríos y peligros...";
            lblEstadoEscaner.Visible = true;
            lblEstadoEscaner.BringToFront();

            await Task.Delay(6000);

            if (lblEstadoEscaner.IsDisposed) return;
            lblEstadoEscaner.Visible = false;

            Image? imgTactica = BuscarImagen($"mapa_tactico_{codigoSector.ToLower()}");
            if (imgTactica != null)
            {
                picMapaTactico.Image = imgTactica;
            }
            else
            {
                picMapaTactico.Image = null;
                picMapaTactico.BackColor = Color.FromArgb(15, 30, 15);
                Log($"[AVISO] Guarda la imagen 'mapa_tactico_{codigoSector.ToLower()}.jpg' en bin\\Debug.", Color.Orange);
            }

            txtLeyendaCartografica.Text = codigoSector switch
            {
                "IRIS-07" => "📍 CARTOGRAFÍA TOPOGRÁFICA: IRIS-07 (NÚCLEO CENTRAL)\n" +
                             "🟦 RÍO CENTRAL: Fluido tóxico de metano. Ralentiza un 40% y corroe el traje.\n" +
                             "🟩 ZONA IZQUIERDA Y CENTRO: Plataforma metálica sólida, segura para caminar.\n" +
                             "🟥 ZONA DERECHA (PELIGRO): Reactores expuestos y torretas hostiles.",
                "NEBULA-X9" => "📍 CARTOGRAFÍA TOPOGRÁFICA: NEBULA-X9 (CINTURÓN DE ASTEROIDES)\n" +
                               "🟦 ESPACIO ABIERTO: Gravedad cero. Solo navegable con propulsores.\n" +
                               "🟩 PLATAFORMA CENTRAL: Roca firme apta para desembarco e infantería.\n" +
                               "🟥 ZONAS ROJAS: Campos minados y lluvia de meteoritos.",
                "VOID-00" => "📍 CARTOGRAFÍA TOPOGRÁFICA: VOID-00 (SINGULARIDAD CUÁNTICA)\n" +
                             "🟦 NIEBLA MAGENTA: Distorsión temporal, reduce velocidad de movimiento un 50%.\n" +
                             "🟩 PUENTE DE ESCOMBROS: Único sendero sólido para cruzar a pie.\n" +
                             "🟥 CENTRO DEL ABISMO: Singularidad letal, desintegración inmediata.",
                "AETHER-4" => "📍 CARTOGRAFÍA TOPOGRÁFICA: AETHER-4 (ESTACIÓN ORBITAL)\n" +
                              "🟦 PISCINAS DE REFRIGERANTE: Agua profunda con amoníaco, evitar inmersión.\n" +
                              "🟩 PISTAS VERDES: Zonas de aterrizaje seguras y despejadas.\n" +
                              "🟥 BARRERAS ELECTRIFICADAS: Muros de plasma de alto voltaje.",
                _ => "📍 CARTOGRAFÍA TOPOGRÁFICA\n🟩 Zonas iluminadas: Transitables.\n🟥 Zonas rojas: Peligro."
            };

            picMapaTactico.Visible = true;
            txtLeyendaCartografica.Visible = true;
            picMapaTactico.BringToFront();
            txtLeyendaCartografica.BringToFront();

            Log($"[CARTOGRAFÍA] Mapa táctico de {codigoSector} desplegado.", Color.LimeGreen);
        }
        #endregion

        #region Inventario y Multijugador
        private void CrearPanelInventario()
        {
            pnlInventario = new Panel { Bounds = new Rectangle(312, 150, 898, 530), BackColor = Color.FromArgb(235, 10, 16, 28), Visible = false };
            pnlInventario.Controls.Add(new Label { Text = "🎒 BODEGA DE CARGA Y MÓDULOS TÁCTICOS", Font = new Font("Consolas", 14, FontStyle.Bold), ForeColor = Color.Gold, Location = new Point(25, 20), AutoSize = true });
            lstInventario = new ListBox { Location = new Point(25, 70), Size = new Size(840, 320), Font = new Font("Consolas", 12), BackColor = Color.FromArgb(16, 24, 40), ForeColor = Color.White };

            Button btnUsarCelula = new Button { Text = "⚡ USAR CÉLULA DE ENERGÍA (+35%)", Location = new Point(25, 410), Size = new Size(340, 48), Font = new Font("Consolas", 10, FontStyle.Bold), BackColor = Color.FromArgb(0, 130, 100), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnUsarCelula.Click += (s, e) =>
            {
                if (player.Inventario.ContainsKey("Células de energía") && player.Inventario["Células de energía"] > 0)
                {
                    player.Inventario["Células de energía"]--;
                    player.NivelEnergia += 35;
                    ActualizarVistaInventario();
                    PostAccion();
                    Log("[INVENTARIO] Célula de energía consumida (+35%).", Color.LimeGreen);
                }
                else
                {
                    Log("[INVENTARIO] No quedan Células de energía disponibles.", Color.Orange);
                }
            };

            pnlInventario.Controls.AddRange(new Control[] { lstInventario, btnUsarCelula });
            this.Controls.Add(pnlInventario);
        }

        private void ActualizarVistaInventario()
        {
            if (lstInventario == null) return;
            lstInventario.Items.Clear();
            if (player.Inventario != null)
            {
                foreach (var item in player.Inventario)
                {
                    lstInventario.Items.Add($"• {item.Key}: {item.Value} unidades disponibles");
                }
            }
            lstInventario.Items.Add($"• Fragmentos de Datos Cuánticos: {player.FragmentosDatos}");
        }

        private void CrearPanelMultijugador()
        {
            pnlMultijugador = new Panel { Bounds = new Rectangle(312, 150, 898, 530), BackColor = Color.FromArgb(235, 10, 16, 28), Visible = false };
            pnlMultijugador.Controls.Add(new Label { Text = "📡 RED TÁCTICA MULTIJUGADOR // BALIZAS ACTIVAS", Font = new Font("Consolas", 14, FontStyle.Bold), ForeColor = Color.Cyan, Location = new Point(25, 20), AutoSize = true });
            RichTextBox txtRed = new RichTextBox { Location = new Point(25, 70), Size = new Size(840, 380), Font = new Font("Consolas", 11), BackColor = Color.FromArgb(16, 24, 40), ForeColor = Color.LimeGreen, ReadOnly = true };
            txtRed.Text = $"[ENLACE ESTABLECIDO] Nodo local: {player.Nombre} ({player.NaveAsignada})\n" +
                          "• Comandante Valkyrie [Sector IRIS-07] - En línea\n" +
                          "• Explorador Kaelen [Sector NEBULA-X9] - En misión de reconocimiento\n" +
                          "• Capitana Nova [Sector AETHER-4] - Escudos al 100%\n\n" +
                          "Todas las balizas sincronizadas con el servidor central NEXUS.";
            pnlMultijugador.Controls.Add(txtRed);
            this.Controls.Add(pnlMultijugador);
        }
        #endregion

        #region Helpers de Interfaz de Usuario
        private void MostrarPanelSecundario(Panel p)
        {
            OcultarPanelesSecundarios();
            p.Visible = true;
            p.BringToFront();
            CargarBotonesSubpanel(p);
        }

        private void OcultarPanelesSecundarios()
        {
            if (pnlCombate != null) pnlCombate.Visible = false;
            if (pnlMapa != null) pnlMapa.Visible = false;
            if (pnlInventario != null) pnlInventario.Visible = false;
            if (pnlMultijugador != null) pnlMultijugador.Visible = false;
            botonesSubpanel.Clear();
        }

        private void PostAccion()
        {
            player.LimitarAtributos();
            ActualizarHUD();
            GestorGuardado.Guardar(player);
        }

        private void ActualizarHUD()
        {
            pbEnergia.Value = Math.Clamp(player.NivelEnergia, 0, 100);
            lblEnergiaVal.Text = $"{pbEnergia.Value}%";

            pbEstabilidad.Value = Math.Clamp(player.NivelEstabilidad, 0, 100);
            lblEstabilidadVal.Text = $"{pbEstabilidad.Value}%";

            pbPeligro.Value = Math.Clamp(player.NivelPeligro, 0, 100);
            lblPeligroVal.Text = $"{pbPeligro.Value}%";

            lblFragVal.Text = $"FRAGMENTOS: {player.FragmentosDatos}";
            lblXPVal.Text = $"XP: {player.XP}";
            lblEscudoEstado.Text = player.EscudosActivos ? "ESCUDOS: ON" : "ESCUDOS: OFF";
            lblEscudoEstado.ForeColor = player.EscudosActivos ? Color.Cyan : Color.Gray;
        }
        #endregion
    }
}