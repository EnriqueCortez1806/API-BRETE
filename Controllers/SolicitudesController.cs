using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

[Route("api/solicitudes")]
[ApiController]
public class SolicitudesController : ControllerBase
{
    private readonly string _connectionString;

    public SolicitudesController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    [HttpGet]
    public IActionResult ObtenerSolicitudes()
    {
        var lista = new List<object>();
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // Hacemos un JOIN con la tabla Usuarios para traer Nombre y FotoUri
        var query = @"SELECT s.Id, s.UsuarioId, s.Titulo, s.Descripcion, s.Categoria, 
                             s.Ubicacion, s.Presupuesto, s.FechaCreacion, s.Estado, s.EsUrgente,
                             u.Nombre AS NombreUsuario, u.FotoUri AS FotoUsuarioUri
                      FROM Solicitudes s
                      LEFT JOIN Usuarios u ON s.UsuarioId = u.Id";

        using var command = new SqlCommand(query, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            lista.Add(new
            {
                Id = reader["Id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Id"]),
                UsuarioId = reader["UsuarioId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["UsuarioId"]),
                Titulo = reader["Titulo"] == DBNull.Value ? "" : reader["Titulo"].ToString(),
                Descripcion = reader["Descripcion"] == DBNull.Value ? "" : reader["Descripcion"].ToString(),
                Categoria = reader["Categoria"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Categoria"]),
                Ubicacion = reader["Ubicacion"] == DBNull.Value ? "" : reader["Ubicacion"].ToString(),
                Presupuesto = reader["Presupuesto"] == DBNull.Value ? "" : reader["Presupuesto"].ToString(),
                FechaCreacion = reader["FechaCreacion"] == DBNull.Value ? null : reader["FechaCreacion"].ToString(),
                Estado = reader["Estado"] == DBNull.Value ? "Activa" : reader["Estado"].ToString(),
                EsUrgente = reader["EsUrgente"] != DBNull.Value && Convert.ToBoolean(reader["EsUrgente"]),
                NombreUsuario = reader["NombreUsuario"] == DBNull.Value ? null : reader["NombreUsuario"].ToString(),
                FotoUsuarioUri = reader["FotoUsuarioUri"] == DBNull.Value ? null : reader["FotoUsuarioUri"].ToString()
            });
        }
        return Ok(lista);
    }

    [HttpPost]
    public IActionResult CrearSolicitud([FromBody] SolicitudDto solicitudDto)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var query = @"INSERT INTO Solicitudes (UsuarioId, Titulo, Descripcion, Categoria, Ubicacion, Presupuesto, Estado, EsUrgente, FechaCreacion) 
                      VALUES (@UsuarioId, @Titulo, @Descripcion, @Categoria, @Ubicacion, @Presupuesto, @Estado, @EsUrgente, GETDATE());
                      SELECT SCOPE_IDENTITY();";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@UsuarioId", solicitudDto.UsuarioId);
        command.Parameters.AddWithValue("@Titulo", solicitudDto.Titulo);
        command.Parameters.AddWithValue("@Descripcion", solicitudDto.Descripcion);
        command.Parameters.AddWithValue("@Categoria", solicitudDto.Categoria);
        command.Parameters.AddWithValue("@Ubicacion", solicitudDto.Ubicacion);
        command.Parameters.AddWithValue("@Presupuesto", solicitudDto.Presupuesto ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Estado", "Activa");
        command.Parameters.AddWithValue("@EsUrgente", solicitudDto.EsUrgente);

        var newId = Convert.ToInt32(command.ExecuteScalar());
        return Ok(new { id = newId, mensaje = "Solicitud creada con éxito" });
    }

    // --- ENDPOINT PARA ACTUALIZAR SOLICITUDES (PUT) ---
    [HttpPut("{id}")]
    public IActionResult ActualizarSolicitud(int id, [FromBody] SolicitudDto solicitudDto)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var query = @"UPDATE Solicitudes 
                      SET Titulo = @Titulo, 
                          Descripcion = @Descripcion, 
                          Categoria = @Categoria, 
                          Ubicacion = @Ubicacion, 
                          Presupuesto = @Presupuesto, 
                          EsUrgente = @EsUrgente 
                      WHERE Id = @Id";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Titulo", solicitudDto.Titulo);
        command.Parameters.AddWithValue("@Descripcion", solicitudDto.Descripcion);
        command.Parameters.AddWithValue("@Categoria", solicitudDto.Categoria);
        command.Parameters.AddWithValue("@Ubicacion", solicitudDto.Ubicacion);
        command.Parameters.AddWithValue("@Presupuesto", solicitudDto.Presupuesto ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@EsUrgente", solicitudDto.EsUrgente);

        int rowsAffected = command.ExecuteNonQuery();
        if (rowsAffected > 0)
        {
            return Ok(new { mensaje = "Solicitud actualizada con éxito" });
        }

        return NotFound(new { mensaje = $"Solicitud con ID {id} no encontrada" });
    }

    // --- ENDPOINT PARA ELIMINAR SOLICITUDES ---
    [HttpDelete("{id}")]
    public IActionResult EliminarSolicitud(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var query = "DELETE FROM Solicitudes WHERE Id = @Id";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);

        int rowsAffected = command.ExecuteNonQuery();
        if (rowsAffected > 0)
        {
            return Ok(new { mensaje = "Solicitud eliminada con éxito de la base de datos" });
        }

        return NotFound(new { mensaje = $"Solicitud con ID {id} no encontrada" });
    }
}

public class SolicitudDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string Titulo { get; set; }
    public string Descripcion { get; set; }
    public int Categoria { get; set; }
    public string Ubicacion { get; set; }
    public string Presupuesto { get; set; }
    public string? FechaCreacion { get; set; }
    public string? Estado { get; set; }
    public bool EsUrgente { get; set; }
    public string? NombreUsuario { get; set; }
    public string? FotoUsuarioUri { get; set; }
}