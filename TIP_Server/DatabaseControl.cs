using System;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;

namespace TIP_Server
{
    public static class DatabaseControl
    {
        private const string IPAddr = "localhost";
        private const string user = "projekt_tip_user";
        private const string password = "tM8;WY2bT84,D@sK";
        private const string databaseName = "projekt_tip";

        private const string connectionString = "server=" + IPAddr + ";database=" + databaseName + ";uid=" + user + ";pwd=" + password + ";";

        private static bool CheckIfUserExists(string username) {
            bool result;
            using (MySqlConnection conn = new MySqlConnection(connectionString)) {
                using (MySqlCommand cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "SELECT COUNT(user_id) FROM users WHERE user_name = @u";
                    cmd.Parameters.AddWithValue("@u", username);
                    result = (int)cmd.ExecuteScalar() > 0;
                }
            }

            return result;
        }

        public static long AddNewUser(string username, string password) {
            byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
            byte[] passwordHashBytes;
            using (SHA512Managed algorithm = new SHA512Managed()) {
                passwordHashBytes = algorithm.ComputeHash(passwordBytes);
            }
            string passwordHash = Convert.ToHexString(passwordHashBytes);

            long insertedID;
            using (MySqlConnection conn = new MySqlConnection(connectionString)) {
                using (MySqlCommand cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "INSERT INTO users (user_name, password_hash) values (@u, @p)";
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", passwordHash);
                    cmd.ExecuteNonQuery();
                    insertedID = cmd.LastInsertedId;
                }
            }

            return insertedID;
        }

        public static bool CheckUserPassword(string username, string password) {
            bool result;
            byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
            byte[] passwordHashBytes;
            using (SHA512Managed algorithm = new SHA512Managed()) {
                passwordHashBytes = algorithm.ComputeHash(passwordBytes);
            }
            string passwordHash = Convert.ToHexString(passwordHashBytes);

            using (MySqlConnection conn = new MySqlConnection(connectionString)) {
                using (MySqlCommand cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT password_hash FROM users WHERE user_name = @u";
                    cmd.Parameters.AddWithValue("@u", username);
                    result = (string)cmd.ExecuteScalar() == passwordHash;
                }
            }

            return result;
        }




    }
}
