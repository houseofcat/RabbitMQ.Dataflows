using System.Net.Security;
using System.Security.Authentication;

namespace HouseofCat.RabbitMQ;

public class SslOptions
{
    /// <summary>
    /// RabbitMQ option to enable SSL.
    /// <para>Set Cf_RabbitPort to 5671 as well as enabling this.</para>
    /// <para>To configure client and server: http://www.rabbitmq.com/ssl.html#configuring-dotnet</para>
    /// </summary>
    public bool EnableSsl { get; set; }

    /// <summary>
    /// RabbitMQ to set the Certificate Server Name.
    /// </summary>
    public string CertServerName { get; set; } = string.Empty;

    /// <summary>
    /// RabbitMQ option to set the local file name path of the cert to use for authentication.
    /// </summary>
    public string LocalCertPath { get; set; } = string.Empty;

    /// <summary>
    /// RabbitMQ option to set the password for the local certificate in use.
    /// </summary>
    public string LocalCertPassword { get; set; } = string.Empty;

    /// <summary>
    /// RabbitMQ option to allow the following acceptable policy errors (if any). Not Readable by Json Config yet.
    /// </summary>
    public SslPolicyErrors AcceptedPolicyErrors { get; set; } = SslPolicyErrors.RemoteCertificateNotAvailable | SslPolicyErrors.RemoteCertificateNameMismatch;

    /// <summary>
    /// RabbitMQ option to specify which secure SSL protocols to use/allow. Not Readable by Json Config yet.
    /// <para>Recommend Tls12+ as the most recent/secure protocol.</para>
    /// </summary>
    public SslProtocols ProtocolVersions { get; set; } = SslProtocols.Tls12 | SslProtocols.Tls13;
}
