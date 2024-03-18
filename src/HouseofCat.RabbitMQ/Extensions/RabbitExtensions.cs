using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ;

public static class RabbitExtensions
{
    public static RabbitOptions GetRabbitOptions(this IConfiguration configuration, string configSectionKey = "RabbitOptions")
    {
        var options = new RabbitOptions();
        configuration.GetSection(configSectionKey).Bind(options);
        return options;
    }

    public static async Task<RabbitOptions> GetRabbitOptionsFromJsonFileAsync(string fileNamePath)
    {
        if (!File.Exists(fileNamePath))
        {
            throw new FileNotFoundException(fileNamePath);
        }

        var rabbitOptionsJson = await File.ReadAllTextAsync(fileNamePath);
        Guard.AgainstNullOrEmpty(rabbitOptionsJson, nameof(rabbitOptionsJson));

        return JsonSerializer.Deserialize<RabbitOptions>(rabbitOptionsJson);
    }
}
