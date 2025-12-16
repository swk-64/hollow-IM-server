using Hollow_IM_Server.Classes.Models;
using Npgsql;
using System.Text.Json;

namespace Hollow_IM_Server.Classes
{
    internal class DBManager
    {
        private string connString;


        public DBManager(string connString)
        {
            this.connString = connString;

        }

        public async Task<ChatModel> JoinChat(UserModel user)
        {
            List<MessageModel> messages;
            int messages_state;

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();

            string sql = @"
                        INSERT INTO users (username)
                        VALUES (@username)
                        ON CONFLICT (username)
                        DO NOTHING
                        ";

            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("username", user.Username);
                _ = await cmd.ExecuteNonQueryAsync();
            }
            ;

            // Get all messages from the chat

            sql = @"
                        -- get all sent messages
                        SELECT COALESCE(json_agg(row_to_json(m)), '[]'::json)
                        FROM (
                            SELECT json_build_object('username', u.username) AS user, m.sent_at, m.content 
                            FROM messages m 
                            JOIN users u ON m.user_id = u.id
                            ) m; 
                        ";
            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                var result = await cmd.ExecuteScalarAsync();

                messages = JsonSerializer.Deserialize<List<MessageModel>>(result!.ToString()!)!;
            }
            ;

            // Get messages_state

            sql = @"SELECT messages_state FROM chats WHERE id = 1;";
            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                var result = await cmd.ExecuteScalarAsync();

                messages_state = (int)result!;
            }
            ;
            await tx.CommitAsync();

            ChatModel chat = new ChatModel { Me = user, MessagesState = messages_state, Messages = messages, Users = new List<UserModel>() };
            return chat;
        }

        public async Task SendMessage(MessageModel message)
        {
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();

            string sql = @"INSERT INTO messages (user_id, sent_at, content) 
                            VALUES ((SELECT id FROM users WHERE username = @username), @sent_at, @content)
                            ";

            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("username", message.User.Username);
                cmd.Parameters.AddWithValue("sent_at", message.SentAt);
                cmd.Parameters.AddWithValue("content", message.Content);
                _ = await cmd.ExecuteNonQueryAsync();
            }
            ;

            sql = @"UPDATE chats SET messages_state = messages_state + 1;";
            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                _ = await cmd.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();

            return;
        }

        public async Task<SyncChatModel> SyncChat(ClientChatState state)
        {
            int last_messages_state;

            List<MessageModel> messages;

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();
            string sql = @"
                        -- get all sent messages between client's current state and the latest state
                        SELECT COALESCE(json_agg(row_to_json(m)), '[]'::json)
                        FROM (
                            SELECT json_build_object('username', u.username) AS user, m.sent_at, m.content 
                            FROM messages m 
                            JOIN users u ON m.user_id = u.id
                            WHERE m.id >= @client_messages_state
                            ) m; 
                        ";

            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("client_messages_state", state.MessagesState);
                var result = await cmd.ExecuteScalarAsync();

                messages = JsonSerializer.Deserialize<List<MessageModel>>(result!.ToString()!)!;

            }
            ;

            // Get messages_state

            sql = @"SELECT messages_state FROM chats WHERE id = 1;";
            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                var result = await cmd.ExecuteScalarAsync();

                last_messages_state = (int)result!;
            }
            ;
            await tx.CommitAsync();

            SyncChatModel syncChat = new SyncChatModel { LastMessagesState = last_messages_state, MessagesDelta = messages, Users = new List<UserModel>() };
            return syncChat;
        }
    }
}