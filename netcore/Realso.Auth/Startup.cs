using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Realso.Auth.Binders;
using IdentityServer4.Models;
using IdentityServer4.Test;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Realso.Auth
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
      // Add framework services.
      services.AddMvc()
      .AddJsonOptions(options =>
      {
        //忽略循环引用
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        //不使用驼峰样式的key
        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
      }
      );
      services.AddMvc(options =>
      {
        // add custom binder to beginning of collection
        options.ModelBinderProviders.Insert(0, new HashtableBinderProvider());
      }); ;
      var rsa = RSA.Create(2048);
      services.AddIdentityServer()
      .AddSigningCredential(new RsaSecurityKey(rsa))
      .AddInMemoryIdentityResources(Config.GetIdentityResources())
      .AddInMemoryApiResources(Config.GetApis())
      .AddInMemoryClients(Config.GetClients())
      .AddResourceOwnerValidator<UserValidator>();
      Console.WriteLine("默认时间偏移:" + new JwtBearerOptions().TokenValidationParameters.ClockSkew.Minutes);
      // services.AddIdentityServer()//Ids4服务
      //     .AddDeveloperSigningCredential()
      //     .AddInMemoryIdentityResources(Config.GetIdentityResources())
      //RSA：证书长度2048以上，否则抛异常
      //配置AccessToken的加密证书
      /*
      var rsa = new RSACryptoServiceProvider();
      //从配置文件获取加密证书
      string SigningCredential = "BwIAAACkAABSU0EyAAgAAAEAAQDx8n4eV8oEni/daFAEVKPuDouya6LZ2KEvmIoxC+FrJEZW8kd39GFxoi4IMUi66xomOWgCepxSVTcb0rEKDQ+2+CHmvUyk18fO9+VkUQbyC07qYW9Wt0ZkpftMJkas5AuSQLeFS6h6pfms7/SSO3YZem3Ktl3yn45vlXbh81ggfSnKzBqINlqpqgH2Q54LIieHNGq1P8OJ0cztAoiyvHgSiPxy9h8XgN4wMeYh+eDOhy1Kbl5h9R1e/nftHLsQUE5u306S4k4BypQF8K/vdnC0rqI0STzKc1pF1rbaYl6aehNnUOwRRuhDkLs1XUKHbKVHlsUhpB6w/j8wKiRf7UyoVXcQ5WoJK0qVlp7f6Cj8WQIL5i3WzGr39wZsN1HUmEZNg1HI1xC8ytRBQY7A9w1KT2oBZ9hOgHMYMN2PLhY/cQ5VWCVbmX8NQwfHcCoBQU/H4yXOfbSG1C5OPxAs+rjbTvDnUhKzaA1FbSaX4oxD7iS4medzAW7k87ulLiFaz9QtFZDdjxHzi3mUTgAlPXOAT1YdB7MZoL0T+ns5Y4B0ap2CGE78s3E6W6ZeYBUSIcC5AAg6Nyim5406LR5tIDaGSo8x0oAGnu8LmHHtyW/5BjFiVkgdMhAe/MHllyv5M/xSjOlXho+uaVCbFSith3ZkKVo75CV/uOWYbmOCORB1ylE614VZSL9Ohn8vTTGnZfBbZl4tIhN15D4gpJRFrAsE81iw5SFi9JmG5GgmUH5vlCxbJ9tKRDiZweLtYvuDHoTXw8lK40VCEqwxkH2NWWrOM5ji9Iqq0NF4HmBHGq6G9ZhPkjwrWl0xC1/l5DfWeRaqCvHyKintsEXz0CMPwzyASfQyy5zZylZ5sVWxUlm3WFT9hJbgjIDTbQm9XbLz4Qa7wDBuMEzVnTyI83+W+TGhF77WrX9NhXfB6iUZukoNph/KE6aAX4JahbceK8JMDTlgaTzI6xhIK/jyOE0anJL/6GUboC3mN50kVTBcg4JvYrL/RQbrvJZEnMaoiOUm+0OK/uOYBdPdn8aZjr7f6NavQa6DU7c5BgN0wgfV/YmsaX9ZtyBc3hWhm9WfKK9XfGPw6brgHCB/rMk86ZnwrGGygUhRV6b2DI8DmxiYrJMnoZxd+BKRBA1CL9nnYmw8GC5LBM6tSgkcr3NWe+ecBLP6hEdTTO5Z+tlQkYiDEbkMjpUXs1ffZd9nIG/xyiK2JiaDP2q0mEKdVnjcwBVl8Jx7ak8sohTECwMCUGdShlL2HgQ9WELa7r8FmAEtfXTMrs47g4QzQk2qG4r5xNdK4QQgNPRZVuX8wbVuMYwk+R7QeuKHbFD3EV9Jej0IXNDbrAPvIJLcu8rCcEAs2DHzNspsWxBjW74WZKCuTPvdePPYqvy8LFo550j0szI3uEQMW2knVFOWOzTBR9UkbqOwjMmcg8Mis4/jMIOQvqs8gT67mjlx60FXjzu9M5FAzOxGKXP68WFuVQ7VUk8bCGyZQNZplC7HBEhRNIDb8k4ylk2fzYX/S6WKIFmqqVb8AfXhphY=";
      rsa.ImportCspBlob(Convert.FromBase64String(SigningCredential));
      //IdentityServer4授权服务配置
      services.AddIdentityServer()
          //.AddTemporarySigningCredential()    //测试的时候可使用临时的证书
          .AddInMemoryIdentityResources(Config.GetIdentityResources())
          .AddInMemoryClients(Config.GetClients())
          .AddSigningCredential(new RsaSecurityKey(rsa))  //设置加密证书
                                                          //如果是client credentials模式那么就不需要设置验证User了
          .AddResourceOwnerValidator<UserValidator>() //User验证接口
                                                      //.AddInMemoryUsers(OAuth2Config.GetUsers())    //将固定的Users加入到内存中
          ; */
      //配置跨域处理
      //将此段代码置于该方法最前面
      services.AddCors(options => {
          options.AddPolicy("AllowHeaders", builder =>
          {
              builder.SetIsOriginAllowed(origin => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
          });
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }

      //app.UseHttpsRedirection();
      app.UseCors("AllowHeaders");
      app.UseIdentityServer();
      app.UseMvc();
    }
  }
}
