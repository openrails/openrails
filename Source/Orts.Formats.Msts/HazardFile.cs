// COPYRIGHT 2009, 2010, 2011, 2013 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{
    public class HazardFile
    {
        public HazardFile(string filename)
        {
            try
            {
                using (STFReader stf = new STFReader(filename, false))
                {
                    stf.ParseFile(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("tr_worldfile", ()=>{ Tr_HazardFile = new Tr_HazardFile(stf); }),
                    });
                    //TODO This should be changed to STFException.TraceError() with defaults values created
                    if (Tr_HazardFile == null) throw new STFException(stf, "Missing Tr_WorldFile");
                }
            }
            finally
            {
            }
        }
        public Tr_HazardFile Tr_HazardFile;
    }



    public class Tr_HazardFile
    {
        public Tr_HazardFile(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("filename", ()=>{ FileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("workers", ()=>{ Workers = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("distance", ()=>{ Distance = stf.ReadFloatBlock(STFReader.UNITS.None, 10); }),
                new STFReader.TokenProcessor("speed", ()=>{ Speed = stf.ReadFloatBlock(STFReader.UNITS.None, 3); }),
                new STFReader.TokenProcessor("idle_key", ()=>{ Idle_Key = stf.ReadVector2Block(STFReader.UNITS.None, Idle_Key); }),
                new STFReader.TokenProcessor("idle_key2", ()=>{ Idle_Key2 = stf.ReadVector2Block(STFReader.UNITS.None, Idle_Key2); }),
                new STFReader.TokenProcessor("surprise_key_left", ()=>{ Surprise_Key_Left = stf.ReadVector2Block(STFReader.UNITS.None, Surprise_Key_Left); }),
                new STFReader.TokenProcessor("surprise_key_right", ()=>{ Surprise_Key_Right = stf.ReadVector2Block(STFReader.UNITS.None, Surprise_Key_Right); }),
                new STFReader.TokenProcessor("success_scarper_key", ()=>{ Success_Scarper_Key = stf.ReadVector2Block(STFReader.UNITS.None, Success_Scarper_Key); }),
           });
            //TODO This should be changed to STFException.TraceError() with defaults values created
            if (FileName == null) throw new STFException(stf, "Missing FileName");
        }

        public string FileName; // ie OdakyuSE - used for MKR,RDB,REF,RIT,TDB,TIT
        public string Workers;
        public float Distance;
        public float Speed;
        public Vector2 Idle_Key = new Vector2();
        public Vector2 Idle_Key2 = new Vector2();
        public Vector2 Surprise_Key_Left = new Vector2();
        public Vector2 Surprise_Key_Right = new Vector2();
        public Vector2 Success_Scarper_Key = new Vector2();

    }
}
