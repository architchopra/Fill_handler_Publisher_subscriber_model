using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tt_net_sdk;

namespace Price_Subs
{
    public struct PriceUpdate
    {

        public FieldsUpdatedEventArgs e { get; }

        public PriceUpdate(FieldsUpdatedEventArgs e)
        {
            //struct type with fieldUpdatedEventArgs directly as variable type
            this.e = e;
        }
    }

}
