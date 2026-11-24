using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace NexusForms
{
    public class RegistroPartida
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
        public string Nombre { get; set; } = "Comandante";
        public string RealidadAsignada { get; set; } = "IRIS-07";
        public string NaveAsignada { get; set; } = "Caza Estelar V-9";
        public int ColorTemaArgb { get; set; }
        public bool EsElite { get; set; }
        public int NivelEnergia { get; set; } = 100;
        public int NivelEstabilidad { get; set; } = 100;
        public int NivelPeligro { get; set; } = 0;
        public int FragmentosDatos { get; set; } = 0;
        public int XP { get; set; } = 0;
        public bool EscudosActivos { get; set; } = true;
        public Dictionary<string, int>? Inventario { get; set; }
        public string FechaGuardado { get; set; } = "";

        public override string ToString()
        {
            return $"[{FechaGuardado}]  {Nombre.ToUpper()}  |  Sector: {RealidadAsignada}  |  Nave: {NaveAsignada}  |  XP: {XP}  |  Frag: {FragmentosDatos}";
        }
    }

    public static class GestorGuardado
    {
        private static readonly string RutaBD = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "base_datos_partidas.json"
        );

        // Mantiene en memoria qué ID de partida está jugando el usuario actualmente
        private static readonly Dictionary<Jugador, string> idsActivos = new Dictionary<Jugador, string>();

        public static List<RegistroPartida> ObtenerTodas()
        {
            if (!File.Exists(RutaBD)) return new List<RegistroPartida>();

            try
            {
                string json = File.ReadAllText(RutaBD);
                var lista = JsonSerializer.Deserialize<List<RegistroPartida>>(json);
                return lista ?? new List<RegistroPartida>();
            }
            catch
            {
                return new List<RegistroPartida>();
            }
        }

        public static bool ExisteAlgunaPartida() => ObtenerTodas().Count > 0;

        public static void AsociarId(Jugador jugador, string id)
        {
            idsActivos[jugador] = id;
        }

        public static void Guardar(Jugador jugador)
        {
            try
            {
                var partidas = ObtenerTodas();

                if (!idsActivos.TryGetValue(jugador, out string? idActual) || string.IsNullOrEmpty(idActual))
                {
                    idActual = Guid.NewGuid().ToString("N").Substring(0, 8);
                    idsActivos[jugador] = idActual;
                }

                var existente = partidas.FirstOrDefault(p => p.Id == idActual);
                if (existente == null)
                {
                    existente = new RegistroPartida { Id = idActual };
                    partidas.Insert(0, existente);
                }

                existente.Nombre = jugador.Nombre;
                existente.RealidadAsignada = jugador.RealidadAsignada;
                existente.NaveAsignada = jugador.NaveAsignada;
                existente.ColorTemaArgb = jugador.ColorTema.ToArgb();
                existente.EsElite = jugador.EsElite;
                existente.NivelEnergia = jugador.NivelEnergia;
                existente.NivelEstabilidad = jugador.NivelEstabilidad;
                existente.NivelPeligro = jugador.NivelPeligro;
                existente.FragmentosDatos = jugador.FragmentosDatos;
                existente.XP = jugador.XP;
                existente.EscudosActivos = jugador.EscudosActivos;
                existente.Inventario = new Dictionary<string, int>(jugador.Inventario);
                existente.FechaGuardado = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                // Ordenar dejando la partida más reciente arriba
                partidas = partidas.OrderByDescending(p => p.Id == idActual).ToList();

                var opciones = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(RutaBD, JsonSerializer.Serialize(partidas, opciones));
            }
            catch
            {
            }
        }

        public static Jugador ConvertirAJugador(RegistroPartida reg)
        {
            var j = new Jugador
            {
                Nombre = reg.Nombre,
                RealidadAsignada = reg.RealidadAsignada,
                NaveAsignada = reg.NaveAsignada,
                ColorTema = Color.FromArgb(reg.ColorTemaArgb),
                EsElite = reg.EsElite,
                NivelEnergia = reg.NivelEnergia,
                NivelEstabilidad = reg.NivelEstabilidad,
                NivelPeligro = reg.NivelPeligro,
                FragmentosDatos = reg.FragmentosDatos,
                XP = reg.XP,
                EscudosActivos = reg.EscudosActivos,
                Inventario = reg.Inventario != null
                    ? new Dictionary<string, int>(reg.Inventario)
                    : new Dictionary<string, int>()
            };

            AsociarId(j, reg.Id);
            return j;
        }

        public static void EliminarPartida(string id)
        {
            var partidas = ObtenerTodas();
            partidas.RemoveAll(p => p.Id == id);
            var opciones = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(RutaBD, JsonSerializer.Serialize(partidas, opciones));
        }
    }
}