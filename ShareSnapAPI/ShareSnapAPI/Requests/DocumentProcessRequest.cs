namespace ShareSnapAPI.Requests
{
    public class DocumentProcessRequest
    {
        public string Document { get; set; }
        public string SocialNetwork { get; set; }

        public string MailAddress { get; set; } = string.Empty;
    }
}
