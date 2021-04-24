using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Archi.IO
{
    public class SaveData
    {
        //explicit data


        // GO Dictionary with GUID keys
        private Dictionary<string, object> saveDict = new Dictionary<string, object>();

        public void Add(string guid, object obj)
        {
            if (!saveDict.ContainsKey(guid))
                saveDict.Add(guid, obj);
            else
                saveDict[guid] = obj;
        }

        public void Remove(string guid)
        {
            if (saveDict.ContainsKey(guid))
                saveDict.Remove(guid);
        }
    }

}

