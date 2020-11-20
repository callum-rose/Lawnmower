using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Utils
{
    public static class StopwatchUtil
    {

        static bool m_initialised;

        // for storing which method called 
        static Dictionary<MethodBase, Stopwatch> m_stopwatchByCallerDict;

        /// <summary>
        /// Starts a timer. The timer is linked to the calling method
        /// </summary>
        public static void Start()
        {
            if (!m_initialised)
                Init();

            // get calling frame
            StackFrame frame = new StackTrace().GetFrame(1);
            MethodBase method = frame.GetMethod();
            Type type = method.ReflectedType;

            if (m_stopwatchByCallerDict.ContainsKey(method))
            {
                LogUtil.WriteWarning("Cannot run more than one timer from "
                + type.FullName + "." + method.Name);
                return;
            }

            //CheckTimers();

            Stopwatch stopwatch = new Stopwatch();
            m_stopwatchByCallerDict.Add(method, stopwatch);
            stopwatch.Start();
        }

        /// <summary>
        /// Stops this timer from the calling method. Returns the elapsed millis.
        /// </summary>
        /// <param name="suppressLog">If set to <c>true</c> suppress log.</param>
        public static long Stop(bool suppressLog = false)
        {
            if (!m_initialised)
                Init();

            // get calling frame
            StackFrame frame = new StackTrace().GetFrame(1);
            MethodBase method = frame.GetMethod();
            Type type = method.ReflectedType;

            if (m_stopwatchByCallerDict.ContainsKey(method))
            {
                Stopwatch stopwatch = m_stopwatchByCallerDict[method];
                stopwatch.Stop();

                if (!suppressLog)
                    LogUtil.Write("Timer started from " + type.FullName + "." + method.Name
                        + " elapsed " + stopwatch.ElapsedMilliseconds + "ms.");

                m_stopwatchByCallerDict.Remove(method);

                //CheckTimers();

                return stopwatch.ElapsedMilliseconds;
            }

            LogUtil.WriteWarning("No timer has been started from "
                + type.Name + "." + method.Name);

            //CheckTimers();

            return -1;
        }

        static void CheckTimers()
        {
            foreach (var keyValuePair in m_stopwatchByCallerDict)
            {
                if (keyValuePair.Value.ElapsedMilliseconds > 10 * 1000)
                    LogUtil.WriteWarning("Timer started by " + keyValuePair.Key.DeclaringType.Name
                        + "." + keyValuePair.Key + " has been running for "
                        + keyValuePair.Value.ElapsedMilliseconds + "ms");
            }
        }

        static void Init()
        {
            m_stopwatchByCallerDict = new Dictionary<MethodBase, Stopwatch>();

            m_initialised = true;
        }
    }
}