using ConnectivityTests.Tests;
using HouseofCat.Logger;
using Microsoft.Extensions.Logging;

var loggerFactory = LogHelper.CreateConsoleLoggerFactory();
LogHelper.LoggerFactory = loggerFactory;
var logger = loggerFactory.CreateLogger<Program>();

var channelPool = await Shared.SetupChannelPoolAsync(logger);
await BasicGetTests.RunBasicGetAsync(logger, channelPool);

Console.ReadLine();
