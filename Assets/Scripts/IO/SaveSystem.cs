using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Archi.IO
{
    public static class SaveSystem
    {
        #region With SaveData

        // All saving is handled with JSON

        private static readonly string SAVE_PATH = Application.persistentDataPath + "/Save/";

        /// <summary>
        /// Convert SaveData to JSON and write to file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="saveData"></param>
        /// <returns></returns>
        public static void Save(string filePath, SaveData saveData)
        {
            string path = ConstructFullPath(filePath);
            string json = JsonUtility.ToJson(saveData);

            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Retrieve JSON and return SaveData
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static SaveData Load(string filePath)
        {
            string path = ConstructFullPath(filePath);

            if (File.Exists(path))
            {
                string saveData = File.ReadAllText(path);
                return JsonUtility.FromJson<SaveData>(saveData);
            }
            else
                return null;
        }

        #endregion

        #region Direct

        /// <summary>
        /// Save string to path on new line. Append or Overwrite
        /// </summary>
        /// <param name="fullFileName"></param>
        /// <param name="content"></param>
        /// <param name="append"></param>
        /// <returns></returns>
        public static void SaveDirect(string fullFileName, string content, bool append = false)
        {
            string path = ConstructFullPath(fullFileName);

            if (!append || !File.Exists(path))
                File.WriteAllText(path, content);
            else
            {
                using (StreamWriter w = File.AppendText(path))
                {
                    w.WriteLine(content);
                }
            }
        }

        /// <summary>
        /// Save each string to path on new line. Append or Overwrite
        /// </summary>
        /// <param name="fullFileName"></param>
        /// <param name="content"></param>
        /// <param name="append"></param>
        /// <returns></returns>
        public static void SaveDirect(string fullFileName, List<string> content, bool append = false)
        {
            string path = ConstructFullPath(fullFileName);

            if (!append || !File.Exists(path))
            {
                using (StreamWriter w = File.CreateText(path))
                {
                    foreach (var c in content)
                        w.WriteLine(c);
                }
            }
            else
            {
                using (StreamWriter w = File.AppendText(path))
                {
                    foreach(var c in content)
                        w.WriteLine(c);
                }
            }
        }

        /// <summary>
        /// Return a list of strings split on new lines
        /// </summary>
        /// <param name="fullFileName"></param>
        /// <returns></returns>
        public static List<string> LoadDirect(string fullFileName)
        {
            string path = ConstructFullPath(fullFileName);

            if (!File.Exists(path))
                return null;
            else
            {
                var result = new List<string>();

                using (StreamReader r = File.OpenText(path))
                {
                    string s = "";

                    while ((s = r.ReadLine()) != null)
                        result.Add(s);
                }

                return result;
            }
        }

        #endregion

        #region Utility

        private static string ConstructFullPath(string filePath)
        {
            if (!Directory.Exists(SAVE_PATH))
                Directory.CreateDirectory(SAVE_PATH);

            return Path.Combine(SAVE_PATH, filePath);
        }

        #endregion
    }
}

