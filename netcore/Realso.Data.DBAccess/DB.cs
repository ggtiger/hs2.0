using System;
using Realso.Utils;

namespace Realso.Data.DBAccess
{
  public class DB
  {
    public static DBHelper GetDBHelper()
    {
      string connectionStr = ConfigHelper.GetConfig("ConnectionStrings:D0001");
      return new DBHelper(connectionStr, Providers.MySQL);
    }
  }
}
