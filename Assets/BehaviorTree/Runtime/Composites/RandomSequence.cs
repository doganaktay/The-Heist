using System;
using System.Collections.Generic;
using System.Linq;

namespace Archi.BT
{
    public class RandomSequence : Sequence
    {
        public RandomSequence(string name, params Node[] childNodes) : base(name, childNodes.OrderBy(g => Guid.NewGuid()).ToArray()) { }
    }
}