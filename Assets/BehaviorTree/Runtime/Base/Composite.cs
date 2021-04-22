using System;
using System.Collections.Generic;
using System.Linq;

namespace Archi.BT
{
    public abstract class Composite : Node
    {
        protected int currentChildIndex = 0;

        protected Composite(string name, params Node[] nodes)
        {
            Name = name;
            ChildNodes.AddRange(nodes);
        }

        protected void ShuffleNodes() => ChildNodes = ChildNodes.OrderBy(g => Guid.NewGuid()).ToList();

        protected void WeightedShuffleNodes(List<(int, Node)> nodeWeights)
        {
            // get node list copy
            List<(int, Node)> availableNodes = new List<(int, Node)>(nodeWeights.OrderBy(g => Guid.NewGuid()));

            // new child list
            List<NodeBase> newChildList = new List<NodeBase>();

            // build new list
            while(availableNodes.Count > 0)
            {
                foreach((int, Node) node in availableNodes)
                {
                    if(GameManager.rngFree.Range(0, availableNodes.Sum(w => w.Item1)) < node.Item1)
                    {
                        newChildList.Add(node.Item2);
                        availableNodes.Remove(node);
                        break;
                    }
                }
            }

            ChildNodes = newChildList.ToList();
        }
    }
}
