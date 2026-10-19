using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

[Route("api/usuarios")]
[ApiController]
public class UsuariosController : ControllerBase
{
    private readonly string _connectionString;

    public UsuariosController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    [HttpGet("{id}")]
    public IActionResult ObtenerUsuario(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var query = "SELECT Id, Correo, Nombre, Puesto, LugarTrabajo, Telefono, DarkMode, FotoUri FROM Usuarios WHERE Id = @Id";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var usuario = new
            {
                Id = reader.GetInt32(0),
                Correo = reader.GetString(1),
                Nombre = reader.GetString(2),
                Puesto = reader.IsDBNull(3) ? null : reader.GetString(3),
                LugarTrabajo = reader.IsDBNull(4) ? null : reader.GetString(4),
                Telefono = reader.IsDBNull(5) ? null : reader.GetString(5),
                DarkMode = reader.GetBoolean(6),
                FotoUri = reader.IsDBNull(7) ? null : reader.GetString(7)
            };
            return Ok(usuario);
        }

        return NotFound(new { mensaje = "Usuario no encontrado" });
    }

    [HttpPost("register")]
    public IActionResult RegistrarUsuario([FromBody] UsuarioDto usuarioDto)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var checkQuery = "SELECT COUNT(1) FROM Usuarios WHERE Correo = @Correo";
        using var checkCmd = new SqlCommand(checkQuery, connection);
        checkCmd.Parameters.AddWithValue("@Correo", usuarioDto.Correo);
        if ((int)checkCmd.ExecuteScalar() > 0)
        {
            return BadRequest(new { mensaje = "El correo ya está registrado." });
        }

        var query = @"INSERT INTO Usuarios (Correo, Contrasena, Nombre, Puesto, LugarTrabajo, Telefono, DarkMode, FotoUri) 
                      VALUES (@Correo, @Contrasena, @Nombre, @Puesto, @LugarTrabajo, @Telefono, @DarkMode, @FotoUri);
                      SELECT SCOPE_IDENTITY();";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Correo", usuarioDto.Correo);
        command.Parameters.AddWithValue("@Contrasena", usuarioDto.Contrasena ?? "123456");
        command.Parameters.AddWithValue("@Nombre", usuarioDto.Nombre);
        command.Parameters.AddWithValue("@Puesto", usuarioDto.Puesto ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@LugarTrabajo", usuarioDto.LugarTrabajo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Telefono", usuarioDto.Telefono ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DarkMode", usuarioDto.DarkMode);
        command.Parameters.AddWithValue("@FotoUri", usuarioDto.FotoUri ?? (object)DBNull.Value);

        var newId = Convert.ToInt32(command.ExecuteScalar());
        return Ok(new { id = newId, mensaje = "Usuario registrado con éxito" });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto loginDto)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var query = "SELECT Id, Correo, Nombre, Puesto, LugarTrabajo, Telefono, DarkMode, FotoUri FROM Usuarios WHERE Correo = @Correo AND Contrasena = @Contrasena";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Correo", loginDto.Correo);
        command.Parameters.AddWithValue("@Contrasena", loginDto.Contrasena);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var usuario = new
            {
                Id = reader.GetInt32(0),
                Correo = reader.GetString(1),
                Nombre = reader.GetString(2),
                Puesto = reader.IsDBNull(3) ? null : reader.GetString(3),
                LugarTrabajo = reader.IsDBNull(4) ? null : reader.GetString(4),
                Telefono = reader.IsDBNull(5) ? null : reader.GetString(5),
                DarkMode = reader.GetBoolean(6),
                FotoUri = reader.IsDBNull(7) ? null : reader.GetString(7)
            };
            return Ok(usuario);
        }

        return Unauthorized(new { mensaje = "Correo o contraseña incorrectos" });
    }

    [HttpPut]
    public IActionResult ActualizarUsuario([FromBody] UsuarioDto usuarioDto)
    {
        // Esto te imprimirá en la consola de C# exactamente qué ID está intentando actualizar
        Console.WriteLine($"[PUT] Intentando actualizar usuario ID: {usuarioDto.Id} - Nombre: {usuarioDto.Nombre}");

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var query = @"UPDATE Usuarios 
                      SET Nombre = @Nombre, Puesto = @Puesto, LugarTrabajo = @LugarTrabajo, 
                          Telefono = @Telefono, DarkMode = @DarkMode, FotoUri = @FotoUri 
                      WHERE Id = @Id";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@Id", usuarioDto.Id);
        command.Parameters.AddWithValue("@Nombre", usuarioDto.Nombre);
        command.Parameters.AddWithValue("@Puesto", usuarioDto.Puesto ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@LugarTrabajo", usuarioDto.LugarTrabajo ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Telefono", usuarioDto.Telefono ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DarkMode", usuarioDto.DarkMode);
        command.Parameters.AddWithValue("@FotoUri", usuarioDto.FotoUri ?? (object)DBNull.Value);

        int rowsAffected = command.ExecuteNonQuery();
        if (rowsAffected > 0)
        {
            return Ok(new { mensaje = "Perfil actualizado correctamente" });
        }

        return NotFound(new { mensaje = $"Usuario con ID {usuarioDto.Id} no encontrado para actualizar en BD" });
    }
}

public class UsuarioDto
{
    public int Id { get; set; }
    public string Correo { get; set; }
    public string? Contrasena { get; set; }
    public string Nombre { get; set; }
    public string? Puesto { get; set; }
    public string? LugarTrabajo { get; set; }
    public string? Telefono { get; set; }
    public string? FotoUri { get; set; }
    public bool DarkMode { get; set; }
}

public class LoginDto
{
    public string Correo { get; set; }
    public string Contrasena { get; set; }
}