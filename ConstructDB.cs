using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hollow_IM_Server
{
    internal class ConstructDB
    {
        static void Main(string connString)
        {
            using var conn = new NpgsqlConnection(connString);
            conn.Open();

            string sql = @"
-- Users
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE
);

-- Single Chat (only one row will exist)
CREATE TABLE chats (
    id SERIAL PRIMARY KEY,
    messages_state INT NOT NULL,
    users_state INT NOT NULL
);

-- Messages
CREATE TABLE messages (
    id SERIAL PRIMARY KEY,
    chat_id INT NOT NULL REFERENCES chats(id) ON DELETE CASCADE,
    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    sent_at TIMESTAMP NOT NULL,
    content TEXT NOT NULL
    messages_state INT NOT NULL,
);

-- Users Delta
CREATE TABLE user_deltas (
    id SERIAL PRIMARY KEY,

    chat_id INT NOT NULL REFERENCES chats(id) ON DELETE CASCADE,
    users_state INT NOT NULL,

    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    change_type BOOLEAN NOT NULL,  -- true = added, false = removed
);
        ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
    }
}
