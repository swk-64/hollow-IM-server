using Hollow_IM_Server.Classes;
using Npgsql;
using System.Net;

// CONFIG
const string connString = "Host=localhost;Port=5432;Database=hollow_im;Username=postgres;Password=password";
const string pkcs12Password = "password";
const int port = 25566;
const string address = "127.0.0.1";


if (args.Length == 0)
{
    Console.WriteLine("No task argument provided.");
    return;
}

switch (args[0])
{
    case "runserver":
        {
            HollowServer server = new HollowServer(port, address, connString, pkcs12Password);

            await server.StartListeningAsync();

            break;
        }
    case "makedb":
        {
            using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            string sql = @"
                                    -- Users
                                    CREATE TABLE users (
                                        id SERIAL PRIMARY KEY,
                                        username VARCHAR(100) NOT NULL UNIQUE
                                    );

                                    -- Single Chat (only one row will exist)
                                    CREATE TABLE chats (
                                        id SERIAL PRIMARY KEY,
                                        messages_state INT NOT NULL DEFAULT 1
                                    );

                                    -- Messages
                                    CREATE TABLE messages (
                                        id SERIAL PRIMARY KEY,
                                        user_id INT NOT NULL REFERENCES users(id),
                                        sent_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                        content TEXT NOT NULL
                                    );

                                    --Insert Single Chat row
                                    INSERT INTO chats DEFAULT VALUES;
                                    ";
            using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
            Console.WriteLine("Database structure was successfully constructed !!!");
            break;
        }
    case "cleardb":
        {
            using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            string sql = "DROP SCHEMA public CASCADE; CREATE SCHEMA public;";
            using var cmd = new NpgsqlCommand(sql, conn, tx);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
            Console.WriteLine("Database structure was successfully removed !!!");
            break;
        }
}