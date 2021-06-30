using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;

namespace TIP_Server
{
    public static class DatabaseControl
    {
        private const string IPAddr = "192.168.1.109";
        private const string user = "projekt_tip_user";
        private const string password = "hSrZzp@x4g<!FY!F";
        private const string databaseName = "projekt_tip";

        private const string connectionString = "Server=" + IPAddr + ";Database=" + databaseName + ";Uid=" + user + ";Pwd=" + password + ";";

        public static bool CheckIfUserExists(string username) {
            bool result;
            using (MySqlConnection conn = new MySqlConnection(connectionString)) {
                using (MySqlCommand cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "SELECT COUNT(user_id) FROM users WHERE user_name = @u";
                    cmd.Parameters.AddWithValue("@u", username);
                    result = (Int64)cmd.ExecuteScalar() > 0;
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

        public static long CheckUserPassword(string username, string password) {
            long userID;
            byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
            byte[] passwordHashBytes;
            using (SHA512Managed algorithm = new SHA512Managed()) {
                passwordHashBytes = algorithm.ComputeHash(passwordBytes);
            }
            string passwordHash = Convert.ToHexString(passwordHashBytes);

            using (MySqlConnection conn = new MySqlConnection(connectionString)) {
                using (MySqlCommand cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "SELECT user_id, password_hash FROM users WHERE user_name = @u";
                    cmd.Parameters.AddWithValue("@u", username);
                    using (MySqlDataReader reader = cmd.ExecuteReader()) {
                        reader.Read();
                        if (reader.HasRows && (string)reader["password_hash"] == passwordHash) userID = (long)reader["user_id"];
                        else userID = -1;
                    }
                }
            }

            return userID;
        }

        public static bool CheckIfRoomExists(string roomName) {
            bool result;
            using (MySqlConnection conn = new MySqlConnection(connectionString)) {
                using (MySqlCommand cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "SELECT COUNT(room_id) FROM rooms WHERE room_name = @u";
                    cmd.Parameters.AddWithValue("@u", roomName);
                    result = (int)cmd.ExecuteScalar() > 0;
                }
            }

            return result;
        }

        public static long AddNewRoom(long roomCreator, string roomName, byte usersLimit, string description) {
            long insertedID;
            using (MySqlConnection conn = new MySqlConnection(connectionString)) {
                using (MySqlCommand cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "INSERT INTO rooms (room_creator, room_name, description, users_limit) values (@c, @u, @p, @l)";
                    cmd.Parameters.AddWithValue("@c", roomCreator);
                    cmd.Parameters.AddWithValue("@u", roomName);
                    cmd.Parameters.AddWithValue("@p", description);
                    cmd.Parameters.AddWithValue("@l", usersLimit);
                    cmd.ExecuteNonQuery();
                    insertedID = cmd.LastInsertedId;
                }
            }

            return insertedID;
        }

        public static bool DeleteRoom(long roomID) {
            bool result;
            using (MySqlConnection conn = new MySqlConnection(connectionString)) {
                using (MySqlCommand cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "DELETE FROM rooms WHERE room_ID = @r";
                    cmd.Parameters.AddWithValue("@r", roomID);
                    cmd.ExecuteNonQuery();
                    result = true;
                }
            }

            return result;
        }

        public static ConcurrentDictionary<long, Room> GetRooms() {
            ConcurrentDictionary<long, Room> rooms = new ConcurrentDictionary<long, Room>();
            using (MySqlConnection conn = new MySqlConnection(connectionString)) {
                using (MySqlCommand cmd = conn.CreateCommand()) {
                    conn.Open();
                    cmd.CommandText = "SELECT room_id, room_creator, room_name, description, users_limit FROM rooms";
                    using (MySqlDataReader reader = cmd.ExecuteReader()) {
                        while (reader.Read()) {
                            rooms.TryAdd((long)reader["room_id"], new Room((long)reader["room_creator"], (string)reader["room_name"], Convert.ToByte((sbyte)reader["users_limit"]), (string)reader["description"]));
                        }
                    }
                }
            }

            return rooms;
        }
    }
}
