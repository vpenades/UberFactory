using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.UberFactory
{
    /// <summary>
    /// simple check to ensure the calling thread is the same that created the object
    /// </summary>
    /// <remarks>
    /// Sometimes it's easier to ensure that a given class is used only
    /// within a specific thread, than make the whole class thread safe.
    /// </remarks>
    public sealed class SingleThreadAffinity
    {
        private readonly int _ThreadAffinity = Environment.CurrentManagedThreadId;

        /// <summary>
        /// throws an exception if the check is done from a thread other than the one that created the object
        /// </summary>
        public void Check()
        {
            if (_ThreadAffinity != Environment.CurrentManagedThreadId) throw new InvalidOperationException();
        }

        /// <summary>
        /// convenience method to use in property getters
        /// </summary>
        /// <example>
        /// property => _ThreadAffinity.CheckedReturn(_Property);
        /// </example>
        /// <typeparam name="T">any type</typeparam>
        /// <param name="value">any value</param>
        /// <returns>the same input value</returns>        
        public T CheckedReturn<T>(T value)
        {
            Check();
            return value;
        }
    }
}
