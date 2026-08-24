using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Realso.Data.ORM
{
    public class TableColumns:IList<TableColumn>
    {
        #region IList<TColumn> 成员

        public int IndexOf(TableColumn item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, TableColumn item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public TableColumn this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region ICollection<TColumn> 成员

        public void Add(TableColumn item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(TableColumn item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(TableColumn[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(TableColumn item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<TColumn> 成员

        public IEnumerator<TableColumn> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable 成员

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
