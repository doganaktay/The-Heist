using System;
using System.Collections.Generic;
using System.Linq;

namespace Archi.BT
{
    public class AlwaysRandomSelector : Selector
    {
        public AlwaysRandomSelector(string name, params Node[] childNodes) : base(name, childNodes)
        {
            ShuffleNodes();
        }

        protected override void OnReset()
        {
            base.OnReset();
            ShuffleNodes();
        }
    }
}