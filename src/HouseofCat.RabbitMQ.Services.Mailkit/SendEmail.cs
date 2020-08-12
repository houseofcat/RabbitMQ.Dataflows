namespace HouseofCat.RabbitMQ.Services
{
    public class SendEmail
    {
        public string EmailAddress { get; set; }
        public string EmailBody { get; set; }
        public string EmailSubject { get; set; }
        public bool IsHtml { get; set; }
    }
}
