// COPYRIGHT 2011, 2012 by the Open Rails project.
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
        readonly Action Failure;
        readonly Action Complete;
        readonly Thread Thread;
        public bool Cancelled;

        Task(Control control, Func<T> work, Action<T> success, Action failure, Action complete)
        {
            Control = control;
            Work = work;
            Success = success;
            Failure = failure;
            Complete = complete;
            Thread = new Thread(TaskWorker);
            Thread.Start();
        }

        public Task(Control control, Func<T> work, Action<T> success)
            : this(control, work, success, null, null)
        {
        }

        public Task(Control control, Func<T> work, Action complete)
            : this(control, work, null, null, complete)
        {
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
            T result = default(T);
            try
            {
                // Get the new background thread to execute the "work" part.
                result = Work();
            }
            catch { }

            var cancelled = false;
            lock (this)
                cancelled = Cancelled;

            try
            {
                // Execute the success/failure handlers if they exist.
                if (!cancelled)
                {
                    if (Success != null)
                        Control.Invoke(Success, result);
                }
                else
                {
                    if (Failure != null)
                        Control.Invoke(Failure);
                }
            }
            catch { }
            try
            {
                // Execute the complete handler if it exists.
                if (Complete != null)
                    Control.Invoke(Complete);
            }
            catch { }
        }
    }
}