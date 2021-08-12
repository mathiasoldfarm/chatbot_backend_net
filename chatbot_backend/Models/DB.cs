using System;
using System.Threading.Tasks;
using Npgsql;

namespace chatbot_backend.Models {
    public static class DB {
        public static NpgsqlConnection connection;
      
        public static void connect() {
            string databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            Uri databaseUri = new Uri(databaseUrl);
            string[] userInfo = databaseUri.UserInfo.Split(':');

            var builder = new NpgsqlConnectionStringBuilder {
                Host = databaseUri.Host,
                Port = databaseUri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = databaseUri.LocalPath.TrimStart('/'),
                Pooling = true,
                SslMode = SslMode.Require,
                TrustServerCertificate = true,
                MaxPoolSize = 1000
            };

            connection = new NpgsqlConnection(builder.ToString());
            connection.Open();
        }
    }
}
