using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;

namespace Archi.BT.Visualizer
{
    public class BTGStackNodeData : StackNode
    {
        public int ColumnId;
        public List<BTGNodeData> childNodes = new List<BTGNodeData>();

    }
}
