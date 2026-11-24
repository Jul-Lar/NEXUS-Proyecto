using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace NexusForms
{
    public class FormPerspectiva25D : Form
    {
        private System.Windows.Forms.Timer timerIntro;
        private Image? imagenLogo;
        private int fase = 0;
        private int tickFase = 0;
        private float opacidad = 0f;
        private float posicionHazLuz = -0.3f;

        public FormPerspectiva25D()
        {
            this.Size = new Size(1024, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;

            CargarLogo();

            timerIntro = new System.Windows.Forms.Timer { Interval = 16 };
            timerIntro.Tick += BucleAnimacionIntro;
            timerIntro.Start();
        }

        private void CargarLogo()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] rutasLogo = {
                Path.Combine(baseDir, "Assets", "logo_nexus.png"),
                Path.Combine(baseDir, "Assets", "logo_nexus.jpg"),
                Path.Combine(baseDir, "logo_nexus.png"),
                Path.Combine(baseDir, "logo_nexus.jpg")
            };

            foreach (var r in rutasLogo)
            {
                if (File.Exists(r))
                {
                    using var fs = new FileStream(r, FileMode.Open, FileAccess.Read);
                    imagenLogo = Image.FromStream(fs);
                    break;
                }
            }
        }

        private void BucleAnimacionIntro(object? sender, EventArgs e)
        {
            tickFase++;

            if (fase == 0)
            {
                if (tickFase >= 20) { fase = 1; tickFase = 0; }
            }
            else if (fase == 1)
            {
                opacidad = Math.Min(1f, opacidad + 0.025f);
                posicionHazLuz += 0.022f;
                if (tickFase >= 130) { fase = 2; tickFase = 0; }
            }
            else if (fase == 2)
            {
                opacidad = Math.Max(0f, opacidad - 0.035f);
                if (opacidad <= 0f) { fase = 3; tickFase = 0; }
            }
            else if (fase == 3)
            {
                timerIntro.Stop();
                this.Hide();
                using (FormInicio menuOriginal = new FormInicio())
                {
                    menuOriginal.ShowDialog();
                }
                this.Close();
            }
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (imagenLogo != null && (fase == 1 || fase == 2))
            {
                int w = this.ClientSize.Width;
                int h = this.ClientSize.Height;
                int logoW = 500;
                int logoH = 200;
                Rectangle destRect = new Rectangle((w - logoW) / 2, (h - logoH) / 2, logoW, logoH);

                ColorMatrix matrix = new ColorMatrix { Matrix33 = Math.Clamp(opacidad, 0f, 1f) };
                using ImageAttributes attr = new ImageAttributes();
                attr.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                g.DrawImage(imagenLogo, destRect, 0, 0, imagenLogo.Width, imagenLogo.Height, GraphicsUnit.Pixel, attr);

                if (posicionHazLuz >= 0f && posicionHazLuz <= 1.2f)
                {
                    float luzX = destRect.X + (destRect.Width * posicionHazLuz);
                    RectangleF rectHaz = new RectangleF(luzX - 45, destRect.Y - 20, 90, destRect.Height + 40);
                    using LinearGradientBrush brHaz = new LinearGradientBrush(
                        rectHaz, Color.Transparent, Color.FromArgb((int)(140 * opacidad), 255, 255, 255), 0f);
                    brHaz.SetSigmaBellShape(0.5f);
                    g.FillRectangle(brHaz, rectHaz);
                }
            }
        }
    }
}