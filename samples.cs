//css_inc recursion.cs
using Microsoft.CSharp;
using System;
using System.Collections.Generic;
using static System.Console;
using static SamplesExtensions;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using TailCall;
using System.Dynamic;
using System.Threading;

public class ScriptClass
{
    string userDocsDir = @"C:\Users\%username%\Documents".ExpandEnvVars();

    public static void Main()
    {
        FindFirstNestedAttachment();
    }

    #region
    // potential syntactic sugar
    //rec void Fib(int fnext, int f, int count)
    //{
    //    if (count == 0)
    //        yield return f;
    //    else
    //        rec Fib(fnext + f, fnext, count - 1);
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

    ///////////////////////////////////////////////////////////////////////////////////
    void RawRecursion()
    {
        Func<int, int, int, int> fib_iter = null;
        fib_iter = (fnext, f, count) =>
                {
                    if (count == 0)
                        return f;
                    else
                        return fib_iter((fnext + f), fnext, (count - 1));
                };

        Func<int, int> fib = n => fib_iter(1, 0, n);

        fib(5);
    }
    ///////////////////////////////////////////////////////////////////////////////////
    void TailedRecursionWithLambda()
    {
        var fib_iter = Recursion.Func<int, int, int, int>(
                         (fnext, f, count, stack) =>
                         {
                             if (count == 0)
                                 stack.Exit(f);
                             else
                                 stack.Push(fnext + f, fnext, count - 1);
                         });

        Func<int, int> fib = n => fib_iter(1, 0, n);

        fib(5);
    }
    ///////////////////////////////////////////////////////////////////////////////////
    void FibImpl(int fnext, int f, int count, StackContext stack)
    {
        if (count == 0)
            stack.Exit(f);
        else
            stack.Push(fnext + f, fnext, count - 1);
    }

    void TailedRecursionWithMethod()
    {
        var fib_iter = Recursion.Func<int, int, int, int>(FibImpl);

        Func<int, int> fib = n => fib_iter(1, 0, n);

        fib(5);
    }
    ///////////////////////////////////////////////////////////////////////////////////
    void WaitForNumOfSeconds()
    {
        var wait = Recursion.Action<int>((count, stack) =>
                                         {
                                             if (count == 0)
                                             {
                                                 stack.Exit();
                                             }
                                             else
                                             {
                                                 Thread.Sleep(1000);
                                                 WriteLine("tick...");
                                                 stack.Push(count - 1);
                                             }
                                         });

        wait(5);
    }
    ///////////////////////////////////////////////////////////////////////////////////
    void VisitDirs()
    {
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

        var dirsToVisit = new Queue<string>()
                              .Add(userDocsDir);

        printFiles(dirsToVisit);
    }
    ///////////////////////////////////////////////////////////////////////////////////
    void CollectFilesWithFunc()
    {
        // Will take a collection of dirs to visit, "basket" (list) to collect all files
        // and return array of collected files
        var getFilesRec = Recursion.Func<List<string>, List<string>, string[]>(
                                         (dirs, files, stack) =>
                                         {
                                             var dir = dirs.Pop();

                                             if (dir != null)
                                             {
                                                 files.AddRange(Directory.GetFiles(dir));
                                                 dirs.AddRange(Directory.GetDirectories(dir));

                                                 stack.Push(dirs, files);
                                             }
                                             else
                                                 stack.Exit(files.ToArray());
                                         });

        var getFiles = new Func<string, string[]>(path => getFilesRec(new List<string>(new[] { path }),
                                                                      new List<string>()));

        var dirFiles = getFiles(userDocsDir);
    }
    ///////////////////////////////////////////////////////////////////////////////////
    void CollectFilesWithCall()
    {
        // similar to code above except that it collects the files to the external "basket"
        // and this one off routine is invoked immediately and once without storing it in the variable.
        var files = new List<string>();

        Recursion.Call(new Queue<string>().Add(userDocsDir),
                      (Queue<string> dirs, StackContext stack) =>
                      {
                          if (dirs.Any())
                          {
                              string dir = dirs.Dequeue();

                              files.AddRange(Directory.GetFiles(dir));
                              dirs.AddRange(Directory.GetDirectories(dir));

                              stack.Push(dirs);
                          }
                          else
                              stack.Exit();
                      });
    }
    ///////////////////////////////////////////////////////////////////////////////////
    class Message
    {
        public Message Add(Message msg) { Messages.Add(msg); return this; }
        public Message AddAttachment(string file) { this.Attachments.Add(file); return this; }
        public List<Message> Messages = new List<Message>();
        public List<string> Attachments = new List<string>();
    }

    static void FindFirstNestedAttachment()
    {
var message = new Message().Add(new Message())
						   .Add(new Message())
						   .Add(new Message().Add(new Message())
											 .Add(new Message().AddAttachment("manual.pdf"))
											 .Add(new Message()))
						   .Add(new Message());

var messages = new List<Message>().Append(message);

var file = Recursion.Func<string>(
					 stack =>
					 {
						 if (messages.Any())
						 {
							 Message msg = messages.Pop();

							 if (msg.Attachments.Any())
								 stack.Exit(msg.Attachments.First());
							 else
								 stack.Push(messages.AppendRange(msg.Messages));
						 }
					 })();
WriteLine(file);
    }

    ///////////////////////////////////////////////////////////////////////////////////
}

static class SamplesExtensions
{
    public static bool Any<T>(this Queue<T> collection) => collection.Count > 0;

    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        foreach (T item in collection)
            action(item);
        return collection;
    }

    public static Queue<T> AddRange<T>(this Queue<T> collection, IEnumerable<T> items)
    {
        foreach (T item in items)
            collection.Enqueue(item);
        return collection;
    }

    public static Queue<T> Add<T>(this Queue<T> collection, T item)
    {
        collection.Enqueue(item);
        return collection;
    }

    public static string ExpandEnvVars(this string text)
    {
        return Environment.ExpandEnvironmentVariables(text);
    }
}