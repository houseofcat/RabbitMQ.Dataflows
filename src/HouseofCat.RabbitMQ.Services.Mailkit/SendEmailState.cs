using HouseofCat.RabbitMQ.Pipelines;

namespace HouseofCat.RabbitMQ.Services
{
    public class SendEmailState : WorkState
    {
        public SendEmail SendEmail { get; set; }
    }
}
