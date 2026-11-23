using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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

        public FormInicio()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.DoubleBuffered = true;
            AsistenteVozIA.Inicializar();
            ConstruirMenu();
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
                    panelMenu.Visible = false;
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
                luzMarcadorMenu.Location = new Point(activo.Left - 10, activo.Top);
                luzMarcadorMenu.Height = activo.Height;
                luzMarcadorMenu.Visible = true;
                luzMarcadorMenu.BringToFront();
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

            string rutaFondo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fondo_nexus.jpg");
            if (File.Exists(rutaFondo))
            {
                this.BackgroundImage = Image.FromFile(rutaFondo);
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }

            panelMenu = new Panel
            {
                Size = new Size(560, 625),
                Location = new Point(220, 14),
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
            RefrescarListaPartidas();
            panelMenu.Visible = false;
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
                MessageBox.Show("Selecciona primero una partida de la lista.", "NEXUS // BASE DE DATOS");
                return;
            }

            Jugador j = GestorGuardado.ConvertirAJugador(reg);
            panelBaseDatos.Visible = false;
            panelMenu.Visible = true;
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
                            MessageBox.Show(mensajeError, "NEXUS // VOZ")));
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