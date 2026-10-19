using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

[Route("api/chats")]
[ApiController]
public class ChatsController : ControllerBase
{
    private readonly string _connectionString;

    public ChatsController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    [HttpGet("usuario/{usuarioId}")]
    public IActionResult ObtenerChatsDelUsuario(int usuarioId)
    {
        var listaChats = new List<object>();
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var query = @"
            SELECT c.Id, c.Usuario1Id, c.Usuario2Id,
                   (SELECT TOP 1 m.Texto FROM Mensajes m WHERE m.ChatId = c.Id ORDER BY m.Fecha DESC) AS UltimoMensaje,
                   (SELECT TOP 1 m.Fecha FROM Mensajes m WHERE m.ChatId = c.Id ORDER BY m.Fecha DESC) AS FechaUltimoMensaje,
                   CASE WHEN c.Usuario1Id = @UsuarioId THEN u2.Nombre ELSE u1.Nombre END AS OtroUsuarioNombre,
                   CASE WHEN c.Usuario1Id = @UsuarioId THEN u2.FotoUri ELSE u1.FotoUri END AS OtroUsuarioFotoUri,
                   CASE WHEN c.Usuario1Id = @UsuarioId THEN c.Usuario2Id ELSE c.Usuario1Id END AS OtroUsuarioId
            FROM Chats c
            LEFT JOIN Usuarios u1 ON c.Usuario1Id = u1.Id
            LEFT JOIN Usuarios u2 ON c.Usuario2Id = u2.Id
            WHERE c.Usuario1Id = @UsuarioId OR c.Usuario2Id = @UsuarioId";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@UsuarioId", usuarioId);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var otroId = Convert.ToInt32(reader["OtroUsuarioId"]);
            listaChats.Add(new
            {
                Id = Convert.ToInt32(reader["Id"]),
                Usuario1Id = Convert.ToInt32(reader["Usuario1Id"]),
                Usuario2Id = Convert.ToInt32(reader["Usuario2Id"]),
                UltimoMensaje = reader["UltimoMensaje"] == DBNull.Value ? "Inicia una conversación" : reader["UltimoMensaje"].ToString(),
                FechaUltimoMensaje = reader["FechaUltimoMensaje"] == DBNull.Value ? null : reader["FechaUltimoMensaje"].ToString(),
                OtroUsuarioNombre = reader["OtroUsuarioNombre"] == DBNull.Value ? $"Usuario #{otroId}" : reader["OtroUsuarioNombre"].ToString(),
                OtroUsuarioFotoUri = reader["OtroUsuarioFotoUri"] == DBNull.Value ? null : reader["OtroUsuarioFotoUri"].ToString()
            });
        }

        return Ok(listaChats);
    }

    [HttpPost("crear")]
    public IActionResult CrearOObtenerChat([FromBody] CrearChatRequest request)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var queryBuscar = @"
            SELECT Id FROM Chats 
            WHERE (Usuario1Id = @U1 AND Usuario2Id = @U2) OR (Usuario1Id = @U2 AND Usuario2Id = @U1)";

        using var cmdBuscar = new SqlCommand(queryBuscar, connection);
        cmdBuscar.Parameters.AddWithValue("@U1", request.Usuario1Id);
        cmdBuscar.Parameters.AddWithValue("@U2", request.Usuario2Id);

        using var reader = cmdBuscar.ExecuteReader();
        if (reader.Read())
        {
            var chatId = Convert.ToInt32(reader["Id"]);
            return Ok(new { Id = chatId, Usuario1Id = request.Usuario1Id, Usuario2Id = request.Usuario2Id });
        }
        reader.Close();

        var queryCrear = "INSERT INTO Chats (Usuario1Id, Usuario2Id) OUTPUT INSERTED.Id VALUES (@U1, @U2)";
        using var cmdCrear = new SqlCommand(queryCrear, connection);
        cmdCrear.Parameters.AddWithValue("@U1", request.Usuario1Id);
        cmdCrear.Parameters.AddWithValue("@U2", request.Usuario2Id);

        var nuevoId = Convert.ToInt32(cmdCrear.ExecuteScalar());
        return Ok(new { Id = nuevoId, Usuario1Id = request.Usuario1Id, Usuario2Id = request.Usuario2Id });
    }

    [HttpGet("{chatId}/mensajes")]
    public IActionResult ObtenerMensajesDelChat(int chatId)
    {
        var listaMensajes = new List<object>();
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var query = "SELECT Id, ChatId, RemitenteId, Texto, Fecha FROM Mensajes WHERE ChatId = @ChatId ORDER BY Fecha ASC";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@ChatId", chatId);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            listaMensajes.Add(new
            {
                Id = Convert.ToInt32(reader["Id"]),
                ChatId = Convert.ToInt32(reader["ChatId"]),
                RemitenteId = Convert.ToInt32(reader["RemitenteId"]),
                Texto = reader["Texto"].ToString(),
                Fecha = reader["Fecha"].ToString()
            });
        }

        return Ok(listaMensajes);
    }

    [HttpPost("mensajes")]
    public IActionResult EnviarMensaje([FromBody] MensajeDto mensajeDto)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var query = "INSERT INTO Mensajes (ChatId, RemitenteId, Texto, Fecha) OUTPUT INSERTED.Id, INSERTED.Fecha VALUES (@ChatId, @RemitenteId, @Texto, GETDATE())";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@ChatId", mensajeDto.ChatId);
        command.Parameters.AddWithValue("@RemitenteId", mensajeDto.RemitenteId);
        command.Parameters.AddWithValue("@Texto", mensajeDto.Texto);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return Ok(new
            {
                Id = Convert.ToInt32(reader["Id"]),
                Fecha = reader["Fecha"].ToString(),
                mensaje = "Mensaje enviado con éxito"
            });
        }

        return BadRequest(new { mensaje = "No se pudo enviar el mensaje" });
    }

    [HttpDelete("{chatId}")]
    public IActionResult EliminarChat(int chatId)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var cmdMsgs = new SqlCommand("DELETE FROM Mensajes WHERE ChatId = @ChatId", connection);
        cmdMsgs.Parameters.AddWithValue("@ChatId", chatId);
        cmdMsgs.ExecuteNonQuery();

        using var cmdChat = new SqlCommand("DELETE FROM Chats WHERE Id = @ChatId", connection);
        cmdChat.Parameters.AddWithValue("@ChatId", chatId);
        int rows = cmdChat.ExecuteNonQuery();

        if (rows > 0) return Ok(new { mensaje = "Chat eliminado con éxito" });
        return NotFound(new { mensaje = "Chat no encontrado" });
    }
}

public class CrearChatRequest
{
    public int Usuario1Id { get; set; }
    public int Usuario2Id { get; set; }
}

public class MensajeDto
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int RemitenteId { get; set; }
    public string Texto { get; set; }
    public string? Fecha { get; set; }
}