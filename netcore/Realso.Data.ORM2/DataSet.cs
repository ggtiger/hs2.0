using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Realso.Data.ORM
{
    public class DataSet
    {
        public IDictionary<string,DataView> Views
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
            }
        }

        public IQuery IQuery
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
            }
        }

        public IList<ViewRelation> Relations
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
            }
        }

        public IViewOperate IOperate
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
            }
        }

        public void AddView()
        {
            throw new System.NotImplementedException();
        }

        public void RemoveView()
        {
            throw new System.NotImplementedException();
        }

        public void FillData()
        {
            throw new System.NotImplementedException();
        }

        public void Save()
        {
            throw new System.NotImplementedException();
        }

        public void Open()
        {
            throw new System.NotImplementedException();
        }
    }
}
