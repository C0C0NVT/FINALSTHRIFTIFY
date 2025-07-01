using System;
using System.Data.SqlClient;
using System.Web.Http;
using System.Security.Cryptography;
using System.Text;

namespace ThriftifyAPI.Controllers
{
    public class AuthController : ApiController
    {
        private string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ThriftifyDB"].ConnectionString;

        [HttpPost]
        [Route("api/auth/login")]
        public IHttpActionResult Login([FromBody] LoginModel model)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT user_id, username, role FROM Users WHERE username = @username AND password = @password";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", model.Username);
                    cmd.Parameters.AddWithValue("@password", HashPassword(model.Password));

                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        return Ok(new
                        {
                            UserId = reader["user_id"],
                            Username = reader["username"],
                            Role = reader["role"]
                        });
                    }
                    else
                    {
                        return Unauthorized();
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}