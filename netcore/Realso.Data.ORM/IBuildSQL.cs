using Realso.Data.ORM.Core;

namespace Realso.Data.ORM
{
  public interface IBuildSQL
  {
    string BuildQuery(Resource resource, QueryInfo queryInfo);
    string BuildSave(DataView view);
    string BuildInsert(DataView view, ViewRow row);
    string BuildBatchInsert(DataView view);
    string BuildDelete(DataView view, ViewRow row);
    string BuildUpdate(DataView view, ViewRow row);
  }
}
