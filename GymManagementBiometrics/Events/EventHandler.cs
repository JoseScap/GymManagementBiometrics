using Newtonsoft.Json;
using System.Collections.Generic;
using GymManagementBiometrics.Messages;

namespace GymManagementBiometrics.EventHandler
{
    public static class EventHandler
    {
        public static void HandleBioPing(SocketIOClient.SocketIO client, object data)
        {
            var dataAsString = data.ToString();
            List<PingMessage> deserializedData = JsonConvert.DeserializeObject<List<PingMessage>>(dataAsString);

            client.EmitAsync("Bio:Pong", deserializedData[0]);
        }
    }
}
