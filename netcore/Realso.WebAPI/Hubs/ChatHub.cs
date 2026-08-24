using System;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
namespace Realso.WebAPI.Hubs
{
  public class ChatHub : Hub
  {

    public void Send(string body)
    {
      Clients.All.SendAsync("Recv", body);
      Clients.All.SendAsync("Send", "收到 over！");
    }

    public override Task OnConnectedAsync()
    {
      Console.WriteLine("哇，有人进来了：{0}", this.Context.ConnectionId);
      return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
      Console.WriteLine("靠，有人跑路了：{0}", this.Context.ConnectionId);
      return base.OnDisconnectedAsync(exception);
    }

    public class MessageBody
    {
      public int Type { get; set; }
      public string UserName { get; set; }
      public string Content { get; set; }
    }
  }
}
