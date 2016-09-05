# Tail Recursion

# Background 
The _Tail Recursion_ problem is not new. As any recursion it represents very intuitive and powerful execution paradigm, which is unfortunately associated with a very serious flaw - potential stack overflow. _Tail Recursion_ on the other hand is a special case of recursion that can be optimized in such a way that stack overflow can be completely eliminated. Some languages recognise this and offer "tail call" optimization in one or another form. Some of them do tail call optimization always (e.g.  Scheme), others under certain conditions (e.g. C#) and some do not support it at all (e.g. Python).

CLR perfectly capable of "tailing" the calls. It processes a special IL instruction for this - `.tail`. F# interpreter/compiler takes advantage of the `.tail` instruction by allowing deterministically request tailing from the user code with the special keyword `rec`. Unfortunately C# doesn't follow the suite. While it perfectly optimises tail-compatible routines in release mode (due to the higher level of optimization associated with the release builds) it doesn't do it for debug mode and even when it does it is still not guaranteed. Thus while C# is definitely capable of tail optimization for all practical reasons it should be treated as a language that is cannot handle tail recursion at compiler level. Thus this non-deterministic nature of C# tail optimization support completely diminishes its benefits.

_The history of .tail and C# is quite interesting. MS was pressed by the developers to implement tail optimization but it fully rejected the proposals due to the implementation difficulties and inability to guarantee no performance cost. From now and then there were reports of "sighting" C# tail optimization either specifically on x64 targets or under other circumstances. Thus currently (Sep 2016) C#6 can definitely compile and optimize tail recursive routines at least in Release configuration for AnyCPU built on x64 machines. But without an official well documented .tail support it is still an exotic feature that hart to rely on._ 

Wikipedia has very good article about the matter: https://en.wikipedia.org/wiki/Tail_call

# Purpose
Any language that doesn't support tail-call optimization has to rely on user level loop-based technique which is called "trampolining". In fact some compilers implement this very technique under the hood. Though when compiler offers no support for this it is up to developer to implement the technique directly in the code.

Fortunately trampolined recursion algorithm is very simple and described in the numerous online resources:
https://en.wikipedia.org/wiki/Tail_call
https://qualityofdata.com/2012/02/03/how-to-run-most-of-the-recursive-functions-iteratively/
http://www.thomaslevesque.com/2011/09/02/tail-recursion-in-c/
http://community.bartdesmet.net/blogs/bart/archive/2009/11/08/jumping-the-trampoline-in-c-stack-friendly-recursion.aspx
Interception technique: http://codereview.stackexchange.com/questions/57839/trampoline-interceptor

Thus for a single one off trampolined recursion it's arguably preferred to do it directly in the code where you need to use it. However if the software solution requires more often recursive behaviour developers can benefit form some sort of generic trampolined recursion solution so there is no need to setup the trampolined infrastructure again and again. This solution is an attempt to bring such a solution to the developers. 
 
# Solution 
The objective of this exercise was to have a generic tailed recursion that would deterministically emit tailed calls while preserving as much as possible the raw C# recursion syntax. The solution itself is extremely simple and the core routine is a single method of ~20 lines of code. The rest is a call context infrastructure and set of convenient API entry points providing convenient signature overloads.

The following is a canonical implementation of Fibonacci sequence with raw recursion in C#:

```C#
Func<int, int, int, int> fib_iter = null;
fib_iter = (fnext, f, count) =>
            {
                if (count == 0)
                    return f;
                else
                    return fib_iter(fnext + f, fnext, count - 1);
            };

Func<int, int> fib = n => fib_iter(1, 0, n);

fib(5);
```
The implementation above would lead to the stack overflow providing the number of fib_iter calls large enough to flood the stack.

The following is the same solution but with the user code tail optimization (trampolined recursion):
```C#
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
``` 
And the non-lambda code is as below:
```C#
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
```
The code sample demonstrates that the overall raw-recursion code layout stays the same except a new extra argument `StackContext` is injected in the implementation signature. This argument is used to schedule the next non-blocking call to the function primary function (the actual recursive function):
```C#
stack.Push(fnext + f, fnext, count - 1);
//instead of 
return FibImpl(fnext + f, fnext, count - 1);
```
And the very same recursion stack context object is used to indicate the full completion of the calculation:
```C#
stack.Exit(f);
//instead of 
return f;
```
It would be interesting if C# follows the steps of F# and implements an explicit recursion keyword (e.g. `rec`) to force tail-call optimization upon user request from code. The ideal approach for this wouold be a syntactic sugar (as with many current C# syntax features). Something like this:
```C#
rec void Fib(int fnext, int f, int count)
{
    if (count == 0)
        yield return f;
    else
        rec Fib(fnext + f, fnext, count - 1);
}
...
Func<int, int> fib = n => Fib(1, 0, n);
fib(5);
```
But let's face it, the chances of this are very slim :) 

# Code

The code contains various samples, which include the code above. The typical use case of recursion with __Tail Recursion__ is a creation of a 'recursive' delegate (either from member method or from lambda) and invoke it when required.
```C#
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

wait(5); //wait fro 5 seconds
```
An alternative approach is to call the provided recursive routine immediately without building and storing the delegate:
```C#
//visit all sub directories and collect all files
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
```