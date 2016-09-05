////css_args /ac
using System;
using System.Collections.Generic;
using static System.Console;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using TailCall;
using System.Dynamic;

public class ScriptClass
{
    public static int Main(string[] args)
    {
        do
        {
            new ScriptClass().main();
            //GC.Collect();
        }
        while (ReadLine() != "x");
        return 0;
    }

    #region
    // potential syntactic sugar
    //tail void Fib(int fnext, int f, int count)
    //{
    //    if (count == 0)
    //        yield return f;
    //    else
    //        tail Fib(fnext + f, fnext, count - 1);
    //}
    //
    //tail void Fib(int fnext, int f, int count)
    //{
    //    if (count == 0)
    //        break f;
    //    else
    //        repeat Fib(fnext + f, fnext, count - 1);
    //}
    #endregion

    void FibImpl(int fnext, int f, int count, StackContext stack)
    {
        if (count == 0)
            stack.Exit(f);
        else
            stack.Push(fnext + f, fnext, count - 1);
    }

    ///////////////////////////////////////////////////////////////////////////////////

    Action BuildPrinter()
    {
        return () => WriteLine("I am a printer");
    }

    int Recurse1(int i)
    {
        if (i < 100000)
            return Recurse1(i + 1);
        else
            return i;
    }

    void Recurse(int i, StackContext context)
    {
        if (i < 100000)
            context.Call(i + 1);
        else
            context.Exit(i);
    }

    void main()
    {
        {
            var rec = Recursion.Func<int, int>(Recurse);

            var rec2 = Recursion.Func<int, int>(
                                     (i, stack) =>
                                     {
                                         if (i < 100000)
                                             stack.Push(i + 1);
                                         else
                                             stack.Exit(i);
                                     });

            var result = Recursion.Call(8, (i, stack) =>
                                           {
                                               if (i < 100000)
                                                   stack.Push(i + 1);
                                               else
                                                   stack.Exit(i);
                                           });

            //dynamic CallData = new ExpandoObject();
            //var eeet = CallData.Con .Test;
            //var dirs = new List

            var root = @"C:\Users\osh\Documents\C# Scripts";
            var files = new List<string>();

            Recursion.Call(new Queue<string>(new[] { root }),
                          (dirs, stack) =>
                          {
                              if (dirs.Any())
                              {
                                  string dir = dirs.Dequeue();

                                  files.AddRange(Directory.GetFiles(dir));
                                  dirs.EnqueueRange(Directory.GetDirectories(dir));

                                  stack.Push(dirs);
                              }
                              else
                                  stack.Exit();
                          });

            dynamic wqet = 9;
            var sw = new Stopwatch();

            dynamic recursive = new ExpandoObject();

            recursive.Print = new Action<string>((msg) => WriteLine(msg));
            recursive.Print("gfd");

            sw.Restart();

            int t = (int)Recursion.Call(1, Recurse);
            sw.Stop();
            WriteLine(sw.Elapsed);

            sw.Restart();

            var ttt = Recursion.Call(1, Recurse);
            sw.Stop();
            WriteLine(sw.Elapsed);

            sw.Restart();
            rec(4);
            sw.Stop();
            WriteLine(sw.Elapsed);

            GC.Collect();

            sw.Restart();
            rec2(4);
            sw.Stop();
            WriteLine(sw.Elapsed);

            return;
            //Recurse(0); return;

            Action<int, int, string, int, int, int, int, int, int, int, int, int> h = null;

            var fib_iter = Recursion.Func<int, int, int, int>(
            (fnext, f, count, stack) =>
            {
                if (count == 0)
                    stack.Exit(f);
                else
                    stack.Push(fnext + f, fnext, count - 1);
            });

            Func<int, int> ifib = n => fib_iter(1, 0, n);

            WriteLine(ifib(1000000));
            return;
        }

        {
            Recursion.Call(8, (n, stack) =>
                              {
                                  if (n == 0)
                                      stack.Exit();
                                  else
                                      stack.Push(n - 1);
                              });

            var countDown = Recursion.Func<int, Func<Action>, Action>(
                                               (n, builder, stack) =>
                                               {
                                                   WriteLine("Enter");

                                                   if (n == 0)
                                                   {
                                                       var act = builder();
                                                       stack.Exit(act);
                                                   }
                                                   else
                                                   {
                                                       stack.Push(n - 1, builder);
                                                   }
                                                   WriteLine("Exit");
                                               });

            Action print = countDown(4, BuildPrinter);

            print();
        }

        //return;

        {
            var agregate_iter = Recursion.Func<int, int, int>(
                                               (n, result, stack) =>
                                               {
                                                   if (n > 6)
                                                       stack.Exit(result);
                                                   else
                                                       stack.Push(n + 1, result * n);
                                               });
            Func<int, int> agregate = n => agregate_iter(n, 1);

            WriteLine(agregate(3));
        }

        //return;

        var getFilesRec = Recursion.Func<Queue<string>, List<string>, string[]>(
           (dirs, files, stack) =>
           {
               if (dirs.Any())
               {
                   stack.Exit(files.ToArray());
               }
               else
               {
                   string dir = dirs.Dequeue();

                   files.AddRange(Directory.GetFiles(dir));
                   dirs.EnqueueRange(Directory.GetDirectories(dir));

                   stack.Push(dirs, files);
               }
           });

        var getFiles = new Func<string, string[]>(path => getFilesRec(new Queue<string>(new[] { path }), new List<string>()));

        var dirFiles = getFiles(@"C:\Users\%username%\Documents\C# Scripts".ExpandEnvVars());

        var printFiles = Recursion.Action<Queue<string>>(
          (dirs, stack) =>
          {
              if (!dirs.Any())
              {
                  stack.Exit();
              }
              else
              {
                  string dir = dirs.Dequeue();

                  Directory.GetFiles(dir)
                           .ForEach(WriteLine);

                  Directory.GetDirectories(dir)
                           .ForEach(dirs.Enqueue);

                  stack.Push(dirs);
              }
          });

        var input = new Queue<string>(new[] { @"C:\Users\%username%\Documents\C# Scripts".ExpandEnvVars() });
        printFiles(input);
    }
}

static class Extensions
{
    public static bool Any<T>(this Queue<T> collection) => collection.Count > 0;

    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        foreach (T item in collection)
            action(item);
        return collection;
    }

    public static void EnqueueRange<T>(this Queue<T> collection, IEnumerable<T> items)
    {
        foreach (T item in items)
            collection.Enqueue(item);
    }

    public static string ExpandEnvVars(this string text)
    {
        return Environment.ExpandEnvironmentVariables(text);
    }
}