using System;
using System.Linq;

namespace Archi.BT
{
    public class RandomSelector : Selector
    {
        public RandomSelector(string name, params Node[] childNodes) : base(name, childNodes.OrderBy(g => Guid.NewGuid()).ToArray()) { }
    }
}