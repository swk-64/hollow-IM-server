using Hollow_IM_Server.Classes.Models;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;

namespace Hollow_IM_Server.Classes
{
    internal class ConnectedClient
    {
        public Guid Id { get; } = Guid.NewGuid();

        public required TcpClient TcpClient { get; set; }

        public required UserModel? User { get; set; }
    }
}
