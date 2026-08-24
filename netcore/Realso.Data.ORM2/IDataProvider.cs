using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Realso.Data.ORM
{
    public interface IDataProvider
    {
        public void Query(DataView view,IQuery query);
        public void FillDataByJson(DataView view,String strJson);
        public void Save();
    }
}
