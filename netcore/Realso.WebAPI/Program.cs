using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Realso.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls(GetUseUrls());

        private static string GetUseUrls()
        {
            // 支持通过环境变量 ASPNETCORE_PORT 指定端口，默认 5001
            // 使用 0.0.0.0 绑定所有 IPv4 地址，避免 localhost 的 IPv6 冲突
            string port = Environment.GetEnvironmentVariable("ASPNETCORE_PORT") ?? "5001";
            return "http://0.0.0.0:" + port;
        }
    }
}
