using System.IO;
using Microsoft.Extensions.Configuration;

namespace Realso.Utils
{
  public class ConfigHelper
  {
    private static readonly object objLock = new object();
    private static ConfigHelper instance = null;

    private IConfigurationRoot Config { get; }

    private ConfigHelper()
    {
      var builder = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
          .AddEnvironmentVariables();
      Config = builder.Build();
    }
    public static ConfigHelper GetInstance()
    {
      if (instance == null)
      {
        lock (objLock)
        {
          if (instance == null)
          {
            instance = new ConfigHelper();
          }
        }
      }
      return instance;
    }
    public static string GetConfig(string name)
    {
      return GetInstance().Config.GetSection(name).Value;
    }
  }
}
