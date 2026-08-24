using System;
using log4net;
using System.IO;
using log4net.Config;

[assembly: log4net.Config.XmlConfigurator(ConfigFileExtension = "config", Watch = true)]
namespace Realso.Utils
{
  public class Logger
  {
    private static ILog logger;
    static Logger()
    {
      if (logger == null)
      {
        var repository = LogManager.CreateRepository("NETCoreRepository");
        //log4net从log4net.config文件中读取配置信息
        XmlConfigurator.Configure(repository, new System.IO.FileInfo("log4net.config"));
        logger = LogManager.GetLogger(repository.Name, "InfoLogger");
      }
    }

    /// <summary>
    /// 普通日志
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    public static void Info(string message, Exception exception = null)
    {
      if (exception == null)
        logger.Info(message);
      else
        logger.Info(message, exception);
    }

    /// <summary>
    /// 告警日志
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    public static void Warn(string message, Exception exception = null)
    {
      if (exception == null)
        logger.Warn(message);
      else
        logger.Warn(message, exception);
    }

    /// <summary>
    /// 错误日志
    /// </summary>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    public static void Error(string message, Exception exception = null)
    {
      if (exception == null)
        logger.Error(message);
      else
        logger.Error(message, exception);
    }
  }
}
