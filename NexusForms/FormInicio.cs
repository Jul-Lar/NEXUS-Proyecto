using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace NexusForms
{
    public partial class FormInicio : Form
    {
        private Panel panelMenu;
        private Panel panelBaseDatos;
        private ListBox lstPartidas;
        private Button btnCargarSeleccionada, btnEliminarSeleccionada, btnCerrarBD;

        private TextBox txtNombre, txtRealidad;
        private ComboBox cmbNave, cmbColor, cmbVelocidad;
        private CheckBox chkElite;
        private Button btnVozNombre, btnIniciar, btnBaseDatos, btnMusica, btnSalir;

        private List<Control> controlesNavegables = new List<Control>();
        private int indiceFoco = 7;
        private Panel? luzMarcadorMenu;

        private System.Windows.Forms.Timer timerMandoMenu;
        private ControlMando.EstadoMando mandoAnterior;
        private int contadorRepeticion = 0;

        // --- MÓDULO DE RECONOCIMIENTO FACIAL BIOMÉTRICO ---
        private Panel panelBiometria;
        private PictureBox picCamara;
        private Label lblEstadoBiometria;
        private Button btnRegistrarRostro, btnAccesoFacial, btnDetenerCamara;
        private ReconocimientoFacial escanerFacial = new ReconocimientoFacial();
        private System.Windows.Forms.Timer timerCamara = new System.Windows.Forms.Timer();
        private bool registrandoRostro = false;
        private int muestrasCapturadas = 0;
        private int framesCoincidencia = 0;
        private string pilotoCandidato = "";

        public FormInicio()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.DoubleBuffered = true;
            AsistenteVozIA.Inicializar();
            ConstruirMenu();
            ConstruirPanelBiometria();
            ConstruirPanelBaseDatos();
            IniciarLectorMandoMenu();

            this.Shown += (s, e) =>
            {
                AudioManager.IniciarMusica();
                this.Activate();
                this.Focus();
            };

            this.FormClosing += (s, e) =>
            {
                timerMandoMenu?.Stop();
                DetenerEscaneoBiometrico();
                escanerFacial.Dispose();
                AsistenteVozIA.Liberar();
            };
        }

        private void ConfirmarYSalirDelJuego()
        {
            MoverFocoA(controlesNavegables.IndexOf(btnSalir));
            AsistenteVozIA.ResponderConVoz("¿Deseas salir de Nexus?");
            var res = MessageBox.Show(
                "¿Seguro que quieres salir de NEXUS OS?",
                "NEXUS // SALIR DEL JUEGO [OPTIONS / START]",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
                Application.Exit();
        }

        private void IniciarLectorMandoMenu()
        {
            timerMandoMenu = new System.Windows.Forms.Timer { Interval = 16 };
            timerMandoMenu.Tick += (s, e) =>
            {
                if (!this.Visible) return;

                var actual = ControlMando.Leer();

                bool pulsaAbajo = actual.Abajo && !mandoAnterior.Abajo;
                bool pulsaArriba = actual.Arriba && !mandoAnterior.Arriba;
                bool pulsaDerecha = actual.Derecha && !mandoAnterior.Derecha;
                bool pulsaIzquierda = actual.Izquierda && !mandoAnterior.Izquierda;

                bool accionActual = actual.BotonSaltar || actual.BotonAcuchillar || actual.BotonEspecial;
                bool accionPrevia = mandoAnterior.BotonSaltar || mandoAnterior.BotonAcuchillar || mandoAnterior.BotonEspecial;

                bool pulsaX = actual.BotonSaltar && !mandoAnterior.BotonSaltar;
                bool pulsaCualquierAccion = accionActual && !accionPrevia;
                bool pulsaCirculo = actual.BotonDisparar && !mandoAnterior.BotonDisparar;
                bool pulsaVoz = actual.BotonVoz && !mandoAnterior.BotonVoz;
                bool pulsaSalir = actual.BotonSalir && !mandoAnterior.BotonSalir;

                if (actual.Abajo || actual.Arriba || actual.Derecha || actual.Izquierda)
                {
                    contadorRepeticion++;
                    if (contadorRepeticion > 10 && contadorRepeticion % 3 == 0)
                    {
                        if (actual.Abajo) pulsaAbajo = true;
                        if (actual.Arriba) pulsaArriba = true;
                        if (actual.Derecha) pulsaDerecha = true;
                        if (actual.Izquierda) pulsaIzquierda = true;
                    }
                }
                else
                {
                    contadorRepeticion = 0;
                }

                mandoAnterior = actual;

                // Botón Options / Start / L2 / R2: Salir del juego en cualquier momento
                if (pulsaSalir)
                {
                    ConfirmarYSalirDelJuego();
                    return;
                }

                // Navegación dentro de la Base de Datos de Partidas si está abierta
                if (panelBaseDatos.Visible)
                {
                    if (pulsaAbajo && lstPartidas.Items.Count > 0)
                    {
                        lstPartidas.SelectedIndex = (lstPartidas.SelectedIndex + 1) % lstPartidas.Items.Count;
                    }
                    else if (pulsaArriba && lstPartidas.Items.Count > 0)
                    {
                        lstPartidas.SelectedIndex = (lstPartidas.SelectedIndex - 1 + lstPartidas.Items.Count) % lstPartidas.Items.Count;
                    }
                    else if (pulsaX)
                    {
                        btnCargarSeleccionada.PerformClick();
                    }
                    else if (pulsaCirculo)
                    {
                        panelBaseDatos.Visible = false;
                        panelMenu.Visible = true;
                        panelBiometria.Visible = true;
                        MoverFocoA(controlesNavegables.IndexOf(btnBaseDatos));
                    }
                    return;
                }

                if (!panelMenu.Visible)
                {
                    if (pulsaCualquierAccion)
                    {
                        this.Activate();
                        panelMenu.Visible = true;
                        panelBiometria.Visible = true;
                        MoverFocoA(controlesNavegables.IndexOf(btnIniciar));
                    }
                    else if (pulsaCirculo)
                    {
                        ConfirmarYSalirDelJuego();
                    }
                    return;
                }

                // Activar dictado de nombre por voz con L1 / R1 / Share o tecla V
                if (pulsaVoz)
                {
                    MoverFocoA(controlesNavegables.IndexOf(btnVozNombre));
                    btnVozNombre.PerformClick();
                    return;
                }

                if (pulsaCirculo)
                {
                    DetenerEscaneoBiometrico();
                    panelMenu.Visible = false;
                    panelBiometria.Visible = false;
                    return;
                }

                Control ctrl = controlesNavegables[indiceFoco];

                if (ctrl is ComboBox cmb)
                {
                    if (pulsaDerecha)
                    {
                        cmb.SelectedIndex = (cmb.SelectedIndex + 1) % cmb.Items.Count;
                        return;
                    }
                    if (pulsaIzquierda)
                    {
                        cmb.SelectedIndex = (cmb.SelectedIndex - 1 + cmb.Items.Count) % cmb.Items.Count;
                        return;
                    }
                }

                if (pulsaAbajo || pulsaDerecha)
                {
                    MoverFocoA(indiceFoco + 1);
                }
                else if (pulsaArriba || pulsaIzquierda)
                {
                    MoverFocoA(indiceFoco - 1);
                }

                if (pulsaX)
                {
                    if (ctrl is CheckBox chk)
                    {
                        chk.Checked = !chk.Checked;
                    }
                    else if (ctrl is Button btn)
                    {
                        btn.PerformClick();
                    }
                    else if (ctrl == txtNombre)
                    {
                        btnVozNombre.PerformClick();
                    }
                    else
                    {
                        btnIniciar.PerformClick();
                    }
                }
            };
            timerMandoMenu.Start();
        }

        private void MoverFocoA(int nuevoIndice)
        {
            if (controlesNavegables.Count == 0) return;
            indiceFoco = (nuevoIndice + controlesNavegables.Count) % controlesNavegables.Count;

            for (int i = 0; i < controlesNavegables.Count; i++)
            {
                bool seleccionado = (i == indiceFoco);
                Control c = controlesNavegables[i];

                if (c is Button b)
                {
                    b.FlatAppearance.BorderColor = seleccionado ? Color.Yellow : Color.FromArgb(0, 180, 220);
                    b.FlatAppearance.BorderSize = seleccionado ? 3 : 1;
                }
                else if (c is TextBox || c is ComboBox)
                {
                    c.BackColor = seleccionado ? Color.FromArgb(42, 74, 125) : Color.FromArgb(18, 28, 48);
                }
                else if (c is CheckBox chk)
                {
                    chk.BackColor = seleccionado ? Color.FromArgb(42, 74, 125) : Color.Transparent;
                }
            }

            Control activo = controlesNavegables[indiceFoco];
            if (luzMarcadorMenu != null)
            {
                if (activo.Parent == panelMenu)
                {
                    luzMarcadorMenu.Location = new Point(activo.Left - 10, activo.Top);
                    luzMarcadorMenu.Height = activo.Height;
                    luzMarcadorMenu.Visible = true;
                    luzMarcadorMenu.BringToFront();
                }
                else
                {
                    luzMarcadorMenu.Visible = false;
                }
            }

            activo.Focus();
        }

        private void ConstruirMenu()
        {
            this.Text = "NEXUS // TERMINAL DE ACCESO";
            this.Size = new Size(1000, 690);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(6, 10, 18);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] rutasFondo =
            {
                Path.Combine(baseDir, "Assets", "fondo_nexus.jpg"),
                Path.Combine(baseDir, "Assets", "fondo_nexus.png"),
                Path.Combine(baseDir, "fondo_nexus.jpg")
            };

            foreach (string rutaFondo in rutasFondo)
            {
                if (File.Exists(rutaFondo))
                {
                    using var fs = new FileStream(rutaFondo, FileMode.Open, FileAccess.Read);
                    this.BackgroundImage = Image.FromStream(fs);
                    this.BackgroundImageLayout = ImageLayout.Stretch;
                    break;
                }
            }

            panelMenu = new Panel
            {
                Size = new Size(560, 625),
                Location = new Point(155, 14),
                BackColor = Color.FromArgb(225, 10, 16, 28),
                Visible = false
            };

            luzMarcadorMenu = new Panel
            {
                Size = new Size(6, 28),
                BackColor = Color.Yellow,
                Visible = false
            };
            panelMenu.Controls.Add(luzMarcadorMenu);

            Label lblBienvenida = new Label
            {
                Text = "✦ BIENVENIDO A NEXUS ✦",
                Font = new Font("Consolas", 13, FontStyle.Bold),
                ForeColor = Color.Gold,
                Location = new Point(25, 12),
                AutoSize = true
            };

            Label lblTitulo = new Label
            {
                Text = "N E X U S   O S",
                Font = new Font("Consolas", 24, FontStyle.Bold),
                ForeColor = Color.Cyan,
                Location = new Point(25, 38),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = "SISTEMA CENTRAL DE EXPLORACIÓN MULTIVERSAL",
                Font = new Font("Consolas", 9.5f),
                ForeColor = Color.LightGray,
                Location = new Point(27, 78),
                AutoSize = true
            };

            int y = 112;

            panelMenu.Controls.Add(new Label { Text = "NOMBRE DEL EXPLORADOR (MICRÓFONO PC):", Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = Color.Cyan, Location = new Point(25, y), AutoSize = true });
            txtNombre = new TextBox
            {
                Text = "Comandante",
                Location = new Point(25, y + 22),
                Size = new Size(400, 28),
                Font = new Font("Consolas", 11),
                BackColor = Color.FromArgb(18, 28, 48),
                ForeColor = Color.White
            };
            panelMenu.Controls.Add(txtNombre);

            btnVozNombre = new Button
            {
                Text = "🎤 VOZ",
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                Location = new Point(432, y + 21),
                Size = new Size(103, 29),
                BackColor = Color.FromArgb(0, 115, 145),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnVozNombre.Click += (s, e) => EscucharNombrePorVoz();
            panelMenu.Controls.Add(btnVozNombre);
            y += 58;

            txtRealidad = CrearCampo(panelMenu, "SECTOR / REALIDAD INICIAL:", ref y, "IRIS-07");

            panelMenu.Controls.Add(new Label { Text = "NAVE ASIGNADA:", Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = Color.Cyan, Location = new Point(25, y), AutoSize = true });
            cmbNave = new ComboBox { Location = new Point(25, y + 22), Size = new Size(245, 28), Font = new Font("Consolas", 10.5f), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(18, 28, 48), ForeColor = Color.White };
            cmbNave.Items.AddRange(new string[] { "Caza Estelar V-9", "Fragata Exploradora", "Interceptor Cuántico" });
            cmbNave.SelectedIndex = 0;
            panelMenu.Controls.Add(cmbNave);

            panelMenu.Controls.Add(new Label { Text = "VELOCIDAD DEL JUEGO:", Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = Color.Cyan, Location = new Point(290, y), AutoSize = true });
            cmbVelocidad = new ComboBox { Location = new Point(290, y + 22), Size = new Size(245, 28), Font = new Font("Consolas", 10.5f), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(18, 28, 48), ForeColor = Color.Gold };
            cmbVelocidad.Items.AddRange(new string[] { "1.0x (Normal)", "2.0x (Acelerada)", "0.5x (Táctica Lenta)" });
            cmbVelocidad.SelectedIndex = 0;
            panelMenu.Controls.Add(cmbVelocidad);
            y += 60;

            panelMenu.Controls.Add(new Label { Text = "TEMA DE INTERFAZ (HUD):", Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = Color.Cyan, Location = new Point(25, y), AutoSize = true });
            cmbColor = new ComboBox { Location = new Point(25, y + 22), Size = new Size(510, 28), Font = new Font("Consolas", 10.5f), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(18, 28, 48), ForeColor = Color.White };
            cmbColor.Items.AddRange(new string[] { "Cian Neón", "Verde Matrix", "Dorado Élite", "Magenta Cuántico" });
            cmbColor.SelectedIndex = 0;
            panelMenu.Controls.Add(cmbColor);
            y += 60;

            chkElite = new CheckBox
            {
                Text = "★ ACTIVAR RANGO ÉLITE (Sistemas al 100% + Protocolo Dios)",
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                ForeColor = Color.Gold,
                Location = new Point(25, y),
                AutoSize = true,
                Checked = true
            };
            panelMenu.Controls.Add(chkElite);
            y += 38;

            btnIniciar = new Button
            {
                Text = "▶ INICIAR NUEVA PARTIDA EN NEXUS",
                Font = new Font("Consolas", 11.5f, FontStyle.Bold),
                Location = new Point(25, y),
                Size = new Size(510, 44),
                BackColor = Color.FromArgb(0, 140, 170),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnIniciar.Click += BtnIniciar_Click;
            panelMenu.Controls.Add(btnIniciar);
            y += 52;

            btnBaseDatos = new Button
            {
                Text = $"🗄️ BASE DE DATOS DE PARTIDAS ({GestorGuardado.ObtenerTodas().Count} GUARDADAS)",
                Font = new Font("Consolas", 10f, FontStyle.Bold),
                Location = new Point(25, y),
                Size = new Size(510, 40),
                BackColor = Color.FromArgb(25, 115, 75),
                ForeColor = Color.Gold,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBaseDatos.Click += (s, e) => AbrirBaseDeDatos();
            panelMenu.Controls.Add(btnBaseDatos);
            y += 48;

            btnMusica = new Button
            {
                Text = "🔊 MÚSICA: ON",
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                Location = new Point(25, y),
                Size = new Size(245, 36),
                BackColor = Color.FromArgb(22, 32, 52),
                ForeColor = Color.LimeGreen,
                FlatStyle = FlatStyle.Flat
            };
            btnMusica.Click += (s, e) =>
            {
                AudioManager.AlternarMusica();
                btnMusica.Text = AudioManager.MusicaActiva ? "🔊 MÚSICA: ON" : "🔇 MÚSICA: OFF";
                btnMusica.ForeColor = AudioManager.MusicaActiva ? Color.LimeGreen : Color.Gray;
            };

            btnSalir = new Button
            {
                Text = "✖ SALIR [OPTIONS]",
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                Location = new Point(290, y),
                Size = new Size(245, 36),
                BackColor = Color.FromArgb(80, 20, 25),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSalir.Click += (s, e) => Application.Exit();

            panelMenu.Controls.AddRange(new Control[] { lblBienvenida, lblTitulo, lblSub, btnMusica, btnSalir });
            this.Controls.Add(panelMenu);

            controlesNavegables = new List<Control>
            {
                txtNombre,      // 0
                btnVozNombre,   // 1
                txtRealidad,    // 2
                cmbNave,        // 3
                cmbVelocidad,   // 4
                cmbColor,       // 5
                chkElite,       // 6
                btnIniciar,     // 7
                btnBaseDatos,   // 8
                btnMusica,      // 9
                btnSalir        // 10
            };
        }

        // --- CONSTRUCCIÓN DEL PANEL LATERAL DE RECONOCIMIENTO FACIAL ---
        private void ConstruirPanelBiometria()
        {
            panelBiometria = new Panel
            {
                Size = new Size(245, 625),
                Location = new Point(730, 14),
                BackColor = Color.FromArgb(225, 10, 16, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            Label lblTituloBio = new Label
            {
                Text = "👁️ VISOR BIOMÉTRICO",
                Font = new Font("Consolas", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 255, 195),
                Location = new Point(14, 15),
                AutoSize = true
            };

            Label lblSubBio = new Label
            {
                Text = "ESCÁNER FACIAL NEXUS",
                Font = new Font("Consolas", 8.5f),
                ForeColor = Color.LightGray,
                Location = new Point(15, 38),
                AutoSize = true
            };

            picCamara = new PictureBox
            {
                Location = new Point(14, 65),
                Size = new Size(215, 225),
                BackColor = Color.FromArgb(6, 14, 24),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            lblEstadoBiometria = new Label
            {
                Text = "SENSOR EN ESPERA.\nRegistra tu rostro o inicia\nacceso mirando a la cámara.",
                Font = new Font("Consolas", 8.5f, FontStyle.Bold),
                ForeColor = Color.Cyan,
                Location = new Point(14, 300),
                Size = new Size(215, 62)
            };

            btnRegistrarRostro = new Button
            {
                Text = "📸 REGISTRAR ROSTRO",
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                Location = new Point(14, 372),
                Size = new Size(215, 44),
                BackColor = Color.FromArgb(0, 120, 130),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRegistrarRostro.Click += BtnRegistrarRostro_Click;

            btnAccesoFacial = new Button
            {
                Text = "👁️ ACCESO BIOMÉTRICO",
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                Location = new Point(14, 426),
                Size = new Size(215, 48),
                BackColor = Color.FromArgb(15, 135, 95),
                ForeColor = Color.FromArgb(220, 255, 240),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAccesoFacial.Click += BtnAccesoFacial_Click;

            btnDetenerCamara = new Button
            {
                Text = "⏹ APAGAR SENSOR",
                Font = new Font("Consolas", 9f, FontStyle.Bold),
                Location = new Point(14, 484),
                Size = new Size(215, 36),
                BackColor = Color.FromArgb(40, 48, 68),
                ForeColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDetenerCamara.Click += (s, e) => DetenerEscaneoBiometrico();

            Label lblNotaBio = new Label
            {
                Text = "Tip: Escribe tu nombre a la\nizquierda antes de pulsar\n'Registrar Rostro'.",
                Font = new Font("Consolas", 8f),
                ForeColor = Color.Gold,
                Location = new Point(14, 540),
                Size = new Size(215, 65)
            };

            panelBiometria.Controls.AddRange(new Control[]
            {
                lblTituloBio, lblSubBio, picCamara, lblEstadoBiometria,
                btnRegistrarRostro, btnAccesoFacial, btnDetenerCamara, lblNotaBio
            });
            this.Controls.Add(panelBiometria);

            controlesNavegables.Add(btnRegistrarRostro); // 11
            controlesNavegables.Add(btnAccesoFacial);    // 12
            controlesNavegables.Add(btnDetenerCamara);   // 13

            timerCamara.Interval = 33;
            timerCamara.Tick += TimerCamara_Tick;
        }

        private void BtnRegistrarRostro_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MostrarAlertaNexus(
                    "NEXUS // BIOMETRÍA",
                    "NOMBRE DE PILOTO REQUERIDO",
                    "Escribe primero tu nombre de Comandante a la izquierda antes de registrar tu rostro.",
                    Color.Gold
                );
                return;
            }

            if (!escanerFacial.IniciarCamara())
            {
                MostrarAlertaNexus(
                    "NEXUS // ERROR DE SENSOR",
                    "CÁMARA WEB NO DETECTADA",
                    "No se encontró ninguna cámara web activa en esta PC.\nConecta una cámara o verifica los permisos de Windows.",
                    Color.OrangeRed
                );
                return;
            }

            registrandoRostro = true;
            muestrasCapturadas = 0;
            lblEstadoBiometria.ForeColor = Color.FromArgb(0, 255, 195);
            lblEstadoBiometria.Text = $"CAPTURANDO ROSTRO DE:\n{txtNombre.Text.Trim().ToUpper()} (0/15)...";
            AsistenteVozIA.ResponderConVoz("Iniciando registro biométrico. Mira fijamente al escáner.");
            timerCamara.Start();
        }

        private void BtnAccesoFacial_Click(object? sender, EventArgs e)
        {
            if (!escanerFacial.IniciarCamara())
            {
                MostrarAlertaNexus(
                    "NEXUS // ERROR DE SENSOR",
                    "CÁMARA WEB NO DETECTADA",
                    "No se encontró ninguna cámara web activa en esta PC.\nConecta una cámara o verifica los permisos de Windows.",
                    Color.OrangeRed
                );
                return;
            }

            registrandoRostro = false;
            framesCoincidencia = 0;
            pilotoCandidato = "";
            lblEstadoBiometria.ForeColor = Color.Cyan;
            lblEstadoBiometria.Text = "ESCANEANDO ROSTRO...\nMira a la cámara para entrar.";
            AsistenteVozIA.ResponderConVoz("Escáner facial activado.");
            timerCamara.Start();
        }

        private void DetenerEscaneoBiometrico()
        {
            timerCamara.Stop();
            escanerFacial.DetenerCamara();
            registrandoRostro = false;
            if (picCamara != null)
            {
                picCamara.Image?.Dispose();
                picCamara.Image = null;
            }
            if (lblEstadoBiometria != null)
            {
                lblEstadoBiometria.ForeColor = Color.Cyan;
                lblEstadoBiometria.Text = "SENSOR EN ESPERA.\nRegistra tu rostro o inicia\nacceso mirando a la cámara.";
            }
        }

        private void TimerCamara_Tick(object? sender, EventArgs e)
        {
            var (frameVisual, detectado, piloto, similitud, rostroMat) = escanerFacial.ProcesarFrame(registrandoRostro);

            if (frameVisual != null)
            {
                picCamara.Image?.Dispose();
                picCamara.Image = frameVisual;
            }

            if (detectado && rostroMat != null)
            {
                using (rostroMat)
                {
                    if (registrandoRostro)
                    {
                        muestrasCapturadas++;
                        escanerFacial.GuardarMuestraRostro(txtNombre.Text, rostroMat, muestrasCapturadas);
                        lblEstadoBiometria.Text = $"CAPTURANDO BIOMETRÍA...\nMuestras: {muestrasCapturadas} / 15";

                        if (muestrasCapturadas >= 15)
                        {
                            string nombreRegistrado = txtNombre.Text.Trim();
                            DetenerEscaneoBiometrico();
                            lblEstadoBiometria.ForeColor = Color.LimeGreen;
                            lblEstadoBiometria.Text = $"✔ ROSTRO REGISTRADO:\n{nombreRegistrado.ToUpper()}";
                            AsistenteVozIA.ResponderConVoz($"Biometría vinculada para {nombreRegistrado}.");

                            MostrarAlertaNexus(
                                "NEXUS // ACCESO VINCULADO",
                                $"✔ BIOMETRÍA ACTIVA: {nombreRegistrado.ToUpper()}",
                                "Tus rasgos faciales han sido guardados en el núcleo.\nAhora puedes entrar mirando a la cámara con 'ACCESO BIOMÉTRICO'.",
                                Color.FromArgb(0, 255, 195)
                            );
                        }
                    }
                    else
                    {
                        if (similitud >= 0.72 && !string.IsNullOrEmpty(piloto))
                        {
                            if (piloto == pilotoCandidato) framesCoincidencia++;
                            else { pilotoCandidato = piloto; framesCoincidencia = 1; }

                            int progreso = Math.Min(100, (framesCoincidencia * 100) / 30);
                            lblEstadoBiometria.ForeColor = Color.FromArgb(0, 255, 195);
                            lblEstadoBiometria.Text = $"PILOTO: {piloto.ToUpper()}\nVERIFICANDO ACCESO: {progreso}%";

                            if (framesCoincidencia >= 30)
                            {
                                string pilotoAutorizado = pilotoCandidato;
                                DetenerEscaneoBiometrico();
                                DesbloquearAccesoPorRostro(pilotoAutorizado);
                            }
                        }
                        else
                        {
                            framesCoincidencia = 0;
                            lblEstadoBiometria.ForeColor = Color.Gold;
                            lblEstadoBiometria.Text = "ROSTRO DETECTADO...\nBuscando coincidencia en BD...";
                        }
                    }
                }
            }
        }

        private void DesbloquearAccesoPorRostro(string nombrePiloto)
        {
            txtNombre.Text = nombrePiloto;
            AsistenteVozIA.ResponderConVoz($"Acceso concedido. Bienvenido Comandante {nombrePiloto}.");

            // Si ya existe una partida guardada con ese nombre de piloto, la carga automáticamente
            var partidaExistente = GestorGuardado.ObtenerTodas()
                .FirstOrDefault(p => p.Nombre.Equals(nombrePiloto, StringComparison.OrdinalIgnoreCase));

            if (partidaExistente != null)
            {
                Jugador jGuardado = GestorGuardado.ConvertirAJugador(partidaExistente);
                AbrirCabina(jGuardado);
            }
            else
            {
                // Si es la primera vez que entra tras registrar su cara, inicia nueva partida con su nombre
                btnIniciar.PerformClick();
            }
        }

        private void MostrarAlertaNexus(string titulo, string subtitulo, string mensaje, Color colorNeon)
        {
            using Form modal = new Form
            {
                Size = new Size(490, 240),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.FromArgb(10, 18, 32),
                ShowInTaskbar = false,
                KeyPreview = true
            };

            modal.Paint += (s, e) =>
            {
                using Pen penExterno = new Pen(colorNeon, 2.5f);
                using Pen penInterno = new Pen(Color.FromArgb(70, colorNeon), 1f);
                e.Graphics.DrawRectangle(penExterno, 1, 1, modal.Width - 3, modal.Height - 3);
                e.Graphics.DrawRectangle(penInterno, 6, 6, modal.Width - 13, modal.Height - 13);
            };

            Panel barraSuperior = new Panel
            {
                Location = new Point(2, 2),
                Size = new Size(modal.Width - 4, 34),
                BackColor = Color.FromArgb(18, 32, 54)
            };

            Label lblTitulo = new Label
            {
                Text = $"◈ {titulo.ToUpper()}",
                Font = new Font("Consolas", 10f, FontStyle.Bold),
                ForeColor = colorNeon,
                Location = new Point(12, 8),
                AutoSize = true
            };
            barraSuperior.Controls.Add(lblTitulo);

            Label lblSub = new Label
            {
                Text = subtitulo.ToUpper(),
                Font = new Font("Consolas", 11.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(22, 50),
                AutoSize = true
            };

            Label lblCuerpo = new Label
            {
                Text = mensaje,
                Font = new Font("Consolas", 9.5f),
                ForeColor = Color.LightGray,
                Location = new Point(22, 82),
                Size = new Size(445, 78)
            };

            Button btnOk = new Button
            {
                Text = "✔ ENTENDIDO [ENTER / X]",
                Font = new Font("Consolas", 10f, FontStyle.Bold),
                Location = new Point(125, 172),
                Size = new Size(240, 42),
                BackColor = Color.FromArgb(0, 125, 115),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderColor = colorNeon;
            btnOk.FlatAppearance.BorderSize = 2;
            btnOk.Click += (s, e) => modal.Close();

            modal.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape || e.KeyCode == Keys.Space)
                    modal.Close();
            };

            System.Windows.Forms.Timer timerModalMando = new System.Windows.Forms.Timer { Interval = 30 };
            timerModalMando.Tick += (s, e) =>
            {
                var m = ControlMando.Leer();
                if (m.BotonSaltar || m.BotonDisparar)
                {
                    timerModalMando.Stop();
                    modal.Close();
                }
            };
            modal.FormClosed += (s, e) => timerModalMando.Dispose();
            timerModalMando.Start();

            modal.Controls.AddRange(new Control[] { barraSuperior, lblSub, lblCuerpo, btnOk });
            modal.ShowDialog(this);
        }

        private void ConstruirPanelBaseDatos()
        {
            panelBaseDatos = new Panel
            {
                Size = new Size(820, 480),
                Location = new Point(90, 95),
                BackColor = Color.FromArgb(240, 10, 16, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            Label lblTituloBD = new Label
            {
                Text = "🗄️ BASE DE DATOS DE PARTIDAS GUARDADAS",
                Font = new Font("Consolas", 15, FontStyle.Bold),
                ForeColor = Color.Cyan,
                Location = new Point(24, 18),
                AutoSize = true
            };

            Label lblSubBD = new Label
            {
                Text = "Haz clic (o doble clic) en cualquier registro para volver exactamente a tu progreso:",
                Font = new Font("Consolas", 9.5f),
                ForeColor = Color.Gold,
                Location = new Point(24, 50),
                AutoSize = true
            };

            lstPartidas = new ListBox
            {
                Location = new Point(24, 82),
                Size = new Size(770, 310),
                Font = new Font("Consolas", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 24, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            lstPartidas.DoubleClick += (s, e) => CargarPartidaDeLaLista();

            btnCargarSeleccionada = new Button
            {
                Text = "▶ VOLVER A ESTE PROGRESO",
                Font = new Font("Consolas", 10.5f, FontStyle.Bold),
                Location = new Point(24, 410),
                Size = new Size(295, 45),
                BackColor = Color.FromArgb(0, 145, 110),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCargarSeleccionada.Click += (s, e) => CargarPartidaDeLaLista();

            btnEliminarSeleccionada = new Button
            {
                Text = "🗑️ ELIMINAR REGISTRO",
                Font = new Font("Consolas", 10f, FontStyle.Bold),
                Location = new Point(335, 410),
                Size = new Size(225, 45),
                BackColor = Color.FromArgb(120, 30, 35),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnEliminarSeleccionada.Click += (s, e) =>
            {
                if (lstPartidas.SelectedItem is RegistroPartida reg)
                {
                    GestorGuardado.EliminarPartida(reg.Id);
                    RefrescarListaPartidas();
                }
            };

            btnCerrarBD = new Button
            {
                Text = "↩ VOLVER AL MENÚ",
                Font = new Font("Consolas", 10f, FontStyle.Bold),
                Location = new Point(575, 410),
                Size = new Size(219, 45),
                BackColor = Color.FromArgb(30, 42, 68),
                ForeColor = Color.Cyan,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCerrarBD.Click += (s, e) =>
            {
                panelBaseDatos.Visible = false;
                panelMenu.Visible = true;
                panelBiometria.Visible = true;
                MoverFocoA(controlesNavegables.IndexOf(btnBaseDatos));
            };

            panelBaseDatos.Controls.AddRange(new Control[]
            {
                lblTituloBD, lblSubBD, lstPartidas,
                btnCargarSeleccionada, btnEliminarSeleccionada, btnCerrarBD
            });
            this.Controls.Add(panelBaseDatos);
            panelBaseDatos.BringToFront();
        }

        private void AbrirBaseDeDatos()
        {
            DetenerEscaneoBiometrico();
            RefrescarListaPartidas();
            panelMenu.Visible = false;
            panelBiometria.Visible = false;
            panelBaseDatos.Visible = true;
            panelBaseDatos.BringToFront();
            lstPartidas.Focus();
        }

        private void RefrescarListaPartidas()
        {
            var todas = GestorGuardado.ObtenerTodas();
            lstPartidas.Items.Clear();
            foreach (var p in todas)
            {
                lstPartidas.Items.Add(p);
            }
            if (lstPartidas.Items.Count > 0)
            {
                lstPartidas.SelectedIndex = 0;
            }
            btnBaseDatos.Text = $"🗄️ BASE DE DATOS DE PARTIDAS ({todas.Count} GUARDADAS)";
        }

        private void CargarPartidaDeLaLista()
        {
            if (lstPartidas.SelectedItem is not RegistroPartida reg)
            {
                MostrarAlertaNexus(
                    "NEXUS // BASE DE DATOS",
                    "NINGÚN REGISTRO SELECCIONADO",
                    "Selecciona primero una partida de la lista antes de continuar.",
                    Color.Gold
                );
                return;
            }

            Jugador j = GestorGuardado.ConvertirAJugador(reg);
            panelBaseDatos.Visible = false;
            panelMenu.Visible = true;
            panelBiometria.Visible = true;
            AbrirCabina(j);
        }

        private void EscucharNombrePorVoz()
        {
            AsistenteVozIA.DictarTextoParaControl(
                nombreDetectado =>
                {
                    if (!this.IsDisposed)
                        this.BeginInvoke(new Action(() => txtNombre.Text = nombreDetectado));
                },
                (textoBoton, grabando) =>
                {
                    if (!this.IsDisposed)
                        this.BeginInvoke(new Action(() =>
                        {
                            btnVozNombre.Text = textoBoton;
                            btnVozNombre.BackColor = grabando
                                ? Color.FromArgb(180, 35, 35)
                                : Color.FromArgb(0, 115, 145);
                        }));
                },
                mensajeError =>
                {
                    if (!this.IsDisposed)
                        this.BeginInvoke(new Action(() =>
                            MostrarAlertaNexus("NEXUS // VOZ", "AVISO DEL SISTEMA DE VOZ", mensajeError, Color.Cyan)));
                }
            );
        }

        private TextBox CrearCampo(Panel p, string etiqueta, ref int y, string def)
        {
            p.Controls.Add(new Label { Text = etiqueta, Font = new Font("Consolas", 9.5f, FontStyle.Bold), ForeColor = Color.Cyan, Location = new Point(25, y), AutoSize = true });
            TextBox tb = new TextBox { Text = def, Location = new Point(25, y + 22), Size = new Size(510, 28), Font = new Font("Consolas", 11), BackColor = Color.FromArgb(18, 28, 48), ForeColor = Color.White };
            p.Controls.Add(tb);
            y += 58;
            return tb;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!panelBaseDatos.Visible && (keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Left || keyData == Keys.Right))
            {
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private int ObtenerVelocidadMs()
        {
            return cmbVelocidad.SelectedIndex switch
            {
                1 => 500,
                2 => 2000,
                _ => 1000
            };
        }

        private void AbrirCabina(Jugador jugador)
        {
            DetenerEscaneoBiometrico();
            timerMandoMenu.Stop();
            this.Hide();
            using (FormJuego cabina = new FormJuego(jugador, ObtenerVelocidadMs()))
            {
                cabina.ShowDialog();
            }

            btnBaseDatos.Text = $"🗄️ BASE DE DATOS DE PARTIDAS ({GestorGuardado.ObtenerTodas().Count} GUARDADAS)";

            this.Show();
            this.Activate();
            timerMandoMenu.Start();
            if (panelMenu.Visible) MoverFocoA(indiceFoco);
        }

        private void BtnIniciar_Click(object? sender, EventArgs e)
        {
            Color tema = cmbColor.SelectedIndex switch
            {
                1 => Color.Lime,
                2 => Color.Gold,
                3 => Color.Magenta,
                _ => Color.Cyan
            };

            Jugador j = new Jugador
            {
                Nombre = string.IsNullOrWhiteSpace(txtNombre.Text) ? "Explorador" : txtNombre.Text.Trim(),
                RealidadAsignada = string.IsNullOrWhiteSpace(txtRealidad.Text) ? "IRIS-07" : txtRealidad.Text.Trim(),
                NaveAsignada = cmbNave.SelectedItem?.ToString() ?? "Caza Estelar V-9",
                ColorTema = tema,
                EsElite = chkElite.Checked
            };

            if (j.EsElite)
            {
                j.NivelEnergia = 100;
                j.NivelEstabilidad = 100;
                j.NivelPeligro = 0;
                j.EscudosActivos = true;
                j.FragmentosDatos = 5;
            }

            // Crea un nuevo registro en la base de datos y lo deja activo
            GestorGuardado.Guardar(j);
            AbrirCabina(j);
        }
    }
}