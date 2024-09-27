namespace GymManagementBiometrics.Messages
{
    public class PingMessage
    {
        public bool Data { get; set; }
    }

    public class ChangeStatusMessage
    {
        public bool Value { get; set; }
    }
}
