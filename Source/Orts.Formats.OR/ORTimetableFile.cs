// COPYRIGHT 2014, 2015 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// OR Timetable file is csv file, with following main layout :
// Top Row : train information
// special items in toprow : 
//    #comment : general comment column (ignored except for first cell with row and column set to #comment)
//    <empty>  : continuation of train from previous column
//
// First column : station names
// special items in first column :
//    #comment   : general comment column (ignored except for first cell with row and column set to #comment)
//    #consist   : train consist
//    #path      : train path
//    #direction : Up or Down
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using GNU.Gettext;

namespace Orts.Formats.OR
{
    /// <summary>
    /// class ORTTPreInfo
    /// provides pre-information for menu
    /// extracts only description and list of trains
    /// </summary>
    
    public class TTPreInfo
    {
        public List<TTTrainPreInfo> Trains = new List<TTTrainPreInfo>();
        public String Description;

        private String Separator;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filePath"></param>

        public TTPreInfo(String filePath)
        {
            Separator = String.Empty;
            try
            {
                using (StreamReader scrStream = new StreamReader(filePath, true))
                {
                    TTFilePreliminaryRead(filePath, scrStream, Separator);
                    scrStream.Close();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Load error for timetable {0} : {1}",System.IO.Path.GetFileNameWithoutExtension(filePath), ex.ToString());
                Description = "<" + "load error:" + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
        }

        public override string ToString()
        {
            return Description;
        }

        /// <summary>
        /// ORTTFilePreliminaryRead
        /// Read function to obtain pre-info
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="scrStream"></param>
        /// <param name="Separator"></param>

        public void TTFilePreliminaryRead(String filePath, StreamReader scrStream, String Separator)
        {
            String readLine;
            String restLine;
            int firstCommentColumn = -1;

            // read first line - first character is separator, rest is train info
            readLine = scrStream.ReadLine();
            if (String.IsNullOrEmpty(Separator)) Separator = readLine.Substring(0, 1);

            restLine = readLine.Substring(1);

            String[] SeparatorArray = new String[1] { Separator };
            String[] Parts = restLine.Split(SeparatorArray, System.StringSplitOptions.None);

            int columnIndex = 1;
            foreach (String headerString in Parts)
            {
                if (String.Compare(headerString, "#comment", true) == 0)
                {
                    if (firstCommentColumn < 0) firstCommentColumn = columnIndex;
                }
                else if (!String.IsNullOrEmpty(headerString) && !headerString.ToLower().Contains("$static"))
                {
                    Trains.Add(new TTTrainPreInfo(columnIndex, headerString));
                }
                columnIndex++;
            }

            // process comment row - cell at first comment row and column is description
            // process path and consist row

            Description = String.Copy(filePath);

            bool descFound = false;
            bool pathFound = false;
            bool consistFound = false;
            bool startFound = false;

            readLine = scrStream.ReadLine();

            while (readLine != null && (!descFound || !pathFound || !consistFound || !startFound))
            {
                Parts = readLine.Split(SeparatorArray, System.StringSplitOptions.None);

                if (!descFound && firstCommentColumn > 0)
                {
                    if (String.Compare(Parts[0], "#comment", true) == 0)
                    {
                        Description = String.Copy(Parts[firstCommentColumn]);
                        descFound = true;
                    }
                }

                if (!pathFound)
                {
                    if (String.Compare(Parts[0].Trim().Substring(0,5), "#path", true) == 0)
                    {
                        pathFound = true;
                        foreach (TTTrainPreInfo train in Trains)
                        {
                            train.Path = String.Copy(Parts[train.Column]);
                        }
                    }
                }

                if (!consistFound)
                {
                    if (String.Compare(Parts[0], "#consist", true) == 0)
                    {
                        consistFound = true;
                        foreach (TTTrainPreInfo train in Trains)
                        {
                            train.Consist = String.Copy(Parts[train.Column]);
                            train.LeadingConsist = ExtractConsist(train.Consist, out train.ReverseConsist);
                        }
                    }
                }

                if (!startFound)
                {
                    if (String.Compare(Parts[0], "#start", true) == 0)
                    {
                        startFound = true;
                        foreach (TTTrainPreInfo train in Trains)
                        {
                            train.StartTime = String.Copy(Parts[train.Column]);
                        }
                    }
                }

                readLine = scrStream.ReadLine();
            }
        }


        private string ExtractConsist(string consistDef, out bool reverse)
        {
            bool isReverse = false;

            string reqString = String.Copy(consistDef);
            string consistProc = String.Copy(consistDef).Trim();

            if (consistProc.Substring(0, 1).Equals("<"))
            {
                int endIndex = consistProc.IndexOf('>');
                if (endIndex < 0)
                {
                    reqString = consistProc.Substring(1);
                    consistProc = String.Empty;
                }
                else
                {
                    reqString = consistProc.Substring(1, endIndex - 1);
                    consistProc = consistProc.Substring(endIndex + 1).Trim();
                }
            }
            else
            {
                int plusIndex = consistProc.IndexOf('+');
                if (plusIndex > 0)
                {
                    reqString = consistProc.Substring(0, plusIndex - 1);

                    int sepIndex = consistDef.IndexOf('$');
                    if (sepIndex > 0)
                    {
                        consistProc = consistDef.Substring(sepIndex).Trim();
                    }
                    else
                    {
                        consistProc = String.Empty;
                    }
                }
                else
                {
                    reqString = String.Copy(consistDef);

                    int sepIndex = consistDef.IndexOf('$');
                    if (sepIndex > 0)
                    {
                        consistProc = consistDef.Substring(sepIndex).Trim();
                    }
                    else
                    {
                        consistProc = String.Empty;
                    }
                }
            }


            if (!String.IsNullOrEmpty(consistProc) && consistProc.Substring(0, 1).Equals("$"))
            {
                if (consistProc.Substring(1, 7).Equals("reverse"))
                {
                    isReverse = true;
                }
            }


            reverse = isReverse;
            return (reqString.Trim());
        }

        public class TTTrainPreInfo : IComparable<TTTrainPreInfo>
        {
            public int Column;                // column index
            public string Train;              // train definition
            public string Consist;            // consist definition (full string)
            public string LeadingConsist;     // consist definition (extracted leading consist)
            public bool ReverseConsist = false;       // use consist in reverse
            public string Path;               // path definition
            public string StartTime;          // starttime definition

            GettextResourceManager Catalog = new GettextResourceManager("Orts.Formats.OR");

            public TTTrainPreInfo(int column, string train)
            {
                Column = column;
                Train = string.Copy(train);
                Consist = string.Empty;
                LeadingConsist = string.Empty;
                Path = string.Empty;
            }

            public int CompareTo(TTTrainPreInfo otherInfo)
            {
                return(String.Compare(this.Train, otherInfo.Train));
            }

            override public string ToString()
            {
                return (Train);
            }

            public string[] ToInfo()
            {
                string[] infoString = new string[] {
                    Catalog.GetStringFmt("Start time: {0}", StartTime),
                };

                return (infoString);
            }
        }
    }

/// <summary>
/// class ORMultiTTPreInfo
/// Creates pre-info for Multi TT file
/// returns Description and list of pre-info per file
/// </summary>

    public class MultiTTPreInfo
    {
        public List<TTPreInfo> ORTTInfo = new List<TTPreInfo>();
        public String Description;

        public MultiTTPreInfo(String filePath, String directory)
        {
            Description = String.Empty;
            try
            {
                using (StreamReader scrStream = new StreamReader(filePath, true))
                {
                    MultiTTFilePreliminaryRead(filePath, directory, scrStream);
                    scrStream.Close();
                    if (String.IsNullOrEmpty(Description)) Description = String.Copy(filePath);
                }
            }
            catch (Exception)
            {
                Description = "<" + "load error:" + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="SingleTTInfo"></param>

        public MultiTTPreInfo(TTPreInfo SingleTTInfo)
        {
            Description = String.Copy(SingleTTInfo.Description);
            ORTTInfo.Add(SingleTTInfo);
        }

        /// <summary>
        /// ORMultiTTFilePreliminaryRead
        /// Reads MultiTTfile for preliminary info
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="directory"></param>
        /// <param name="scrStream"></param>

        void MultiTTFilePreliminaryRead(String filePath, String directory, StreamReader scrStream)
        {
            String readLine;

            // read first line - first character is separator, rest is train info
            readLine = scrStream.ReadLine();

            while (readLine != null)
            {
                if (!String.IsNullOrEmpty(readLine))
                {
                    if (String.Compare(readLine.Substring(0, 1), "#") == 0)
                    {
                        if (String.IsNullOrEmpty(Description)) Description = String.Copy(readLine.Substring(1));
                    }
                    else
                    {
                        String ttfile = System.IO.Path.Combine(directory, readLine);
                        ORTTInfo.Add(new TTPreInfo(ttfile));
                    }
                }
                readLine = scrStream.ReadLine();
            }
        }
    }

    /// <summary>
    /// class MultiTTInfo
    /// extracts filenames from multiTTfile, extents names to full path
    /// </summary>

    public class MultiTTInfo
    {
        public List<string> TTFiles = new List<string>();
        public string Description;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="directory"></param>

        public MultiTTInfo(String filePath, String directory)
        {
            Description = String.Empty;
            try
            {
                using (StreamReader scrStream = new StreamReader(filePath, true))
                {
                    MultiTTFileRead(filePath, directory, scrStream);
                    scrStream.Close();
                    if (String.IsNullOrEmpty(Description)) Description = String.Copy(filePath);
                }
            }
            catch (Exception)
            {
                Description = "<" + "load error:" + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
        }

        /// <summary>
        /// MultiTTFileRead
        /// Reads multiTTfile and extracts filenames
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="directory"></param>
        /// <param name="scrStream"></param>

        void MultiTTFileRead(String filePath, String directory, StreamReader scrStream)
        {
            String readLine;

            // read first line - first character is separator, rest is train info
            readLine = scrStream.ReadLine();

            while (readLine != null)
            {
                if (!String.IsNullOrEmpty(readLine))
                {
                    if (String.Compare(readLine.Substring(0, 1), "#") == 0)
                    {
                        if (String.IsNullOrEmpty(Description)) Description = String.Copy(readLine.Substring(1));
                    }
                    else
                    {
                        String ttfile = System.IO.Path.Combine(directory, readLine);
                        TTFiles.Add(ttfile);
                    }
                }
                readLine = scrStream.ReadLine();
            }
        }
    }
}




