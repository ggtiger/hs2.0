using System.Diagnostics.Tracing;
using System.Reflection.Emit;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Realso.Data.ORM;
using System.Web.Http;
using Microsoft.AspNetCore.Cors;
using Realso.Core.Base;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using IdentityModel.Client;

namespace Realso.Auth.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class UserController : BaseControl
  {
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IClientStore _clientStore;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IEventService _events;

    public UserController(IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events
            )
    {
      _interaction = interaction;
      _clientStore = clientStore;
      _schemeProvider = schemeProvider;
      _events = events;
    }

    // POST api/values
    [HttpPost("login")]
    [EnableCors("AllowHeaders")]
    public async Task<IActionResult> Login([FromForm] string USERNAME, [FromForm] string PASSWORD, string returnUrl = null)
    {
      BaseModel qview = this.GetModel("KEY", "VSS_USER");
      QueryInfo queryInfo = new QueryInfo();
      queryInfo.FilterCode = "F02";
      USERNAME = JsonConvert.DeserializeObject<String>(USERNAME);
      PASSWORD = JsonConvert.DeserializeObject<String>(PASSWORD);
      queryInfo.FilterParams["USERNAME"] = USERNAME;
      queryInfo.FilterParams["PASSWORD"] = PASSWORD;
      qview.Open(queryInfo);
      if (qview.GetView().Count == 0)
      {
        responseModel.SetData(false);
        return this.doResponse();
      }
      qview.SetValue("PASSWORD", "");
      string USERID = qview.GetValue("ID");
      var client = new HttpClient();
      var disco = await client.GetDiscoveryDocumentAsync("http://localhost:5000");
      var tokenResponse = await client.RequestPasswordTokenAsync(new PasswordTokenRequest
      {
        Address = disco.TokenEndpoint,
        ClientId = "ro.client",
        ClientSecret = "secret",
        UserName = USERNAME,
        Password = PASSWORD,
        Scope = "api1"
      });
      if (tokenResponse.IsError)
      {
        Console.WriteLine(tokenResponse.Error);
        responseModel.SetFailed(tokenResponse.Error);
        return this.doResponse();
      }
      Hashtable ret = new Hashtable();
      ret["userInfo"] = qview.GetView()[0];
      ret["token"] = tokenResponse.Json;
      responseModel.SetData(ret);
      AuthenticationProperties props = new AuthenticationProperties
      {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(1))
      };
      //await HttpContext.SignInAsync(USERID, USERNAME, props);
      return this.doResponse();
    }

    [HttpPost("loginout")]
    [EnableCors("AllowHeaders")]
    public async Task<IActionResult> LoginOut(string returnUrl = null)
    {
      var client = new HttpClient();
      await HttpContext.SignOutAsync();
      return this.doResponse();
    }
  }
}
