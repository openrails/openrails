// COPYRIGHT 2011, 2012 by the Open Rails project.
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

            // If the control we've been passed is still setting itself up, we must wait before invoking the callbacks.
            while (!Control.IsHandleCreated)
                Thread.Sleep(100);

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
