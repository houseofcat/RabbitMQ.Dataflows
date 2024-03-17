using ConnectivityTests.Tests;
using HouseofCat.Logger;
using Microsoft.Extensions.Logging;

var loggerFactory = LogHelper.CreateConsoleLoggerFactory();
LogHelper.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

await BasicGetTests.RunBasicGetAsync(logger, "./RabbitMQ.BasicGet.json");

logger.LogInformation("Tests complete! Press return to exit....");

Console.ReadLine();
