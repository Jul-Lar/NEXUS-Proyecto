using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NexusForms
{
    public class FormAviones1945 : Form
    {
        private Jugador playerGlobal;
        private System.Windows.Forms.Timer gameTimer = null!;
        private Random rand = new Random();
        private Image? fondoEspacialExterno = null;
        private Bitmap? fondoEspacialCache = null;

        private float naveX = 460, naveY = 560;
        private int salud = 220, maxSalud = 220;
        private int escudo = 120, maxEscudo = 120;
        private int flashEscudo = 0;
        private int puntaje = 0;
        private int fragmentosGanados = 0;
        private int oleada = 1;
        private int tick = 0;
        private int screenShake = 0;
        private bool juegoTerminado = false;

        private int tipoArma = 1;
        private int nivelArma = 1;
        private int dronesOrbitales = 0;
        private int energiaLaser = 100;
        private bool disparandoLaser = false;
        private int cdDisparo = 0;
        private int cargaSupernova = 100;

        private bool keyIzq, keyDer, keyArr, keyAba, keyDisparo, keySupernova;
        private bool prevSupernova = false, prevCambio = false, prevVoz = false, prevSalir = false;

        // Caché GDI+ para 60 FPS sin pausas del Garbage Collector
        private static readonly Font fontBoss = new Font("Consolas", 10f, FontStyle.Bold);
        private static readonly Font fontDrop = new Font("Consolas", 8.5f, FontStyle.Bold);
        private static readonly Font fontHolo = new Font("Consolas", 9.5f, FontStyle.Bold);
        private static readonly Font fontHudTop = new Font("Consolas", 9.8f, FontStyle.Bold);
        private static readonly Font fontHudBot = new Font("Consolas", 9.1f, FontStyle.Bold);

        private static readonly SolidBrush brHudBgCache = new SolidBrush(Color.FromArgb(232, 6, 10, 22));
        private static readonly SolidBrush brAuraLaserCache = new SolidBrush(Color.FromArgb(95, Color.Magenta));
        private static readonly SolidBrush brAuraMotorCache = new SolidBrush(Color.FromArgb(90, Color.Cyan));
        private static readonly SolidBrush brAuraOvniCache = new SolidBrush(Color.FromArgb(55, Color.SpringGreen));
        private static readonly SolidBrush brCupulaOvniCache = new SolidBrush(Color.FromArgb(135, Color.Aquamarine));
        private static readonly SolidBrush brAuraCoreBossCache = new SolidBrush(Color.FromArgb(95, Color.SpringGreen));

        private static readonly Pen penHudLineCache = new Pen(Color.Cyan, 2);
        private static readonly Pen penFugazCache = new Pen(Color.FromArgb(180, Color.White), 2);
        private static readonly Pen penEstelaCache = new Pen(Color.FromArgb(110, Color.Cyan), 2.5f);
        private static readonly Pen penObusBordeCache = new Pen(Color.Gold, 1.2f);
        private static readonly Pen penObusOjivaCache = new Pen(Color.White, 1f);
        private static readonly Pen penAlaCazaCache = new Pen(Color.Cyan, 1.8f);
        private static readonly Pen penFuselajeCazaCache = new Pen(Color.Cyan, 1.5f);
        private static readonly Pen penOvniBordeCache = new Pen(Color.Cyan, 1.6f);
        private static readonly Pen penOvniBordeHitCache = new Pen(Color.White, 1.6f);
        private static readonly Pen penOvniCupulaCache = new Pen(Color.White, 1.4f);
        private static readonly Pen penCruceroBordeCache = new Pen(Color.OrangeRed, 2f);
        private static readonly Pen penCruceroBordeHitCache = new Pen(Color.White, 2f);
        private static readonly Pen penCruceroCoreCache = new Pen(Color.Gold, 1.5f);
        private static readonly Pen penBossBordeCache = new Pen(Color.SpringGreen, 2.5f);
        private static readonly Pen penBossBordeHitCache = new Pen(Color.White, 2.5f);
        private static readonly Pen penBossCoreCache = new Pen(Color.White, 2f);
        private static readonly Pen penDropBordeCache = new Pen(Color.White, 2);

        private string ultimoSonido = "";
        private int tickUltimoSonido = -10;

        private List<EstrellaSciFi> estrellas = new List<EstrellaSciFi>();
        private List<PlanetaSciFi> planetas = new List<PlanetaSciFi>();
        private List<EstrellaFugaz> fugaces = new List<EstrellaFugaz>();
        private List<PointF> estelaIzq = new List<PointF>();
        private List<PointF> estelaDer = new List<PointF>();

        private List<NaveAlien> enemigos = new List<NaveAlien>();
        private List<DisparoSciFi> disparos = new List<DisparoSciFi>();
        private List<ModuloMejora> mejoras = new List<ModuloMejora>();
        private List<OndaChoque> ondas = new List<OndaChoque>();
        private List<ParticulaSciFi> particulas = new List<ParticulaSciFi>();
        private List<TextoHolo> textosHolo = new List<TextoHolo>();

        private Panel? pnlFin;
        private System.Windows.Forms.Timer? timerFinMando;

        private class EstrellaSciFi { public float X, Y, Vel, Tam; public Color Color; }
        private class PlanetaSciFi { public float X, Y, Radio, Vel; public Color C1, C2, ColorAnillo; public bool TieneAnillo; }
        private class EstrellaFugaz { public float X, Y, VX, VY; public int Vida; }

        private class NaveAlien
        {
            public string Tipo = "";
            public string Nombre = "";
            public float X, Y, VX, VY, Fase;
            public int Salud, MaxSalud, Radio, CooldownDisparo, FlashImpacto, Puntos;
        }

        private class DisparoSciFi
        {
            public float X, Y, VX, VY;
            public bool EsJugador;
            public bool EsObusCanon;
            public bool EsMisilRastreador;
            public int Danio, Radio, Vida;
            public Color Color;
        }

        private class ModuloMejora
        {
            public string Tipo = "";
            public string Etiqueta = "";
            public float X, Y;
            public Color Color;
        }

        private class OndaChoque { public float X, Y, RadioMax; public int Vida, MaxVida; public Color Color; }
        private class ParticulaSciFi { public float X, Y, VX, VY, Tam; public int Vida, MaxVida; public Color Color; }
        private class TextoHolo { public string Texto = ""; public float X, Y; public int Vida; public Color Color; }

        public FormAviones1945(Jugador jugador)
        {
            playerGlobal = jugador;
            this.Text = $"NEXUS // STARFIGHTER SCI-FI - SECTOR {playerGlobal.RealidadAsignada}";
            this.Size = new Size(920, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.BackColor = Color.FromArgb(4, 6, 16);

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            SintetizadorSonido.Inicializar();
            AsistenteVozIA.Inicializar();
            AsistenteVozIA.MostrarSubtituloEnJuego = (txt, dur) => AgregarTextoHolo(txt, 75, 82, Color.Cyan);
            AsistenteVozIA.ObtenerContextoJuegoActual = () =>
            {
                bool hayJefe = enemigos.Any(e => e.Tipo == "DREADNOUGHT");
                if (hayJefe)
                    return $"Alerta máxima: destruye al Leviatán Madre Xeno de la oleada {oleada}. Usa el láser o detona la Supernova si está al cien por ciento.";
                return $"Estás en la oleada {oleada} con {salud} de casco y {escudo} de escudo. Elimina la flota alienígena, recoge los módulos de mejora y carga tu Supernova.";
            };

            IntentarCargarImagenFondo();
            PreRenderizarFondoEspacial();
            InicializarCosmos();

            this.KeyDown += SciFi_KeyDown;
            this.KeyUp += SciFi_KeyUp;
            this.FormClosing += (s, e) =>
            {
                gameTimer?.Stop();
                timerFinMando?.Stop();
                fondoEspacialExterno?.Dispose();
                fondoEspacialCache?.Dispose();
            };

            gameTimer = new System.Windows.Forms.Timer { Interval = 16 };
            gameTimer.Tick += BucleCombateEspacial;
            gameTimer.Start();
        }

        private void TocarSonidoRapido(string nombre, int volumen = -1)
        {
            if (SintetizadorSonido.Silenciado) return;
            if (ultimoSonido == nombre && tick - tickUltimoSonido < 4)
                return;

            ultimoSonido = nombre;
            tickUltimoSonido = tick;

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

        private void IntentarCargarImagenFondo()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] nombresArchivo =
            {
                "fondo_espacio.jpg", "fondo_espacio.png", "espacio.jpg", "espacio.png",
                "galaxia.jpg", "galaxia.png", "space.jpg", "space.png"
            };

            foreach (string nombre in nombresArchivo)
            {
                string[] rutas =
                {
                    Path.Combine(baseDir, "Assets", nombre),
                    Path.Combine(baseDir, "Resources", nombre),
                    Path.Combine(baseDir, nombre)
                };

                foreach (string ruta in rutas)
                {
                    if (File.Exists(ruta))
                    {
                        try
                        {
                            using var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read);
                            fondoEspacialExterno = Image.FromStream(fs);
                            return;
                        }
                        catch { }
                    }
                }
            }
        }

        private void PreRenderizarFondoEspacial()
        {
            int w = Math.Max(1, this.ClientSize.Width);
            int h = Math.Max(1, this.ClientSize.Height);
            fondoEspacialCache = new Bitmap(w, h);
            using Graphics g = Graphics.FromImage(fondoEspacialCache);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (fondoEspacialExterno != null)
            {
                g.DrawImage(fondoEspacialExterno, new Rectangle(0, 0, w, h));
            }
            else
            {
                using (var brEspacio = new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.FromArgb(3, 5, 16), Color.FromArgb(12, 8, 32), 90f))
                    g.FillRectangle(brEspacio, 0, 0, w, h);

                using var brNeb1 = new SolidBrush(Color.FromArgb(28, Color.DeepSkyBlue));
                using var brNeb2 = new SolidBrush(Color.FromArgb(24, Color.MediumOrchid));
                g.FillEllipse(brNeb1, -80, 120, 520, 340);
                g.FillEllipse(brNeb2, 460, 260, 540, 360);
            }
        }

        private void InicializarCosmos()
        {
            Color[] coloresEstrella = { Color.White, Color.Cyan, Color.LightSkyBlue, Color.Plum, Color.Gold };
            for (int i = 0; i < 140; i++)
            {
                int capa = i % 3;
                estrellas.Add(new EstrellaSciFi
                {
                    X = rand.Next(0, 920),
                    Y = rand.Next(0, 680),
                    Vel = capa == 0 ? 1.0f : (capa == 1 ? 2.6f : 5.2f),
                    Tam = capa == 0 ? 1.5f : (capa == 1 ? 2.4f : 3.4f),
                    Color = Color.FromArgb(capa == 0 ? 110 : (capa == 1 ? 185 : 255), coloresEstrella[i % coloresEstrella.Length])
                });
            }

            planetas.Add(new PlanetaSciFi
            {
                X = 210,
                Y = 160,
                Radio = 68,
                Vel = 0.22f,
                C1 = Color.FromArgb(55, 110, 210),
                C2 = Color.FromArgb(10, 20, 48),
                ColorAnillo = Color.FromArgb(110, Color.Cyan),
                TieneAnillo = true
            });

            planetas.Add(new PlanetaSciFi
            {
                X = 740,
                Y = 420,
                Radio = 44,
                Vel = 0.35f,
                C1 = Color.FromArgb(180, 55, 140),
                C2 = Color.FromArgb(28, 10, 35),
                ColorAnillo = Color.Magenta,
                TieneAnillo = false
            });

            AgregarTextoHolo("✦ [1] PLASMA [2] LÁSER [3] OBÚS | [E] SUPERNOVA | [L1/V] IA VOZ | [OPT] SALIR ✦", 65, 490, Color.Gold);
        }

        private void SciFi_KeyDown(object? sender, KeyEventArgs e)
        {
            if (juegoTerminado)
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space || e.KeyCode == Keys.Escape)
                    this.Close();
                return;
            }

            if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left) keyIzq = true;
            if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right) keyDer = true;
            if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) keyArr = true;
            if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) keyAba = true;
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.J || e.KeyCode == Keys.Z) keyDisparo = true;
            if (e.KeyCode == Keys.E || e.KeyCode == Keys.K || e.KeyCode == Keys.X) keySupernova = true;
            if (e.KeyCode == Keys.V) AsistenteVozIA.ActivarEscuchaPorMando();

            if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1) SeleccionarArma(1);
            if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2) SeleccionarArma(2);
            if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3) SeleccionarArma(3);
            if (e.KeyCode == Keys.Tab || e.KeyCode == Keys.Q)
                SeleccionarArma((tipoArma % 3) + 1);

            if (e.KeyCode == Keys.M)
            {
                SintetizadorSonido.Silenciado = !SintetizadorSonido.Silenciado;
                if (!SintetizadorSonido.Silenciado) TocarSonidoRapido("click");
                AgregarTextoHolo(SintetizadorSonido.Silenciado ? "🔇 AUDIO SILENCIADO" : "🔊 AUDIO ACTIVADO", naveX - 65, naveY - 50, Color.White);
            }

            if (e.KeyCode == Keys.Escape) FinalizarPartida(true);
        }

        private void SciFi_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left) keyIzq = false;
            if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right) keyDer = false;
            if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) keyArr = false;
            if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) keyAba = false;
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.J || e.KeyCode == Keys.Z) keyDisparo = false;
            if (e.KeyCode == Keys.E || e.KeyCode == Keys.K || e.KeyCode == Keys.X) keySupernova = false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (juegoTerminado) return;
            if (e.Button == MouseButtons.Left) keyDisparo = true;
            if (e.Button == MouseButtons.Right) SeleccionarArma((tipoArma % 3) + 1);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left) keyDisparo = false;
        }

        private void SeleccionarArma(int nueva)
        {
            tipoArma = nueva;
            TocarSonidoRapido("click");
            string nom = nueva == 1 ? $"BALAS PERFORANTES NV.{nivelArma}" : (nueva == 2 ? $"LÁSER ANTIMATERIA NV.{nivelArma}" : $"CAÑÓN DE OBUSES NV.{nivelArma}");
            Color c = nueva == 1 ? Color.Cyan : (nueva == 2 ? Color.Magenta : Color.OrangeRed);
            AgregarTextoHolo($"SISTEMA ACTIVO: {nom}", naveX - 95, naveY - 48, c);
        }

        private void BucleCombateEspacial(object? sender, EventArgs e)
        {
            if (juegoTerminado) return;

            tick++;
            if (screenShake > 0) screenShake--;
            if (flashEscudo > 0) flashEscudo--;
            if (cdDisparo > 0) cdDisparo--;
            if (tick % 6 == 0 && cargaSupernova < 100) cargaSupernova++;
            if (tick % 12 == 0 && escudo < maxEscudo) escudo++;

            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;

            for (int i = 0; i < estrellas.Count; i++)
            {
                var st = estrellas[i];
                st.Y += st.Vel;
                if (st.Y > h)
                {
                    st.Y = 0;
                    st.X = rand.Next(0, w);
                }
            }

            for (int i = 0; i < planetas.Count; i++)
            {
                var pl = planetas[i];
                pl.Y += pl.Vel;
                if (pl.Y - pl.Radio > h + 40)
                {
                    pl.Y = -pl.Radio - 50;
                    pl.X = rand.Next(100, w - 100);
                }
            }

            if (tick % 75 == 0 && fugaces.Count < 2)
            {
                fugaces.Add(new EstrellaFugaz
                {
                    X = rand.Next(150, w),
                    Y = rand.Next(40, 220),
                    VX = -14f,
                    VY = 9f,
                    Vida = 22
                });
            }

            for (int i = fugaces.Count - 1; i >= 0; i--)
            {
                fugaces[i].X += fugaces[i].VX;
                fugaces[i].Y += fugaces[i].VY;
                fugaces[i].Vida--;
                if (fugaces[i].Vida <= 0) fugaces.RemoveAt(i);
            }

            var mando = ControlMando.Leer();

            bool pulsaSalir = mando.BotonSalir && !prevSalir;
            prevSalir = mando.BotonSalir;
            if (pulsaSalir)
            {
                FinalizarPartida(true);
                return;
            }

            float mx = 0, my = 0;
            if (keyIzq || mando.Izquierda) mx -= 1;
            if (keyDer || mando.Derecha) mx += 1;
            if (keyArr || mando.Arriba) my -= 1;
            if (keyAba || mando.Abajo) my += 1;

            if (mx != 0 || my != 0)
            {
                float len = (float)Math.Sqrt(mx * mx + my * my);
                naveX = Math.Clamp(naveX + (mx / len) * 7.0f, 45, w - 45);
                naveY = Math.Clamp(naveY + (my / len) * 7.0f, 75, h - 45);
            }

            estelaIzq.Add(new PointF(naveX - 32, naveY + 14));
            estelaDer.Add(new PointF(naveX + 32, naveY + 14));
            if (estelaIzq.Count > 12) estelaIzq.RemoveAt(0);
            if (estelaDer.Count > 12) estelaDer.RemoveAt(0);
            for (int k = 0; k < estelaIzq.Count; k++)
            {
                estelaIzq[k] = new PointF(estelaIzq[k].X, estelaIzq[k].Y + 3.5f);
                estelaDer[k] = new PointF(estelaDer[k].X, estelaDer[k].Y + 3.5f);
            }

            if (tick % 2 == 0)
            {
                GenerarParticulas(naveX - 7, naveY + 28, Color.Cyan, 1, 1.5f);
                GenerarParticulas(naveX + 7, naveY + 28, Color.DeepSkyBlue, 1, 1.5f);
            }

            bool pulsaVoz = mando.BotonVoz && !prevVoz;
            prevVoz = mando.BotonVoz;
            if (pulsaVoz) AsistenteVozIA.ActivarEscuchaPorMando();

            bool pulsaCambio = mando.BotonEspecial && !prevCambio;
            prevCambio = mando.BotonEspecial;
            if (pulsaCambio) SeleccionarArma((tipoArma % 3) + 1);

            bool pulsaNova = (keySupernova || mando.BotonAcuchillar) && !prevSupernova;
            prevSupernova = keySupernova || mando.BotonAcuchillar;
            if (pulsaNova && cargaSupernova >= 100)
                DetonarSupernova();

            bool disparando = keyDisparo || mando.BotonDisparar || mando.BotonSaltar;
            disparandoLaser = false;

            if (disparando)
            {
                if (tipoArma == 2 && energiaLaser > 0)
                {
                    disparandoLaser = true;
                    if (tick % 2 == 0) energiaLaser = Math.Max(0, energiaLaser - 1);
                    ProcesarLaserAntimateria();
                }
                else
                {
                    if (tipoArma == 2 && energiaLaser == 0) tipoArma = 1;
                    if (cdDisparo == 0) DispararArsenalSciFi();
                }
            }
            else if (energiaLaser < 100 && tick % 2 == 0)
            {
                energiaLaser++;
            }

            if (dronesOrbitales > 0 && tick % 18 == 0)
            {
                for (int d = 0; d < dronesOrbitales; d++)
                {
                    double ang = tick * 0.09 + d * Math.PI;
                    float dx = naveX + (float)Math.Cos(ang) * 52;
                    float dy = naveY + (float)Math.Sin(ang) * 52;
                    disparos.Add(new DisparoSciFi { X = dx, Y = dy - 10, VX = 0, VY = -17, EsJugador = true, Danio = 15 + nivelArma * 4, Radio = 5, Vida = 60, Color = Color.SpringGreen });
                }
            }

            GenerarFlotaAlienigena();
            ActualizarEnemigos();
            ActualizarDisparos();
            ActualizarMejoras();
            ActualizarEfectos();

            this.Invalidate();
        }

        private void DispararArsenalSciFi()
        {
            if (tipoArma == 1)
            {
                TocarSonidoRapido("disparo", 45);
                cdDisparo = Math.Max(6, 12 - nivelArma);
                int danio = 18 + nivelArma * 6;

                if (nivelArma == 1)
                {
                    disparos.Add(new DisparoSciFi { X = naveX - 11, Y = naveY - 28, VX = 0, VY = -16, EsJugador = true, Danio = danio, Radio = 6, Vida = 60, Color = Color.Cyan });
                    disparos.Add(new DisparoSciFi { X = naveX + 11, Y = naveY - 28, VX = 0, VY = -16, EsJugador = true, Danio = danio, Radio = 6, Vida = 60, Color = Color.Cyan });
                }
                else if (nivelArma == 2)
                {
                    disparos.Add(new DisparoSciFi { X = naveX, Y = naveY - 35, VX = 0, VY = -17.5f, EsJugador = true, Danio = danio + 5, Radio = 7, Vida = 60, Color = Color.Gold });
                    disparos.Add(new DisparoSciFi { X = naveX - 18, Y = naveY - 22, VX = -1.8f, VY = -16.5f, EsJugador = true, Danio = danio, Radio = 6, Vida = 60, Color = Color.Cyan });
                    disparos.Add(new DisparoSciFi { X = naveX + 18, Y = naveY - 22, VX = 1.8f, VY = -16.5f, EsJugador = true, Danio = danio, Radio = 6, Vida = 60, Color = Color.Cyan });
                }
                else if (nivelArma == 3)
                {
                    for (int s = -1; s <= 1; s += 2)
                    {
                        disparos.Add(new DisparoSciFi { X = naveX + s * 10, Y = naveY - 32, VX = 0, VY = -18.5f, EsJugador = true, Danio = danio + 6, Radio = 7, Vida = 60, Color = Color.Gold });
                        disparos.Add(new DisparoSciFi { X = naveX + s * 22, Y = naveY - 20, VX = s * 2.6f, VY = -17.5f, EsJugador = true, Danio = danio, Radio = 6, Vida = 60, Color = Color.Cyan });
                    }
                }
                else
                {
                    for (int s = -1; s <= 1; s += 2)
                    {
                        disparos.Add(new DisparoSciFi { X = naveX + s * 10, Y = naveY - 34, VX = 0, VY = -19.5f, EsJugador = true, Danio = danio + 10, Radio = 7, Vida = 60, Color = Color.Gold });
                        disparos.Add(new DisparoSciFi { X = naveX + s * 22, Y = naveY - 22, VX = s * 2.5f, VY = -18.5f, EsJugador = true, Danio = danio, Radio = 6, Vida = 60, Color = Color.Cyan });
                    }
                    if (tick % 16 == 0)
                    {
                        TocarSonidoRapido("misil", 80);
                        disparos.Add(new DisparoSciFi { X = naveX - 30, Y = naveY - 10, VX = -3.2f, VY = -11, EsJugador = true, EsMisilRastreador = true, Danio = 48, Radio = 8, Vida = 95, Color = Color.OrangeRed });
                        disparos.Add(new DisparoSciFi { X = naveX + 30, Y = naveY - 10, VX = 3.2f, VY = -11, EsJugador = true, EsMisilRastreador = true, Danio = 48, Radio = 8, Vida = 95, Color = Color.OrangeRed });
                    }
                }
            }
            else if (tipoArma == 3)
            {
                TocarSonidoRapido("canon", 90);
                cdDisparo = Math.Max(14, 23 - nivelArma * 2);
                int danioObus = 58 + nivelArma * 24;
                screenShake = 3;

                disparos.Add(new DisparoSciFi
                {
                    X = naveX,
                    Y = naveY - 35,
                    VX = 0,
                    VY = -14.5f,
                    EsJugador = true,
                    EsObusCanon = true,
                    Danio = danioObus,
                    Radio = 10 + nivelArma * 2,
                    Vida = 70,
                    Color = Color.OrangeRed
                });

                if (nivelArma >= 3)
                {
                    disparos.Add(new DisparoSciFi { X = naveX - 22, Y = naveY - 22, VX = -1.8f, VY = -14f, EsJugador = true, EsObusCanon = true, Danio = danioObus / 2, Radio = 8, Vida = 70, Color = Color.Gold });
                    disparos.Add(new DisparoSciFi { X = naveX + 22, Y = naveY - 22, VX = 1.8f, VY = -14f, EsJugador = true, EsObusCanon = true, Danio = danioObus / 2, Radio = 8, Vida = 70, Color = Color.Gold });
                }
            }
        }

        private void ProcesarLaserAntimateria()
        {
            TocarSonidoRapido("laser_continuo", 65);
            float anchoHaz = 15 + nivelArma * 5;
            int danioLaser = 7 + nivelArma * 3;

            for (int i = enemigos.Count - 1; i >= 0; i--)
            {
                var en = enemigos[i];
                if (en.Y < naveY && Math.Abs(en.X - naveX) <= en.Radio + anchoHaz / 2f)
                {
                    en.Salud -= danioLaser;
                    en.FlashImpacto = 2;
                    if (tick % 2 == 0)
                        GenerarParticulas(naveX, en.Y + en.Radio, Color.Magenta, 5, 5.5f);

                    if (en.Salud <= 0) DestruirEnemigo(i);
                }
            }
        }

        private void DetonarSupernova()
        {
            TocarSonidoRapido("supernova");
            cargaSupernova = 0;
            screenShake = 18;
            ondas.Add(new OndaChoque { X = naveX, Y = naveY - 120, RadioMax = 680, Vida = 24, MaxVida = 24, Color = Color.Cyan });
            ondas.Add(new OndaChoque { X = naveX, Y = naveY - 120, RadioMax = 520, Vida = 20, MaxVida = 20, Color = Color.Magenta });
            AgregarTextoHolo("✦ ¡DESCARGA SUPERNOVA DE ANTIMATERIA! ✦", naveX - 135, naveY - 65, Color.Cyan);

            disparos.RemoveAll(b => !b.EsJugador);

            for (int i = enemigos.Count - 1; i >= 0; i--)
            {
                var en = enemigos[i];
                en.Salud -= 200 + nivelArma * 55;
                en.FlashImpacto = 8;
                GenerarParticulas(en.X, en.Y, Color.Cyan, 24, 7.5f);
                if (en.Salud <= 0) DestruirEnemigo(i);
            }
        }

        private void GenerarFlotaAlienigena()
        {
            oleada = 1 + puntaje / 1200;
            bool hayJefe = enemigos.Any(e => e.Tipo == "DREADNOUGHT");

            if (!hayJefe && puntaje > 0 && puntaje % 1200 < 120 && enemigos.Count < 3)
            {
                TocarSonidoRapido("alerta");
                enemigos.Add(new NaveAlien
                {
                    Tipo = "DREADNOUGHT",
                    Nombre = $"LEVIATÁN MADRE XENO MK-{oleada}",
                    X = this.ClientSize.Width / 2f,
                    Y = -85,
                    VX = 2.4f,
                    VY = 1.5f,
                    Salud = 720 + oleada * 260,
                    MaxSalud = 720 + oleada * 260,
                    Radio = 70,
                    CooldownDisparo = 46,
                    Puntos = 650
                });
                AgregarTextoHolo($"⚠ ¡ALERTA: LEVIATÁN MADRE XENO MK-{oleada}! ⚠", 265, 120, Color.OrangeRed);
                return;
            }

            int freq = Math.Max(24, 58 - oleada * 4);
            if (!hayJefe && tick % freq == 0 && enemigos.Count < 11)
            {
                int roll = rand.Next(100);
                float sx = rand.Next(70, this.ClientSize.Width - 70);

                if (roll < 45)
                {
                    enemigos.Add(new NaveAlien
                    {
                        Tipo = "CAZA",
                        Nombre = "Platillo Scout",
                        X = sx,
                        Y = -35,
                        VX = 0,
                        VY = 4.5f + oleada * 0.3f,
                        Fase = (float)(rand.NextDouble() * 6),
                        Salud = 44 + oleada * 12,
                        MaxSalud = 44 + oleada * 12,
                        Radio = 27,
                        CooldownDisparo = rand.Next(40, 85),
                        Puntos = 90
                    });
                }
                else if (roll < 75)
                {
                    enemigos.Add(new NaveAlien
                    {
                        Tipo = "INTERCEPTOR",
                        Nombre = "Bio-Avispa Xeno",
                        X = sx,
                        Y = -35,
                        VX = 0,
                        VY = 6.8f + oleada * 0.35f,
                        Salud = 34 + oleada * 8,
                        MaxSalud = 34 + oleada * 8,
                        Radio = 24,
                        CooldownDisparo = 999,
                        Puntos = 110
                    });
                }
                else
                {
                    enemigos.Add(new NaveAlien
                    {
                        Tipo = "CRUCERO",
                        Nombre = "Crucero Imperial",
                        X = sx,
                        Y = -50,
                        VX = 0,
                        VY = 2.6f,
                        Salud = 140 + oleada * 32,
                        MaxSalud = 140 + oleada * 32,
                        Radio = 38,
                        CooldownDisparo = 62,
                        Puntos = 210
                    });
                }
            }
        }

        private void ActualizarEnemigos()
        {
            for (int i = enemigos.Count - 1; i >= 0; i--)
            {
                var en = enemigos[i];
                if (en.FlashImpacto > 0) en.FlashImpacto--;

                if (en.Tipo == "DREADNOUGHT")
                {
                    if (en.Y < 145) en.Y += en.VY;
                    en.X += en.VX;
                    if (en.X < 130 || en.X > this.ClientSize.Width - 130) en.VX = -en.VX;

                    en.CooldownDisparo--;
                    if (en.CooldownDisparo <= 0)
                    {
                        en.CooldownDisparo = 45;
                        for (int ang = -2; ang <= 2; ang++)
                        {
                            disparos.Add(new DisparoSciFi
                            {
                                X = en.X + ang * 24,
                                Y = en.Y + 45,
                                VX = ang * 1.9f,
                                VY = 8.8f,
                                EsJugador = false,
                                Danio = 18,
                                Radio = 7,
                                Vida = 150,
                                Color = Color.SpringGreen
                            });
                        }
                    }
                }
                else if (en.Tipo == "INTERCEPTOR")
                {
                    float dx = naveX - en.X;
                    en.Y += en.VY;
                    en.X += Math.Sign(dx) * Math.Min(Math.Abs(dx) * 0.045f, 3.6f);
                }
                else if (en.Tipo == "CAZA")
                {
                    en.Y += en.VY;
                    en.X += (float)Math.Sin(tick * 0.07 + en.Fase) * 3.1f;
                }
                else
                {
                    en.Y += en.VY;
                }

                if (en.Tipo != "DREADNOUGHT" && en.Tipo != "INTERCEPTOR")
                {
                    en.CooldownDisparo--;
                    if (en.CooldownDisparo <= 0 && en.Y > 40 && en.Y < this.ClientSize.Height - 140)
                    {
                        en.CooldownDisparo = en.Tipo == "CRUCERO" ? 72 : 92;
                        float dx = naveX - en.X;
                        float dy = naveY - en.Y;
                        float len = Math.Max(1f, (float)Math.Sqrt(dx * dx + dy * dy));

                        if (en.Tipo == "CRUCERO")
                        {
                            for (int s = -1; s <= 1; s++)
                            {
                                disparos.Add(new DisparoSciFi
                                {
                                    X = en.X,
                                    Y = en.Y + 25,
                                    VX = (dx / len) * 7.8f + s * 2.1f,
                                    VY = (dy / len) * 7.8f,
                                    EsJugador = false,
                                    Danio = 15,
                                    Radio = 6,
                                    Vida = 150,
                                    Color = Color.MediumOrchid
                                });
                            }
                        }
                        else
                        {
                            disparos.Add(new DisparoSciFi
                            {
                                X = en.X,
                                Y = en.Y + 20,
                                VX = (dx / len) * 8.5f,
                                VY = (dy / len) * 8.5f,
                                EsJugador = false,
                                Danio = 12,
                                Radio = 5,
                                Vida = 150,
                                Color = Color.SpringGreen
                            });
                        }
                    }
                }

                if (Distancia(naveX, naveY, en.X, en.Y) < en.Radio + 24)
                {
                    RecibirDanio(en.Tipo == "DREADNOUGHT" ? 35 : 22);
                    if (en.Tipo != "DREADNOUGHT") { DestruirEnemigo(i); continue; }
                }

                if (en.Y > this.ClientSize.Height + 90) enemigos.RemoveAt(i);
            }
        }

        private void ActualizarDisparos()
        {
            for (int i = disparos.Count - 1; i >= 0; i--)
            {
                var b = disparos[i];

                if (b.EsMisilRastreador && enemigos.Count > 0)
                {
                    NaveAlien obj = enemigos[0];
                    float mejorDist = Distancia(b.X, b.Y, obj.X, obj.Y);
                    for (int eIdx = 1; eIdx < enemigos.Count; eIdx++)
                    {
                        float d = Distancia(b.X, b.Y, enemigos[eIdx].X, enemigos[eIdx].Y);
                        if (d < mejorDist) { mejorDist = d; obj = enemigos[eIdx]; }
                    }

                    float dx = obj.X - b.X;
                    float dy = obj.Y - b.Y;
                    float len = Math.Max(1f, (float)Math.Sqrt(dx * dx + dy * dy));
                    b.VX += (dx / len) * 0.9f;
                    b.VY += (dy / len) * 0.9f;
                    float vMag = (float)Math.Sqrt(b.VX * b.VX + b.VY * b.VY);
                    b.VX = (b.VX / vMag) * 14.5f;
                    b.VY = (b.VY / vMag) * 14.5f;
                    GenerarParticulas(b.X, b.Y + 8, Color.OrangeRed, 1, 1.5f);
                }

                b.X += b.VX;
                b.Y += b.VY;
                b.Vida--;

                if (b.Vida <= 0 || b.Y < -40 || b.Y > this.ClientSize.Height + 40 || b.X < -40 || b.X > this.ClientSize.Width + 40)
                {
                    disparos.RemoveAt(i);
                    continue;
                }

                if (b.EsJugador)
                {
                    for (int j = enemigos.Count - 1; j >= 0; j--)
                    {
                        var en = enemigos[j];
                        if (Distancia(b.X, b.Y, en.X, en.Y) < b.Radio + en.Radio)
                        {
                            en.Salud -= b.Danio;
                            en.FlashImpacto = 3;
                            GenerarParticulas(b.X, b.Y, b.Color, 8, 4f);

                            if (b.EsObusCanon)
                            {
                                TocarSonidoRapido("explosion", 50);
                                screenShake = 6;
                                ondas.Add(new OndaChoque { X = b.X, Y = b.Y, RadioMax = 120, Vida = 14, MaxVida = 14, Color = Color.OrangeRed });
                                for (int k = enemigos.Count - 1; k >= 0; k--)
                                {
                                    if (k != j && Distancia(b.X, b.Y, enemigos[k].X, enemigos[k].Y) < 120)
                                    {
                                        enemigos[k].Salud -= b.Danio / 2;
                                        enemigos[k].FlashImpacto = 3;
                                        if (enemigos[k].Salud <= 0)
                                        {
                                            if (k < j) j--;
                                            DestruirEnemigo(k);
                                        }
                                    }
                                }
                            }

                            disparos.RemoveAt(i);
                            if (j < enemigos.Count && enemigos[j].Salud <= 0)
                                DestruirEnemigo(j);
                            break;
                        }
                    }
                }
                else
                {
                    if (Distancia(b.X, b.Y, naveX, naveY) < b.Radio + 20)
                    {
                        RecibirDanio(b.Danio);
                        disparos.RemoveAt(i);
                    }
                }
            }
        }

        private void DestruirEnemigo(int idx)
        {
            TocarSonidoRapido("explosion", 45);
            var en = enemigos[idx];
            puntaje += en.Puntos;
            fragmentosGanados += en.Tipo == "DREADNOUGHT" ? 6 : 1;
            cargaSupernova = Math.Min(100, cargaSupernova + (en.Tipo == "DREADNOUGHT" ? 50 : 12));
            screenShake = en.Tipo == "DREADNOUGHT" ? 16 : 6;

            ondas.Add(new OndaChoque { X = en.X, Y = en.Y, RadioMax = en.Radio * 2.5f, Vida = 16, MaxVida = 16, Color = Color.Cyan });
            GenerarParticulas(en.X, en.Y, Color.Gold, en.Tipo == "DREADNOUGHT" ? 48 : 20, 6.8f);
            AgregarTextoHolo($"+{en.Puntos}", en.X - 15, en.Y, Color.Cyan);

            if (en.Tipo == "DREADNOUGHT" || en.Tipo == "CRUCERO" || rand.Next(100) < 28)
            {
                string[] drops = { "UP", "LASER", "CANON", "DRON", "ESCUDO" };
                string elegido = drops[rand.Next(drops.Length)];
                Color cDrop = elegido switch
                {
                    "UP" => Color.Gold,
                    "LASER" => Color.Magenta,
                    "CANON" => Color.OrangeRed,
                    "DRON" => Color.SpringGreen,
                    _ => Color.DeepSkyBlue
                };
                string etq = elegido switch
                {
                    "UP" => "POW",
                    "LASER" => "LSR",
                    "CANON" => "CAÑ",
                    "DRON" => "DRN",
                    _ => "SHD"
                };
                mejoras.Add(new ModuloMejora { Tipo = elegido, Etiqueta = etq, X = en.X, Y = en.Y, Color = cDrop });
            }

            enemigos.RemoveAt(idx);
        }

        private void ActualizarMejoras()
        {
            for (int i = mejoras.Count - 1; i >= 0; i--)
            {
                var mw = mejoras[i];
                mw.Y += 2.6f;

                if (Distancia(naveX, naveY, mw.X, mw.Y) < 42)
                {
                    TocarSonidoRapido("powerup");
                    GenerarParticulas(mw.X, mw.Y, mw.Color, 20, 5f);

                    if (mw.Tipo == "UP")
                    {
                        nivelArma = Math.Min(4, nivelArma + 1);
                        AgregarTextoHolo($"★ ¡BALAS Y MISILES NIVEL {nivelArma}! ★", naveX - 85, naveY - 42, Color.Gold);
                    }
                    else if (mw.Tipo == "LASER")
                    {
                        tipoArma = 2;
                        energiaLaser = 100;
                        nivelArma = Math.Min(4, nivelArma + 1);
                        AgregarTextoHolo($"★ ¡LÁSER ANTIMATERIA (NV.{nivelArma})! ★", naveX - 95, naveY - 42, Color.Magenta);
                    }
                    else if (mw.Tipo == "CANON")
                    {
                        tipoArma = 3;
                        nivelArma = Math.Min(4, nivelArma + 1);
                        AgregarTextoHolo($"★ ¡CAÑÓN DE OBUSES (NV.{nivelArma})! ★", naveX - 95, naveY - 42, Color.OrangeRed);
                    }
                    else if (mw.Tipo == "DRON")
                    {
                        dronesOrbitales = Math.Min(2, dronesOrbitales + 1);
                        AgregarTextoHolo($"★ ¡DRON ORBITAL ACTIVO ({dronesOrbitales}/2)! ★", naveX - 80, naveY - 42, Color.SpringGreen);
                    }
                    else
                    {
                        escudo = maxEscudo;
                        salud = Math.Min(maxSalud, salud + 60);
                        AgregarTextoHolo("★ ¡ESCUDO HEXAGONAL RESTAURADO! ★", naveX - 105, naveY - 42, Color.DeepSkyBlue);
                    }

                    mejoras.RemoveAt(i);
                    continue;
                }

                if (mw.Y > this.ClientSize.Height + 30) mejoras.RemoveAt(i);
            }
        }

        private void RecibirDanio(int danio)
        {
            TocarSonidoRapido("danio", 100);
            screenShake = 8;
            GenerarParticulas(naveX, naveY, Color.OrangeRed, 12, 5f);

            if (escudo > 0)
            {
                flashEscudo = 10;
                int abs = Math.Min(escudo, danio);
                escudo -= abs;
                danio -= abs;
            }
            salud -= danio;

            if (salud <= 0) FinalizarPartida(false);
        }

        private void ActualizarEfectos()
        {
            for (int i = ondas.Count - 1; i >= 0; i--)
            {
                ondas[i].Vida--;
                if (ondas[i].Vida <= 0) ondas.RemoveAt(i);
            }
            for (int i = particulas.Count - 1; i >= 0; i--)
            {
                particulas[i].X += particulas[i].VX;
                particulas[i].Y += particulas[i].VY;
                particulas[i].Vida--;
                if (particulas[i].Vida <= 0) particulas.RemoveAt(i);
            }
            for (int i = textosHolo.Count - 1; i >= 0; i--)
            {
                textosHolo[i].Y -= 0.9f;
                textosHolo[i].Vida--;
                if (textosHolo[i].Vida <= 0) textosHolo.RemoveAt(i);
            }
        }

        private void GenerarParticulas(float x, float y, Color c, int cant, float velMax)
        {
            if (particulas.Count > 160) cant = Math.Min(cant, 4);
            for (int i = 0; i < cant; i++)
            {
                double ang = rand.NextDouble() * Math.PI * 2;
                float v = (float)(rand.NextDouble() * velMax + 1);
                int vida = rand.Next(10, 22);
                particulas.Add(new ParticulaSciFi { X = x, Y = y, VX = (float)Math.Cos(ang) * v, VY = (float)Math.Sin(ang) * v, Tam = rand.Next(3, 8), Vida = vida, MaxVida = vida, Color = c });
            }
        }

        private void AgregarTextoHolo(string txt, float x, float y, Color c)
        {
            textosHolo.Add(new TextoHolo { Texto = txt, X = x, Y = y, Vida = 75, Color = c });
        }

        private static float Distancia(float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private void FinalizarPartida(bool retirado)
        {
            if (juegoTerminado) return;
            juegoTerminado = true;
            gameTimer.Stop();

            TocarSonidoRapido(retirado ? "victoria" : "gameover");

            int xpGanada = puntaje / 5 + oleada * 100;
            playerGlobal.FragmentosDatos += fragmentosGanados;
            playerGlobal.XP += xpGanada;
            GestorGuardado.Guardar(playerGlobal);

            Color cBorde = retirado ? Color.Cyan : Color.OrangeRed;
            pnlFin = new Panel
            {
                Size = new Size(540, 315),
                Location = new Point((this.ClientSize.Width - 540) / 2, (this.ClientSize.Height - 315) / 2),
                BackColor = Color.FromArgb(242, 8, 12, 26)
            };
            pnlFin.Paint += (s, e) =>
            {
                using var pen = new Pen(cBorde, 3);
                e.Graphics.DrawRectangle(pen, 1, 1, pnlFin.Width - 3, pnlFin.Height - 3);
            };

            Label lblT = new Label { Text = retirado ? "✦ RETORNO DE FLOTA ESTELAR ✦" : "⚠ CAZA DESTRUIDO EN ÓRBITA PROFUNDA ⚠", Font = new Font("Consolas", 13.5f, FontStyle.Bold), ForeColor = cBorde, Location = new Point(24, 24), Size = new Size(490, 32) };
            Label lblD = new Label
            {
                Text = $"PUNTAJE ESTELAR    : {puntaje} PTS (Oleada {oleada})\n" +
                       $"ARSENAL ALCANZADO  : Nivel {nivelArma} + {dronesOrbitales} Drones\n" +
                       $"FRAGMENTOS DATOS   : +{fragmentosGanados}\n" +
                       $"EXPERIENCIA GANADA : +{xpGanada} XP",
                Font = new Font("Consolas", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(28, 76),
                Size = new Size(480, 125)
            };

            Button btnCerrar = new Button
            {
                Text = "▶ VOLVER A LA CABINA [X]",
                Font = new Font("Consolas", 11.5f, FontStyle.Bold),
                Location = new Point(120, 232),
                Size = new Size(300, 48),
                BackColor = Color.FromArgb(0, 140, 175),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderColor = Color.Yellow;
            btnCerrar.FlatAppearance.BorderSize = 2;
            btnCerrar.Click += (s, e) => this.Close();

            pnlFin.Controls.AddRange(new Control[] { lblT, lblD, btnCerrar });
            this.Controls.Add(pnlFin);
            pnlFin.BringToFront();
            btnCerrar.Focus();

            bool xPrev = true;
            timerFinMando = new System.Windows.Forms.Timer { Interval = 30 };
            timerFinMando.Tick += (s, e) =>
            {
                var m = ControlMando.Leer();
                bool pulsa = (m.BotonSaltar || m.BotonDisparar || m.BotonSalir) && !xPrev;
                xPrev = m.BotonSaltar || m.BotonDisparar || m.BotonSalir;
                if (pulsa) this.Close();
            };
            timerFinMando.Start();
        }

        private void DibujarProyectilReal(Graphics g, DisparoSciFi b)
        {
            var st = g.Save();
            g.TranslateTransform(b.X, b.Y);

            float angulo = (float)(Math.Atan2(b.VY, b.VX) * 180.0 / Math.PI) + 90f;
            g.RotateTransform(angulo);

            if (b.EsMisilRastreador)
            {
                int llama = 8 + (tick % 6);
                g.FillPolygon(Brushes.OrangeRed, new[] { new PointF(-3, 12), new PointF(0, 12 + llama), new PointF(3, 12) });
                g.FillPolygon(Brushes.Yellow, new[] { new PointF(-1.5f, 12), new PointF(0, 12 + llama * 0.6f), new PointF(1.5f, 12) });

                g.FillPolygon(Brushes.Crimson, new[] { new PointF(-3, 4), new PointF(-8, 12), new PointF(-3, 11) });
                g.FillPolygon(Brushes.Crimson, new[] { new PointF(3, 4), new PointF(8, 12), new PointF(3, 11) });

                g.FillRectangle(Brushes.WhiteSmoke, -3, -8, 6, 20);
                g.DrawRectangle(Pens.DimGray, -3, -8, 6, 20);

                g.FillPolygon(Brushes.OrangeRed, new[] { new PointF(0, -16), new PointF(3.5f, -8), new PointF(-3.5f, -8) });
            }
            else if (b.EsObusCanon)
            {
                float esc = 0.85f + nivelArma * 0.12f;
                g.ScaleTransform(esc, esc);

                int llama = 9 + (tick % 5);
                g.FillPolygon(Brushes.OrangeRed, new[] { new PointF(-4, 10), new PointF(0, 10 + llama), new PointF(4, 10) });

                g.FillRectangle(Brushes.DimGray, -5, -4, 10, 14);
                g.FillRectangle(Brushes.Orange, -5, 2, 10, 3);
                g.DrawRectangle(penObusBordeCache, -5, -4, 10, 14);

                PointF[] ojiva = { new PointF(0, -18), new PointF(5, -4), new PointF(-5, -4) };
                g.FillPolygon(Brushes.Gold, ojiva);
                g.DrawPolygon(penObusOjivaCache, ojiva);
            }
            else if (b.EsJugador)
            {
                using (var brEstela = new LinearGradientBrush(new Rectangle(-3, 0, 6, 18), b.Color, Color.Transparent, 90f))
                    g.FillRectangle(brEstela, -2.5f, 2, 5, 16);

                PointF[] bala =
                {
                    new PointF(0, -14),
                    new PointF(3.2f, -4),
                    new PointF(2.8f, 6),
                    new PointF(-2.8f, 6),
                    new PointF(-3.2f, -4)
                };
                using (var brBala = new SolidBrush(b.Color))
                    g.FillPolygon(brBala, bala);

                g.FillPolygon(Brushes.White, new[] { new PointF(0, -11), new PointF(1.5f, -3), new PointF(1.2f, 4), new PointF(-1.2f, 4), new PointF(-1.5f, -3) });
            }
            else
            {
                PointF[] dardo =
                {
                    new PointF(0, -12),
                    new PointF(4, 2),
                    new PointF(0, 10),
                    new PointF(-4, 2)
                };
                using (var brDardo = new SolidBrush(b.Color))
                    g.FillPolygon(brDardo, dardo);
                g.FillEllipse(Brushes.White, -1.5f, -4, 3, 6);
            }

            g.Restore(st);
        }

        private void DibujarCazaEstelar(Graphics g, float x, float y)
        {
            var st = g.Save();
            g.TranslateTransform(x, y);

            int llama = 14 + (tick % 8);
            g.FillEllipse(brAuraMotorCache, -16, 20, 14, llama + 8);
            g.FillEllipse(brAuraMotorCache, 2, 20, 14, llama + 8);
            g.FillPolygon(Brushes.Cyan, new[] { new PointF(-12, 24), new PointF(-9, 24 + llama), new PointF(-6, 24) });
            g.FillPolygon(Brushes.Cyan, new[] { new PointF(6, 24), new PointF(9, 24 + llama), new PointF(12, 24) });

            using (var pathAlas = new GraphicsPath())
            {
                pathAlas.AddBezier(new PointF(0, -18), new PointF(20, -10), new PointF(36, 0), new PointF(42, 16));
                pathAlas.AddLine(new PointF(42, 16), new PointF(32, 24));
                pathAlas.AddBezier(new PointF(32, 24), new PointF(20, 14), new PointF(12, 14), new PointF(0, 16));
                pathAlas.AddBezier(new PointF(0, 16), new PointF(-12, 14), new PointF(-20, 14), new PointF(-32, 24));
                pathAlas.AddLine(new PointF(-32, 24), new PointF(-42, 16));
                pathAlas.AddBezier(new PointF(-42, 16), new PointF(-36, 0), new PointF(-20, -10), new PointF(0, -18));

                using var brAlas = new LinearGradientBrush(new Rectangle(-42, -18, 84, 44), Color.FromArgb(20, 160, 240), Color.FromArgb(10, 32, 80), 90f);
                g.FillPath(brAlas, pathAlas);
                g.DrawPath(penAlaCazaCache, pathAlas);
            }

            g.FillEllipse(Brushes.Gold, -39, -3, 5, 18);
            g.FillEllipse(Brushes.Gold, 34, -3, 5, 18);

            using (var brFuselaje = new LinearGradientBrush(new Rectangle(-10, -38, 20, 68), Color.White, Color.DeepSkyBlue, 0f))
            {
                g.FillEllipse(brFuselaje, -9, -36, 18, 64);
                g.DrawEllipse(penFuselajeCazaCache, -9, -36, 18, 64);
            }

            using (var brCabina = new LinearGradientBrush(new Rectangle(-5, -18, 10, 20), Color.Gold, Color.OrangeRed, 90f))
                g.FillEllipse(brCabina, -4, -18, 8, 18);

            g.Restore(st);
        }

        private void DibujarNaveAlienigena(Graphics g, NaveAlien en)
        {
            var st = g.Save();
            g.TranslateTransform(en.X, en.Y);
            bool hit = en.FlashImpacto > 0;

            if (en.Tipo == "CAZA")
            {
                g.FillEllipse(brAuraOvniCache, -36, -18, 72, 36);

                using (var brTractor = new LinearGradientBrush(new Rectangle(-12, 8, 24, 20), Color.FromArgb(90, Color.SpringGreen), Color.Transparent, 90f))
                    g.FillPolygon(brTractor, new[] { new PointF(-6, 8), new PointF(6, 8), new PointF(14, 26), new PointF(-14, 26) });

                using (var brDisco = new LinearGradientBrush(new Rectangle(-32, -12, 64, 24),
                    hit ? Color.White : Color.MediumSlateBlue,
                    hit ? Color.White : Color.FromArgb(25, 20, 55), 90f))
                {
                    g.FillEllipse(brDisco, -32, -11, 64, 22);
                    g.DrawEllipse(hit ? penOvniBordeHitCache : penOvniBordeCache, -32, -11, 64, 22);
                }

                for (int l = -2; l <= 2; l++)
                {
                    Brush brLuz = ((tick / 4 + l) % 2 == 0) ? Brushes.SpringGreen : Brushes.Gold;
                    g.FillEllipse(brLuz, l * 11 - 3, -2, 6, 6);
                }

                g.FillPie(brCupulaOvniCache, -16, -24, 32, 26, 180, 180);
                g.DrawArc(penOvniCupulaCache, -16, -24, 32, 26, 180, 180);

                g.FillEllipse(Brushes.LimeGreen, -6, -18, 12, 11);
                g.FillEllipse(Brushes.Black, -4, -14, 3, 4);
                g.FillEllipse(Brushes.Black, 1, -14, 3, 4);
            }
            else if (en.Tipo == "INTERCEPTOR")
            {
                float aleteo = (float)Math.Sin(tick * 0.45 + en.Fase) * 8f;

                using (var brAla = new SolidBrush(Color.FromArgb(125, hit ? Color.White : Color.MediumOrchid)))
                using (var penAla = new Pen(hit ? Color.White : Color.Magenta, 1.4f))
                {
                    g.FillEllipse(brAla, -34, -12 + aleteo * 0.5f, 26, 12);
                    g.DrawEllipse(penAla, -34, -12 + aleteo * 0.5f, 26, 12);
                    g.FillEllipse(brAla, 8, -12 + aleteo * 0.5f, 26, 12);
                    g.DrawEllipse(penAla, 8, -12 + aleteo * 0.5f, 26, 12);

                    g.FillEllipse(brAla, -28, -2 - aleteo * 0.4f, 20, 9);
                    g.FillEllipse(brAla, 8, -2 - aleteo * 0.4f, 20, 9);
                }

                using (var brBio = new LinearGradientBrush(new Rectangle(-12, -24, 24, 48),
                    hit ? Color.White : Color.Crimson,
                    hit ? Color.White : Color.FromArgb(65, 10, 35), 90f))
                {
                    g.FillEllipse(brBio, -8, -24, 16, 22);
                    g.FillEllipse(brBio, -11, -8, 22, 26);
                }

                using (var penPinza = new Pen(hit ? Color.White : Color.Gold, 2.5f))
                {
                    g.DrawArc(penPinza, -10, 10, 10, 16, 90, 150);
                    g.DrawArc(penPinza, 0, 10, 10, 16, -60, 150);
                }

                g.FillEllipse(Brushes.SpringGreen, -7, 8, 5, 7);
                g.FillEllipse(Brushes.SpringGreen, 2, 8, 5, 7);
            }
            else if (en.Tipo == "CRUCERO")
            {
                g.FillEllipse(Brushes.Cyan, -22, -32, 8, 14);
                g.FillEllipse(Brushes.Cyan, 14, -32, 8, 14);

                using (var pathNave = new GraphicsPath())
                {
                    pathNave.AddBezier(new PointF(0, 36), new PointF(22, 26), new PointF(44, 6), new PointF(46, -16));
                    pathNave.AddBezier(new PointF(46, -16), new PointF(28, -26), new PointF(14, -22), new PointF(0, -28));
                    pathNave.AddBezier(new PointF(0, -28), new PointF(-14, -22), new PointF(-28, -26), new PointF(-46, -16));
                    pathNave.AddBezier(new PointF(-46, -16), new PointF(-44, 6), new PointF(-22, 26), new PointF(0, 36));

                    using var brNave = new LinearGradientBrush(new Rectangle(-46, -28, 92, 64),
                        hit ? Color.White : Color.FromArgb(75, 85, 115),
                        hit ? Color.White : Color.FromArgb(24, 28, 42), 90f);
                    g.FillPath(brNave, pathNave);
                    g.DrawPath(hit ? penCruceroBordeHitCache : penCruceroBordeCache, pathNave);
                }

                g.FillEllipse(Brushes.OrangeRed, -10, -6, 20, 22);
                g.DrawEllipse(penCruceroCoreCache, -10, -6, 20, 22);
                g.FillEllipse(Brushes.Cyan, -4, 16, 8, 10);
            }
            else
            {
                using var penTent = new Pen(hit ? Color.White : Color.MediumOrchid, 4.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                for (int t = -3; t <= 3; t++)
                {
                    float wave = (float)Math.Sin(tick * 0.14 + t) * 14f;
                    g.DrawBezier(penTent,
                        new PointF(t * 18, 22),
                        new PointF(t * 20 + wave, 42),
                        new PointF(t * 16 - wave, 58),
                        new PointF(t * 18 + wave * 0.6f, 74));
                }

                using (var pathBoss = new GraphicsPath())
                {
                    pathBoss.AddBezier(new PointF(0, -54), new PointF(48, -54), new PointF(92, -32), new PointF(96, 4));
                    pathBoss.AddBezier(new PointF(96, 4), new PointF(76, 32), new PointF(36, 36), new PointF(0, 42));
                    pathBoss.AddBezier(new PointF(0, 42), new PointF(-36, 36), new PointF(-76, 32), new PointF(-96, 4));
                    pathBoss.AddBezier(new PointF(-96, 4), new PointF(-92, -32), new PointF(-48, -54), new PointF(0, -54));

                    using var brBoss = new LinearGradientBrush(new Rectangle(-96, -54, 192, 96),
                        hit ? Color.White : Color.FromArgb(110, 18, 95),
                        hit ? Color.White : Color.FromArgb(22, 8, 32), 90f);
                    g.FillPath(brBoss, pathBoss);
                    g.DrawPath(hit ? penBossBordeHitCache : penBossBordeCache, pathBoss);
                }

                g.FillEllipse(Brushes.Gold, -48, -8, 14, 10);
                g.FillEllipse(Brushes.Gold, 34, -8, 14, 10);
                g.FillEllipse(Brushes.Crimson, -30, 4, 10, 8);
                g.FillEllipse(Brushes.Crimson, 20, 4, 10, 8);

                int pulso = 16 + (tick % 7);
                g.FillEllipse(brAuraCoreBossCache, -pulso - 6, -16, (pulso + 6) * 2, (pulso + 6) * 2);
                g.FillEllipse(Brushes.SpringGreen, -pulso, -10, pulso * 2, pulso * 2);
                g.DrawEllipse(penBossCoreCache, -pulso, -10, pulso * 2, pulso * 2);
            }

            g.Restore(st);
        }

        private void DibujarHexagonoEscudo(Graphics g, float cx, float cy, float radio, int alpha)
        {
            PointF[] hex = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                double ang = (Math.PI / 3.0) * i + (tick * 0.02);
                hex[i] = new PointF(cx + (float)Math.Cos(ang) * radio, cy + (float)Math.Sin(ang) * radio);
            }
            using var penHex = new Pen(Color.FromArgb(alpha, Color.Cyan), 2f);
            g.DrawPolygon(penHex, hex);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (screenShake > 0)
                g.TranslateTransform(rand.Next(-4, 5), rand.Next(-4, 5));

            if (fondoEspacialCache != null)
                g.DrawImageUnscaled(fondoEspacialCache, 0, 0);

            for (int i = 0; i < planetas.Count; i++)
            {
                var pl = planetas[i];
                RectangleF rPl = new RectangleF(pl.X - pl.Radio, pl.Y - pl.Radio, pl.Radio * 2, pl.Radio * 2);
                using var brPl = new LinearGradientBrush(rPl, pl.C1, pl.C2, 45f);
                g.FillEllipse(brPl, rPl);
                if (pl.TieneAnillo)
                {
                    using var penAnillo = new Pen(pl.ColorAnillo, 4);
                    g.DrawEllipse(penAnillo, pl.X - pl.Radio * 1.65f, pl.Y - pl.Radio * 0.35f, pl.Radio * 3.3f, pl.Radio * 0.7f);
                }
            }

            for (int i = 0; i < estrellas.Count; i++)
            {
                var st = estrellas[i];
                using var brSt = new SolidBrush(st.Color);
                g.FillEllipse(brSt, st.X, st.Y, st.Tam, st.Tam);
            }

            for (int i = 0; i < fugaces.Count; i++)
            {
                var fg = fugaces[i];
                g.DrawLine(penFugazCache, fg.X, fg.Y, fg.X - fg.VX * 2.5f, fg.Y - fg.VY * 2.5f);
            }

            if (estelaIzq.Count > 2)
            {
                g.DrawLines(penEstelaCache, estelaIzq.ToArray());
                g.DrawLines(penEstelaCache, estelaDer.ToArray());
            }

            if (disparandoLaser)
            {
                float anchoHaz = 15 + nivelArma * 5;
                g.FillRectangle(brAuraLaserCache, naveX - anchoHaz / 2f, 54, anchoHaz, naveY - 90);
                g.FillRectangle(Brushes.White, naveX - anchoHaz / 5f, 54, anchoHaz / 2.5f, naveY - 90);
            }

            DibujarCazaEstelar(g, naveX, naveY);

            if (escudo > 0)
            {
                int alphaEsc = flashEscudo > 0 ? 200 : 65;
                DibujarHexagonoEscudo(g, naveX, naveY, 44, alphaEsc);
            }

            for (int d = 0; d < dronesOrbitales; d++)
            {
                double ang = tick * 0.09 + d * Math.PI;
                float dx = naveX + (float)Math.Cos(ang) * 52;
                float dy = naveY + (float)Math.Sin(ang) * 52;
                g.FillEllipse(Brushes.SpringGreen, dx - 7, dy - 7, 14, 14);
                g.DrawEllipse(Pens.White, dx - 7, dy - 7, 14, 14);
            }

            for (int i = 0; i < enemigos.Count; i++)
            {
                var en = enemigos[i];
                DibujarNaveAlienigena(g, en);
                if (en.Tipo == "DREADNOUGHT")
                {
                    g.DrawString(en.Nombre, fontBoss, Brushes.Gold, en.X - 110, en.Y - 82);
                    g.FillRectangle(Brushes.Black, en.X - 75, en.Y - 64, 150, 8);
                    g.FillRectangle(Brushes.SpringGreen, en.X - 74, en.Y - 63, (int)(148 * (en.Salud / (float)en.MaxSalud)), 6);
                }
            }

            for (int i = 0; i < mejoras.Count; i++)
            {
                var mw = mejoras[i];
                using var brAura = new SolidBrush(Color.FromArgb(100, mw.Color));
                using var brCore = new SolidBrush(mw.Color);
                g.FillEllipse(brAura, mw.X - 24, mw.Y - 24, 48, 48);
                g.FillEllipse(brCore, mw.X - 15, mw.Y - 15, 30, 30);
                g.DrawEllipse(penDropBordeCache, mw.X - 15, mw.Y - 15, 30, 30);
                g.DrawString(mw.Etiqueta, fontDrop, Brushes.Black, mw.X - 12, mw.Y - 7);
            }

            for (int i = 0; i < disparos.Count; i++)
                DibujarProyectilReal(g, disparos[i]);

            for (int i = 0; i < ondas.Count; i++)
            {
                var o = ondas[i];
                float prog = 1f - (o.Vida / (float)o.MaxVida);
                float r = o.RadioMax * prog;
                int alpha = Math.Clamp((int)(210 * (1f - prog)), 10, 210);
                using var penEx = new Pen(Color.FromArgb(alpha, o.Color), 4);
                g.DrawEllipse(penEx, o.X - r, o.Y - r, r * 2, r * 2);
            }

            for (int i = 0; i < particulas.Count; i++)
            {
                var pt = particulas[i];
                int alpha = Math.Clamp((int)(255 * (pt.Vida / (float)pt.MaxVida)), 20, 255);
                using var brPt = new SolidBrush(Color.FromArgb(alpha, pt.Color));
                g.FillEllipse(brPt, pt.X, pt.Y, pt.Tam, pt.Tam);
            }

            for (int i = 0; i < textosHolo.Count; i++)
            {
                var tf = textosHolo[i];
                using var brTf = new SolidBrush(tf.Color);
                g.DrawString(tf.Texto, fontHolo, brTf, tf.X, tf.Y);
            }

            g.FillRectangle(brHudBgCache, 0, 0, this.Width, 54);
            g.DrawLine(penHudLineCache, 0, 54, this.Width, 54);

            g.DrawString($"CASCO: {Math.Max(0, salud)}/{maxSalud}  |  ESCUDO: {escudo}%  |  PUNTOS: {puntaje}  |  OLEADA: {oleada}  |  SUPERNOVA [E]: {cargaSupernova}%",
                fontHudTop, Brushes.Cyan, 14, 8);

            string armaTxt = tipoArma == 1 ? $"[1] BALAS PLASMA NV.{nivelArma}"
                : (tipoArma == 2 ? $"[2] LÁSER ANTIMATERIA NV.{nivelArma} ({energiaLaser}%)"
                : $"[3] CAÑÓN OBÚS NV.{nivelArma}");

            g.DrawString($"ARSENAL: {armaTxt}  |  DRONES: {dronesOrbitales}/2  |  VOZ: [L1/V]  |  SALIR: [OPTIONS]",
                fontHudBot, Brushes.Gold, 14, 30);
        }
    }
}