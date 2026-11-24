using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.Data.SqlClient;

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
        public Dictionary<string, int> Inventario { get; set; } = new Dictionary<string, int>();
        public EstadoMision MisionActual { get; set; } = new EstadoMision();
        public string FechaGuardado { get; set; } = "";

        public override string ToString()
        {
            return $"[{FechaGuardado}]  {Nombre.ToUpper()}  |  Sector: {RealidadAsignada}  |  Nave: {NaveAsignada}  |  XP: {XP}  |  Frag: {FragmentosDatos}";
        }
    }

    public static class GestorGuardado
    {
        public static string CadenaConexion { get; set; } =
            "Server=.\\SQLEXPRESS;Database=NEXUS_DB;Trusted_Connection=True;TrustServerCertificate=True;";

        private static readonly Dictionary<Jugador, string> idsActivos = new Dictionary<Jugador, string>();

        public static bool ProbarConexion(out string mensajeError)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(CadenaConexion))
                {
                    conn.Open();
                    mensajeError = "Conexión exitosa con NEXUS_DB.";
                    return true;
                }
            }
            catch (Exception ex)
            {
                mensajeError = "Error de conexión SQL Server: " + ex.Message;
                return false;
            }
        }

        public static List<RegistroPartida> ObtenerTodas()
        {
            var lista = new List<RegistroPartida>();
            try
            {
                using (SqlConnection conn = new SqlConnection(CadenaConexion))
                {
                    conn.Open();
                    string sql = @"
                        SELECT p.IdPartida, j.Nombre, p.RealidadAsignada, n.NombreNave,
                               j.ColorTemaArgb, j.EsElite, p.NivelEnergia, p.NivelEstabilidad,
                               p.NivelPeligro, p.FragmentosDatos, p.XP, p.EscudosActivos,
                               p.FechaGuardado,
                               ISNULL(m.ZonaAlcanzada, 0) AS ZonaAlcanzada,
                               ISNULL(m.NucleoEncontrado, 0) AS NucleoEncontrado,
                               ISNULL(m.EntidadSobrevivida, 0) AS EntidadSobrevivida
                        FROM Partidas p
                        INNER JOIN Jugadores j ON p.IdJugador = j.IdJugador
                        INNER JOIN Naves n ON p.IdNave = n.IdNave
                        LEFT JOIN EstadoMisionPartida m ON p.IdPartida = m.IdPartida
                        ORDER BY p.FechaGuardado DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var reg = new RegistroPartida
                            {
                                Id = reader["IdPartida"].ToString() ?? "",
                                Nombre = reader["Nombre"].ToString() ?? "Comandante",
                                RealidadAsignada = reader["RealidadAsignada"].ToString() ?? "IRIS-07",
                                NaveAsignada = reader["NombreNave"].ToString() ?? "Caza Estelar V-9",
                                ColorTemaArgb = Convert.ToInt32(reader["ColorTemaArgb"]),
                                EsElite = Convert.ToBoolean(reader["EsElite"]),
                                NivelEnergia = Convert.ToInt32(reader["NivelEnergia"]),
                                NivelEstabilidad = Convert.ToInt32(reader["NivelEstabilidad"]),
                                NivelPeligro = Convert.ToInt32(reader["NivelPeligro"]),
                                FragmentosDatos = Convert.ToInt32(reader["FragmentosDatos"]),
                                XP = Convert.ToInt32(reader["XP"]),
                                EscudosActivos = Convert.ToBoolean(reader["EscudosActivos"]),
                                FechaGuardado = Convert.ToDateTime(reader["FechaGuardado"]).ToString("dd/MM/yyyy HH:mm"),
                                MisionActual = new EstadoMision
                                {
                                    ZonaAlcanzada = Convert.ToBoolean(reader["ZonaAlcanzada"]),
                                    NucleoEncontrado = Convert.ToBoolean(reader["NucleoEncontrado"]),
                                    EntidadSobrevivida = Convert.ToBoolean(reader["EntidadSobrevivida"])
                                }
                            };
                            lista.Add(reg);
                        }
                    }

                    foreach (var reg in lista)
                    {
                        string sqlInv = "SELECT NombreItem, Cantidad FROM InventarioPartida WHERE IdPartida = @id";
                        using (SqlCommand cmdInv = new SqlCommand(sqlInv, conn))
                        {
                            cmdInv.Parameters.AddWithValue("@id", reg.Id);
                            using (SqlDataReader rInv = cmdInv.ExecuteReader())
                            {
                                while (rInv.Read())
                                {
                                    reg.Inventario[rInv["NombreItem"].ToString() ?? ""] = Convert.ToInt32(rInv["Cantidad"]);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al consultar partidas: " + ex.Message);
            }
            return lista;
        }

        public static bool ExisteAlgunaPartida() => ObtenerTodas().Count > 0;

        public static void AsociarId(Jugador jugador, string id)
        {
            idsActivos[jugador] = id;
        }

        public static void Guardar(Jugador jugador)
        {
            if (jugador == null) throw new ArgumentNullException(nameof(jugador));
            if (string.IsNullOrWhiteSpace(jugador.Nombre))
                throw new ArgumentException("El nombre del comandante no puede estar vacío.");

            jugador.LimitarAtributos();

            if (!idsActivos.TryGetValue(jugador, out string? idActual) || string.IsNullOrEmpty(idActual))
            {
                idActual = Guid.NewGuid().ToString("N").Substring(0, 8);
                idsActivos[jugador] = idActual;
            }

            using (SqlConnection conn = new SqlConnection(CadenaConexion))
            {
                conn.Open();
                SqlTransaction tx = conn.BeginTransaction();

                try
                {
                    int idJugador;
                    string sqlGetJug = "SELECT IdJugador FROM Jugadores WHERE Nombre = @nom";
                    using (SqlCommand cmd = new SqlCommand(sqlGetJug, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@nom", jugador.Nombre.Trim());
                        object? res = cmd.ExecuteScalar();
                        if (res != null)
                        {
                            idJugador = Convert.ToInt32(res);
                            string sqlUpdJug = "UPDATE Jugadores SET EsElite=@elite, ColorTemaArgb=@color WHERE IdJugador=@idJ";
                            using (SqlCommand cmdUpd = new SqlCommand(sqlUpdJug, conn, tx))
                            {
                                cmdUpd.Parameters.AddWithValue("@elite", jugador.EsElite);
                                cmdUpd.Parameters.AddWithValue("@color", jugador.ColorTema.ToArgb());
                                cmdUpd.Parameters.AddWithValue("@idJ", idJugador);
                                cmdUpd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string sqlInsJug = "INSERT INTO Jugadores (Nombre, EsElite, ColorTemaArgb) OUTPUT INSERTED.IdJugador VALUES (@nom, @elite, @color)";
                            using (SqlCommand cmdIns = new SqlCommand(sqlInsJug, conn, tx))
                            {
                                cmdIns.Parameters.AddWithValue("@nom", jugador.Nombre.Trim());
                                cmdIns.Parameters.AddWithValue("@elite", jugador.EsElite);
                                cmdIns.Parameters.AddWithValue("@color", jugador.ColorTema.ToArgb());
                                idJugador = Convert.ToInt32(cmdIns.ExecuteScalar());
                            }
                        }
                    }

                    int idNave;
                    string sqlGetNave = "SELECT IdNave FROM Naves WHERE NombreNave = @nave";
                    using (SqlCommand cmd = new SqlCommand(sqlGetNave, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@nave", jugador.NaveAsignada);
                        object? res = cmd.ExecuteScalar();
                        if (res != null)
                        {
                            idNave = Convert.ToInt32(res);
                        }
                        else
                        {
                            string sqlInsNave = "INSERT INTO Naves (NombreNave, ClaseNave) OUTPUT INSERTED.IdNave VALUES (@nave, 'Personalizada')";
                            using (SqlCommand cmdIns = new SqlCommand(sqlInsNave, conn, tx))
                            {
                                cmdIns.Parameters.AddWithValue("@nave", jugador.NaveAsignada);
                                idNave = Convert.ToInt32(cmdIns.ExecuteScalar());
                            }
                        }
                    }

                    string sqlCheckPartida = "SELECT COUNT(1) FROM Partidas WHERE IdPartida = @idP";
                    int existePartida;
                    using (SqlCommand cmd = new SqlCommand(sqlCheckPartida, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@idP", idActual);
                        existePartida = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    if (existePartida > 0)
                    {
                        string sqlUpdP = @"
                            UPDATE Partidas SET
                                IdJugador=@idJ, IdNave=@idN, RealidadAsignada=@real,
                                NivelEnergia=@ene, NivelEstabilidad=@est, NivelPeligro=@pel,
                                FragmentosDatos=@frag, XP=@xp, EscudosActivos=@esc,
                                FechaGuardado=GETDATE()
                            WHERE IdPartida=@idP";
                        using (SqlCommand cmd = new SqlCommand(sqlUpdP, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@idJ", idJugador);
                            cmd.Parameters.AddWithValue("@idN", idNave);
                            cmd.Parameters.AddWithValue("@real", jugador.RealidadAsignada);
                            cmd.Parameters.AddWithValue("@ene", jugador.NivelEnergia);
                            cmd.Parameters.AddWithValue("@est", jugador.NivelEstabilidad);
                            cmd.Parameters.AddWithValue("@pel", jugador.NivelPeligro);
                            cmd.Parameters.AddWithValue("@frag", jugador.FragmentosDatos);
                            cmd.Parameters.AddWithValue("@xp", jugador.XP);
                            cmd.Parameters.AddWithValue("@esc", jugador.EscudosActivos);
                            cmd.Parameters.AddWithValue("@idP", idActual);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string sqlInsP = @"
                            INSERT INTO Partidas (IdPartida, IdJugador, IdNave, RealidadAsignada,
                                NivelEnergia, NivelEstabilidad, NivelPeligro, FragmentosDatos, XP, EscudosActivos, FechaGuardado)
                            VALUES (@idP, @idJ, @idN, @real, @ene, @est, @pel, @frag, @xp, @esc, GETDATE())";
                        using (SqlCommand cmd = new SqlCommand(sqlInsP, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@idP", idActual);
                            cmd.Parameters.AddWithValue("@idJ", idJugador);
                            cmd.Parameters.AddWithValue("@idN", idNave);
                            cmd.Parameters.AddWithValue("@real", jugador.RealidadAsignada);
                            cmd.Parameters.AddWithValue("@ene", jugador.NivelEnergia);
                            cmd.Parameters.AddWithValue("@est", jugador.NivelEstabilidad);
                            cmd.Parameters.AddWithValue("@pel", jugador.NivelPeligro);
                            cmd.Parameters.AddWithValue("@frag", jugador.FragmentosDatos);
                            cmd.Parameters.AddWithValue("@xp", jugador.XP);
                            cmd.Parameters.AddWithValue("@esc", jugador.EscudosActivos);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    using (SqlCommand cmdDelInv = new SqlCommand("DELETE FROM InventarioPartida WHERE IdPartida = @idP", conn, tx))
                    {
                        cmdDelInv.Parameters.AddWithValue("@idP", idActual);
                        cmdDelInv.ExecuteNonQuery();
                    }

                    if (jugador.Inventario != null)
                    {
                        foreach (var item in jugador.Inventario)
                        {
                            string sqlInsInv = "INSERT INTO InventarioPartida (IdPartida, NombreItem, Cantidad) VALUES (@idP, @item, @cant)";
                            using (SqlCommand cmdInv = new SqlCommand(sqlInsInv, conn, tx))
                            {
                                cmdInv.Parameters.AddWithValue("@idP", idActual);
                                cmdInv.Parameters.AddWithValue("@item", item.Key);
                                cmdInv.Parameters.AddWithValue("@cant", item.Value);
                                cmdInv.ExecuteNonQuery();
                            }
                        }
                    }

                    using (SqlCommand cmdDelMis = new SqlCommand("DELETE FROM EstadoMisionPartida WHERE IdPartida = @idP", conn, tx))
                    {
                        cmdDelMis.Parameters.AddWithValue("@idP", idActual);
                        cmdDelMis.ExecuteNonQuery();
                    }

                    string sqlInsMis = @"
                        INSERT INTO EstadoMisionPartida (IdPartida, ZonaAlcanzada, NucleoEncontrado, EntidadSobrevivida)
                        VALUES (@idP, @zona, @nuc, @ent)";
                    using (SqlCommand cmdMis = new SqlCommand(sqlInsMis, conn, tx))
                    {
                        cmdMis.Parameters.AddWithValue("@idP", idActual);
                        cmdMis.Parameters.AddWithValue("@zona", jugador.MisionActual?.ZonaAlcanzada ?? false);
                        cmdMis.Parameters.AddWithValue("@nuc", jugador.MisionActual?.NucleoEncontrado ?? false);
                        cmdMis.Parameters.AddWithValue("@ent", jugador.MisionActual?.EntidadSobrevivida ?? false);
                        cmdMis.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
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
                    : new Dictionary<string, int>(),
                MisionActual = reg.MisionActual ?? new EstadoMision()
            };

            AsociarId(j, reg.Id);
            return j;
        }

        public static void EliminarPartida(string id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(CadenaConexion))
                {
                    conn.Open();
                    string sql = "DELETE FROM Partidas WHERE IdPartida = @id";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al eliminar partida: " + ex.Message);
            }
        }
    }
}