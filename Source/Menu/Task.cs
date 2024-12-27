// COPYRIGHT 2011, 2012, 2013 by the Open Rails project.
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

// Undefining this will let all exceptions be uncaught for debugging.
#define TASK_CATCH_EXCEPTIONS_AND_THREADED

// Defining this will log information on the processing of background loading tasks.
//#define DEBUG_BACKGROUND_TASKS

using System;
using System.Diagnostics;
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
#if DEBUG_BACKGROUND_TASKS
            Trace.TraceInformation("Task<{0}> created", typeof(T).ToString());
#endif
            Control = control;
            Work = work;
            Success = success;
            Failure = failure;
            Complete = complete;
#if TASK_CATCH_EXCEPTIONS_AND_THREADED
            Thread = new Thread(TaskWorker);
            Thread.CurrentUICulture = System.Globalization.CultureInfo.CurrentUICulture;
            Thread.Start();
#else
            TaskWorker();
#endif
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
#if DEBUG_BACKGROUND_TASKS
            Trace.TraceInformation("Task<{0}> cancelled", typeof(T).ToString());
#endif
            lock (this)
            {
                Cancelled = true;
            }
        }

        void TaskWorker()
        {
#if DEBUG_BACKGROUND_TASKS
            var typeName = typeof(T).ToString();
#endif
            T result = default(T);
#if TASK_CATCH_EXCEPTIONS_AND_THREADED
            try
            {
#endif
                // Get the new background thread to execute the "work" part.
#if DEBUG_BACKGROUND_TASKS
                Trace.TraceInformation("Task<{0}> invoke Work()", typeName);
#endif
                result = Work();
#if TASK_CATCH_EXCEPTIONS_AND_THREADED
            }
#if DEBUG_BACKGROUND_TASKS
            catch (Exception error)
            {
                Trace.TraceInformation("Task<{0}> work error: {1}", typeName, error.ToString());
#else
            catch (Exception)
            {
#endif
            }
#endif

            var cancelled = false;
            lock (this)
                cancelled = Cancelled;
#if DEBUG_BACKGROUND_TASKS
            Trace.TraceInformation("Task<{0}> cancelled = {1}", typeName, cancelled);
#endif

#if TASK_CATCH_EXCEPTIONS_AND_THREADED
            // If the control we've been passed is still setting itself up, we must wait before invoking the callbacks.
            while (!Control.IsHandleCreated && !Control.IsDisposed)
                Thread.Sleep(100);

            // The control we were meant to be informing has gone away entirely, so we must give up here.
            if (Control.IsDisposed)
                return;
#endif

#if TASK_CATCH_EXCEPTIONS_AND_THREADED
            try
            {
#endif
                // Execute the success/failure handlers if they exist.
                if (!cancelled)
                {
#if DEBUG_BACKGROUND_TASKS
                    Trace.TraceInformation("Task<{0}> invoke Success()", typeName);
#endif
                    if (Success != null)
#if TASK_CATCH_EXCEPTIONS_AND_THREADED
                        Control.Invoke(Success, result);
#else
                        Success(result);
#endif
                }
                else
                {
#if DEBUG_BACKGROUND_TASKS
                    Trace.TraceInformation("Task<{0}> invoke Failure()", typeName);
#endif
                    if (Failure != null)
#if TASK_CATCH_EXCEPTIONS_AND_THREADED
                        Control.Invoke(Failure);
#else
                        Failure();
#endif
                }
#if TASK_CATCH_EXCEPTIONS_AND_THREADED
            }
            catch { }
            try
            {
#endif
#if DEBUG_BACKGROUND_TASKS
                Trace.TraceInformation("Task<{0}> invoke Complete()", typeName);
#endif
                // Execute the complete handler if it exists.
                if (Complete != null)
#if TASK_CATCH_EXCEPTIONS_AND_THREADED
                    Control.Invoke(Complete);
#else
                    Complete();
#endif
#if TASK_CATCH_EXCEPTIONS_AND_THREADED
            }
#if DEBUG_BACKGROUND_TASKS
            catch (Exception error)
            {
                Trace.TraceInformation("Task<{0}> invoke error: {1}", typeName, error.ToString());
#else
            catch (Exception)
            {
#endif
            }
#endif
        }
    }
}
