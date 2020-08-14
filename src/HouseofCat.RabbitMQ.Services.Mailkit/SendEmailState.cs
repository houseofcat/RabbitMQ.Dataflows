using HouseofCat.RabbitMQ.WorkState;

namespace HouseofCat.RabbitMQ.Services
{
    public class SendEmailState : RabbitWorkState
    {
        public SendEmail SendEmail { get; set; }
    }
}
