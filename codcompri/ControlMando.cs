using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NexusForms
{
    public static class ControlMando
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct JOYINFOEX
        {
            public int dwSize;
            public int dwFlags;
            public int dwXpos;
            public int dwYpos;
            public int dwZpos;
            public int dwRpos;
            public int dwUpos;
            public int dwVpos;
            public int dwButtons;
            public int dwButtonNumber;
            public int dwPOV;
            public int dwReserved1;
            public int dwReserved2;
        }

        [DllImport("winmm.dll")]
        private static extern int joyGetPosEx(int uJoyID, ref JOYINFOEX pji);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static bool TeclaPresionada(Keys tecla) => (GetAsyncKeyState((int)tecla) & 0x8000) != 0;

        public struct EstadoMando
        {
            public bool Conectado;
            public bool Izquierda;
            public bool Derecha;
            public bool Arriba;
            public bool Abajo;
            public bool BotonSaltar;     // X (Aceptar / Sí en menú, Saltar en juego)
            public bool BotonDisparar;   // Círculo / Redondo (Volver atrás en menú, Disparar en juego)
            public bool BotonAcuchillar; // Cuadrado (Acuchillar / Supernova)
            public bool BotonEspecial;   // Triángulo (Poder / Cambiar arma)
            public bool BotonVoz;        // L1 / R1 / Share (Activar Asistente IA por Voz)
            public bool BotonSalir;      // Options / Start / L2 / R2 / PS (Salir del juego / Salir de misión)
        }

        public static EstadoMando Leer()
        {
            EstadoMando estado = new EstadoMando();
            JOYINFOEX info = new JOYINFOEX
            {
                dwSize = Marshal.SizeOf(typeof(JOYINFOEX)),
                dwFlags = 0x000000FF // JOY_RETURNALL
            };

            for (int id = 0; id < 16; id++)
            {
                if (joyGetPosEx(id, ref info) == 0)
                {
                    estado.Conectado = true;

                    // Joystick izquierdo
                    if (info.dwXpos < 20000) estado.Izquierda = true;
                    if (info.dwXpos > 45000) estado.Derecha = true;
                    if (info.dwYpos < 20000) estado.Arriba = true;
                    if (info.dwYpos > 45000) estado.Abajo = true;

                    // Cruceta digital (D-Pad)
                    if (info.dwPOV != 65535)
                    {
                        if (info.dwPOV == 27000 || info.dwPOV == 31500 || info.dwPOV == 22500) estado.Izquierda = true;
                        if (info.dwPOV == 9000 || info.dwPOV == 4500 || info.dwPOV == 13500) estado.Derecha = true;
                        if (info.dwPOV == 0 || info.dwPOV == 4500 || info.dwPOV == 31500) estado.Arriba = true;
                        if (info.dwPOV == 18000 || info.dwPOV == 13500 || info.dwPOV == 22500) estado.Abajo = true;
                    }

                    // Botones PlayStation / DirectInput
                    if ((info.dwButtons & 1) != 0) estado.BotonAcuchillar = true; // Cuadrado
                    if ((info.dwButtons & 2) != 0) estado.BotonSaltar = true;     // X
                    if ((info.dwButtons & 4) != 0) estado.BotonDisparar = true;   // Círculo
                    if ((info.dwButtons & 8) != 0) estado.BotonEspecial = true;   // Triángulo
                    if ((info.dwButtons & 16) != 0 || (info.dwButtons & 32) != 0 || (info.dwButtons & 256) != 0)
                        estado.BotonVoz = true; // L1 / R1 / Share
                    if ((info.dwButtons & 64) != 0 || (info.dwButtons & 128) != 0 ||
                        (info.dwButtons & 512) != 0 || (info.dwButtons & 1024) != 0)
                        estado.BotonSalir = true; // L2 / R2 / Options(Start) / PS
                }
            }

            // Respaldo de teclado / mando virtual en modo teclado
            if (TeclaPresionada(Keys.Left)) estado.Izquierda = true;
            if (TeclaPresionada(Keys.Right)) estado.Derecha = true;
            if (TeclaPresionada(Keys.Up)) estado.Arriba = true;
            if (TeclaPresionada(Keys.Down)) estado.Abajo = true;
            if (TeclaPresionada(Keys.Enter) || TeclaPresionada(Keys.Space)) estado.BotonSaltar = true;
            if (TeclaPresionada(Keys.Escape)) estado.BotonDisparar = true;
            if (TeclaPresionada(Keys.V)) estado.BotonVoz = true;
            if (TeclaPresionada(Keys.F4)) estado.BotonSalir = true;

            if (estado.Izquierda || estado.Derecha || estado.Arriba || estado.Abajo ||
                estado.BotonSaltar || estado.BotonDisparar || estado.BotonAcuchillar ||
                estado.BotonEspecial || estado.BotonVoz || estado.BotonSalir)
            {
                estado.Conectado = true;
            }

            return estado;
        }
    }
}