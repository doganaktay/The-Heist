using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Archi.IO
{
    public class SaveHandler
    {
        private string fileBaseName;
        private string fileExtension;
        private int maxSaveCount;
        private int currentSaveIndex;

        public SaveHandler(string fileBaseName, string fileExtension, int maxSaveCount = 1)
        {
            this.fileBaseName = fileBaseName;
            this.fileExtension = fileExtension;

            if (maxSaveCount <= 0)
                this.maxSaveCount = 1;
            else
                this.maxSaveCount = maxSaveCount;
        }

        private string ConstructFilePath()
        {
            string index = maxSaveCount == 1 ? "" : "_" + ((currentSaveIndex++ % maxSaveCount) + 1);
            return fileBaseName + index + "." + fileExtension;
        }

    }
}

