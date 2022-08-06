using Autofac.Extensions.DependencyInjection;
using Demkin.Blog.Utils.Help;
using Demkin.Blog.Utils.SystemConfig;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Serilog;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Demkin.Blog.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                IConfigurationRoot config = null;
                if (environment == "Development")
                {
                    config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile(AppDomain.CurrentDomain.BaseDirectory + $"\\appsettings.{environment}.json", optional: true, reloadOnChange: true).Build();
                }
                else
                {
                    config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile(AppDomain.CurrentDomain.BaseDirectory + "\\appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile(AppDomain.CurrentDomain.BaseDirectory + $"\\appsettings.{environment}.json", optional: true, reloadOnChange: true)
                        .Build();
                }

                // 配置文件热更新
                ChangeToken.OnChange(() => config.GetReloadToken(), () =>
                {
                    //Console.WriteLine(SiteInfo.IsDebugSql);
                });

                Log.Logger = new LoggerConfiguration()
                         .ReadFrom.Configuration(config)
                         .CreateLogger();

                var builder = CreateHostBuilder(args);

                // 更换依赖注入容器
                builder.UseServiceProviderFactory(new AutofacServiceProviderFactory());

                // 判断如果是window，可使用服务的方式启动
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Nuget: Microsoft.Extensions.Hosting.WindowsServices
                    builder.UseWindowsService();
                }
                // 修改默认的日志框架
                builder.UseSerilog();

                var host = builder.Build();

                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "启动失败");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls(args.Length == 0 ? $"http://*:8090" : $"http://*:{args[0]}");
                    webBuilder.UseStartup<Startup>();
                });
    }
}