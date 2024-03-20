namespace HouseofCat.RabbitMQ;

public class OAuth2Options
{
    public string TokenEndpointUrl { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }

    /// <summary>
    /// The OAuth2 Client name to use for distinction (if you use more than one).
    /// </summary>
    public string OAuth2ClientName { get; set; } = "RabbitMQ.Client.OAuth2.Default";
}
