using IdentityServer4.Models;
using IdentityServer4.Validation;
using Realso.Data.DBAccess;
using Realso.Utils;
using System;
using System.Threading.Tasks;

namespace Realso.Auth
{
  public class UserValidator : IResourceOwnerPasswordValidator
  {
    public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
      var db = DB.GetDBHelper();
      var row = db.QueryFirstOrDefault(
        "SELECT ID, PASSWORD, ISUSE FROM TSS_USER WHERE USERNAME=@USERNAME",
        new { USERNAME = context.UserName });
      bool ok = false;
      if (row != null && Convert.ToInt32(row.ISUSE ?? 0) == 1)
      {
        string stored = row.PASSWORD;
        if (!string.IsNullOrEmpty(stored) && stored.Contains("$"))
        {
          ok = PasswordHelper.VerifyPassword(context.Password, stored);
        }
        else
        {
          // 存量明文密码：比对成功则自动升级为哈希存储
          ok = stored == context.Password;
          if (ok)
          {
            db.Execute("UPDATE TSS_USER SET PASSWORD=@PASSWORD WHERE ID=@ID",
              new { PASSWORD = PasswordHelper.HashPassword(context.Password), ID = (string)row.ID });
          }
        }
      }
      context.Result = ok
        ? new GrantValidationResult(subject: context.UserName, authenticationMethod: "custom")
        : new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid custom credential");
      return Task.FromResult(0);
    }
  }
}
