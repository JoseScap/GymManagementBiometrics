using Newtonsoft.Json;
using System.Collections.Generic;
using GymManagementBiometrics.Messages;
using System.Windows.Forms;

namespace GymManagementBiometrics.Messages
{
    public class MessageHandler
    {
        public void HandleBioPing(SocketIOClient.SocketIO client, object data)
        {
            var dataAsString = data.ToString();
            List<PingMessage> deserializedData = JsonConvert.DeserializeObject<List<PingMessage>>(dataAsString);

            client.EmitAsync("Bio:Pong", deserializedData[0]);
        }

        public void HandleBioChangeStatus(
        SocketIOClient.SocketIO client,
        object data,
        ref bool isRegister,
        ref TextBox statusTextBox)
        {
            var dataAsString = data.ToString();
            List<ChangeStatusMessage> deserializedData = JsonConvert.DeserializeObject<List<ChangeStatusMessage>>(dataAsString);
            var value = deserializedData[0].Value;

            isRegister = value;
            statusTextBox.Text = isRegister ? "Registrando" : "Identificando";
        }
    }
}
