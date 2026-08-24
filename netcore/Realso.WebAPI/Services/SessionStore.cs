using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Realso.Data.DBAccess;

namespace Realso.WebAPI.Services
{
  public class AssistantMessage
  {
    public string Role;
    public string Content;
  }

  public class AssistantSession
  {
    public string ConversationId;
    public List<AssistantMessage> Messages = new List<AssistantMessage>();
    // 完整LLM消息历史（含tool_calls/tool results，内存缓存，供下次对话恢复上下文）
    public List<object> FullMessages = null;
  }

  /// <summary>
  /// 会话持久化抽象。生产用 DbConversationRepo（DBHelper），
  /// 测试用内存 fake。崩溃后从 DB 重建消息历史的能力 M2 补（LoadFromDb）。
  /// </summary>
  public interface IConversationRepo
  {
    string Create(string userId, string userName);
    void AppendMessage(string conversationId, string role, string content, string blocksJson);
    // List<AssistantMessage> LoadFromDb(string conversationId);  // M2：崩溃重建
  }

  /// <summary>
  /// 会话上下文：内存缓存（热数据，循环内读写）+ DB 持久（IConversationRepo）。
  /// 同步策略：用户消息/工具结果立即落库，助理最终回答 AddAssistant 时落库。
  /// </summary>
  public class SessionStore
  {
    private static readonly ConcurrentDictionary<string, AssistantSession> _cache = new ConcurrentDictionary<string, AssistantSession>();
    private readonly IConversationRepo _repo;

    public SessionStore(IConversationRepo repo = null) { _repo = repo; }

    public string Create(string userId, string userName)
    {
      string id = _repo != null ? _repo.Create(userId, userName) : Guid.NewGuid().ToString("N");
      _cache[id] = new AssistantSession { ConversationId = id };
      return id;
    }

    public AssistantSession Load(string conversationId)
    {
      return _cache.GetOrAdd(conversationId, k => new AssistantSession { ConversationId = k });
    }

    public void AddUser(string conversationId, string content)
    {
      Load(conversationId).Messages.Add(new AssistantMessage { Role = "user", Content = content });
      _repo?.AppendMessage(conversationId, "user", content, null);
    }

    public void AddAssistant(string conversationId, string content)
    {
      Load(conversationId).Messages.Add(new AssistantMessage { Role = "assistant", Content = content });
      _repo?.AppendMessage(conversationId, "assistant", content, null);
    }

    // 保存完整LLM消息历史（含tool_calls/tool results，跳过system）
    public void SaveFullMessages(string conversationId, List<object> messages)
    {
      var session = Load(conversationId);
      // 跳过system（每次BuildLlmMessages重新加新system）
      session.FullMessages = messages.Count > 1 ? messages.GetRange(1, messages.Count - 1) : new List<object>();
    }
  }

  /// <summary>生产用：DBHelper 持久化会话与消息</summary>
  public class DbConversationRepo : IConversationRepo
  {
    public string Create(string userId, string userName)
    {
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        var id = Guid.NewGuid().ToString("N");
        helper.Execute(
          @"INSERT INTO TBS_ASSISTANT_CONVERSATION (ID,USERID,USERNAME,TITLE,CREATETIME,UPDATETIME,ISDELETED)
            VALUES (@ID,@UID,@UN,NULL,NOW(),NOW(),0)",
          new { ID = id, UID = userId, UN = userName });
        return id;
      }
    }

    public void AppendMessage(string conversationId, string role, string content, string blocksJson)
    {
      DBHelper helper = DB.GetDBHelper();
      using (helper)
      {
        helper.Execute(
          @"INSERT INTO TBS_ASSISTANT_MESSAGE (ID,CONVERSATIONID,ROLE,CONTENT,BLOCKSJSON,CREATETIME,ISDELETED)
            VALUES (@ID,@CID,@ROLE,@C,@B,NOW(),0)",
          new { ID = Guid.NewGuid().ToString("N"), CID = conversationId, ROLE = role, C = content, B = blocksJson });
      }
    }
  }
}
