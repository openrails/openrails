// COPYRIGHT 2011 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Threading;
using System.Windows.Forms;

namespace ORTS
{
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
				T result = Work();
				var cancelled = false;
				lock (this)
					cancelled = Cancelled;
				if (!cancelled)
					Control.Invoke(Success, result);
			}
			catch (Exception)
			{
			}
		}
	}
}
