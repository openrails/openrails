// COPYRIGHT 2010 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS
{
	/// <summary>
	/// Explicitly sets the name of the thread on which the target will run.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	sealed class ThreadNameAttribute : Attribute
	{
		readonly string threadName;

		// This is a positional argument
		public ThreadNameAttribute(string threadName)
		{
			this.threadName = threadName;
		}

		public string ThreadName
		{
			get { return threadName; }
		}
	}

	/// <summary>
	/// Defines a thread on which the target is allowed to run; multiple threads may be allowed for a single target.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
	sealed class CallOnThreadAttribute : Attribute
	{
		readonly string threadName;

		// This is a positional argument
		public CallOnThreadAttribute(string threadName)
		{
			this.threadName = threadName;
		}

		public string ThreadName
		{
			get { return threadName; }
		}
	}
}
