using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace NexusForms
{
    public class ReconocimientoFacial : IDisposable
    {
        private VideoCapture? camara;
        private CascadeClassifier? detectorRostro;
        private readonly string rutaCascade;
        private readonly string carpetaRostros;
        private float lineaEscaneoY = 0f;
        private bool bajando = true;

        public bool CamaraActiva => camara != null && camara.IsOpened();

        public ReconocimientoFacial()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            rutaCascade = Path.Combine(baseDir, "Assets", "haarcascade_frontalface_default.xml");
            carpetaRostros = Path.Combine(baseDir, "Assets", "Rostros");
            Directory.CreateDirectory(carpetaRostros);

            if (File.Exists(rutaCascade))
                detectorRostro = new CascadeClassifier(rutaCascade);
        }

        public bool IniciarCamara(int indiceCamara = 0)
        {
            DetenerCamara();
            camara = new VideoCapture(indiceCamara, VideoCaptureAPIs.DSHOW);
            return camara.IsOpened();
        }

        public void DetenerCamara()
        {
            if (camara != null)
            {
                if (camara.IsOpened()) camara.Release();
                camara.Dispose();
                camara = null;
            }
        }

        /// <summary>
        /// Procesa un frame de cámara, dibuja el visor futurista verde/cian
        /// y opcionalmente registra muestras o verifica identidad.
        /// </summary>
        public (Bitmap? FrameVisual, bool RostroDetectado, string PilotoReconocido, double Similitud, Mat? RostroRecortado) ProcesarFrame(
            bool modoRegistro = false)
        {
            if (camara == null || !camara.IsOpened() || detectorRostro == null)
                return (null, false, "", 0, null);

            using Mat frame = new Mat();
            if (!camara.Read(frame) || frame.Empty())
                return (null, false, "", 0, null);

            // Efecto espejo para que sea natural como un espejo
            Cv2.Flip(frame, frame, FlipMode.Y);

            using Mat gris = new Mat();
            Cv2.CvtColor(frame, gris, ColorConversionCodes.BGR2GRAY);
            Cv2.EqualizeHist(gris, gris);

            Rect[] rostros = detectorRostro.DetectMultiScale(
                gris,
                scaleFactor: 1.15,
                minNeighbors: 5,
                flags: HaarDetectionTypes.ScaleImage,
                minSize: new OpenCvSharp.Size(110, 110)
            );

            Bitmap bmp = BitmapConverter.ToBitmap(frame);
            bool detectado = rostros.Length > 0;
            string pilotoIdentificado = "";
            double mejorSimilitud = 0;
            Mat? rostroNormalizado = null;

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Mira central tenue del escáner NEXUS
                DibujarRetículaHUD(g, bmp.Width, bmp.Height);

                if (detectado)
                {
                    // Tomar el rostro más grande (el más cercano a la cámara)
                    Rect r = rostros.OrderByDescending(x => x.Width * x.Height).First();

                    // Recortar y normalizar a 120x120 en escala de grises
                    using Mat recorte = new Mat(gris, r);
                    rostroNormalizado = new Mat();
                    Cv2.Resize(recorte, rostroNormalizado, new OpenCvSharp.Size(120, 120));

                    if (!modoRegistro)
                    {
                        (pilotoIdentificado, mejorSimilitud) = IdentificarPiloto(rostroNormalizado);
                    }

                    bool autorizado = modoRegistro || mejorSimilitud >= 0.72;
                    Color colorVisor = autorizado
                        ? Color.FromArgb(0, 255, 195)   // Verde/Cian neón NEXUS
                        : Color.FromArgb(0, 200, 255);  // Cian escaneando

                    DibujarVisorFuturista(g, r, colorVisor, modoRegistro, pilotoIdentificado, mejorSimilitud);
                }
            }

            return (bmp, detectado, pilotoIdentificado, mejorSimilitud, rostroNormalizado);
        }

        public void GuardarMuestraRostro(string nombrePiloto, Mat rostroNormalizado, int numeroMuestra)
        {
            string nombreLimpio = string.Concat(nombrePiloto.Trim().Split(Path.GetInvalidFileNameChars()));
            string dirPiloto = Path.Combine(carpetaRostros, nombreLimpio);
            Directory.CreateDirectory(dirPiloto);

            string archivo = Path.Combine(dirPiloto, $"muestra_{numeroMuestra:00}.png");
            Cv2.ImWrite(archivo, rostroNormalizado);
        }

        private (string Nombre, double Similitud) IdentificarPiloto(Mat rostroActual)
        {
            if (!Directory.Exists(carpetaRostros)) return ("", 0);

            string mejorPiloto = "";
            double maxScore = 0;

            using Mat histActual = CalcularHistogramaLBP(rostroActual);

            foreach (string dirPiloto in Directory.GetDirectories(carpetaRostros))
            {
                string nombre = Path.GetFileName(dirPiloto);
                string[] muestras = Directory.GetFiles(dirPiloto, "*.png");
                if (muestras.Length == 0) continue;

                double sumaScores = 0;
                int validas = 0;

                foreach (string imgPath in muestras)
                {
                    using Mat imgGuardada = Cv2.ImRead(imgPath, ImreadModes.Grayscale);
                    if (imgGuardada.Empty()) continue;

                    using Mat histGuardado = CalcularHistogramaLBP(imgGuardada);
                    double corr = Cv2.CompareHist(histActual, histGuardado, HistCompMethods.Correl);
                    sumaScores += Math.Max(0, corr);
                    validas++;
                }

                if (validas > 0)
                {
                    double promedio = sumaScores / validas;
                    if (promedio > maxScore)
                    {
                        maxScore = promedio;
                        mejorPiloto = nombre;
                    }
                }
            }

            return (mejorPiloto, maxScore);
        }

        /// <summary>
        /// Divide el rostro en una cuadrícula de 4x4 zonas y calcula histogramas locales
        /// (mismo principio espacial que LBPH para comparar rasgos faciales).
        /// </summary>
        private Mat CalcularHistogramaLBP(Mat rostro120x120)
        {
            List<Mat> histogramas = new List<Mat>();
            int celdaW = rostro120x120.Width / 4;
            int celdaH = rostro120x120.Height / 4;

            for (int fila = 0; fila < 4; fila++)
            {
                for (int col = 0; col < 4; col++)
                {
                    Rect subRect = new Rect(col * celdaW, fila * celdaH, celdaW, celdaH);
                    using Mat celda = new Mat(rostro120x120, subRect);
                    Mat hist = new Mat();
                    Cv2.CalcHist(
                        new[] { celda },
                        new[] { 0 },
                        null,
                        hist,
                        1,
                        new[] { 32 },
                        new[] { new Rangef(0, 256) }
                    );
                    Cv2.Normalize(hist, hist, 0, 1, NormTypes.MinMax);
                    histogramas.Add(hist);
                }
            }

            Mat descriptorFinal = new Mat();
            Cv2.VConcat(histogramas.ToArray(), descriptorFinal);
            foreach (var h in histogramas) h.Dispose();
            return descriptorFinal;
        }

        private void DibujarRetículaHUD(Graphics g, int w, int h)
        {
            using Pen penGuia = new Pen(Color.FromArgb(65, 0, 230, 210), 1f);
            int cx = w / 2, cy = h / 2;
            g.DrawEllipse(penGuia, cx - 95, cy - 120, 190, 240);
            g.DrawLine(penGuia, cx - 15, cy, cx + 15, cy);
            g.DrawLine(penGuia, cx, cy - 15, cx, cy + 15);
        }

        private void DibujarVisorFuturista(
            Graphics g, Rect r, Color color, bool modoRegistro, string piloto, double similitud)
        {
            int largoEsquina = Math.Max(18, r.Width / 5);
            using Pen penGrueso = new Pen(color, 3f);
            using Pen penFino = new Pen(Color.FromArgb(110, color), 1f);
            using SolidBrush brushFondoTexto = new SolidBrush(Color.FromArgb(175, 5, 20, 30));
            using SolidBrush brushTexto = new SolidBrush(color);
            using Font fontHud = new Font("Consolas", 9.5f, FontStyle.Bold);

            // Marco tenue completo
            g.DrawRectangle(penFino, r.X, r.Y, r.Width, r.Height);

            // 4 Esquinas tácticas futuristas
            // Superior izquierda
            g.DrawLine(penGrueso, r.X, r.Y, r.X + largoEsquina, r.Y);
            g.DrawLine(penGrueso, r.X, r.Y, r.X, r.Y + largoEsquina);
            // Superior derecha
            g.DrawLine(penGrueso, r.Right, r.Y, r.Right - largoEsquina, r.Y);
            g.DrawLine(penGrueso, r.Right, r.Y, r.Right, r.Y + largoEsquina);
            // Inferior izquierda
            g.DrawLine(penGrueso, r.X, r.Bottom, r.X + largoEsquina, r.Bottom);
            g.DrawLine(penGrueso, r.X, r.Bottom, r.X, r.Bottom - largoEsquina);
            // Inferior derecha
            g.DrawLine(penGrueso, r.Right, r.Bottom, r.Right - largoEsquina, r.Bottom);
            g.DrawLine(penGrueso, r.Right, r.Bottom, r.Right, r.Bottom - largoEsquina);

            // Línea láser horizontal animada subiendo y bajando dentro del rostro
            if (bajando)
            {
                lineaEscaneoY += 6f;
                if (lineaEscaneoY >= r.Height) bajando = false;
            }
            else
            {
                lineaEscaneoY -= 6f;
                if (lineaEscaneoY <= 0) bajando = true;
            }

            float yLaser = r.Y + Math.Clamp(lineaEscaneoY, 0, r.Height);
            using Pen penLaser = new Pen(Color.FromArgb(200, 0, 255, 180), 2f);
            g.DrawLine(penLaser, r.X + 4, yLaser, r.Right - 4, yLaser);

            // Etiqueta HUD superior
            string etiqueta = modoRegistro
                ? "CAPTURANDO BIOMETRÍA..."
                : (similitud >= 0.72
                    ? $"ID: {piloto.ToUpper()} [{(int)(similitud * 100)}%]"
                    : $"ESCANEANDO... [{(int)(similitud * 100)}%]");

            SizeF tamTexto = g.MeasureString(etiqueta, fontHud);
            float boxY = Math.Max(4, r.Y - tamTexto.Height - 6);
            g.FillRectangle(brushFondoTexto, r.X, boxY, tamTexto.Width + 10, tamTexto.Height + 4);
            g.DrawString(etiqueta, fontHud, brushTexto, r.X + 5, boxY + 2);
        }

        public void Dispose()
        {
            DetenerCamara();
            detectorRostro?.Dispose();
        }
    }
}