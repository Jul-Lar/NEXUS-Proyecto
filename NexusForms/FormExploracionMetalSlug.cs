using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NexusForms
{
    public class PersonajeJugable
    {
        public string Nombre { get; set; } = "";
        public string Rol { get; set; } = "";
        public string FormaVisual { get; set; } = "";
        public string NombreMapaPropio { get; set; } = "";
        public string EstiloCartografia { get; set; } = "";
        public string VentajaExploracion { get; set; } = "";
        public Color ColorPrincipal { get; set; }
        public Color ColorSecundario { get; set; }
        public Color ColorFondoVacio { get; set; }
        public Color ColorTierra { get; set; }
        public Color ColorBordeTierra { get; set; }
        public Color ColorAguaLava { get; set; }
        public Color ColorFlora { get; set; }
        public float Velocidad { get; set; }
        public int SaludMax { get; set; }
        public int DanioBase { get; set; }
    }

    public class FormExploracionMetalSlug : Form
    {
        private Jugador playerGlobal;
        private List<PersonajeJugable> catalogoExploradores = new List<PersonajeJugable>();
        private PersonajeJugable exploradorActual = null!;

        private Panel pnlSelector = null!;
        private Panel? pnlReporteFinal;
        private System.Windows.Forms.Timer? gameTimer;
        private System.Windows.Forms.Timer? timerSelectorMando;
        private System.Windows.Forms.Timer? timerReporteMando;
        private List<Button> botonesSelector = new List<Button>();
        private int indiceSelector = 0;
        private ControlMando.EstadoMando mandoPrevio;

        // Mundo de Exploración (3000 x 2400) con Niebla de Guerra
        private const int MUNDO_W = 3000;
        private const int MUNDO_H = 2400;
        private const int CELDA_NIEBLA = 120;
        private bool[,] zonaDescubierta = new bool[MUNDO_W / CELDA_NIEBLA + 1, MUNDO_H / CELDA_NIEBLA + 1];
        private float camX = 0, camY = 0;

        // CARTOGRAFÍA FÍSICA ÚNICA POR MAPA
        private bool usaIslasFlotantes = false;
        private List<Rectangle> zonasTierra = new List<Rectangle>();
        private List<Rectangle> murosSolidos = new List<Rectangle>();
        private List<Rectangle> zonasPeligroAguaLava = new List<Rectangle>();
        private List<Rectangle> puentesSeguros = new List<Rectangle>();
        private List<(PointF A, PointF B, Color Color)> portalesWarp = new List<(PointF A, PointF B, Color Color)>();
        private int cdWarp = 0;

        // Estado del Explorador
        private float jugX = 1500, jugY = 1950;
        private float dirX = 0, dirY = -1;
        private int salud = 200, maxSalud = 200;
        private int fragmentos = 0;
        private int enemigosVencidos = 0;
        private int cofresAbiertos = 0;
        private int tickContador = 0;
        private int frameAnimacion = 0;
        private int screenShake = 0;
        private bool expedicionTerminada = false;

        // Gemas Elementales recolectadas
        private bool tieneGemaRubi = false;
        private bool tieneGemaZafiro = false;
        private bool tieneGemaEsmeralda = false;
        private bool tieneGemaAmatista = false;

        // Cooldowns de Combate
        private int cdDisparo = 0;
        private int cdEspada = 0;
        private int animEspada = 0;
        private int cdDash = 0;
        private int dashActivo = 0;
        private int cargaPoderGemas = 100;

        // Controles
        private bool keyIzq, keyDer, keyArr, keyAba;
        private bool keyDisparo, keyEspada, keyDash, keyPoder;
        private bool prevEspada = false, prevDash = false, prevPoder = false, prevVoz = false, prevSalir = false;

        // Banner
        private string mensajeAviso = "";
        private int avisoTimer = 240;

        // Caché GDI+ para 60 FPS sin pausas de Garbage Collector
        private static readonly Font fontSantuario = new Font("Consolas", 11f, FontStyle.Bold);
        private static readonly Font fontPortal = new Font("Consolas", 10f, FontStyle.Bold);
        private static readonly Font fontGema = new Font("Consolas", 9f, FontStyle.Bold);
        private static readonly Font fontEnemigo = new Font("Consolas", 8.5f, FontStyle.Bold);
        private static readonly Font fontHud = new Font("Consolas", 10.2f, FontStyle.Bold);
        private static readonly Font fontAviso = new Font("Consolas", 9.3f, FontStyle.Bold);
        private static readonly Font fontSlot = new Font("Consolas", 8f, FontStyle.Bold);

        private static readonly SolidBrush brNieblaCache = new SolidBrush(Color.FromArgb(215, 4, 7, 14));
        private static readonly SolidBrush brHudBgCache = new SolidBrush(Color.FromArgb(230, 8, 12, 22));
        private static readonly SolidBrush brAvisoBgCache = new SolidBrush(Color.FromArgb(215, 10, 16, 28));
        private static readonly SolidBrush brMinimapBgCache = new SolidBrush(Color.FromArgb(230, 6, 10, 18));
        private static readonly SolidBrush brPuenteCache = new SolidBrush(Color.FromArgb(62, 78, 102));
        private static readonly SolidBrush brCofreCerradoCache = new SolidBrush(Color.Goldenrod);
        private static readonly SolidBrush brCofreAbiertoCache = new SolidBrush(Color.FromArgb(55, 55, 65));
        private static readonly SolidBrush brHieloCache = new SolidBrush(Color.FromArgb(110, Color.DeepSkyBlue));
        private static readonly SolidBrush brSlotVacioCache = new SolidBrush(Color.FromArgb(22, 28, 40));
        private static readonly Pen penNubeCache = new Pen(Color.FromArgb(30, Color.White), 2);
        private static readonly Pen penPuenteCache = new Pen(Color.Gold, 2.5f);
        private static readonly Pen penGemaBordeCache = new Pen(Color.White, 2);

        private SolidBrush? brVacioMapa;
        private SolidBrush? brTierraMapa;
        private SolidBrush? brPeligroMapa;
        private SolidBrush? brIslaMinimap;
        private Pen? penBordeTierraMapa;
        private Pen? penHudBordeMapa;

        // Control anti-saturación de audio
        private string ultimoSonido = "";
        private int tickUltimoSonido = -10;

        // Entidades del Mundo
        private List<RuinaSantuario> ruinas = new List<RuinaSantuario>();
        private List<DecoracionMapa> decoraciones = new List<DecoracionMapa>();
        private List<GemaMundo> gemas = new List<GemaMundo>();
        private List<CofreTesoro> cofres = new List<CofreTesoro>();
        private List<GuardianEnemigo> enemigos = new List<GuardianEnemigo>();
        private List<ProyectilAventura> proyectiles = new List<ProyectilAventura>();
        private List<OndaVisual> ondas = new List<OndaVisual>();
        private List<ParticulaAventura> particulas = new List<ParticulaAventura>();
        private List<ParticulaAmbiente> ambienteFlotante = new List<ParticulaAmbiente>();
        private PointF altarPortal = new PointF(1500, 320);
        private Random rand = new Random();

        private class RuinaSantuario { public Rectangle Rect; public string Nombre = ""; public Color ColorBorde; }
        private class DecoracionMapa { public float X, Y; public string Tipo = ""; public Color Color; }
        private class ParticulaAmbiente { public float X, Y, VX, VY, Fase; public Color Color; }
        private class GemaMundo { public string Id = "", Nombre = "", Efecto = ""; public float X, Y; public Color Color; public bool Recogida; }
        private class CofreTesoro { public float X, Y; public bool Abierto; }
        private class GuardianEnemigo
        {
            public string Nombre = "", Tipo = "";
            public bool EsJefeGuardian;
            public float X, Y, HomeX, HomeY;
            public int Salud, MaxSalud, CooldownDisparo, CongeladoTimer, FlashImpacto;
        }
        private class ProyectilAventura { public float X, Y, VX, VY; public bool EsJugador, Congela; public int Danio, Vida, Radio; public Color Color; }
        private class OndaVisual { public float X, Y, Radio; public Color Color; public int Vida, MaxVida; }
        private class ParticulaAventura { public float X, Y, VX, VY, Tamano; public Color Color; public int Vida, MaxVida; }

        public FormExploracionMetalSlug(Jugador jugador)
        {
            playerGlobal = jugador;
            this.Text = $"NEXUS // CARTOGRAFÍA MULTIVERSO - SECTOR {playerGlobal.RealidadAsignada}";
            this.Size = new Size(1120, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.BackColor = Color.FromArgb(10, 14, 22);

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            SintetizadorSonido.Inicializar();
            AsistenteVozIA.Inicializar();
            InicializarExploradores();
            MostrarSelectorExploradores();
        }

        private void TocarSonidoRapido(string nombre, int volumen = -1)
        {
            if (ultimoSonido == nombre && tickContador - tickUltimoSonido < 3)
                return;

            ultimoSonido = nombre;
            tickUltimoSonido = tickContador;

            Task.Run(() =>
            {
                try
                {
                    if (volumen >= 0) SintetizadorSonido.Reproducir(nombre, volumen);
                    else SintetizadorSonido.Reproducir(nombre);
                }
                catch { }
            });
        }

        private void InicializarExploradores()
        {
            catalogoExploradores = new List<PersonajeJugable>
            {
                new PersonajeJugable
                {
                    Nombre = "VALKYRIE",
                    Rol = "Cazadora Celeste",
                    FormaVisual = "Caza",
                    NombreMapaPropio = "ARCHIPIÉLAGO DE ISLAS",
                    EstiloCartografia = "7 Islas flotantes sobre las nubes conectadas por puentes estrechos.",
                    VentajaExploracion = "Alta velocidad + Radar amplio en las islas del cielo.",
                    ColorPrincipal = Color.DeepSkyBlue,
                    ColorSecundario = Color.Cyan,
                    ColorFondoVacio = Color.FromArgb(8, 18, 36),
                    ColorTierra = Color.FromArgb(26, 48, 78),
                    ColorBordeTierra = Color.Cyan,
                    ColorAguaLava = Color.FromArgb(0, 180, 255),
                    ColorFlora = Color.Cyan,
                    Velocidad = 5.6f,
                    SaludMax = 200,
                    DanioBase = 26
                },
                new PersonajeJugable
                {
                    Nombre = "KAELEN",
                    Rol = "Guardián de Magma",
                    FormaVisual = "Tanque",
                    NombreMapaPropio = "CRÁTER DE ANILLOS",
                    EstiloCartografia = "Lago central de magma rodeado por anillos de obsidiana y pasarelas.",
                    VentajaExploracion = "Gran resistencia + Espada ancha contra titanes.",
                    ColorPrincipal = Color.Goldenrod,
                    ColorSecundario = Color.Gold,
                    ColorFondoVacio = Color.FromArgb(24, 12, 12),
                    ColorTierra = Color.FromArgb(44, 26, 24),
                    ColorBordeTierra = Color.OrangeRed,
                    ColorAguaLava = Color.FromArgb(235, 75, 15),
                    ColorFlora = Color.Gold,
                    Velocidad = 4.8f,
                    SaludMax = 290,
                    DanioBase = 32
                },
                new PersonajeJugable
                {
                    Nombre = "NOVA",
                    Rol = "Erudita Astral",
                    FormaVisual = "Espectro",
                    NombreMapaPropio = "LABERINTO DE PORTALES",
                    EstiloCartografia = "9 Cámaras cerradas por muros arcanos y 4 Portales Warp.",
                    VentajaExploracion = "Largo alcance + Poder de Gemas recarga rápido.",
                    ColorPrincipal = Color.MediumOrchid,
                    ColorSecundario = Color.Magenta,
                    ColorFondoVacio = Color.FromArgb(16, 10, 28),
                    ColorTierra = Color.FromArgb(28, 18, 48),
                    ColorBordeTierra = Color.Magenta,
                    ColorAguaLava = Color.FromArgb(170, 40, 220),
                    ColorFlora = Color.Magenta,
                    Velocidad = 5.2f,
                    SaludMax = 190,
                    DanioBase = 35
                },
                new PersonajeJugable
                {
                    Nombre = "REX-7",
                    Rol = "Artillero de Hierro",
                    FormaVisual = "Mecha",
                    NombreMapaPropio = "FORTALEZA BÚNKER",
                    EstiloCartografia = "Pasillos militares blindados, barricadas de acero y fosos.",
                    VentajaExploracion = "Disparo doble continuo + Alto daño en corredores.",
                    ColorPrincipal = Color.Crimson,
                    ColorSecundario = Color.OrangeRed,
                    ColorFondoVacio = Color.FromArgb(18, 20, 24),
                    ColorTierra = Color.FromArgb(34, 38, 44),
                    ColorBordeTierra = Color.Gold,
                    ColorAguaLava = Color.FromArgb(210, 140, 20),
                    ColorFlora = Color.OrangeRed,
                    Velocidad = 5.0f,
                    SaludMax = 230,
                    DanioBase = 30
                },
                new PersonajeJugable
                {
                    Nombre = "SPECTRA",
                    Rol = "Druida Estelar",
                    FormaVisual = "Orbe",
                    NombreMapaPropio = "VALLE FLUVIAL ANCESTRAL",
                    EstiloCartografia = "Bosque abierto cruzado por un río en 'S' y 3 puentes rúnicos.",
                    VentajaExploracion = "Regeneración natural de vida entre árboles luminosos.",
                    ColorPrincipal = Color.SeaGreen,
                    ColorSecundario = Color.SpringGreen,
                    ColorFondoVacio = Color.FromArgb(10, 24, 18),
                    ColorTierra = Color.FromArgb(16, 36, 28),
                    ColorBordeTierra = Color.SpringGreen,
                    ColorAguaLava = Color.FromArgb(0, 195, 165),
                    ColorFlora = Color.SpringGreen,
                    Velocidad = 5.2f,
                    SaludMax = 220,
                    DanioBase = 27
                }
            };
        }

        private void MostrarSelectorExploradores()
        {
            pnlSelector = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(10, 15, 26) };

            Label lblTitulo = new Label
            {
                Text = "ELIGE EXPLORADOR: CADA UNO TIENE UNA CARTOGRAFÍA Y ARQUITECTURA ÚNICA",
                Font = new Font("Consolas", 14.5f, FontStyle.Bold),
                ForeColor = Color.Cyan,
                Location = new Point(25, 18),
                AutoSize = true
            };

            Label lblControles = new Label
            {
                Text = "[O] Disparar | [□] Espada | [X] Dash | [△] Poder | [L1/V] IA Voz | [OPTIONS] Salir a Cabina",
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                ForeColor = Color.Gold,
                Location = new Point(25, 48),
                AutoSize = true
            };

            pnlSelector.Controls.Add(lblTitulo);
            pnlSelector.Controls.Add(lblControles);

            int x = 25;
            foreach (var p in catalogoExploradores)
            {
                Panel card = new Panel
                {
                    Location = new Point(x, 86),
                    Size = new Size(204, 520),
                    BackColor = Color.FromArgb(18, 26, 44),
                    BorderStyle = BorderStyle.FixedSingle
                };

                PictureBox preview = new PictureBox
                {
                    Location = new Point(32, 12),
                    Size = new Size(138, 138),
                    BackColor = Color.FromArgb(10, 14, 24)
                };
                preview.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    Bitmap sprite = SpriteFactory.ObtenerSpriteOperativo(p.FormaVisual, p.ColorPrincipal, p.ColorSecundario, 0);
                    e.Graphics.DrawImage(sprite, 17, 17, 104, 104);
                };

                Label lblNom = new Label
                {
                    Text = p.Nombre,
                    Font = new Font("Consolas", 14f, FontStyle.Bold),
                    ForeColor = p.ColorSecundario,
                    Location = new Point(8, 152),
                    Size = new Size(186, 24),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                Label lblMapa = new Label
                {
                    Text = p.NombreMapaPropio,
                    Font = new Font("Consolas", 8.5f, FontStyle.Bold),
                    ForeColor = Color.Gold,
                    Location = new Point(6, 178),
                    Size = new Size(190, 20),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                Label lblDesc = new Label
                {
                    Text = $"CARTOGRAFÍA:\n{p.EstiloCartografia}\n\nVENTAJA:\n{p.VentajaExploracion}\n\nVIDA: {p.SaludMax} | ATQ: {p.DanioBase}",
                    Font = new Font("Consolas", 8.5f),
                    ForeColor = Color.White,
                    Location = new Point(10, 204),
                    Size = new Size(184, 240)
                };

                Button btnElegir = new Button
                {
                    Text = "EXPLORAR MAPA",
                    Location = new Point(14, 452),
                    Size = new Size(174, 48),
                    Font = new Font("Consolas", 10f, FontStyle.Bold),
                    BackColor = p.ColorSecundario,
                    ForeColor = Color.Black,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnElegir.Click += (s, e) => IniciarExploracion(p);

                card.Controls.AddRange(new Control[] { preview, lblNom, lblMapa, lblDesc, btnElegir });
                pnlSelector.Controls.Add(card);
                botonesSelector.Add(btnElegir);
                x += 214;
            }

            this.Controls.Add(pnlSelector);
            ResaltarSelector(0);

            timerSelectorMando = new System.Windows.Forms.Timer { Interval = 16 };
            timerSelectorMando.Tick += (s, e) =>
            {
                if (!pnlSelector.Visible) return;
                var m = ControlMando.Leer();
                if (m.Derecha && !mandoPrevio.Derecha) { TocarSonidoRapido("click"); ResaltarSelector(indiceSelector + 1); }
                if (m.Izquierda && !mandoPrevio.Izquierda) { TocarSonidoRapido("click"); ResaltarSelector(indiceSelector - 1); }
                if (m.BotonSaltar && !mandoPrevio.BotonSaltar) botonesSelector[indiceSelector].PerformClick();
                if ((m.BotonDisparar && !mandoPrevio.BotonDisparar) || (m.BotonSalir && !mandoPrevio.BotonSalir)) this.Close();
                mandoPrevio = m;
            };
            timerSelectorMando.Start();

            this.FormClosing += (s, e) =>
            {
                timerSelectorMando?.Stop();
                timerReporteMando?.Stop();
                gameTimer?.Stop();
                brVacioMapa?.Dispose();
                brTierraMapa?.Dispose();
                brPeligroMapa?.Dispose();
                brIslaMinimap?.Dispose();
                penBordeTierraMapa?.Dispose();
                penHudBordeMapa?.Dispose();
            };
        }

        private void ResaltarSelector(int idx)
        {
            if (botonesSelector.Count == 0) return;
            indiceSelector = (idx + botonesSelector.Count) % botonesSelector.Count;
            for (int i = 0; i < botonesSelector.Count; i++)
            {
                botonesSelector[i].FlatAppearance.BorderColor = i == indiceSelector ? Color.Yellow : Color.Black;
                botonesSelector[i].FlatAppearance.BorderSize = i == indiceSelector ? 4 : 1;
            }
            botonesSelector[indiceSelector].Focus();
        }

        private void IniciarExploracion(PersonajeJugable elegido)
        {
            TocarSonidoRapido("powerup");
            timerSelectorMando?.Stop();
            exploradorActual = elegido;

            AsistenteVozIA.MostrarSubtituloEnJuego = (txt, dur) => MostrarAviso(txt, dur);
            AsistenteVozIA.ObtenerContextoJuegoActual = () =>
            {
                int faltan = 4 - ContarGemas();
                if (faltan > 0)
                    return $"Exploras {exploradorActual.NombreMapaPropio}. Te faltan {faltan} gemas elementales; sigue los puntos de colores en tu minimapa y derrota a sus guardianes. Tienes {salud} de vida.";
                return $"Ya tienes las 4 gemas elementales. Dirígete al punto dorado en el minimapa y entra al Portal Ancestral para completar la expedición.";
            };

            brVacioMapa?.Dispose();
            brTierraMapa?.Dispose();
            brPeligroMapa?.Dispose();
            brIslaMinimap?.Dispose();
            penBordeTierraMapa?.Dispose();
            penHudBordeMapa?.Dispose();

            brVacioMapa = new SolidBrush(elegido.ColorFondoVacio);
            brTierraMapa = new SolidBrush(elegido.ColorTierra);
            brPeligroMapa = new SolidBrush(Color.FromArgb(145, elegido.ColorAguaLava));
            brIslaMinimap = new SolidBrush(Color.FromArgb(140, elegido.ColorBordeTierra));
            penBordeTierraMapa = new Pen(elegido.ColorBordeTierra, 3);
            penHudBordeMapa = new Pen(elegido.ColorSecundario, 2);

            pnlSelector.Visible = false;
            this.Text = $"NEXUS // {elegido.Nombre} EN {elegido.NombreMapaPropio}";
            this.Focus();

            maxSalud = elegido.SaludMax;
            salud = maxSalud;

            ConstruirCartografiaReal(elegido);
            DescubrirNieblaAlrededor(jugX, jugY);

            this.KeyDown += Aventura_KeyDown;
            this.KeyUp += Aventura_KeyUp;

            gameTimer = new System.Windows.Forms.Timer { Interval = 16 };
            gameTimer.Tick += BucleExploracion;
            gameTimer.Start();
        }

        private void ConstruirCartografiaReal(PersonajeJugable p)
        {
            usaIslasFlotantes = false;
            zonasTierra.Clear();
            murosSolidos.Clear();
            zonasPeligroAguaLava.Clear();
            puentesSeguros.Clear();
            portalesWarp.Clear();
            ruinas.Clear();
            decoraciones.Clear();
            ambienteFlotante.Clear();
            gemas.Clear();
            cofres.Clear();
            enemigos.Clear();

            PointF pRubi, pZafiro, pEsmeralda, pAmatista;
            string[] nombresSantuarios;
            string[] nombresGuardianes;

            switch (p.Nombre)
            {
                case "VALKYRIE":
                    usaIslasFlotantes = true;
                    jugX = 1500;
                    jugY = 2100;
                    altarPortal = new PointF(1500, 260);
                    pRubi = new PointF(450, 500);
                    pZafiro = new PointF(2550, 500);
                    pEsmeralda = new PointF(450, 1900);
                    pAmatista = new PointF(2550, 1900);

                    zonasTierra.Add(new Rectangle(1200, 1850, 600, 450));
                    zonasTierra.Add(new Rectangle(1150, 950, 700, 500));
                    zonasTierra.Add(new Rectangle(1200, 100, 600, 420));
                    zonasTierra.Add(new Rectangle(200, 250, 520, 500));
                    zonasTierra.Add(new Rectangle(2280, 250, 520, 500));
                    zonasTierra.Add(new Rectangle(200, 1650, 520, 500));
                    zonasTierra.Add(new Rectangle(2280, 1650, 520, 500));

                    puentesSeguros.Add(new Rectangle(1440, 1450, 120, 400));
                    puentesSeguros.Add(new Rectangle(1440, 520, 120, 430));
                    puentesSeguros.Add(new Rectangle(720, 1140, 430, 120));
                    puentesSeguros.Add(new Rectangle(1850, 1140, 430, 120));
                    puentesSeguros.Add(new Rectangle(400, 750, 120, 900));
                    puentesSeguros.Add(new Rectangle(2480, 750, 120, 900));
                    zonasTierra.Add(new Rectangle(360, 1100, 200, 200));
                    zonasTierra.Add(new Rectangle(2440, 1100, 200, 200));

                    nombresSantuarios = new[] { "ISLA SOLAR", "ISLA GLACIAL", "ISLA VERDE", "ISLA TRUENO", "ISLA DEL PORTAL" };
                    nombresGuardianes = new[] { "VALQUIRIA SOLAR", "HALCÓN GLACIAL", "CENTINELA ALADO", "DRAGÓN CELESTE" };
                    break;

                case "KAELEN":
                    jugX = 1500;
                    jugY = 2160;
                    altarPortal = new PointF(1500, 240);
                    pRubi = new PointF(1500, 1200);
                    pZafiro = new PointF(420, 1200);
                    pEsmeralda = new PointF(2580, 1200);
                    pAmatista = new PointF(680, 460);

                    zonasTierra.Add(new Rectangle(0, 0, MUNDO_W, MUNDO_H));
                    zonasPeligroAguaLava.Add(new Rectangle(950, 750, 1100, 900));
                    zonasTierra.Add(new Rectangle(1320, 1020, 360, 360));

                    puentesSeguros.Add(new Rectangle(1445, 720, 110, 310));
                    puentesSeguros.Add(new Rectangle(1445, 1370, 110, 310));
                    puentesSeguros.Add(new Rectangle(920, 1145, 410, 110));
                    puentesSeguros.Add(new Rectangle(1670, 1145, 410, 110));

                    murosSolidos.Add(new Rectangle(650, 750, 60, 900));
                    murosSolidos.Add(new Rectangle(2290, 750, 60, 900));
                    murosSolidos.Add(new Rectangle(950, 520, 1100, 55));
                    murosSolidos.Add(new Rectangle(950, 1825, 1100, 55));

                    nombresSantuarios = new[] { "NÚCLEO DEL CRÁTER", "GRUTA FRÍA", "FORJA ESMERALDA", "ALTAR DE CENIZA", "PORTAL VOLCÁNICO" };
                    nombresGuardianes = new[] { "TITÁN DE MAGMA", "GÓLEM DE HIELO", "COLOSO DE HIERRO", "SEÑOR DEL FUEGO" };
                    break;

                case "NOVA":
                    jugX = 450;
                    jugY = 2020;
                    altarPortal = new PointF(2550, 380);
                    pRubi = new PointF(450, 380);
                    pZafiro = new PointF(2550, 2020);
                    pEsmeralda = new PointF(1500, 1200);
                    pAmatista = new PointF(1500, 380);

                    zonasTierra.Add(new Rectangle(0, 0, MUNDO_W, MUNDO_H));

                    murosSolidos.Add(new Rectangle(980, 0, 55, 620));
                    murosSolidos.Add(new Rectangle(980, 820, 55, 760));
                    murosSolidos.Add(new Rectangle(980, 1780, 55, 620));

                    murosSolidos.Add(new Rectangle(1965, 0, 55, 620));
                    murosSolidos.Add(new Rectangle(1965, 820, 55, 760));
                    murosSolidos.Add(new Rectangle(1965, 1780, 55, 620));

                    murosSolidos.Add(new Rectangle(0, 780, 760, 55));
                    murosSolidos.Add(new Rectangle(1220, 780, 560, 55));
                    murosSolidos.Add(new Rectangle(2240, 780, 760, 55));

                    murosSolidos.Add(new Rectangle(0, 1565, 760, 55));
                    murosSolidos.Add(new Rectangle(1220, 1565, 560, 55));
                    murosSolidos.Add(new Rectangle(2240, 1565, 760, 55));

                    portalesWarp.Add((new PointF(220, 1200), new PointF(2780, 1200), Color.Cyan));
                    portalesWarp.Add((new PointF(1500, 2220), new PointF(1500, 620), Color.Magenta));

                    nombresSantuarios = new[] { "CÁMARA NOROESTE", "CÁMARA SURESTE", "NÚCLEO ASTRAL", "SALA DEL NORTE", "PORTAL DIMENSIONAL" };
                    nombresGuardianes = new[] { "ARCONTE RUBÍ", "ESPECTRO ZAFIRO", "ORBE CUSTODIO", "TEJEDOR ASTRAL" };
                    break;

                case "REX-7":
                    jugX = 280;
                    jugY = 1200;
                    altarPortal = new PointF(2720, 1200);
                    pRubi = new PointF(820, 360);
                    pZafiro = new PointF(2180, 360);
                    pEsmeralda = new PointF(820, 2040);
                    pAmatista = new PointF(2180, 2040);

                    zonasTierra.Add(new Rectangle(0, 0, MUNDO_W, MUNDO_H));

                    zonasPeligroAguaLava.Add(new Rectangle(1120, 0, 130, MUNDO_H));
                    zonasPeligroAguaLava.Add(new Rectangle(1750, 0, 130, MUNDO_H));

                    foreach (int py in new[] { 320, 1140, 1980 })
                    {
                        puentesSeguros.Add(new Rectangle(1100, py, 170, 120));
                        puentesSeguros.Add(new Rectangle(1730, py, 170, 120));
                    }

                    murosSolidos.Add(new Rectangle(520, 580, 480, 50));
                    murosSolidos.Add(new Rectangle(520, 1770, 480, 50));
                    murosSolidos.Add(new Rectangle(2000, 580, 480, 50));
                    murosSolidos.Add(new Rectangle(2000, 1770, 480, 50));
                    murosSolidos.Add(new Rectangle(1465, 650, 70, 1100));

                    nombresSantuarios = new[] { "REACTOR ALFA", "SILO CRIOGÉNICO", "DEPÓSITO BIO-04", "TORRE DE CONTROL", "COMPUERTA OMEGA" };
                    nombresGuardianes = new[] { "MECHA PESADO", "TORRETA LÁSER", "AUTÓMATA BLINDADO", "UNIDAD EXTERMINIO" };
                    break;

                default:
                    jugX = 1500;
                    jugY = 2080;
                    altarPortal = new PointF(1500, 280);
                    pRubi = new PointF(460, 540);
                    pZafiro = new PointF(2540, 540);
                    pEsmeralda = new PointF(460, 1840);
                    pAmatista = new PointF(2540, 1840);

                    zonasTierra.Add(new Rectangle(0, 0, MUNDO_W, MUNDO_H));

                    zonasPeligroAguaLava.Add(new Rectangle(0, 1340, 1150, 140));
                    zonasPeligroAguaLava.Add(new Rectangle(1010, 960, 140, 520));
                    zonasPeligroAguaLava.Add(new Rectangle(1010, 960, 1990, 140));

                    puentesSeguros.Add(new Rectangle(400, 1320, 120, 180));
                    puentesSeguros.Add(new Rectangle(1440, 940, 120, 180));
                    puentesSeguros.Add(new Rectangle(2480, 940, 120, 180));

                    nombresSantuarios = new[] { "ARBOLEDA ÍGNEA", "LAGO CRISTALINO", "CORAZÓN VERDE", "RAÍCES ESTELARES", "ALTAR DE GAIA" };
                    nombresGuardianes = new[] { "ESPÍRITU DEL FUEGO", "GUARDIÁN DEL RÍO", "ANCESTRO DEL BOSQUE", "BESTIA DE SPORAS" };
                    break;
            }

            PointF[] puntosGemas = { pRubi, pZafiro, pEsmeralda, pAmatista };
            Color[] coloresSantuarios = { Color.OrangeRed, Color.DeepSkyBlue, Color.SpringGreen, Color.MediumOrchid };
            for (int i = 0; i < 4; i++)
            {
                ruinas.Add(new RuinaSantuario
                {
                    Rect = new Rectangle((int)puntosGemas[i].X - 165, (int)puntosGemas[i].Y - 150, 330, 300),
                    Nombre = nombresSantuarios[i],
                    ColorBorde = coloresSantuarios[i]
                });
            }
            ruinas.Add(new RuinaSantuario
            {
                Rect = new Rectangle((int)altarPortal.X - 190, (int)altarPortal.Y - 130, 380, 260),
                Nombre = nombresSantuarios[4],
                ColorBorde = Color.Gold
            });

            foreach (var r in ruinas)
            {
                decoraciones.Add(new DecoracionMapa { X = r.Rect.Left + 14, Y = r.Rect.Top + 14, Tipo = "ANTORCHA", Color = r.ColorBorde });
                decoraciones.Add(new DecoracionMapa { X = r.Rect.Right - 14, Y = r.Rect.Top + 14, Tipo = "ANTORCHA", Color = r.ColorBorde });
                decoraciones.Add(new DecoracionMapa { X = r.Rect.Left + 14, Y = r.Rect.Bottom - 14, Tipo = "ANTORCHA", Color = r.ColorBorde });
                decoraciones.Add(new DecoracionMapa { X = r.Rect.Right - 14, Y = r.Rect.Bottom - 14, Tipo = "ANTORCHA", Color = r.ColorBorde });
            }

            string decoPrincipal = p.Nombre switch
            {
                "VALKYRIE" => "CRISTAL",
                "KAELEN" => "PILAR",
                "NOVA" => "CRISTAL",
                "REX-7" => "PILAR",
                _ => "ARBOL"
            };

            for (int d = 0; d < 120; d++)
            {
                float dx = rand.Next(100, MUNDO_W - 100);
                float dy = rand.Next(100, MUNDO_H - 100);
                if (!EsPuntoCaminable(dx, dy)) continue;
                if (Distancia(dx, dy, jugX, jugY) < 160) continue;

                decoraciones.Add(new DecoracionMapa
                {
                    X = dx,
                    Y = dy,
                    Tipo = (d % 2 == 0) ? decoPrincipal : "CRISTAL",
                    Color = (d % 2 == 0) ? p.ColorFlora : p.ColorAguaLava
                });
            }

            for (int a = 0; a < 75; a++)
            {
                ambienteFlotante.Add(new ParticulaAmbiente
                {
                    X = rand.Next(0, MUNDO_W),
                    Y = rand.Next(0, MUNDO_H),
                    VX = (float)(rand.NextDouble() * 1.4 - 0.7),
                    VY = (float)(rand.NextDouble() * -1.2 - 0.3),
                    Fase = (float)(rand.NextDouble() * Math.PI * 2),
                    Color = (a % 2 == 0) ? p.ColorFlora : Color.Gold
                });
            }

            gemas.Add(new GemaMundo { Id = "RUBI", Nombre = "GEMA RUBÍ DE FUEGO", Efecto = "+35% Daño Explosivo", X = pRubi.X, Y = pRubi.Y, Color = Color.OrangeRed });
            gemas.Add(new GemaMundo { Id = "ZAFIRO", Nombre = "GEMA ZAFIRO GLACIAL", Efecto = "Congela y Ralentiza Enemigos", X = pZafiro.X, Y = pZafiro.Y, Color = Color.DeepSkyBlue });
            gemas.Add(new GemaMundo { Id = "ESMERALDA", Nombre = "GEMA ESMERALDA VITAL", Efecto = "+80 Vida Máxima y Regeneración", X = pEsmeralda.X, Y = pEsmeralda.Y, Color = Color.SpringGreen });
            gemas.Add(new GemaMundo { Id = "AMATISTA", Nombre = "GEMA AMATISTA ELÉCTRICA", Efecto = "+Velocidad y Disparo Rápido", X = pAmatista.X, Y = pAmatista.Y, Color = Color.MediumOrchid });

            string[] tiposGuardian = { "Mecha", "Torreta", "Mecha", "Dron" };
            for (int g = 0; g < 4; g++)
            {
                enemigos.Add(new GuardianEnemigo
                {
                    Nombre = nombresGuardianes[g],
                    Tipo = tiposGuardian[g],
                    EsJefeGuardian = true,
                    X = puntosGemas[g].X,
                    Y = puntosGemas[g].Y + 70,
                    HomeX = puntosGemas[g].X,
                    HomeY = puntosGemas[g].Y,
                    Salud = 240,
                    MaxSalud = 240
                });
            }

            string[] tipos = { "Dron", "Mecha", "Torreta" };
            int generados = 0, intentos = 0;
            while (generados < 18 && intentos < 250)
            {
                intentos++;
                float ex = rand.Next(200, MUNDO_W - 200);
                float ey = rand.Next(200, MUNDO_H - 200);
                if (!EsPuntoCaminable(ex, ey) || Distancia(ex, ey, jugX, jugY) < 420) continue;

                enemigos.Add(new GuardianEnemigo
                {
                    Nombre = "ACECHADOR",
                    Tipo = tipos[generados % tipos.Length],
                    EsJefeGuardian = false,
                    X = ex,
                    Y = ey,
                    HomeX = ex,
                    HomeY = ey,
                    Salud = 95,
                    MaxSalud = 95,
                    CooldownDisparo = rand.Next(30, 80)
                });
                generados++;
            }

            int cofresGen = 0;
            intentos = 0;
            while (cofresGen < 14 && intentos < 250)
            {
                intentos++;
                float cx = rand.Next(200, MUNDO_W - 200);
                float cy = rand.Next(200, MUNDO_H - 200);
                if (!EsPuntoCaminable(cx, cy) || Distancia(cx, cy, jugX, jugY) < 260) continue;

                cofres.Add(new CofreTesoro { X = cx, Y = cy, Abierto = false });
                cofresGen++;
            }

            MostrarAviso($"MAPA: {p.NombreMapaPropio} — {p.EstiloCartografia}", 240);
        }

        private bool EsPuntoCaminable(float x, float y)
        {
            if (x < 45 || x > MUNDO_W - 45 || y < 45 || y > MUNDO_H - 45) return false;
            Point pt = new Point((int)x, (int)y);
            Rectangle hitbox = new Rectangle((int)x - 18, (int)y - 18, 36, 36);

            for (int i = 0; i < murosSolidos.Count; i++)
                if (murosSolidos[i].IntersectsWith(hitbox)) return false;

            if (usaIslasFlotantes)
            {
                for (int i = 0; i < zonasTierra.Count; i++)
                    if (zonasTierra[i].Contains(pt)) return true;
                for (int i = 0; i < puentesSeguros.Count; i++)
                    if (puentesSeguros[i].Contains(pt)) return true;
                return false;
            }

            return true;
        }

        private void Aventura_KeyDown(object? sender, KeyEventArgs e)
        {
            if (expedicionTerminada)
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space || e.KeyCode == Keys.Escape)
                    this.Close();
                return;
            }

            if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left) keyIzq = true;
            if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right) keyDer = true;
            if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) keyArr = true;
            if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) keyAba = true;

            if (e.KeyCode == Keys.J || e.KeyCode == Keys.Z) keyDisparo = true;
            if (e.KeyCode == Keys.K || e.KeyCode == Keys.X) keyEspada = true;
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.L) keyDash = true;
            if (e.KeyCode == Keys.E || e.KeyCode == Keys.C) keyPoder = true;
            if (e.KeyCode == Keys.V) AsistenteVozIA.ActivarEscuchaPorMando();
            if (e.KeyCode == Keys.Escape) FinalizarExpedicion(false, true);
        }

        private void Aventura_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left) keyIzq = false;
            if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right) keyDer = false;
            if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) keyArr = false;
            if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) keyAba = false;

            if (e.KeyCode == Keys.J || e.KeyCode == Keys.Z) keyDisparo = false;
            if (e.KeyCode == Keys.K || e.KeyCode == Keys.X) keyEspada = false;
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.L) keyDash = false;
            if (e.KeyCode == Keys.E || e.KeyCode == Keys.C) keyPoder = false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (pnlSelector.Visible || expedicionTerminada) return;
            if (e.Button == MouseButtons.Left) keyDisparo = true;
            if (e.Button == MouseButtons.Right) EjecutarCorteEspada();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left) keyDisparo = false;
        }

        private void DescubrirNieblaAlrededor(float px, float py)
        {
            int radioCeldas = exploradorActual.FormaVisual == "Caza" ? 4 : 3;
            int cx = (int)(px / CELDA_NIEBLA);
            int cy = (int)(py / CELDA_NIEBLA);
            int maxIx = zonaDescubierta.GetLength(0) - 1;
            int maxIy = zonaDescubierta.GetLength(1) - 1;

            for (int ix = Math.Max(0, cx - radioCeldas); ix <= Math.Min(maxIx, cx + radioCeldas); ix++)
                for (int iy = Math.Max(0, cy - radioCeldas); iy <= Math.Min(maxIy, cy + radioCeldas); iy++)
                    zonaDescubierta[ix, iy] = true;
        }

        private void BucleExploracion(object? sender, EventArgs e)
        {
            if (expedicionTerminada) return;

            tickContador++;
            if (screenShake > 0) screenShake--;
            if (avisoTimer > 0) avisoTimer--;
            if (cdDisparo > 0) cdDisparo--;
            if (cdEspada > 0) cdEspada--;
            if (animEspada > 0) animEspada--;
            if (cdDash > 0) cdDash--;
            if (dashActivo > 0) dashActivo--;
            if (cdWarp > 0) cdWarp--;

            var mando = ControlMando.Leer();

            bool pulsaSalir = mando.BotonSalir && !prevSalir;
            prevSalir = mando.BotonSalir;
            if (pulsaSalir)
            {
                FinalizarExpedicion(false, true);
                return;
            }

            int recargaPoder = exploradorActual.FormaVisual == "Espectro" ? 3 : 5;
            if (tickContador % recargaPoder == 0 && cargaPoderGemas < 100) cargaPoderGemas++;

            for (int i = 0; i < ambienteFlotante.Count; i++)
            {
                var amb = ambienteFlotante[i];
                amb.X += amb.VX + (float)Math.Sin(tickContador * 0.04 + amb.Fase) * 0.5f;
                amb.Y += amb.VY;
                if (amb.Y < 0) amb.Y = MUNDO_H;
                if (amb.X < 0) amb.X = MUNDO_W;
                if (amb.X > MUNDO_W) amb.X = 0;
            }

            if ((tieneGemaEsmeralda || exploradorActual.FormaVisual == "Orbe") && tickContador % 35 == 0 && salud < maxSalud)
                salud = Math.Min(maxSalud, salud + 5);

            Point ptJug = new Point((int)jugX, (int)jugY);
            bool sobrePuente = false;
            for (int i = 0; i < puentesSeguros.Count; i++)
                if (puentesSeguros[i].Contains(ptJug)) { sobrePuente = true; break; }

            bool enPeligroTerreno = false;
            if (!sobrePuente)
            {
                for (int i = 0; i < zonasPeligroAguaLava.Count; i++)
                    if (zonasPeligroAguaLava[i].Contains(ptJug)) { enPeligroTerreno = true; break; }
            }

            float multTerreno = 1f;
            if (enPeligroTerreno && dashActivo == 0)
            {
                multTerreno = 0.52f;
                if (exploradorActual.Nombre == "KAELEN" && tickContador % 25 == 0)
                {
                    TocarSonidoRapido("danio", 110);
                    salud -= 6;
                    GenerarParticulas(jugX, jugY, Color.OrangeRed, 5, 3f);
                    if (salud <= 0) { FinalizarExpedicion(false, false); return; }
                }
            }

            float mx = 0, my = 0;
            if (keyIzq || mando.Izquierda) mx -= 1;
            if (keyDer || mando.Derecha) mx += 1;
            if (keyArr || mando.Arriba) my -= 1;
            if (keyAba || mando.Abajo) my += 1;

            if (mx != 0 || my != 0)
            {
                float len = (float)Math.Sqrt(mx * mx + my * my);
                mx /= len;
                my /= len;
                dirX = mx;
                dirY = my;

                float velExtraGema = tieneGemaAmatista ? 1.15f : 0f;
                float multDash = dashActivo > 0 ? 2.6f : 1f;
                float velTotal = (exploradorActual.Velocidad + velExtraGema) * multDash * multTerreno;

                float nextX = jugX + mx * velTotal;
                float nextY = jugY + my * velTotal;
                if (EsPuntoCaminable(nextX, jugY) || (dashActivo > 0 && usaIslasFlotantes)) jugX = nextX;
                if (EsPuntoCaminable(jugX, nextY) || (dashActivo > 0 && usaIslasFlotantes)) jugY = nextY;

                if (usaIslasFlotantes && dashActivo == 0 && !EsPuntoCaminable(jugX, jugY))
                {
                    jugX -= mx * 16f;
                    jugY -= my * 16f;
                }

                DescubrirNieblaAlrededor(jugX, jugY);

                if (tickContador % 4 == 0) frameAnimacion = (frameAnimacion + 1) % 4;
                if (dashActivo == 0 && tickContador % 16 == 0) TocarSonidoRapido("paso", 120);
                if (dashActivo > 0 && tickContador % 2 == 0)
                    GenerarParticulas(jugX, jugY, exploradorActual.ColorSecundario, 4, 3f);
            }
            else
            {
                frameAnimacion = 0;
            }

            if (cdWarp == 0)
            {
                foreach (var w in portalesWarp)
                {
                    if (Distancia(jugX, jugY, w.A.X, w.A.Y) < 48)
                    {
                        TocarSonidoRapido("portal");
                        jugX = w.B.X;
                        jugY = w.B.Y;
                        cdWarp = 60;
                        DescubrirNieblaAlrededor(jugX, jugY);
                        GenerarParticulas(jugX, jugY, w.Color, 26, 6f);
                        MostrarAviso("✦ TELETRANSPORTE DIMENSIONAL ACTIVADO ✦", 90);
                        break;
                    }
                    if (Distancia(jugX, jugY, w.B.X, w.B.Y) < 48)
                    {
                        TocarSonidoRapido("portal");
                        jugX = w.A.X;
                        jugY = w.A.Y;
                        cdWarp = 60;
                        DescubrirNieblaAlrededor(jugX, jugY);
                        GenerarParticulas(jugX, jugY, w.Color, 26, 6f);
                        MostrarAviso("✦ TELETRANSPORTE DIMENSIONAL ACTIVADO ✦", 90);
                        break;
                    }
                }
            }

            bool pulsaVoz = mando.BotonVoz && !prevVoz;
            prevVoz = mando.BotonVoz;
            if (pulsaVoz) AsistenteVozIA.ActivarEscuchaPorMando();

            bool disparar = keyDisparo || mando.BotonDisparar;
            bool pulsaEspada = (keyEspada || mando.BotonAcuchillar) && !prevEspada;
            bool pulsaDash = (keyDash || mando.BotonSaltar) && !prevDash;
            bool pulsaPoder = (keyPoder || mando.BotonEspecial) && !prevPoder;

            prevEspada = keyEspada || mando.BotonAcuchillar;
            prevDash = keyDash || mando.BotonSaltar;
            prevPoder = keyPoder || mando.BotonEspecial;

            if (pulsaDash && cdDash == 0)
            {
                TocarSonidoRapido("salto");
                cdDash = 55;
                dashActivo = 12;
                GenerarParticulas(jugX, jugY, Color.Cyan, 16, 5f);
            }

            if (pulsaEspada && cdEspada == 0) EjecutarCorteEspada();
            if (pulsaPoder && cargaPoderGemas >= 100) EjecutarExplosionDeGemas();
            if (disparar && cdDisparo == 0) DispararProyectilElemental();

            RevisarGemasCofresYPortal();
            ActualizarEnemigos();
            ActualizarProyectiles();
            ActualizarOndasYParticulas();

            float objX = jugX - this.ClientSize.Width / 2f;
            float objY = jugY - this.ClientSize.Height / 2f;
            camX += (objX - camX) * 0.16f;
            camY += (objY - camY) * 0.16f;
            camX = Math.Clamp(camX, 0, Math.Max(0, MUNDO_W - this.ClientSize.Width));
            camY = Math.Clamp(camY, 0, Math.Max(0, MUNDO_H - this.ClientSize.Height));

            this.Invalidate();
        }

        private void DispararProyectilElemental()
        {
            TocarSonidoRapido(exploradorActual.FormaVisual == "Mecha" ? "metralleta" : (tieneGemaRubi ? "canon" : "disparo"), 55);
            cdDisparo = tieneGemaAmatista ? 10 : 16;

            float vx = dirX, vy = dirY;
            GuardianEnemigo? cercano = null;
            float mejorDist = 380f;
            for (int i = 0; i < enemigos.Count; i++)
            {
                float d = Distancia(jugX, jugY, enemigos[i].X, enemigos[i].Y);
                if (d < mejorDist)
                {
                    mejorDist = d;
                    cercano = enemigos[i];
                }
            }

            if (cercano != null)
            {
                float dx = cercano.X - jugX;
                float dy = cercano.Y - jugY;
                float len = Math.Max(1f, (float)Math.Sqrt(dx * dx + dy * dy));
                vx = dx / len;
                vy = dy / len;
                dirX = vx;
                dirY = vy;
            }

            int danio = exploradorActual.DanioBase + (tieneGemaRubi ? 14 : 0);
            Color colorBala = tieneGemaRubi ? Color.OrangeRed
                : (tieneGemaZafiro ? Color.DeepSkyBlue
                : (tieneGemaAmatista ? Color.Magenta : exploradorActual.ColorSecundario));

            int alcance = exploradorActual.FormaVisual == "Espectro" ? 36 : 26;

            proyectiles.Add(new ProyectilAventura
            {
                X = jugX + vx * 24,
                Y = jugY + vy * 24,
                VX = vx * 15f,
                VY = vy * 15f,
                EsJugador = true,
                Danio = danio,
                Vida = alcance,
                Radio = tieneGemaRubi ? 12 : 9,
                Color = colorBala,
                Congela = tieneGemaZafiro
            });

            if (exploradorActual.FormaVisual == "Mecha")
            {
                proyectiles.Add(new ProyectilAventura
                {
                    X = jugX - vy * 14,
                    Y = jugY + vx * 14,
                    VX = vx * 15f,
                    VY = vy * 15f,
                    EsJugador = true,
                    Danio = danio / 2,
                    Vida = alcance,
                    Radio = 7,
                    Color = Color.Gold,
                    Congela = tieneGemaZafiro
                });
            }
        }

        private void EjecutarCorteEspada()
        {
            TocarSonidoRapido("cuchillo", 65);
            cdEspada = 22;
            animEspada = 10;
            float radioCorte = exploradorActual.FormaVisual == "Tanque" ? 130f : 100f;
            int danioEspada = exploradorActual.DanioBase * 2 + (tieneGemaRubi ? 20 : 0);

            ondas.Add(new OndaVisual { X = jugX, Y = jugY, Radio = radioCorte, Color = exploradorActual.ColorSecundario, Vida = 10, MaxVida = 10 });

            for (int i = enemigos.Count - 1; i >= 0; i--)
            {
                var en = enemigos[i];
                if (Distancia(jugX, jugY, en.X, en.Y) <= radioCorte)
                {
                    en.Salud -= danioEspada;
                    en.FlashImpacto = 4;
                    if (tieneGemaZafiro) en.CongeladoTimer = 110;
                    screenShake = 6;
                    GenerarParticulas(en.X, en.Y, Color.Cyan, 16, 6f);
                    if (en.Salud <= 0) EliminarEnemigo(i);
                }
            }
        }

        private void EjecutarExplosionDeGemas()
        {
            TocarSonidoRapido("especial");
            cargaPoderGemas = 0;
            screenShake = 14;
            int gemasCount = ContarGemas();
            int danioNova = 90 + gemasCount * 45;

            MostrarAviso($"✦ ¡LIBERACIÓN ELEMENTAL ({gemasCount} GEMAS ACTIVAS)! ✦", 120);
            ondas.Add(new OndaVisual { X = jugX, Y = jugY, Radio = 250, Color = Color.Gold, Vida = 22, MaxVida = 22 });

            for (int i = enemigos.Count - 1; i >= 0; i--)
            {
                var en = enemigos[i];
                if (Distancia(jugX, jugY, en.X, en.Y) <= 260)
                {
                    en.Salud -= danioNova;
                    en.FlashImpacto = 6;
                    if (tieneGemaZafiro) en.CongeladoTimer = 150;
                    GenerarParticulas(en.X, en.Y, Color.Gold, 22, 7f);
                    if (en.Salud <= 0) EliminarEnemigo(i);
                }
            }
        }

        private void RevisarGemasCofresYPortal()
        {
            for (int i = 0; i < gemas.Count; i++)
            {
                var g = gemas[i];
                if (g.Recogida) continue;
                if (Distancia(jugX, jugY, g.X, g.Y) < 55)
                {
                    TocarSonidoRapido("victoria");
                    g.Recogida = true;
                    screenShake = 10;
                    GenerarParticulas(g.X, g.Y, g.Color, 40, 8f);

                    if (g.Id == "RUBI") tieneGemaRubi = true;
                    else if (g.Id == "ZAFIRO") tieneGemaZafiro = true;
                    else if (g.Id == "ESMERALDA")
                    {
                        tieneGemaEsmeralda = true;
                        maxSalud += 80;
                        salud = maxSalud;
                    }
                    else if (g.Id == "AMATISTA") tieneGemaAmatista = true;

                    int total = ContarGemas();
                    MostrarAviso($"✦ ¡ENCONTRASTE LA {g.Nombre}! ({g.Efecto}) [{total}/4] ✦", 200);
                }
            }

            for (int i = 0; i < cofres.Count; i++)
            {
                var c = cofres[i];
                if (c.Abierto) continue;
                if (Distancia(jugX, jugY, c.X, c.Y) < 52)
                {
                    TocarSonidoRapido("moneda");
                    c.Abierto = true;
                    cofresAbiertos++;
                    fragmentos += 2;
                    salud = Math.Min(maxSalud, salud + 45);
                    cargaPoderGemas = Math.Min(100, cargaPoderGemas + 35);
                    GenerarParticulas(c.X, c.Y, Color.Gold, 24, 6f);
                    MostrarAviso("¡COFRE DEL TESORO ABIERTO! (+2 Fragmentos y +45 Vida)", 120);
                }
            }

            if (Distancia(jugX, jugY, altarPortal.X, altarPortal.Y) < 75)
            {
                int total = ContarGemas();
                if (total >= 4) FinalizarExpedicion(true, false);
                else if (tickContador % 90 == 0)
                {
                    TocarSonidoRapido("alerta");
                    MostrarAviso($"EL PORTAL ESTÁ SELLADO: FALTAN {4 - total} GEMAS ELEMENTALES", 90);
                }
            }
        }

        private void ActualizarEnemigos()
        {
            for (int i = enemigos.Count - 1; i >= 0; i--)
            {
                var en = enemigos[i];
                if (en.FlashImpacto > 0) en.FlashImpacto--;
                if (en.CongeladoTimer > 0) { en.CongeladoTimer--; continue; }

                float dist = Distancia(jugX, jugY, en.X, en.Y);
                if (dist < 440)
                {
                    float dx = jugX - en.X;
                    float dy = jugY - en.Y;
                    float len = Math.Max(1f, (float)Math.Sqrt(dx * dx + dy * dy));

                    if (en.Tipo != "Torreta" && dist > 140)
                    {
                        float velEn = en.EsJefeGuardian ? 2.5f : 3.2f;
                        float nx = en.X + (dx / len) * velEn;
                        float ny = en.Y + (dy / len) * velEn;
                        if (EsPuntoCaminable(nx, en.Y)) en.X = nx;
                        if (EsPuntoCaminable(en.X, ny)) en.Y = ny;
                    }

                    en.CooldownDisparo--;
                    if (en.CooldownDisparo <= 0)
                    {
                        TocarSonidoRapido(en.EsJefeGuardian ? "canon" : "disparo", 85);
                        en.CooldownDisparo = en.EsJefeGuardian ? 55 : 80;
                        proyectiles.Add(new ProyectilAventura
                        {
                            X = en.X,
                            Y = en.Y,
                            VX = (dx / len) * 9.5f,
                            VY = (dy / len) * 9.5f,
                            EsJugador = false,
                            Danio = en.EsJefeGuardian ? 18 : 11,
                            Vida = 42,
                            Radio = en.EsJefeGuardian ? 11 : 8,
                            Color = Color.OrangeRed
                        });
                    }
                }
            }
        }

        private void ActualizarProyectiles()
        {
            for (int i = proyectiles.Count - 1; i >= 0; i--)
            {
                var p = proyectiles[i];
                p.X += p.VX;
                p.Y += p.VY;
                p.Vida--;

                bool chocaMuro = false;
                for (int m = 0; m < murosSolidos.Count; m++)
                    if (murosSolidos[m].Contains((int)p.X, (int)p.Y)) { chocaMuro = true; break; }

                if (p.Vida <= 0 || chocaMuro)
                {
                    proyectiles.RemoveAt(i);
                    continue;
                }

                if (p.EsJugador)
                {
                    for (int j = enemigos.Count - 1; j >= 0; j--)
                    {
                        var en = enemigos[j];
                        if (Distancia(p.X, p.Y, en.X, en.Y) < p.Radio + 30)
                        {
                            TocarSonidoRapido("impacto", 45);
                            en.Salud -= p.Danio;
                            en.FlashImpacto = 3;
                            if (p.Congela) en.CongeladoTimer = 95;
                            GenerarParticulas(p.X, p.Y, p.Color, 8, 4f);
                            proyectiles.RemoveAt(i);

                            if (en.Salud <= 0) EliminarEnemigo(j);
                            break;
                        }
                    }
                }
                else
                {
                    if (dashActivo == 0 && Distancia(p.X, p.Y, jugX, jugY) < p.Radio + 22)
                    {
                        TocarSonidoRapido("danio", 100);
                        salud -= p.Danio;
                        screenShake = 5;
                        GenerarParticulas(jugX, jugY, Color.Crimson, 8, 4f);
                        proyectiles.RemoveAt(i);

                        if (salud <= 0) { FinalizarExpedicion(false, false); return; }
                    }
                }
            }
        }

        private void EliminarEnemigo(int indice)
        {
            var en = enemigos[indice];
            TocarSonidoRapido("explosion", en.EsJefeGuardian ? 120 : 55);
            enemigosVencidos++;
            fragmentos += en.EsJefeGuardian ? 3 : 1;
            cargaPoderGemas = Math.Min(100, cargaPoderGemas + 20);
            GenerarParticulas(en.X, en.Y, Color.OrangeRed, 28, 7f);

            if (en.EsJefeGuardian)
                MostrarAviso($"¡DERROTASTE AL {en.Nombre}! RECOGE SU GEMA ELEMENTAL", 150);

            enemigos.RemoveAt(indice);
        }

        private void ActualizarOndasYParticulas()
        {
            for (int i = ondas.Count - 1; i >= 0; i--)
            {
                ondas[i].Vida--;
                if (ondas[i].Vida <= 0) ondas.RemoveAt(i);
            }
            for (int i = particulas.Count - 1; i >= 0; i--)
            {
                var pt = particulas[i];
                pt.X += pt.VX;
                pt.Y += pt.VY;
                pt.Vida--;
                if (pt.Vida <= 0) particulas.RemoveAt(i);
            }
        }

        private void GenerarParticulas(float x, float y, Color c, int cant, float velMax)
        {
            if (particulas.Count > 140) cant = Math.Min(cant, 4);
            for (int i = 0; i < cant; i++)
            {
                double ang = rand.NextDouble() * Math.PI * 2;
                float v = (float)(rand.NextDouble() * velMax + 1);
                int vida = rand.Next(12, 24);
                particulas.Add(new ParticulaAventura { X = x, Y = y, VX = (float)Math.Cos(ang) * v, VY = (float)Math.Sin(ang) * v, Tamano = rand.Next(4, 9), Color = c, Vida = vida, MaxVida = vida });
            }
        }

        private void MostrarAviso(string texto, int duracion)
        {
            mensajeAviso = texto;
            avisoTimer = duracion;
        }

        private int ContarGemas() =>
            (tieneGemaRubi ? 1 : 0) + (tieneGemaZafiro ? 1 : 0) + (tieneGemaEsmeralda ? 1 : 0) + (tieneGemaAmatista ? 1 : 0);

        private static float Distancia(float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private void FinalizarExpedicion(bool portalAbierto, bool retirado)
        {
            if (expedicionTerminada) return;
            expedicionTerminada = true;
            gameTimer?.Stop();

            TocarSonidoRapido(portalAbierto || retirado ? "victoria" : "gameover");

            int gemasTotales = ContarGemas();
            int xpGanada = (gemasTotales * 150) + (enemigosVencidos * 35) + (portalAbierto ? 400 : 0);
            int fragsGanados = fragmentos + (portalAbierto ? 8 : 0);

            playerGlobal.FragmentosDatos += fragsGanados;
            playerGlobal.XP += xpGanada;
            if (portalAbierto) playerGlobal.MisionActual.ZonaAlcanzada = true;
            GestorGuardado.Guardar(playerGlobal);

            Color cBorde = portalAbierto ? Color.Gold : (retirado ? Color.Cyan : Color.OrangeRed);
            string titulo = portalAbierto
                ? "✦ ¡PORTAL ANCESTRAL ABIERTO! EXPEDICIÓN COMPLETADA ✦"
                : (retirado ? "✦ RETORNO TÁCTICO DE EXPEDICIÓN ✦" : "⚠ TRAJE SIN ENERGÍA - RESCATE EN BASE ⚠");

            pnlReporteFinal = new Panel
            {
                Size = new Size(580, 335),
                Location = new Point((this.ClientSize.Width - 580) / 2, (this.ClientSize.Height - 335) / 2),
                BackColor = Color.FromArgb(242, 10, 16, 28)
            };
            pnlReporteFinal.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var penOuter = new Pen(cBorde, 3);
                e.Graphics.DrawRectangle(penOuter, 1, 1, pnlReporteFinal.Width - 3, pnlReporteFinal.Height - 3);
            };

            Label lblHead = new Label { Text = $"NEXUS // REPORTE: {exploradorActual.NombreMapaPropio}", Font = new Font("Consolas", 10f, FontStyle.Bold), ForeColor = Color.Cyan, Location = new Point(24, 18), AutoSize = true };
            Label lblEst = new Label { Text = titulo, Font = new Font("Consolas", 12.8f, FontStyle.Bold), ForeColor = cBorde, Location = new Point(24, 48), Size = new Size(530, 30) };
            Label lblDet = new Label
            {
                Text = $"EXPLORADOR        : {exploradorActual.Nombre}\n" +
                       $"GEMAS ENCONTRADAS : {gemasTotales} / 4 Gemas Elementales\n" +
                       $"GUARDIANES VENCIDOS: {enemigosVencidos}  |  COFRES: {cofresAbiertos}\n" +
                       $"FRAGMENTOS        : +{fragsGanados} sincronizados\n" +
                       $"EXPERIENCIA       : +{xpGanada} XP",
                Font = new Font("Consolas", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(28, 92),
                Size = new Size(520, 130)
            };

            Button btnVolver = new Button
            {
                Text = "▶ VOLVER A LA CABINA [X]",
                Font = new Font("Consolas", 11.5f, FontStyle.Bold),
                Location = new Point(140, 255),
                Size = new Size(300, 48),
                BackColor = Color.FromArgb(0, 140, 175),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnVolver.FlatAppearance.BorderColor = Color.Yellow;
            btnVolver.FlatAppearance.BorderSize = 2;
            btnVolver.Click += (s, e) => this.Close();

            pnlReporteFinal.Controls.AddRange(new Control[] { lblHead, lblEst, lblDet, btnVolver });
            this.Controls.Add(pnlReporteFinal);
            pnlReporteFinal.BringToFront();
            btnVolver.Focus();

            bool xPrev = true;
            timerReporteMando = new System.Windows.Forms.Timer { Interval = 30 };
            timerReporteMando.Tick += (s, e) =>
            {
                var m = ControlMando.Leer();
                bool pulsa = (m.BotonSaltar || m.BotonDisparar || m.BotonSalir) && !xPrev;
                xPrev = m.BotonSaltar || m.BotonDisparar || m.BotonSalir;
                if (pulsa) this.Close();
            };
            timerReporteMando.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (exploradorActual == null) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighSpeed;

            if (screenShake > 0)
                g.TranslateTransform(rand.Next(-4, 5), rand.Next(-4, 5));

            Rectangle vistaCam = new Rectangle((int)camX - 120, (int)camY - 120, this.ClientSize.Width + 240, this.ClientSize.Height + 240);

            var estadoCam = g.Save();
            g.TranslateTransform(-camX, -camY);

            // 1. Fondo del mundo (solo el área visible)
            if (brVacioMapa != null)
                g.FillRectangle(brVacioMapa, vistaCam);

            if (usaIslasFlotantes)
            {
                for (int nx = 100; nx < MUNDO_W; nx += 320)
                {
                    if (nx + 180 < vistaCam.Left || nx > vistaCam.Right) continue;
                    for (int ny = 100; ny < MUNDO_H; ny += 280)
                    {
                        if (ny + 50 < vistaCam.Top || ny > vistaCam.Bottom) continue;
                        g.DrawEllipse(penNubeCache, nx + (tickContador / 4 % 40), ny, 140, 45);
                    }
                }
            }

            // 2. Zonas de Tierra Firme / Islas (solo visibles)
            if (brTierraMapa != null && penBordeTierraMapa != null)
            {
                for (int i = 0; i < zonasTierra.Count; i++)
                {
                    var z = zonasTierra[i];
                    if (!z.IntersectsWith(vistaCam)) continue;
                    g.FillRectangle(brTierraMapa, z);
                    if (usaIslasFlotantes) g.DrawRectangle(penBordeTierraMapa, z);
                }
            }

            // 3. Ríos / Lagos de Lava / Fosos
            if (brPeligroMapa != null)
            {
                using var penOnda = new Pen(Color.FromArgb(120, Color.White), 2) { DashStyle = DashStyle.Dash, DashOffset = -(tickContador % 30) };
                for (int i = 0; i < zonasPeligroAguaLava.Count; i++)
                {
                    var zp = zonasPeligroAguaLava[i];
                    if (!zp.IntersectsWith(vistaCam)) continue;
                    g.FillRectangle(brPeligroMapa, zp);
                    g.DrawLine(penOnda, zp.Left + 12, zp.Top + zp.Height / 2, zp.Right - 12, zp.Top + zp.Height / 2);
                }
            }

            // 4. Puentes y Pasarelas Seguras
            for (int i = 0; i < puentesSeguros.Count; i++)
            {
                var rPuente = puentesSeguros[i];
                if (!rPuente.IntersectsWith(vistaCam)) continue;
                g.FillRectangle(brPuenteCache, rPuente);
                g.DrawRectangle(penPuenteCache, rPuente);
            }

            // 5. Muros Sólidos
            if (penBordeTierraMapa != null)
            {
                for (int i = 0; i < murosSolidos.Count; i++)
                {
                    var m = murosSolidos[i];
                    if (!m.IntersectsWith(vistaCam)) continue;
                    using var brMuro = new LinearGradientBrush(m, ControlPaint.Light(exploradorActual.ColorTierra, 0.35f), Color.Black, 45f);
                    g.FillRectangle(brMuro, m);
                    g.DrawRectangle(penBordeTierraMapa, m);
                }
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 6. Portales Warp
            foreach (var w in portalesWarp)
            {
                if (vistaCam.Contains((int)w.A.X, (int)w.A.Y) || vistaCam.Contains((int)w.B.X, (int)w.B.Y))
                {
                    using var brW = new SolidBrush(Color.FromArgb(95, w.Color));
                    g.FillEllipse(brW, w.A.X - 38, w.A.Y - 38, 76, 76);
                    g.DrawEllipse(penGemaBordeCache, w.A.X - 30, w.A.Y - 30, 60, 60);
                    g.FillEllipse(brW, w.B.X - 38, w.B.Y - 38, 76, 76);
                    g.DrawEllipse(penGemaBordeCache, w.B.X - 30, w.B.Y - 30, 60, 60);
                }
            }

            // 7. Santuarios y Templos
            for (int i = 0; i < ruinas.Count; i++)
            {
                var r = ruinas[i];
                if (!r.Rect.IntersectsWith(vistaCam)) continue;
                using var penS = new Pen(r.ColorBorde, 3);
                g.DrawRectangle(penS, r.Rect);
                g.DrawString(r.Nombre, fontSantuario, Brushes.White, r.Rect.X + 20, r.Rect.Y + 12);
            }

            // 8. Decoración viva del mundo (solo dentro de cámara)
            for (int i = 0; i < decoraciones.Count; i++)
            {
                var d = decoraciones[i];
                if (!vistaCam.Contains((int)d.X, (int)d.Y)) continue;

                if (d.Tipo == "ANTORCHA")
                {
                    int llama = 10 + (tickContador / 3 % 5);
                    using var brAura = new SolidBrush(Color.FromArgb(75, d.Color));
                    using var brFuego = new SolidBrush(d.Color);
                    g.FillEllipse(brAura, d.X - 24, d.Y - 24, 48, 48);
                    g.FillEllipse(brFuego, d.X - 7, d.Y - 7 - llama / 2, 14, llama);
                    g.FillEllipse(Brushes.White, d.X - 3, d.Y - 3, 6, 6);
                }
                else if (d.Tipo == "ARBOL")
                {
                    float sway = (float)Math.Sin(tickContador * 0.05 + d.X) * 3f;
                    using var brCopa = new SolidBrush(Color.FromArgb(140, d.Color));
                    using var penCopa = new Pen(d.Color, 1.5f);
                    g.FillRectangle(Brushes.SaddleBrown, d.X - 5, d.Y - 8, 10, 22);
                    g.FillEllipse(brCopa, d.X - 26 + sway, d.Y - 36, 52, 42);
                    g.DrawEllipse(penCopa, d.X - 26 + sway, d.Y - 36, 52, 42);
                }
                else if (d.Tipo == "CRISTAL")
                {
                    using var brCris = new SolidBrush(Color.FromArgb(150, d.Color));
                    PointF[] tri = { new PointF(d.X, d.Y - 24), new PointF(d.X + 12, d.Y + 6), new PointF(d.X - 12, d.Y + 6) };
                    g.FillPolygon(brCris, tri);
                    g.DrawPolygon(Pens.White, tri);
                }
                else
                {
                    g.FillRectangle(Brushes.DimGray, d.X - 10, d.Y - 22, 20, 32);
                    g.DrawRectangle(Pens.Gold, d.X - 10, d.Y - 22, 20, 32);
                }
            }

            // 9. Portal Ancestral (Meta)
            if (vistaCam.Contains((int)altarPortal.X, (int)altarPortal.Y))
            {
                bool listoPortal = ContarGemas() >= 4;
                Color cPortal = listoPortal ? Color.Gold : Color.Cyan;
                using var brPortalAura = new SolidBrush(Color.FromArgb(85, cPortal));
                using var penPortal = new Pen(cPortal, 4);
                g.FillEllipse(brPortalAura, altarPortal.X - 75, altarPortal.Y - 75, 150, 150);
                g.DrawEllipse(penPortal, altarPortal.X - 55, altarPortal.Y - 55, 110, 110);
                g.DrawString(listoPortal ? "¡ENTRA AL PORTAL!" : $"PORTAL ({ContarGemas()}/4 GEMAS)",
                    fontPortal, Brushes.Gold, altarPortal.X - 80, altarPortal.Y - 78);
            }

            // 10. Cofres y Gemas
            for (int i = 0; i < cofres.Count; i++)
            {
                var c = cofres[i];
                if (!vistaCam.Contains((int)c.X, (int)c.Y)) continue;
                Rectangle rc = new Rectangle((int)c.X - 20, (int)c.Y - 16, 40, 32);
                g.FillRectangle(c.Abierto ? brCofreAbiertoCache : brCofreCerradoCache, rc);
                g.DrawRectangle(c.Abierto ? Pens.Gray : Pens.Yellow, rc);
                if (!c.Abierto) g.FillRectangle(Brushes.Cyan, rc.X + 16, rc.Y + 10, 8, 8);
            }

            float pulso = (float)Math.Sin(tickContador * 0.12) * 5f;
            for (int i = 0; i < gemas.Count; i++)
            {
                var gm = gemas[i];
                if (gm.Recogida || !vistaCam.Contains((int)gm.X, (int)gm.Y)) continue;
                using var brAura = new SolidBrush(Color.FromArgb(95, gm.Color));
                using var brGema = new SolidBrush(gm.Color);
                g.FillEllipse(brAura, gm.X - 30, gm.Y - 30 + pulso, 60, 60);

                PointF[] rombo = { new PointF(gm.X, gm.Y - 20 + pulso), new PointF(gm.X + 16, gm.Y + pulso), new PointF(gm.X, gm.Y + 20 + pulso), new PointF(gm.X - 16, gm.Y + pulso) };
                g.FillPolygon(brGema, rombo);
                g.DrawPolygon(penGemaBordeCache, rombo);
                g.DrawString(gm.Nombre, fontGema, Brushes.White, gm.X - 68, gm.Y - 42);
            }

            // 11. Villanos y Jugador animados
            for (int i = 0; i < enemigos.Count; i++)
            {
                var en = enemigos[i];
                if (!vistaCam.Contains((int)en.X, (int)en.Y)) continue;
                int tam = en.EsJefeGuardian ? 110 : 88;
                Bitmap sprEn = SpriteFactory.ObtenerSpriteVillano(en.Tipo, tickContador / 5, en.FlashImpacto > 0);
                g.DrawImage(sprEn, en.X - tam / 2f, en.Y - tam / 2f, tam, tam);

                if (en.CongeladoTimer > 0)
                    g.FillEllipse(brHieloCache, en.X - 36, en.Y - 36, 72, 72);

                g.DrawString(en.Nombre, fontEnemigo, en.EsJefeGuardian ? Brushes.Gold : Brushes.OrangeRed, en.X - 42, en.Y - tam / 2f - 22);
                g.FillRectangle(Brushes.Black, en.X - 36, en.Y - tam / 2f - 8, 72, 6);
                g.FillRectangle(Brushes.SpringGreen, en.X - 35, en.Y - tam / 2f - 7, (int)(70 * (en.Salud / (float)en.MaxSalud)), 4);
            }

            Bitmap sprJug = SpriteFactory.ObtenerSpriteOperativo(exploradorActual.FormaVisual, exploradorActual.ColorPrincipal, exploradorActual.ColorSecundario, frameAnimacion);
            var stJug = g.Save();
            if (dirX < 0)
            {
                g.TranslateTransform(jugX + 50, jugY - 50);
                g.ScaleTransform(-1, 1);
                g.DrawImage(sprJug, 0, 0, 100, 100);
            }
            else
            {
                g.DrawImage(sprJug, jugX - 50, jugY - 50, 100, 100);
            }
            g.Restore(stJug);

            // 12. Ambiente, Ondas, Proyectiles, Partículas y Niebla
            for (int i = 0; i < ambienteFlotante.Count; i++)
            {
                var amb = ambienteFlotante[i];
                if (!vistaCam.Contains((int)amb.X, (int)amb.Y)) continue;
                using var brAmb = new SolidBrush(Color.FromArgb(145, amb.Color));
                g.FillEllipse(brAmb, amb.X, amb.Y, 4, 4);
            }

            for (int i = 0; i < ondas.Count; i++)
            {
                var o = ondas[i];
                int alpha = Math.Clamp((int)(140 * (o.Vida / (float)o.MaxVida)), 15, 150);
                using var penOnda = new Pen(Color.FromArgb(alpha, o.Color), 4);
                g.DrawEllipse(penOnda, o.X - o.Radio, o.Y - o.Radio, o.Radio * 2, o.Radio * 2);
            }

            for (int i = 0; i < proyectiles.Count; i++)
            {
                var p = proyectiles[i];
                if (!vistaCam.Contains((int)p.X, (int)p.Y)) continue;
                using var brP = new SolidBrush(p.Color);
                g.FillEllipse(brP, p.X - p.Radio, p.Y - p.Radio, p.Radio * 2, p.Radio * 2);
            }

            for (int i = 0; i < particulas.Count; i++)
            {
                var pt = particulas[i];
                if (!vistaCam.Contains((int)pt.X, (int)pt.Y)) continue;
                int alpha = Math.Clamp((int)(255 * (pt.Vida / (float)pt.MaxVida)), 20, 255);
                using var brPt = new SolidBrush(Color.FromArgb(alpha, pt.Color));
                g.FillEllipse(brPt, pt.X, pt.Y, pt.Tamano, pt.Tamano);
            }

            g.SmoothingMode = SmoothingMode.HighSpeed;

            int startIx = Math.Max(0, (int)(camX / CELDA_NIEBLA) - 1);
            int endIx = Math.Min(zonaDescubierta.GetLength(0) - 1, (int)((camX + this.ClientSize.Width) / CELDA_NIEBLA) + 1);
            int startIy = Math.Max(0, (int)(camY / CELDA_NIEBLA) - 1);
            int endIy = Math.Min(zonaDescubierta.GetLength(1) - 1, (int)((camY + this.ClientSize.Height) / CELDA_NIEBLA) + 1);

            for (int ix = startIx; ix <= endIx; ix++)
                for (int iy = startIy; iy <= endIy; iy++)
                    if (!zonaDescubierta[ix, iy])
                        g.FillRectangle(brNieblaCache, ix * CELDA_NIEBLA, iy * CELDA_NIEBLA, CELDA_NIEBLA, CELDA_NIEBLA);

            g.Restore(estadoCam);

            // ================== HUD Y MINIMAPA CARTOGRÁFICO ==================
            g.FillRectangle(brHudBgCache, 0, 0, this.Width, 52);
            if (penHudBordeMapa != null)
                g.DrawLine(penHudBordeMapa, 0, 52, this.Width, 52);

            g.DrawString($"VIDA: {salud}/{maxSalud} | PODER(△): {cargaPoderGemas}% | FRAGS: {fragmentos} | VOZ: [L1/V] | SALIR: [OPT]",
                fontHud, Brushes.Cyan, 16, 16);

            DibujarSlotGema(g, this.Width - 255, 11, "RUBÍ", tieneGemaRubi, Color.OrangeRed);
            DibujarSlotGema(g, this.Width - 192, 11, "ZAFIRO", tieneGemaZafiro, Color.DeepSkyBlue);
            DibujarSlotGema(g, this.Width - 129, 11, "ESMER.", tieneGemaEsmeralda, Color.SpringGreen);
            DibujarSlotGema(g, this.Width - 66, 11, "AMAT.", tieneGemaAmatista, Color.MediumOrchid);

            if (avisoTimer > 0)
            {
                g.FillRectangle(brAvisoBgCache, 180, 62, 760, 32);
                g.DrawRectangle(Pens.Gold, 180, 62, 760, 32);
                g.DrawString(mensajeAviso, fontAviso, Brushes.Gold, 194, 70);
            }

            int mmX = 16, mmY = this.ClientSize.Height - 166, mmW = 190, mmH = 150;
            g.FillRectangle(brMinimapBgCache, mmX, mmY, mmW, mmH);

            if (usaIslasFlotantes && brIslaMinimap != null)
            {
                for (int i = 0; i < zonasTierra.Count; i++)
                {
                    var z = zonasTierra[i];
                    g.FillRectangle(brIslaMinimap, mmX + (z.X / (float)MUNDO_W) * mmW, mmY + (z.Y / (float)MUNDO_H) * mmH, (z.Width / (float)MUNDO_W) * mmW, (z.Height / (float)MUNDO_H) * mmH);
                }
            }

            for (int i = 0; i < murosSolidos.Count; i++)
            {
                var m = murosSolidos[i];
                g.FillRectangle(Brushes.Gray, mmX + (m.X / (float)MUNDO_W) * mmW, mmY + (m.Y / (float)MUNDO_H) * mmH, Math.Max(2, (m.Width / (float)MUNDO_W) * mmW), Math.Max(2, (m.Height / (float)MUNDO_H) * mmH));
            }

            if (penHudBordeMapa != null)
                g.DrawRectangle(penHudBordeMapa, mmX, mmY, mmW, mmH);

            for (int i = 0; i < gemas.Count; i++)
            {
                var gm = gemas[i];
                if (gm.Recogida) continue;
                using var brG = new SolidBrush(gm.Color);
                g.FillEllipse(brG, mmX + (gm.X / MUNDO_W) * mmW - 4, mmY + (gm.Y / MUNDO_H) * mmH - 4, 8, 8);
            }

            g.FillRectangle(Brushes.Gold, mmX + (altarPortal.X / MUNDO_W) * mmW - 4, mmY + (altarPortal.Y / MUNDO_H) * mmH - 4, 8, 8);
            g.FillEllipse(Brushes.White, mmX + (jugX / MUNDO_W) * mmW - 4, mmY + (jugY / MUNDO_H) * mmH - 4, 8, 8);
        }

        private void DibujarSlotGema(Graphics g, int x, int y, string nombre, bool obtenida, Color colorGema)
        {
            Rectangle r = new Rectangle(x, y, 56, 30);
            if (obtenida)
            {
                using var br = new SolidBrush(Color.FromArgb(190, colorGema));
                g.FillRectangle(br, r);
            }
            else
            {
                g.FillRectangle(brSlotVacioCache, r);
            }
            g.DrawRectangle(obtenida ? Pens.White : Pens.Gray, r);
            g.DrawString(nombre, fontSlot, obtenida ? Brushes.Black : Brushes.Gray, x + 5, y + 9);
        }
    }
}