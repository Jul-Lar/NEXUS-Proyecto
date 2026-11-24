using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace NexusForms
{
    public static class SpriteFactory
    {
        private static Dictionary<string, Bitmap> cache = new Dictionary<string, Bitmap>();

        public static Image? CargarPngSiExiste(string nombreArchivo)
        {
            string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nombreArchivo);
            if (File.Exists(ruta))
            {
                try
                {
                    using var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read);
                    return Image.FromStream(fs);
                }
                catch { }
            }
            return null;
        }

        // Genera operativos Sci-Fi modernos con 4 frames fluidos, aura de energía, propulsores animados y visor brillante
        public static Bitmap ObtenerSpriteOperativo(string tipo, Color c1, Color c2, int framePaso)
        {
            int f = Math.Abs(framePaso) % 4;
            string key = $"nextgen_op_{tipo}_{c1.ToArgb()}_{f}";
            if (cache.TryGetValue(key, out var bmp)) return bmp;

            bmp = new Bitmap(104, 104);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                int bobY = (f == 1 || f == 3) ? -3 : (f == 2 ? -1 : 0);
                int pIzqX = f switch { 1 => 26, 2 => 34, 3 => 44, _ => 32 };
                int pDerX = f switch { 1 => 50, 2 => 42, 3 => 32, _ => 46 };
                int llamaJet = 10 + (f * 5);

                // Aura holográfica suave alrededor del traje
                using (var brAura = new SolidBrush(Color.FromArgb(35, c2)))
                    g.FillEllipse(brAura, 14, 10 + bobY, 76, 84);

                // Sombra dinámica
                using (var brSombra = new SolidBrush(Color.FromArgb(145, 0, 0, 0)))
                    g.FillEllipse(brSombra, 22, 88, 60, 11);

                // Mochila propulsora con fuego de plasma animado
                Rectangle rectPack = new Rectangle(16, 36 + bobY, 16, 30);
                using (var brPack = new LinearGradientBrush(rectPack, Color.FromArgb(80, 95, 115), Color.FromArgb(22, 28, 38), 45f))
                    RellenarRectanguloRedondeado(g, brPack, rectPack, 4);
                using (var brLlama = new LinearGradientBrush(new Rectangle(16, 65 + bobY, 16, llamaJet + 4), Color.White, c2, 90f))
                    g.FillPolygon(brLlama, new[] { new Point(18, 66 + bobY), new Point(30, 66 + bobY), new Point(24, 66 + bobY + llamaJet) });

                // Piernas articuladas con brillo neón
                using (var brPierna = new LinearGradientBrush(new Rectangle(24, 62, 42, 28), Color.FromArgb(52, 62, 78), Color.FromArgb(16, 20, 28), 90f))
                {
                    RellenarRectanguloRedondeado(g, brPierna, new Rectangle(pIzqX, 64, 12, 22), 4);
                    RellenarRectanguloRedondeado(g, brPierna, new Rectangle(pDerX, 64, 12, 22), 4);
                }
                using (var brRodilla = new SolidBrush(c2))
                {
                    g.FillEllipse(brRodilla, pIzqX + 2, 70, 8, 6);
                    g.FillEllipse(brRodilla, pDerX + 2, 70, 8, 6);
                }

                // Torso blindado futurista
                Rectangle rectTorso = (tipo == "Tanque")
                    ? new Rectangle(24, 30 + bobY, 46, 36)
                    : new Rectangle(29, 33 + bobY, 36, 34);

                using (var brTorso = new LinearGradientBrush(rectTorso, ControlPaint.Light(c1, 0.28f), ControlPaint.Dark(c1, 0.45f), 45f))
                    RellenarRectanguloRedondeado(g, brTorso, rectTorso, 8);
                DibujarRectanguloRedondeado(g, new Pen(c2, 1.5f), rectTorso, 8);

                // Reactor en el pecho pulsante
                int pulsoReactor = (f % 2 == 0) ? 10 : 12;
                using (var brCoreGlow = new SolidBrush(Color.FromArgb(120, c2)))
                    g.FillEllipse(brCoreGlow, rectTorso.X + rectTorso.Width / 2 - 9, rectTorso.Y + 7, 18, 18);
                using (var brCore = new SolidBrush(Color.White))
                    g.FillEllipse(brCore, rectTorso.X + rectTorso.Width / 2 - pulsoReactor / 2, rectTorso.Y + 11, pulsoReactor, pulsoReactor);

                // Casco aerodinámico con cresta energética
                Rectangle rectCasco = new Rectangle(31, 8 + bobY, 34, 28);
                using (var brCasco = new LinearGradientBrush(rectCasco, ControlPaint.Light(c1, 0.35f), ControlPaint.Dark(c1, 0.4f), 60f))
                    g.FillEllipse(brCasco, rectCasco);
                g.DrawEllipse(new Pen(c2, 1.5f), rectCasco);

                // Visor luminoso estilo cyberpunk
                Rectangle rectVisor = new Rectangle(44, 16 + bobY, 20, 10);
                using (var brVisor = new LinearGradientBrush(rectVisor, Color.White, c2, 0f))
                    RellenarRectanguloRedondeado(g, brVisor, rectVisor, 5);

                // Cañón / Rifle de plasma avanzado
                int retrocesoArma = (f == 2) ? -2 : 0;
                Rectangle rectArma = new Rectangle(38 + retrocesoArma, 46 + bobY, 48, 12);
                using (var brArma = new LinearGradientBrush(rectArma, Color.FromArgb(100, 118, 140), Color.FromArgb(24, 30, 42), 90f))
                    RellenarRectanguloRedondeado(g, brArma, rectArma, 4);
                using (var brCanonNeon = new SolidBrush(c2))
                    g.FillRectangle(brCanonNeon, 46 + retrocesoArma, 50 + bobY, 32, 4);
                g.FillEllipse(Brushes.White, 82 + retrocesoArma, 48 + bobY, 7, 8);
            }

            cache[key] = bmp;
            return bmp;
        }

        // Genera Villanos Sci-Fi con 4 frames de animación continua (turbinas giratorias, núcleos pulsantes, brazos articulados y rayos)
        public static Bitmap ObtenerSpriteVillano(string tipo, int framePaso, bool flashImpacto)
        {
            int f = Math.Abs(framePaso) % 4;
            string key = $"nextgen_villano_{tipo}_{f}_{flashImpacto}";
            if (cache.TryGetValue(key, out var bmp)) return bmp;

            bmp = new Bitmap(92, 92);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                Color cPrincipal = flashImpacto ? Color.White : Color.FromArgb(215, 28, 52);
                Color cOscuro = flashImpacto ? Color.LightGray : Color.FromArgb(35, 14, 28);
                Color cMetal = flashImpacto ? Color.White : Color.FromArgb(82, 90, 108);
                Color cEnergia = flashImpacto ? Color.Cyan : Color.OrangeRed;

                // Aura de amenaza roja
                using (var brAura = new SolidBrush(Color.FromArgb(38, Color.Crimson)))
                    g.FillEllipse(brAura, 10, 10, 72, 72);

                if (tipo == "Dron")
                {
                    int flotaY = f switch { 1 => -3, 2 => 0, 3 => 3, _ => 0 };
                    int anchoHelice = (f % 2 == 0) ? 68 : 34;

                    // Turbinas superiores girando dinámicamente
                    using (var brHelice = new SolidBrush(Color.FromArgb(190, Color.Cyan)))
                        g.FillEllipse(brHelice, 46 - anchoHelice / 2, 10 + flotaY, anchoHelice, 7);
                    g.FillRectangle(Brushes.Silver, 42, 12 + flotaY, 8, 10);

                    // Alas articuladas que baten
                    int alaOffset = (f == 1 || f == 3) ? -4 : 2;
                    Point[] alaIzq = { new Point(24, 32 + flotaY), new Point(4, 42 + flotaY + alaOffset), new Point(18, 52 + flotaY) };
                    Point[] alaDer = { new Point(68, 32 + flotaY), new Point(88, 42 + flotaY + alaOffset), new Point(74, 52 + flotaY) };
                    using (var brAla = new SolidBrush(cMetal))
                    {
                        g.FillPolygon(brAla, alaIzq);
                        g.FillPolygon(brAla, alaDer);
                    }
                    g.DrawPolygon(new Pen(Color.Gold, 1.5f), alaIzq);
                    g.DrawPolygon(new Pen(Color.Gold, 1.5f), alaDer);

                    // Chasis esférico blindado
                    Rectangle cuerpo = new Rectangle(20, 22 + flotaY, 52, 42);
                    using (var brCuerpo = new LinearGradientBrush(cuerpo, cPrincipal, cOscuro, 45f))
                        g.FillEllipse(brCuerpo, cuerpo);
                    g.DrawEllipse(new Pen(Color.Gold, 2), cuerpo);

                    // Ojo láser con escáner móvil
                    int pupilaX = 38 + (f - 1) * 3;
                    g.FillEllipse(Brushes.Black, 28, 32 + flotaY, 36, 20);
                    using (var brOjo = new SolidBrush(cEnergia))
                        g.FillEllipse(brOjo, 32, 35 + flotaY, 28, 14);
                    g.FillEllipse(Brushes.Yellow, pupilaX, 38 + flotaY, 9, 8);

                    // Garras inferiores electrificadas
                    using var penGarra = new Pen(Color.Cyan, 2.5f);
                    g.DrawLines(penGarra, new[] { new Point(30, 62 + flotaY), new Point(22, 74 + flotaY), new Point(30, 82 + flotaY) });
                    g.DrawLines(penGarra, new[] { new Point(62, 62 + flotaY), new Point(70, 74 + flotaY), new Point(62, 82 + flotaY) });
                }
                else if (tipo == "Torreta")
                {
                    int retroceso = (f == 2) ? 3 : 0;

                    using (var brSombra = new SolidBrush(Color.FromArgb(145, 0, 0, 0)))
                        g.FillEllipse(brSombra, 10, 78, 72, 10);

                    // Patas hidráulicas reforzadas
                    using (var brPata = new SolidBrush(cMetal))
                    {
                        g.FillPolygon(brPata, new[] { new Point(26, 56), new Point(10, 82), new Point(24, 82), new Point(36, 60) });
                        g.FillPolygon(brPata, new[] { new Point(66, 56), new Point(82, 82), new Point(68, 82), new Point(56, 60) });
                        g.FillRectangle(brPata, 40, 56, 12, 26);
                    }

                    // Cúpula blindada con placas doradas
                    Rectangle cupula = new Rectangle(22 + retroceso, 22, 50, 40);
                    using (var brCup = new LinearGradientBrush(cupula, cPrincipal, cOscuro, 90f))
                        RellenarRectanguloRedondeado(g, brCup, cupula, 14);
                    DibujarRectanguloRedondeado(g, new Pen(Color.Gold, 2), cupula, 14);

                    // Doble cañón pesado con carga de energía
                    g.FillRectangle(Brushes.Black, 4 + retroceso, 31, 26, 8);
                    g.FillRectangle(Brushes.Cyan, 4 + retroceso, 33, 10, 4);
                    g.FillRectangle(Brushes.Black, 6 + retroceso, 44, 24, 8);
                    g.FillRectangle(Brushes.Cyan, 6 + retroceso, 46, 10, 4);

                    // Núcleo óptico pulsante
                    int tamOjo = (f % 2 == 0) ? 14 : 11;
                    g.FillEllipse(Brushes.Yellow, 42 + retroceso - tamOjo / 2, 40 - tamOjo / 2, tamOjo, tamOjo);
                    g.FillEllipse(Brushes.Red, 39 + retroceso, 37, 6, 6);
                }
                else // MECHA EXTERMINADOR ARTICULADO
                {
                    int paso = f switch { 1 => -4, 2 => 0, 3 => 4, _ => 0 };
                    int torsoY = (f % 2 == 1) ? -2 : 0;

                    using (var brSombra = new SolidBrush(Color.FromArgb(145, 0, 0, 0)))
                        g.FillEllipse(brSombra, 14, 80, 64, 10);

                    // Piernas robóticas con pistones
                    using (var brPierna = new SolidBrush(cMetal))
                    {
                        g.FillRectangle(brPierna, 28 + paso, 58, 13, 22);
                        g.FillRectangle(brPierna, 50 - paso, 58, 13, 22);
                    }
                    g.FillRectangle(Brushes.Gold, 26 + paso, 77, 17, 7);
                    g.FillRectangle(Brushes.Gold, 48 - paso, 77, 17, 7);

                    // Torso blindado
                    Rectangle torso = new Rectangle(24, 26 + torsoY, 44, 36);
                    using (var brTorso = new LinearGradientBrush(torso, cPrincipal, cOscuro, 45f))
                        RellenarRectanguloRedondeado(g, brTorso, torso, 8);
                    DibujarRectanguloRedondeado(g, new Pen(Color.Gold, 1.5f), torso, 8);

                    // Hombreras con energía
                    g.FillPolygon(Brushes.Gold, new[] { new Point(56, 24 + torsoY), new Point(78, 16 + torsoY), new Point(72, 36 + torsoY), new Point(56, 36 + torsoY) });

                    // Casco con visor láser animado
                    Rectangle cabeza = new Rectangle(27, 6 + torsoY, 34, 24);
                    using (var brCab = new LinearGradientBrush(cabeza, cMetal, Color.Black, 90f))
                        RellenarRectanguloRedondeado(g, brCab, cabeza, 7);
                    g.FillRectangle(Brushes.Cyan, 29, 14 + torsoY, 18, 6);
                    g.FillRectangle(Brushes.White, 31, 15 + torsoY, 8, 4);

                    // Reactor en pecho
                    g.FillEllipse(Brushes.OrangeRed, 35, 36 + torsoY, 14, 14);
                    g.FillEllipse(Brushes.Yellow, 39, 40 + torsoY, 6, 6);

                    // Cañón de brazo articulado
                    g.FillRectangle(Brushes.Black, 4, 40 + torsoY, 38, 12);
                    g.FillRectangle(Brushes.OrangeRed, 4, 43 + torsoY, 14, 6);
                }
            }

            cache[key] = bmp;
            return bmp;
        }

        public static Bitmap ObtenerSpriteAvionJugador(int frameHelice)
        {
            string key = $"modern_avion_jug_{frameHelice % 3}";
            if (cache.TryGetValue(key, out var bmp)) return bmp;

            bmp = new Bitmap(104, 104);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                int largoLlama = 16 + (frameHelice % 3) * 5;
                using (var brPlasma = new LinearGradientBrush(new Rectangle(36, 82, 32, largoLlama + 4), Color.Cyan, Color.Transparent, 90f))
                {
                    g.FillPolygon(brPlasma, new[] { new Point(38, 82), new Point(46, 82), new Point(42, 82 + largoLlama) });
                    g.FillPolygon(brPlasma, new[] { new Point(58, 82), new Point(66, 82), new Point(62, 82 + largoLlama) });
                }

                Point[] alaIzq = { new Point(46, 30), new Point(4, 64), new Point(10, 74), new Point(46, 64) };
                Point[] alaDer = { new Point(58, 30), new Point(100, 64), new Point(94, 74), new Point(58, 64) };
                using (var brAla = new LinearGradientBrush(new Rectangle(0, 30, 104, 45), Color.FromArgb(30, 110, 220), Color.FromArgb(12, 42, 95), 90f))
                {
                    g.FillPolygon(brAla, alaIzq);
                    g.FillPolygon(brAla, alaDer);
                }
                using (var penBordeAla = new Pen(Color.Cyan, 1.5f))
                {
                    g.DrawPolygon(penBordeAla, alaIzq);
                    g.DrawPolygon(penBordeAla, alaDer);
                }

                Rectangle rectFuselaje = new Rectangle(42, 8, 20, 78);
                using (var brFuselaje = new LinearGradientBrush(rectFuselaje, Color.FromArgb(140, 195, 255), Color.FromArgb(18, 45, 90), 0f))
                    g.FillEllipse(brFuselaje, rectFuselaje);
                g.DrawEllipse(Pens.Black, rectFuselaje);

                Rectangle rectCabina = new Rectangle(46, 30, 12, 24);
                using (var brCabina = new LinearGradientBrush(rectCabina, Color.White, Color.DeepSkyBlue, 45f))
                    g.FillEllipse(brCabina, rectCabina);
            }

            cache[key] = bmp;
            return bmp;
        }

        public static Bitmap ObtenerSpriteAvionEnemigo(int frameHelice)
        {
            string key = $"modern_avion_ene_{frameHelice % 2}";
            if (cache.TryGetValue(key, out var bmp)) return bmp;

            bmp = new Bitmap(80, 80);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                Point[] alas = { new Point(40, 56), new Point(4, 28), new Point(10, 18), new Point(40, 30), new Point(70, 18), new Point(76, 28) };
                using (var brAlas = new LinearGradientBrush(new Rectangle(0, 18, 80, 40), Color.FromArgb(95, 105, 115), Color.FromArgb(35, 40, 48), 90f))
                    g.FillPolygon(brAlas, alas);
                g.DrawPolygon(Pens.Crimson, alas);

                Rectangle rectF = new Rectangle(32, 8, 16, 62);
                using (var brF = new LinearGradientBrush(rectF, Color.Silver, Color.FromArgb(40, 45, 55), 0f))
                    g.FillEllipse(brF, rectF);
                g.FillEllipse(Brushes.Crimson, 36, 28, 8, 18);
            }

            cache[key] = bmp;
            return bmp;
        }

        public static Bitmap ObtenerSpriteJefeAereo()
        {
            string key = "modern_jefe_aereo";
            if (cache.TryGetValue(key, out var bmp)) return bmp;

            bmp = new Bitmap(260, 140);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                Point[] alaGrande = { new Point(130, 105), new Point(6, 52), new Point(20, 24), new Point(130, 42), new Point(240, 24), new Point(254, 52) };
                using (var brAla = new LinearGradientBrush(new Rectangle(0, 20, 260, 90), Color.FromArgb(75, 82, 95), Color.FromArgb(24, 28, 36), 90f))
                    g.FillPolygon(brAla, alaGrande);
                g.DrawPolygon(new Pen(Color.Gold, 2), alaGrande);

                Rectangle rectF = new Rectangle(108, 12, 44, 116);
                using (var brF = new LinearGradientBrush(rectF, Color.Crimson, Color.FromArgb(65, 10, 18), 0f))
                    g.FillEllipse(brF, rectF);
                g.FillEllipse(Brushes.Cyan, 120, 64, 20, 28);
            }

            cache[key] = bmp;
            return bmp;
        }

        private static void RellenarRectanguloRedondeado(Graphics g, Brush brush, Rectangle bounds, int radius)
        {
            using GraphicsPath path = CrearRutaRedondeada(bounds, radius);
            g.FillPath(brush, path);
        }

        private static void DibujarRectanguloRedondeado(Graphics g, Pen pen, Rectangle bounds, int radius)
        {
            using GraphicsPath path = CrearRutaRedondeada(bounds, radius);
            g.DrawPath(pen, path);
        }

        private static GraphicsPath CrearRutaRedondeada(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}