using System.Collections.Generic;
using Realso.WebAPI.Services;
using Xunit;

namespace Realso.Assistant.Test.Assistant
{
  public class SessionStoreTests
  {
    /// <summary>内存 fake repo，仅记录持久化调用，便于断言</summary>
    private class FakeRepo : IConversationRepo
    {
      public int CreateCalls;
      public int AppendCalls;
      public string Create(string userId, string userName) { CreateCalls++; return "conv-fake-001"; }
      public void AppendMessage(string conversationId, string role, string content, string blocksJson) { AppendCalls++; }
    }

    [Fact]
    public void Create_ReturnsId_LoadEmpty()
    {
      var store = new SessionStore(new FakeRepo());
      var id = store.Create("u1", "张三");
      Assert.Equal("conv-fake-001", id);
      Assert.Empty(store.Load(id).Messages);
    }

    [Fact]
    public void AddUser_RetainedInMemory_AndPersisted()
    {
      var repo = new FakeRepo();
      var store = new SessionStore(repo);
      var id = store.Create("u1", "张三");
      store.AddUser(id, "你好");
      Assert.Single(store.Load(id).Messages);
      Assert.Equal("你好", store.Load(id).Messages[0].Content);
      Assert.Equal(1, repo.AppendCalls);   // 持久化被调用
    }

    [Fact]
    public void Load_UnknownId_ReturnsEmptySession()
    {
      var store = new SessionStore(new FakeRepo());
      var s = store.Load("not-exist");
      Assert.Empty(s.Messages);
    }
  }
}
