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

        //Increase the throttle, return the power % (number from 0 to 1)
        float Increase(float elapsedSeconds);
        float Decrease(float elapsedSeconds);

        //Loads the controller from a stream
        void Parse(STFReader f);

        //Return true is the throttle has notches
        //Or false when it is a continuous value (like a steam locomotive regulator)
        bool IsNotched();

        //returns true if this controller was loaded and can be used
        //Some notched controllers will have stepSize == 0, those are invalid
        bool IsValid();

        string GetStatus();

        void Save(BinaryWriter outf);
    }
}
