using System.Collections.Generic;
using System.Data;
using Dapper;

namespace Realso.Data.ORM
{
  public class ParamInfo
  {
    public object Value { get; set; }
    public DbType Type { get; set; }
    public ParameterDirection Direction { get; set; }
    public ParamInfo(object value, DbType dbType = DbType.String, ParameterDirection direction = ParameterDirection.Input)
    {
      this.Value = value;
      this.Type = dbType;
      this.Direction = direction;
    }
  }
  public class ExecInfo
  {

    public ExecInfo(string SQL, Dictionary<string, object> Params)
    {
      this.SQL = SQL;
      this.Params = Params;
    }

    public string SQL { get; set; }

    public Dictionary<string, object> Params { get; set; }

    public DynamicParameters parameters = new DynamicParameters();

    public CommandType commandType = CommandType.Text;

    public object GetParameters()
    {

      this.parameters = new DynamicParameters();//建立一个parem对象
      foreach (string key in Params.Keys)
      {
        ParamInfo pinfo = Params[key] as ParamInfo;
        if (pinfo != null)
        {
          if (pinfo.Direction == ParameterDirection.Output)
          {
            parameters.Add("@" + key, pinfo.Value, pinfo.Type, pinfo.Direction);
            this.commandType = CommandType.StoredProcedure;
          }
          else
          {
            parameters.Add("@" + key, pinfo.Value, DbType.String, ParameterDirection.Input);
          }
        }
        else
        {
          parameters.Add("@" + key, Params[key]);
        }
      }
      if (this.commandType == CommandType.StoredProcedure)
      {
        return parameters;
      }
      else
      {
        return Params;
      }
    }

    public void SetParameters()
    {
      if (this.commandType == CommandType.StoredProcedure)
      {
        foreach (string key in Params.Keys)
        {
          ParamInfo pinfo = Params[key] as ParamInfo;
          if (pinfo != null)
          {
            if (pinfo.Direction == ParameterDirection.Output)
            {
              pinfo.Value = parameters.Get<object>("@" + key);
            }
          }
        }
      }
    }
  }
}
