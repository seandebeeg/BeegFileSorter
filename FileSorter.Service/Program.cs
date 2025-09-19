using FileSorter;
using FileSorter.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var store = new SettingsFileStore();

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureAppConfiguration((context, config) =>
    {
      config.AddJsonFile(store.ConfigPath, optional: true, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
      services.AddSingleton(store);
      services.Configure<FileSortSettings>(context.Configuration.GetSection("FileSort"));
      services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
