// COPYRIGHT 2011 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Threading;
using System.Windows.Forms;

namespace ORTS
{
	/// <summary>
	/// Allows work to be done as a background task so that the form remains responsive (e.g. to a cancel or close button).
    /// Note: The "work" part is done by the background thread and the "success" part is done by the form's thread, so
    /// make sure the "success" part is short. 
	/// </summary>
	/// <typeparam name="T"></typeparam>
    public sealed class Task<T>
	{
		readonly Control Control;
		readonly Func<T> Work;
		readonly Action<T> Success;
		readonly Thread Thread;
		bool Cancelled;

		public Task(Control control, Func<T> work, Action<T> success)
		{
			Control = control;
			Work = work;
			Success = success;
			Thread = new Thread(TaskWorker);
			Thread.Start();
		}

		public void Cancel()
		{
			lock (this)
			{
				Cancelled = true;
			}
		}

		void TaskWorker()
		{
			try
			{
				// Get the new background thread to execute the "work" part.
                T result = Work();
				var cancelled = false;
				lock (this)
					cancelled = Cancelled;
				if (!cancelled)
                    // Get the form's thread to execute the "success" part.
                    Control.Invoke( Success, result );
            }
			catch (Exception)
			{
			}
		}
	}
}
