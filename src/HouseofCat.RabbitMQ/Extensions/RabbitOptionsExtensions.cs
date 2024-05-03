using HouseofCat.Utilities;
using HouseofCat.Utilities.Errors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace HouseofCat.RabbitMQ;

public static class RabbitOptionsExtensions
{
    public static RabbitOptions GetRabbitOptions(
        this IConfiguration configuration,
        string configSectionKey = Constants.DefaultRabbitOptionsConfigKey)
    {
        var options = new RabbitOptions();
        configuration.GetSection(configSectionKey).Bind(options);
        return options;
    }

    public static async Task<RabbitOptions> GetRabbitOptionsFromJsonFileAsync(string fileNamePath)
    {
        var rabbitOptions = await JsonFileReader.ReadFileAsync<RabbitOptions>(fileNamePath);
        Guard.AgainstNull(rabbitOptions, nameof(rabbitOptions));

        return rabbitOptions;
    }
}
