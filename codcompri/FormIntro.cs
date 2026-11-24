using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace NexusForms
{
    public class FormIntro : Form
    {
        private System.Windows.Forms.Timer timerIntro;
        private Image? imagenLogo;

        private int fase = 0;
        private int tickFase = 0;
        private float opacidad = 0f;
        private float posicionHazLuz = -0.3f;
        private float anguloHexagono = 0f;
        private bool vozReproducida = false;

        public FormIntro()
        {
            this.Text = "NEXUS // OPERACIÓN DIMENSIONAL";
            this.Size = new Size(1024, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;

            CargarYLimpiarLogo();

            timerIntro = new System.Windows.Forms.Timer { Interval = 16 };
            timerIntro.Tick += BucleAnimacionIntro;
            timerIntro.Start();
        }

        private void CargarYLimpiarLogo()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] rutasLogo =
            {
                Path.Combine(baseDir, "Assets", "logo_nexus.png"),
                Path.Combine(baseDir, "Assets", "logo_nexus.jpg"),
                Path.Combine(baseDir, "Assets", "logo_nexus.jpeg"),
                Path.Combine(baseDir, "logo_nexus.png"),
                Path.Combine(baseDir, "logo_nexus.jpg")
            };

            foreach (var r in rutasLogo)
            {
                if (File.Exists(r))
                {
                    using var fs = new FileStream(r, FileMode.Open, FileAccess.Read);
                    using var original = new Bitmap(fs);
                    imagenLogo = EliminarCuadrosTransparenciaFalsa(original);
                    break;
                }
            }
        }

        private Bitmap EliminarCuadrosTransparenciaFalsa(Bitmap origen)
        {
            Bitmap limpio = new Bitmap(origen.Width, origen.Height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < origen.Height; y++)
            {
                for (int x = 0; x < origen.Width; x++)
                {
                    Color c = origen.GetPixel(x, y);
                    int max = Math.Max(c.R, Math.Max(c.G, c.B));
                    int min = Math.Min(c.R, Math.Min(c.G, c.B));
                    int saturacion = max - min;

                    bool esCuadroFondo = (saturacion < 24 && max < 115);
                    limpio.SetPixel(x, y, esCuadroFondo ? Color.Transparent : c);
                }
            }
            return limpio;
        }

        private void ReproducirVozBienvenida()
        {
            if (vozReproducida) return;
            vozReproducida = true;

            try
            {
                string rutaWav = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "bienvenido.wav");
                if (File.Exists(rutaWav))
                {
                    using SoundPlayer player = new SoundPlayer(rutaWav);
                    player.Play();
                    return;
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-NoProfile -WindowStyle Hidden -Command \"Add-Type -AssemblyName System.Speech; $s = New-Object System.Speech.Synthesis.SpeechSynthesizer; $s.Rate = -1; $s.Volume = 100; $s.Speak('Bienvenido a Nexus')\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch
            {
            }
        }

        private void BucleAnimacionIntro(object? sender, EventArgs e)
        {
            tickFase++;

            switch (fase)
            {
                case 0:
                    if (tickFase >= 20)
                    {
                        fase = 1;
                        tickFase = 0;
                        ReproducirVozBienvenida();
                    }
                    break;

                case 1:
                    opacidad = Math.Min(1f, opacidad + 0.025f);
                    posicionHazLuz += 0.022f;
                    if (tickFase >= 135) { fase = 2; tickFase = 0; }
                    break;

                case 2:
                    opacidad = Math.Max(0f, opacidad - 0.035f);
                    if (opacidad <= 0f) { fase = 3; tickFase = 0; }
                    break;

                case 3:
                    opacidad = Math.Min(1f, opacidad + 0.03f);
                    if (tickFase >= 95) { fase = 4; tickFase = 0; }
                    break;

                case 4:
                    opacidad = Math.Max(0f, opacidad - 0.04f);
                    if (opacidad <= 0f) { fase = 5; tickFase = 0; }
                    break;

                case 5:
                    opacidad = Math.Min(1f, opacidad + 0.035f);
                    anguloHexagono = (anguloHexagono + 5f) % 360f;
                    if (tickFase >= 105) { fase = 6; tickFase = 0; }
                    break;

                case 6:
                    timerIntro.Stop();
                    this.Hide();
                    using (FormInicio menuPrincipal = new FormInicio())
                    {
                        menuPrincipal.ShowDialog();
                    }
                    this.Close();
                    break;
            }

            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;

            if (fase == 1 || fase == 2)
            {
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(w / 2 - 380, h / 2 - 220, 760, 440);
                    using PathGradientBrush pgb = new PathGradientBrush(path)
                    {
                        CenterColor = Color.FromArgb((int)(55 * opacidad), 25, 45, 75),
                        SurroundColors = new[] { Color.Black }
                    };
                    g.FillRectangle(pgb, 0, 0, w, h);
                }

                if (imagenLogo != null)
                {
                    int logoW = 540;
                    int logoH = 260;
                    Rectangle destRect = new Rectangle((w - logoW) / 2, (h - logoH) / 2 - 10, logoW, logoH);
                    DibujarImagenConOpacidad(g, imagenLogo, destRect, opacidad);

                    if (posicionHazLuz >= 0f && posicionHazLuz <= 1.2f)
                    {
                        float luzX = destRect.X + (destRect.Width * posicionHazLuz);
                        RectangleF rectHaz = new RectangleF(luzX - 45, destRect.Y - 20, 90, destRect.Height + 40);
                        using LinearGradientBrush brHaz = new LinearGradientBrush(
                            rectHaz,
                            Color.Transparent,
                            Color.FromArgb((int)(140 * opacidad), 255, 225, 150),
                            0f);
                        brHaz.SetSigmaBellShape(0.5f);
                        g.FillRectangle(brHaz, rectHaz);
                    }
                }
            }
            else if (fase == 3 || fase == 4)
            {
                int alpha = (int)(255 * opacidad);
                using Font fEstudio = new Font("Consolas", 28f, FontStyle.Bold);
                using Font fSub = new Font("Consolas", 11f, FontStyle.Regular);
                using SolidBrush brOro = new SolidBrush(Color.FromArgb(alpha, 235, 200, 135));
                using SolidBrush brGris = new SolidBrush(Color.FromArgb(alpha, 160, 170, 185));

                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("NEXUS STUDIOS", fEstudio, brOro, new Rectangle(0, h / 2 - 45, w, 50), sf);
                g.DrawString("A  C #   G D I +   T A C T I C A L   E N G I N E", fSub, brGris, new Rectangle(0, h / 2 + 12, w, 30), sf);
            }
            else if (fase == 5)
            {
                using var brFondo = new LinearGradientBrush(this.ClientRectangle, Color.FromArgb(10, 18, 34), Color.FromArgb(4, 6, 14), 90f);
                g.FillRectangle(brFondo, this.ClientRectangle);

                if (imagenLogo != null)
                {
                    DibujarImagenConOpacidad(g, imagenLogo, new Rectangle((w - 440) / 2, 75, 440, 210), opacidad);
                }

                int cx = w / 2;
                int cy = 365;
                DibujarHexagonoGiratorio(g, cx, cy, 24, anguloHexagono);

                using Font fCarga = new Font("Segoe UI", 11f, FontStyle.Bold);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString("Validando ajustes del juego...", fCarga, Brushes.White, cx, cy + 38, sf);
            }
        }

        private void DibujarImagenConOpacidad(Graphics g, Image img, Rectangle dest, float alpha)
        {
            ColorMatrix matrix = new ColorMatrix { Matrix33 = Math.Clamp(alpha, 0f, 1f) };
            using ImageAttributes attr = new ImageAttributes();
            attr.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            g.DrawImage(img, dest, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attr);
        }

        private void DibujarHexagonoGiratorio(Graphics g, int cx, int cy, float radio, float angulo)
        {
            var estado = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(angulo);

            PointF[] pts = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                double rad = (Math.PI / 3.0) * i;
                pts[i] = new PointF((float)(Math.Cos(rad) * radio), (float)(Math.Sin(rad) * radio));
            }

            using Pen penOro = new Pen(Color.Gold, 3f);
            using SolidBrush brCentro = new SolidBrush(Color.FromArgb(60, Color.Gold));
            g.FillPolygon(brCentro, pts);
            g.DrawPolygon(penOro, pts);
            g.Restore(estado);
        }
    }
}