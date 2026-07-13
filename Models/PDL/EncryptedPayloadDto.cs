namespace ServiceApotheke.API.Models.PDL
{
    public class EncryptedPayloadDto
    {
        public string CiphertextBase64 { get; set; }
        public string IvBase64 { get; set; }
    }
}
