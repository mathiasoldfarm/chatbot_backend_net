using System;
using Npgsql;

namespace chatbot_backend.Controllers.Utils {
    public static class Unpacker {
        public static string UnpackDBStringValue(int index, NpgsqlDataReader reader) {
            if (reader[index] == DBNull.Value) {
                return "";
            }
            return (string)reader[index];
        }

        public static int UnpackDBIntValue(int index, NpgsqlDataReader reader) {
            if (reader[index] == DBNull.Value) {
                return -1;
            }
            return (int)reader[index];
        }

        public static bool UnpackDBBoolValue(int index, NpgsqlDataReader reader) {
            if (reader[index] == DBNull.Value) {
                return false;
            }
            return (bool)reader[index];
        }
    }
}
