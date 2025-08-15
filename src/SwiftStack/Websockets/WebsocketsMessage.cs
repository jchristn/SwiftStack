namespace SwiftStack.Websockets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using WatsonWebsocket;

    /// <summary>
    /// Websockets message.
    /// </summary>
    public class WebsocketsMessage
    {
        #region Public-Members

        /// <summary>
        /// Websocket payload type.
        /// </summary>
        public WebSocketMessageType Payload { get; set; } = WebSocketMessageType.Text;

        /// <summary>
        /// GUID.
        /// </summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Sender GUID.
        /// </summary>
        public Guid? Sender { get; set; } = null;

        /// <summary>
        /// Conversation GUID.
        /// </summary>
        public Guid? Conversation { get; set; } = null;

        /// <summary>
        /// IP:port of the sender.
        /// </summary>
        public string IpPort
        {
            get
            {
                return Ip + ":" + Port.ToString();
            }
        }

        /// <summary>
        /// IP address of the sender.
        /// </summary>
        public string Ip { get; set; } = null;

        /// <summary>
        /// Port number for the sender.
        /// </summary>
        public int Port { get; set; } = 0;

        /// <summary>
        /// Sender name, if any.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Reply-to GUID.
        /// </summary>
        public Guid? ReplyTo { get; set; } = null;

        /// <summary>
        /// Route by which the message should be handled.
        /// </summary>
        public string Route { get; set; } = null;

        /// <summary>
        /// Message payload.
        /// </summary>
        public ArraySegment<byte> Data { get; set; } = new ArraySegment<byte>(Array.Empty<byte>());

        #endregion

        #region Private-Members

        private WatsonWsServer _Server = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Websockets message.
        /// </summary>
        public WebsocketsMessage()
        {

        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Set the WebSocket server reference for response helper methods.
        /// This is typically called internally by WebsocketsApp.
        /// </summary>
        /// <param name="server">The WebSocket server.</param>
        public void SetWebsocketServer(WatsonWsServer server)
        {
            _Server = server;
        }

        /// <summary>
        /// Send a response back to the client that sent this message.
        /// </summary>
        /// <param name="response">The response to send. Can be a string or an object that will be serialized to JSON.</param>
        /// <returns>Task.</returns>
        public async Task RespondAsync(object response)
        {
            if (_Server == null)
                throw new InvalidOperationException("WebSocket server reference not set. Cannot send response.");

            var clients = _Server.ListClients().ToList();
            var client = clients.FirstOrDefault(c => c.IpPort == this.IpPort);

            if (client != null)
            {
                if (response is string str)
                {
                    await _Server.SendAsync(client.Guid, str);
                }
                else if (response is byte[] bytes)
                {
                    await _Server.SendAsync(client.Guid, bytes, WebSocketMessageType.Binary);
                }
                else
                {
                    // Serialize object to JSON
                    string json = JsonSerializer.Serialize(response);
                    await _Server.SendAsync(client.Guid, json);
                }
            }
            else
            {
                throw new InvalidOperationException($"Client {IpPort} not found. Cannot send response.");
            }
        }

        /// <summary>
        /// Send a response back to the client that sent this message, using a custom WebSocket server instance.
        /// </summary>
        /// <param name="server">The WebSocket server to use.</param>
        /// <param name="response">The response to send.</param>
        /// <returns>Task.</returns>
        public async Task RespondAsync(WatsonWsServer server, object response)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            var clients = server.ListClients().ToList();
            var client = clients.FirstOrDefault(c => c.IpPort == this.IpPort);

            if (client != null)
            {
                if (response is string str)
                {
                    await server.SendAsync(client.Guid, str);
                }
                else if (response is byte[] bytes)
                {
                    await server.SendAsync(client.Guid, bytes, WebSocketMessageType.Binary);
                }
                else
                {
                    // Serialize object to JSON
                    string json = JsonSerializer.Serialize(response);
                    await server.SendAsync(client.Guid, json);
                }
            }
            else
            {
                throw new InvalidOperationException($"Client {IpPort} not found. Cannot send response.");
            }
        }

        /// <summary>
        /// Get the message data as a UTF-8 string.
        /// </summary>
        /// <returns>The data as a string.</returns>
        public string DataAsString()
        {
            if (Data.Array == null || Data.Count == 0)
                return string.Empty;

            return Encoding.UTF8.GetString(Data.Array, Data.Offset, Data.Count);
        }

        /// <summary>
        /// Get the message data as a byte array.
        /// </summary>
        /// <returns>The data as a byte array.</returns>
        public byte[] DataAsBytes()
        {
            if (Data.Array == null || Data.Count == 0)
                return Array.Empty<byte>();

            byte[] result = new byte[Data.Count];
            Array.Copy(Data.Array, Data.Offset, result, 0, Data.Count);
            return result;
        }

        #endregion
    }
}