using IdentityServer4.Models;
using IdentityServer4.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Realso.Auth
{
  public class UserValidator : IResourceOwnerPasswordValidator
  {
    public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
      /*
      if (context.UserName == "admin" && context.Password == "admin")
      {
        context.Result = new GrantValidationResult(subject: "admin", authenticationMethod: "custom");
      }
      else
      {
        context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid custom credential");
      }*/
      context.Result = new GrantValidationResult(subject: context.UserName, authenticationMethod: "custom");
      return Task.FromResult(0);
    }
  }
}
