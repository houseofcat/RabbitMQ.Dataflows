using HouseofCat.RabbitMQ.Pipelines;

namespace HouseofCat.RabbitMQ.Services
{
    public class SendEmailState : RabbitWorkState
    {
        public SendEmail SendEmail { get; set; }
    }
}
