using MSTS;
using System;
using System.IO;

namespace ORTS
{
    /**
     * This interface is used to specify how controls will work.
     * 
     * We have a class for implementing each type of controller that locomotives use, being the commons
     * the Notched and not Notched controller.          
     * 
     */
    public interface IController
    {
        //Create a new controller exactly like this one
        IController Clone();

        float Update(float elapsedSeconds);

        void StartIncrease();
        void StopIncrease();
        void StartDecrease();
        void StopDecrease();        

        //Loads the controller from a stream
        void Parse(STFReader stf);        

        //returns true if this controller was loaded and can be used
        //Some notched controllers will have stepSize == 0, those are invalid
        bool IsValid();

        string GetStatus();

        void Save(BinaryWriter outf);
    }
}
