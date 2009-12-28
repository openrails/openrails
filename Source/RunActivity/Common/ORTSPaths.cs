using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ORTS
{
    class ORTSPaths
    {

        public static string FindTrainCarPlugin( string initialFolder, string filename )
        {
            string dllPath = initialFolder + "\\" + filename;  // search in trainset folder
            if (File.Exists(dllPath))
                return dllPath;
            string rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(initialFolder)))+ "\\OpenRails";
            if( Directory.Exists( rootFolder ) )
            {
                dllPath = rootFolder + "\\" + filename;
                if (File.Exists(dllPath))
                    return dllPath;
            }

            return filename;   // then search in OpenRails program folder
        }
    }
}
