#region Licence...
/*
The MIT License (MIT)
Copyright (c) 2016 Oleg Shilo
Permission is hereby granted, 
free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion
using System;
using System.Linq;
using System.Collections.Generic;
using static System.Console;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;
using System.Dynamic;

//good reads:
/*
 * https://en.wikipedia.org/wiki/Tail_call
 * https://qualityofdata.com/2012/02/03/how-to-run-most-of-the-recursive-functions-iteratively/
 * http://www.thomaslevesque.com/2011/09/02/tail-recursion-in-c/
 * http://community.bartdesmet.net/blogs/bart/archive/2009/11/08/jumping-the-trampoline-in-c-stack-friendly-recursion.aspx
 * Interception technique: http://codereview.stackexchange.com/questions/57839/trampoline-interceptor
 */

namespace TailCall
{
    public static class Recursion
    {
        //------------------------------------------------
        //Build: void -> TResult
        public static Func<TResult> Func<TResult>(Action<StackContext> routine)
        {
            return () => DynamicCall<TResult>(routine);
        }

        //Build: void -> void
        public static Action Action(Action<StackContext> routine)
        {
            return () => DynamicCall<object>(routine);
        }

        //Invoke: T -> object
        public static object Call(Action<StackContext> routine)
        {
            return DynamicCall<object>(routine);
        }
        //------------------------------------------------
        //Build: T -> TResult
        public static Func<T, TResult> Func<T, TResult>(Action<T, StackContext> routine)
        {
            return arg => DynamicCall<TResult>(routine, arg);
        }

        //Build: T -> void
        public static Action<T> Action<T>(Action<T, StackContext> routine)
        {
            return arg => DynamicCall<object>(routine, arg);
        }

        //Invoke: T -> object
        public static object Call<T>(T arg, Action<T, StackContext> routine)
        {
            return DynamicCall<object>(routine, arg);
        }
        //------------------------------------------------
        //Build: T1, T2 -> TResult
        public static Func<T1, T2, TResult> Func<T1, T2, TResult>(Action<T1, T2, StackContext> routine)
        {
            return (arg1, arg2) => DynamicCall<TResult>(routine, arg1, arg2);
        }

        //Build: T1, T2 -> void
        public static Action<T1, T2> Action<T1, T2>(Action<T1, T2, StackContext> routine)
        {
            return (arg1, arg2) => DynamicCall<object>(routine, arg1, arg2);
        }

        //Invoke: T1, T2 -> object
        public static object Call<T1, T2>(T1 arg1, T2 arg2, Action<T1, T2, StackContext> routine)
        {
            return DynamicCall<object>(routine, arg1, arg2);
        }
        //------------------------------------------------
        //Build: T1, T2, T3 -> TResult
        public static Func<T1, T2, T3, TResult> Func<T1, T2, T3, TResult>(Action<T1, T2, T3, StackContext> routine)
        {
            return (arg1, arg2, arg3) => DynamicCall<TResult>(routine, arg1, arg2, arg3);
        }

        //Build: T1, T2 -> void
        public static Action<T1, T2, T3> Action<T1, T2, T3>(Action<T1, T2, T3, StackContext> routine)
        {
            return (arg1, arg2, arg3) => DynamicCall<object>(routine, arg1, arg2, arg3);
        }

        //Invoke: T1, T2, T3 -> object
        public static object Call<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3, Action<T1, T2, T3, StackContext> routine)
        {
            return DynamicCall<object>(routine, arg1, arg2, arg3);
        }

        //------------------------------------------------
#if DEBUG
        //may need to be re-enabled in the future (e.g. for portability reasons) 
        internal static object StaticCall<T>(T arg, Action<T, StackContext> routine)
        {
            var context = new StackContext(routine, arg);
            context.IsFirstCall = true;
            while (!context.ExitRequested)
            {
                //exit by default; it's up to 'routine' to schedule the continuation
                context.ExitRequested = true;
                routine((T) context.nextCallArgs[0], context);
                context.IsFirstCall = false;
            }

            return context.Result;
        }
#endif
        internal static T DynamicCall<T>(dynamic routine, params object[] args)
        {
            var context = new StackContext(routine);
            context.nextCallArgs.AddRange(args);
            context.IsFirstCall = true;

            while (!context.ExitRequested)
            {
                //exit by default; it's up to 'routine' to schedule the continuation
                context.ExitRequested = true;
                switch (args.Length)
                {
                    case 0: routine(context); break;
                    case 1: routine((dynamic)context.nextCallArgs[0], context); break;
                    case 2: routine((dynamic)context.nextCallArgs[0], (dynamic)context.nextCallArgs[1], context); break;
                    case 3: routine((dynamic)context.nextCallArgs[0], (dynamic)context.nextCallArgs[1], (dynamic)context.nextCallArgs[2], context); break;
                    case 4: routine((dynamic)context.nextCallArgs[0], (dynamic)context.nextCallArgs[1], (dynamic)context.nextCallArgs[2], (dynamic)context.nextCallArgs[3], context); break;
                    case 5: routine((dynamic)context.nextCallArgs[0], (dynamic)context.nextCallArgs[1], (dynamic)context.nextCallArgs[2], (dynamic)context.nextCallArgs[3], (dynamic)context.nextCallArgs[4], context); break;
                }
            }

            context.IsFirstCall = false;
            return (T)context.Result;
        }
    }

    public static class Extensions
    {
        public static List<T> AppendRange<T>(this List<T> collection, IEnumerable<T> items)
        {
            collection.AddRange(items);
            return collection;
        }

        public static List<T> Append<T>(this List<T> collection, T item)
        {
            collection.Add(item);
            return collection;
        }

        public static T Pop<T>(this IList<T> collection) where T : class
        {
            lock (collection)
            {
                if (collection.Count > 0)
                {
                    T result = collection[0];
                    collection.RemoveAt(0);
                    return result;
                }
                return null;
            }
        }
    }

    public class StackContext
    {
        internal bool ExitRequested;
        internal object Result;
        public object Routine { get; internal set; }
        public bool IsFirstCall { get; internal set; }
        internal List<object> nextCallArgs = new List<object>();
        internal List<dynamic> nextCallDArgs = new List<dynamic>();

        public dynamic Data = new ExpandoObject();

        public StackContext(object routine, params object[] args)
        {
            Routine = routine;
            nextCallArgs.AddRange(args);
        }

        public void Exit(object result = null)
        {
            Result = result;
            // practically not need;
            // It will serve only extremely rare cases like when 
            // 'Exit' being called after 'Push' call
            ExitRequested = true;
        }

        public void Call(params object[] args)
        {
            Result = null;
            ExitRequested = false;
            nextCallArgs.Clear();
            nextCallArgs.AddRange(args);
        }

        public void Push(params object[] args)
        {
            Result = null;
            ExitRequested = false;
            nextCallArgs.Clear();
            nextCallArgs.AddRange(args);
        }
    }
}