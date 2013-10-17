// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they bagin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using System;
using System.IO;

namespace HtmlRenderer.Utils
{
    /// <summary>
    /// Static class that contains argument-checking methods
    /// </summary>
#if CF_1_0
    internal class ArgChecker
#else
    internal static class ArgChecker
#endif
    {
        /// <summary>
        /// Validate given <see cref="condition"/> is true, otherwise throw exception.
        /// </summary>
        /// <typeparam name="TException">Exception type to throw.</typeparam>
        /// <param name="condition">Condition to assert.</param>
        /// <param name="message">Exception message in-case of assert failure.</param>
        public static void AssertIsTrue<TException>(bool condition, string message) where TException : Exception, new()
        {
#if DEBUG
            // Checks whether the condition is false
            if (!condition)
            {
                // Throwing exception
#if PC
                throw (TException)Activator.CreateInstance(typeof(TException), message);
#else
                throw (TException)Activator.CreateInstance(typeof(TException));
#endif
            }
#endif
        }

        /// <summary>
        /// Validate given argument isn't Null.
        /// </summary>
        /// <param name="arg">argument to validate</param>
        /// <param name="argName">Name of the argument checked</param>
        /// <exception cref="System.ArgumentNullException">if <paramref name="arg"/> is Null</exception>
        public static void AssertArgNotNull(object arg, string argName)
        {
#if DEBUG
            if (arg == null)
            {
                throw new ArgumentNullException(argName);
            }
#endif
        }

        /// <summary>
        /// Validate given argument isn't <see cref="System.IntPtr.Zero"/>.
        /// </summary>
        /// <param name="arg">argument to validate</param>
        /// <param name="argName">Name of the argument checked</param>
        /// <exception cref="System.ArgumentNullException">if <paramref name="arg"/> is <see cref="System.IntPtr.Zero"/></exception>
        public static void AssertArgNotNull(IntPtr arg, string argName)
        {
#if DEBUG
            if (arg == IntPtr.Zero)
            {
                throw new ArgumentException("IntPtr argument cannot be Zero", argName);
            }
#endif
        }

        /// <summary>
        /// Validate given argument isn't Null or empty.
        /// </summary>
        /// <param name="arg">argument to validate</param>
        /// <param name="argName">Name of the argument checked</param>
        /// <exception cref="System.ArgumentNullException">if <paramref name="arg"/> is Null or empty</exception>
        public static void AssertArgNotNullOrEmpty(string arg, string argName)
        {
#if DEBUG
            if (string.IsNullOrEmpty(arg))
            {
                throw new ArgumentNullException(argName);
            }
#endif
        }

        /// <summary>
        /// Validate given argument isn't Null.
        /// </summary>
        /// <typeparam name="T">Type expected of <see cref="arg"/></typeparam>
        /// <param name="arg">argument to validate</param>
        /// <param name="argName">Name of the argument checked</param>
        /// <exception cref="System.ArgumentNullException">if <paramref name="arg"/> is Null</exception>
        /// <returns><see cref="arg"/> cast as <see cref="T"/></returns>
        public static T AssertArgOfType<T>(object arg, string argName)
        {
#if DEBUG
            AssertArgNotNull(arg, argName);

            if (arg is T)
            {
                return (T)arg;
            }
            throw new ArgumentException(string.Format("Given argument isn't of type '{0}'.", typeof(T).Name), argName);
#else
            return (T)arg;
#endif
        }

        /// <summary>
        /// Validate given argument isn't Null or empty AND argument value is the path of existing file.
        /// </summary>
        /// <param name="arg">argument to validate</param>
        /// <param name="argName">Name of the argument checked</param>
        /// <exception cref="System.ArgumentNullException">if <paramref name="arg"/> is Null or empty</exception>
        /// <exception cref="System.IO.FileNotFoundException">if <see cref="arg"/> file-path not exist</exception>
        public static void AssertFileExist(string arg, string argName)
        {
#if DEBUG
            AssertArgNotNullOrEmpty(arg, argName);

            if (false == File.Exists(arg))
            {
                throw new FileNotFoundException(string.Format("Given file in argument '{0}' not exist.", argName));
            }
#endif
        }

        public static IDisposable Profile(string msg, bool profile)
        {
#if PROFILE
            return profile ? ODP.Diagnostics.Profiler.Default.Measure(msg) : null;
#else
            return null;
#endif
        }

        public static void ProfileDump()
        {
#if PROFILE
            ODP.Diagnostics.LogFn.Log(ODP.Diagnostics.Profiler.Default.DumpGraph());
            ODP.Diagnostics.Profiler.Default.Clear();
#endif
        }
    }
}
