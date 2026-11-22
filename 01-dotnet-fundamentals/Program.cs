// .NET fundamentals you can SEE: value vs reference types, boxing, GC generations, IDisposable.
using System.Diagnostics;

Console.WriteLine("=== 1. Value type vs reference type ===");
var p1 = new PointStruct { X = 1 };
var p2 = p1;            // copies the value
p2.X = 99;
Console.WriteLine($"struct: p1.X = {p1.X}, p2.X = {p2.X}   (independent copies)");

var c1 = new PointClass { X = 1 };
var c2 = c1;            // copies the reference
c2.X = 99;
Console.WriteLine($"class : c1.X = {c1.X}, c2.X = {c2.X}   (same object)");

Console.WriteLine("\n=== 2. Boxing cost ===");
const int N = 5_000_000;
var sw = Stopwatch.StartNew();
long before = GC.GetAllocatedBytesForCurrentThread();
object boxed = 0;
for (int i = 0; i < N; i++) boxed = i;          // each assignment allocates a box on the heap
long boxedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
Console.WriteLine($"boxing {N:N0} ints allocated ~{boxedBytes / 1024 / 1024} MB in {sw.ElapsedMilliseconds} ms (last={boxed})");

before = GC.GetAllocatedBytesForCurrentThread();
int notBoxed = 0;
for (int i = 0; i < N; i++) notBoxed = i;      // stays on the stack
Console.WriteLine($"no boxing allocated {GC.GetAllocatedBytesForCurrentThread() - before} bytes (last={notBoxed})");

Console.WriteLine("\n=== 3. GC generations ===");
var obj = new byte[100];
Console.WriteLine($"new object generation: {GC.GetGeneration(obj)}");
GC.Collect(); GC.WaitForPendingFinalizers();
Console.WriteLine($"after 1 collection   : {GC.GetGeneration(obj)}  (survivors are promoted)");
GC.Collect(); GC.WaitForPendingFinalizers();
Console.WriteLine($"after 2 collections  : {GC.GetGeneration(obj)}");
var large = new byte[100_000];
Console.WriteLine($"100 KB array (Large Object Heap) generation: {GC.GetGeneration(large)}");

Console.WriteLine("\n=== 4. IDisposable and using ===");
using (var res = new AuditFile("audit.log"))
{
    res.Write("report generated");
}   // Dispose runs here, deterministically
Console.WriteLine("resource released at end of using block");

Console.WriteLine("\n=== 5. String immutability ===");
string s = "bank";
string t = s;
s += "ing";
Console.WriteLine($"s = {s}, t = {t}   (+= created a new string)");

struct PointStruct { public int X; }
class PointClass { public int X; }

sealed class AuditFile : IDisposable
{
    private readonly string _name;
    private bool _disposed;
    public AuditFile(string name) { _name = name; Console.WriteLine($"opened {_name}"); }
    public void Write(string line)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Console.WriteLine($"write: {line}");
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Console.WriteLine($"closed {_name}");
    }
}
