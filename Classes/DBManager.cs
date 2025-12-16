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
            List<UserModel> users;
            int messages_state;
            int users_state;

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();

            string sql = @"
                        WITH upsert_user AS (
                            INSERT INTO users (username, is_connected)
                            VALUES (@username, TRUE)
                            ON CONFLICT (username)
                            DO UPDATE SET is_connected = TRUE
                            RETURNING id AS user_id
                        )
                        INSERT INTO user_deltas (user_id, change_type)
                        SELECT user_id, TRUE
                        FROM upsert_user;

                        UPDATE chats SET users_state = users_state + 1;
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

            // Get all connected users from the chat

            sql = @"
                        -- get connected users
                        SELECT COALESCE(json_agg(row_to_json(u)), '[]'::json)
                        FROM (SELECT username FROM users WHERE is_connected = TRUE) u;
                        ";
            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                var result = await cmd.ExecuteScalarAsync();

                users = JsonSerializer.Deserialize<List<UserModel>>(result!.ToString()!)!;
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

            // Get users_state

            sql = @"SELECT users_state FROM chats WHERE id = 1;";
            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                var result = await cmd.ExecuteScalarAsync();

                users_state = (int)result!;
            }
            ;
            await tx.CommitAsync();

            ChatModel chat = new ChatModel { Me = user, MessagesState = messages_state, Messages = messages, UsersState = users_state, Users = users };
            return chat;
        }

        public async Task<MessageModel> SendMessage(MessageModel message)
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

            return message;
        }

        public async Task LeaveChat(UserModel user)
        {
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync();

            string sql = @"
                            WITH existing_user AS (
                                UPDATE users
                                SET is_connected = FALSE
                                WHERE username = @username
                                RETURNING id AS user_id
                            )
                            INSERT INTO user_deltas (user_id, change_type)
                            SELECT user_id, FALSE
                            FROM existing_user;

                            UPDATE chats SET users_state = users_state + 1;
                        ";

            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("username", user.Username);
                _ = await cmd.ExecuteNonQueryAsync();
            }
            ;
            await tx.CommitAsync();
        }

        public async Task<SyncChatModel> SyncChat(ClientChatState state)
        {
            int last_messages_state;
            int last_users_state;

            List<MessageModel> messages;
            List<UserDelta> user_deltas;

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
            sql = @"
                        -- get all user deltas between client's current state and the latest state
                        SELECT COALESCE(json_agg(row_to_json(d)), '[]'::json)
                        FROM (
                            SELECT json_build_object('username', u.username) AS user_to_change, d.change_type
                            FROM user_deltas d 
                            JOIN users u ON d.user_id = u.id
                            WHERE d.id >= @client_users_state
                            ) d; 
                        ";

            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("client_users_state", state.UsersState);
                var result = await cmd.ExecuteScalarAsync();

                user_deltas = JsonSerializer.Deserialize<List<UserDelta>>(result!.ToString()!)!;

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

            // Get users_state

            sql = @"SELECT users_state FROM chats WHERE id = 1;";
            await using (var cmd = new NpgsqlCommand(sql, conn))
            {
                var result = await cmd.ExecuteScalarAsync();

                last_users_state = (int)result!;
            }
            ;
            await tx.CommitAsync();

            SyncChatModel syncChat = new SyncChatModel { LastMessagesState = last_messages_state, MessagesDelta = messages, LastUsersState = last_users_state, UsersDelta = user_deltas };
            return syncChat;
        }
    }
}