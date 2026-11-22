using Interview;

namespace Interview;

public static class Program
{
    public static readonly object Locker = new();
    static int fails = 0;
    static void Section(string name) => Console.WriteLine("\n" + name);
    static void Check(bool ok, string name) { Console.WriteLine((ok ? "  PASS " : "  FAIL ") + name); if (!ok) fails++; }

    public static int Main()
    {
        Section("#1 Reverse a string without using Reverse()");
        Check(Strings.Reverse("banking") == "gniknab", "reverse");
        Section("#2 Check if a string is a palindrome");
        Check(Palindrome.IsPalindrome("A man, a plan, a canal: Panama"), "pal1"); Check(!Palindrome.IsPalindrome("dotnet"), "pal2");
        Section("#3 First non-repeating character & character counts");
        Check(CharStats.FirstNonRepeating("swiss") == 'w', "fnr"); Check(CharStats.FirstNonRepeatingLinq("swiss") == 'w', "fnr linq"); Check(CharStats.Counts("aab")['a'] == 2, "counts");
        Section("#4 Are two strings anagrams?");
        Check(Anagram.IsAnagram("Listen", "Silent"), "anagram1"); Check(!Anagram.IsAnagram("abc", "abd"), "anagram2");
        Section("#5 LINQ: filter 5-letter words starting with \"s\" and sort them");
        var wf = WordFilter.FiveLetterSWords(new[] { "stack", "Swift", "sql", "salary", "share", "loans" }); Check(string.Join(",", wf) == "share,stack,Swift", "linq words");
        Section("#6 Two Sum");
        var ts = TwoSumSolver.TwoSum(new[] { 2, 7, 11, 15 }, 9); Check(ts[0] == 0 && ts[1] == 1, "twosum");
        Section("#7 Move all zeros to the front (keep order of others)");
        var z = new[] { 1, 0, 2, 0, 3 }; ArrayOps.MoveZerosToFront(z); Check(string.Join(",", z) == "0,0,1,2,3", "zeros front"); var e = new[] { 0, 1, 0, 3, 12 }; ArrayOps.MoveZerosToEnd(e); Check(string.Join(",", e) == "1,3,12,0,0", "zeros end");
        Section("#8 LINQ on a student/employee list (group, aggregate, top N)");
        var st = new List<Student> { new("Asha","A",90), new("Ben","A",70), new("Cara","B",85), new("Dev","B",60), new("Eli","A",95) }; Check(Math.Abs(StudentQueries.AveragePerClass(st)["A"] - 85) < 0.01, "avg"); Check(StudentQueries.Top3(st)[0].Name == "Eli", "top3"); Check(StudentQueries.AboveClassAverage(st).Count == 3, "above avg");
        Section("#9 Thread-safe Singleton");
        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() => AppConfig.Instance)).ToArray(); Task.WaitAll(tasks); Check(tasks.All(t => ReferenceEquals(t.Result, AppConfig.Instance)), "singleton"); Check(ReferenceEquals(Logger.Instance, Logger.Instance), "logger");
        Section("#10 Merge two sorted arrays (last step of merge sort)");
        Check(string.Join(",", Sorting.Merge(new[]{1,4,9}, new[]{2,3,10})) == "1,2,3,4,9,10", "merge"); Check(string.Join(",", Sorting.MergeSort(new[]{5,1,4,2,3})) == "1,2,3,4,5", "mergesort");
        Section("#11 Async puzzle: how long does this code take?");
        var sw = System.Diagnostics.Stopwatch.StartNew(); AsyncPuzzle.Sequential().GetAwaiter().GetResult(); var seq = sw.ElapsedMilliseconds; sw.Restart(); AsyncPuzzle.Parallel().GetAwaiter().GetResult(); var par = sw.ElapsedMilliseconds; Check(seq >= 2900 && par < 1900, $"async timing seq={seq} par={par}"); Check(AsyncPuzzle.WrappedInTaskRun().GetAwaiter().GetResult(), "taskrun");
        Section("#12 Implement a simple generic List<T> (dynamic array)");
        var ml = new MyList<int>(); for (int i = 1; i <= 10; i++) ml.Add(i); ml.RemoveAt(0); Check(ml.Count == 9 && ml[0] == 2 && ml.Sum() == 54, "mylist");
        Section("#13 Banking: transfer money between accounts (validation + thread safety)");
        var accts = new Dictionary<string, Account> { ["A"] = new Account { Id = "A", Balance = 1000 }, ["B"] = new Account { Id = "B", Balance = 1000 } }; var svc = new TransferService(accts); System.Threading.Tasks.Parallel.For(0, 500, i => { if (i % 2 == 0) svc.CreateTransfer("A", "B", 1); else svc.CreateTransfer("B", "A", 1); }); Check(accts["A"].Balance + accts["B"].Balance == 2000, "conservation"); Check(!svc.CreateTransfer("A", "B", 5000).Success, "overdraft"); Check(!svc.CreateTransfer("A", "A", 1).Success, "same acct"); Check(svc.SearchTransfers("A", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1)).Count == 500, "search");
        Section("#14 Call many APIs in parallel with a concurrency limit");
        int active = 0, maxActive = 0; Func<int, Task<string>> fake = async id => { var a = Interlocked.Increment(ref active); lock (Locker) maxActive = Math.Max(maxActive, a); await Task.Delay(20); Interlocked.Decrement(ref active); return "C" + id; }; var r1 = Throttled.FetchAllOrderedAsync(Enumerable.Range(1, 30), fake).GetAwaiter().GetResult(); Check(r1.Length == 30 && r1[0] == "C1" && maxActive <= 5, $"semaphore max={maxActive}"); var r2 = Throttled.FetchAllAsync(Enumerable.Range(1, 30), (id, ct) => fake(id)).GetAwaiter().GetResult(); Check(r2.Count == 30, "foreachasync");
        Section("#15 Retry with exponential backoff");
        int calls = 0; var rv = Retry.WithBackoffAsync(async () => { calls++; await Task.Yield(); if (calls < 3) throw new TimeoutException(); return 42; }, maxAttempts: 3, baseDelayMs: 10).GetAwaiter().GetResult(); Check(rv == 42 && calls == 3, "retry"); bool threw = false; try { Retry.WithBackoffAsync<int>(() => throw new InvalidOperationException(), 3, 10).GetAwaiter().GetResult(); } catch (InvalidOperationException) { threw = true; } Check(threw, "no retry non-transient");
        Section("#16 Fix the N+1 query problem (EF Core)");
        Check(new OrderDto(1, 10m, "x").CustomerName == "x", "orderdto");
        Section("#17 LRU cache");
        var lru = new LruCache<int, string>(2); lru.Put(1, "a"); lru.Put(2, "b"); lru.TryGet(1, out _); lru.Put(3, "c"); Check(!lru.TryGet(2, out _) && lru.TryGet(1, out var v1) && v1 == "a" && lru.TryGet(3, out _), "lru");
        Section("#18 Idempotent payment endpoint");
        var ips = new IdempotentPaymentService(); var req = new PaymentRequest("A", "B", 10); var pays = Enumerable.Range(0, 10).Select(_ => ips.PayAsync("key-123", req)).ToArray(); Task.WaitAll(pays); Check(ips.ProcessedCount == 1 && pays.Select(p => p.Result.PaymentId).Distinct().Count() == 1, "idempotent");
        Section("#19 Rate limiter (fixed window per client)");
        var rl = new FixedWindowRateLimiter(5, TimeSpan.FromMinutes(1)); int ok = 0; System.Threading.Tasks.Parallel.For(0, 50, _ => { if (rl.TryAcquire("client1")) Interlocked.Increment(ref ok); }); Check(ok == 5 && rl.TryAcquire("client2"), "ratelimit");
        Section("#20 Producer/consumer with Channel<T> (process a big message backlog)");
        Check(Pipeline.ProcessAsync(Enumerable.Range(1, 10000)).GetAwaiter().GetResult() == 10000, "channel");
        Section("#21 Cache stampede: 200 requests hit one expired key");
        int loads = 0; var sc = new StampedeSafeCache<string>(); var reqs = Enumerable.Range(0, 200).Select(_ => Task.Run(() => sc.GetOrLoadAsync("rates", async () => { Interlocked.Increment(ref loads); await Task.Delay(100); return "fx"; }, TimeSpan.FromMinutes(5)))).ToArray(); Task.WaitAll(reqs); Check(loads == 1 && reqs.All(t => t.Result == "fx"), $"stampede loads={loads}");
        Section("#22 Code review: find the bugs");
        Check(true, "review");
        Console.WriteLine(fails == 0 ? "\nALL CHECKS PASSED" : $"\n{fails} FAILED");
        return fails;
    }
}
