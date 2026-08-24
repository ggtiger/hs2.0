using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using Realso.WebAPI.Binders;
using Realso.WebAPI.Services;
using Realso.WebAPI.Services.Agent;
using Realso.WebAPI.Services.AiDev;
using System;
namespace Realso.WebAPI
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
      services.AddSignalR();
      services.AddAuthentication("Bearer")
            .AddCookie("Cookies")
            .AddJwtBearer("Bearer", options =>
            {
              options.Authority = Configuration["Auth:Authority"] ?? "http://localhost:5000";
              options.RequireHttpsMetadata = false;
              options.Audience = "api1";
              options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(10);
            });
      services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
      // Add framework services.
      services.AddMvcCore()
       .AddAuthorization()
      //全局配置Json序列化处理
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
      });
      //配置跨域处理
      services.AddCors(options =>
      {
        options.AddPolicy("SignalrCore", set =>
        {
          set.SetIsOriginAllowed(origin => true)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials();
        });
        options.AddPolicy("AllowHeaders", builder =>
              {
                builder.SetIsOriginAllowed(origin => true)
                     .AllowAnyOrigin() //允许任何来源的主机访问
                     .AllowAnyMethod()
                     .AllowAnyHeader();
              });
      });
      services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
      services.Configure< Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = int.MaxValue;// 60000000;
                options.MultipartHeadersLengthLimit = int.MaxValue;
            });
      // 智能助理服务注册
      // 统一 LLM 客户端（合并 DeepSeek+Vision，单例 HttpClient）
      services.AddSingleton(new LlmClient(new HttpClient()));
      // Agent 标准框架（阶段 1 基建：AgentEngine/DevAgentEngine/ToolRegistry）
      services.AddSingleton(sp => new AgentEngine(sp.GetRequiredService<LlmClient>()));
      services.AddSingleton(sp => new DevAgentEngine(sp.GetRequiredService<LlmClient>()));
      services.AddSingleton(sp => new AssistantAgentEngine(sp.GetRequiredService<LlmClient>()));
      services.AddSingleton<IToolRegistry, ToolRegistry>();
      services.AddScoped(sp => new LlmConfigService(Configuration["Assistant:AesKey"]));
      services.AddScoped<IConversationRepo, DbConversationRepo>();
      services.AddScoped<SessionStore>();
      services.AddScoped<UsageLogger>();
      services.AddMemoryCache();
      services.AddScoped<PromptService>();
      // 注册硬编码提示词默认值（表里没数据时兜底）
      PromptDefaults.Register();
      // 内置工具清单同步到 tss_ai_tool(EXECUTORTYPE=builtin, 配置中心可见完整清单)
      BuiltinToolSync.SyncAll();

      // AI 开发助理服务注册（Chunk 3）
      // ChangeSetEngine/ChangeSetExporter 无状态，用 Scoped；AiDevOrchestrator 依赖前两者
      services.AddScoped<ChangeSetEngine>();
      services.AddScoped<ChangeSetExporter>();
      services.AddScoped<AiDevOrchestrator>();
      // 模块向导分步生成编排器（复用 DeepSeekClient + LlmConfigService + ChangeSetEngine）
      services.AddScoped<WizardStepOrchestrator>();
      // Chunk 4: 升级执行器（无状态，Scoped）
      services.AddScoped<UpgradeExecutor>();
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
      app.UseAuthentication();
      app.UseCors("SignalrCore");
      app.UseSignalR(routes =>
            {
              routes.MapHub<Hubs.ChatHub>("/chatHub");
              routes.MapHub<Hubs.AssistantHub>("/assistantHub");
            });
      app.UseMvc();
    }
  }
}
