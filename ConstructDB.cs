using Hollow_IM_Server.Classes;
using Hollow_IM_Server.Classes.Models;
using Npgsql;

class Programm
{
    public static async Task Main(string[] args)
    {
        string connString = "Host=localhost;Port=5432;Database=hollow_im;Username=postgres;Password=1o52!Rv";

        if (args.Length == 0)
        {
            Console.WriteLine("No task argument provided.");
            return;
        }

        switch (args[0])
        {
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
            case "test1":
                {
                    using var conn = new NpgsqlConnection(connString);
                    await conn.OpenAsync();
                    UserModel user = new UserModel { Username = "creeper" };

                    DBManager manager = new DBManager(connString);

                    var result = await manager.JoinChat(user);
                    Console.WriteLine(result);
                    break;
                }
            case "test2":
                {
                    using var conn = new NpgsqlConnection(connString);
                    await conn.OpenAsync();
                    UserModel user = new UserModel { Username = "creeper" };

                    DateTime now = DateTime.Now;

                    string content = "PPSSSSSHHHHHH!!";

                    MessageModel message = new MessageModel { Content = content, SentAt = now, User = user };

                    DBManager manager = new DBManager(connString);

                    var result = await manager.SendMessage(message);
                    Console.WriteLine(result);
                    break;
                }
            case "test3":
                {
                    using var conn = new NpgsqlConnection(connString);
                    await conn.OpenAsync();
                    UserModel user = new UserModel { Username = "swk64" };

                    DBManager manager = new DBManager(connString);

                    var result = await manager.JoinChat(user);
                    Console.WriteLine(result);
                    break;
                }
            case "test4":
                {
                    using var conn = new NpgsqlConnection(connString);
                    await conn.OpenAsync();
                    UserModel user = new UserModel { Username = "swk64" };

                    DateTime now = DateTime.Now;

                    string content = "Heeeeeey!";

                    MessageModel message = new MessageModel { Content = content, SentAt = now, User = user };

                    DBManager manager = new DBManager(connString);

                    var result = await manager.SendMessage(message);
                    Console.WriteLine(result);
                    break;
                }
            case "test5":
                {
                    using var conn = new NpgsqlConnection(connString);
                    await conn.OpenAsync();
                    ClientChatState state = new ClientChatState { MessagesState = 3, UsersState = 2 };

                    DBManager manager = new DBManager(connString);

                    var result = await manager.SyncChat(state);
                    Console.WriteLine(result);
                    break;
                }
            case "test6":
                {
                    using var conn = new NpgsqlConnection(connString);
                    await conn.OpenAsync();
                    UserModel user = new UserModel { Username = "swk64" };

                    DateTime now = DateTime.Now;

                    string content = "OH, nooooo!";

                    MessageModel message = new MessageModel { Content = content, SentAt = now, User = user };

                    DBManager manager = new DBManager(connString);

                    var result = await manager.SendMessage(message);
                    Console.WriteLine(result);
                    break;
                }
            case "test7":
                {
                    using var conn = new NpgsqlConnection(connString);
                    await conn.OpenAsync();
                    UserModel user = new UserModel { Username = "swk64" };

                    DBManager manager = new DBManager(connString);

                    await manager.LeaveChat(user);
                    //Console.WriteLine(result);
                    break;
                }
            default:
                {
                    Console.WriteLine("something went wrong!");
                    break;
                }
        }
        return;
    }
}