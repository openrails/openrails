using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MenuWPF
{
    internal class EngineInfo
    {
        #region Properties
        /// <summary>
        /// The short name of the engine
        /// </summary>
        internal string ID {get; set;}
        /// <summary>
        /// The main shape of the engine
        /// </summary>
        internal string Shape { get; set; }
        /// <summary>
        /// The freight anim shape, if any
        /// </summary>
        internal string FreightAnim { get; set; }
        /// <summary>
        /// The lenght of the engine
        /// </summary>
        internal double Length { get; set; }
        /// <summary>
        /// The mass in tons, of the engine 
        /// </summary>
        internal double Mass { get; set; }
        /// <summary>
        /// The coupling type of the engine
        /// </summary>
        internal CouplingType Coupling { get; set; }
        /// <summary>
        /// The type of the engine (electric, diesel, steam)
        /// </summary>
        internal EngineType Type { get; set; }
        /// <summary>
        /// The maximum power developped by the engine in kW
        /// </summary>
        internal double MaxPower { get; set; }
        /// <summary>
        /// The maximum initial traction force in kN
        /// </summary>
        internal double MaxForce { get; set; }
        /// <summary>
        /// The maximum continuous traction force in kN
        /// </summary>
        internal double MaxContinuousForce { get; set; }
        /// <summary>
        /// The long name of the engine
        /// </summary>
        internal string Name { get; set; }
        /// <summary>
        /// The description of the engine
        /// </summary>
        internal string Description { get; set; }

        #endregion

        #region Constructor

        internal EngineInfo()
        {

        }

        #endregion

        #region Overriden methods

        public override bool Equals(object obj)
        {
            return this.Name == ((EngineInfo)obj).Name;
        }

        public override string ToString()
        {
            return this.Name;
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        #endregion

    }

    internal class EngineInfoEqualityComparer : IEqualityComparer<EngineInfo>
    {

        #region IEqualityComparer<EngineInfo> Members

        public bool Equals(EngineInfo x, EngineInfo y)
        {
            return Equals(x.Name, y.Name);
        }

        public int GetHashCode(EngineInfo obj)
        {
            return obj.Name.GetHashCode();
        }

        #endregion
    }

    internal enum CouplingType
    {
        Automatic,
        Chain,
        Bar
    }

    internal enum EngineType
    {
        Electric,
        Diesel,
        Steam
    }
}
