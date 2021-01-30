using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Archi.BT
{
    public class WeightedRandomSelector : Selector
    {
        private List<(int, Node)> nodeWeights = new List<(int, Node)>();

        public WeightedRandomSelector(string name, int[] weights, params Node[] childNodes) : base(name, childNodes)
        {
            BuildWeights(name, weights, childNodes);
            WeightedShuffleNodes(nodeWeights);
        }

        protected override void OnReset()
        {
            base.OnReset();
            WeightedShuffleNodes(nodeWeights);
        }

        private void BuildWeights(string name, int[] weights, params Node[] childNodes)
        {
            if (weights.Length != childNodes.Length)
            {
                Debug.LogError($"Wrong weight count for {name}, setting equal weights");
                for (int i = 0; i < childNodes.Length; i++)
                {
                    nodeWeights.Add((1, childNodes[i]));
                }
            }
            else
            {
                for (int i = 0; i < weights.Length; i++)
                {
                    nodeWeights.Add((weights[i], childNodes[i]));
                }
            }
        }
    }
}
