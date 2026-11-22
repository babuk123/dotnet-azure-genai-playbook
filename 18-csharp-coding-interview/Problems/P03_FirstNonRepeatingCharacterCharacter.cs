namespace Interview;

// #3 First non-repeating character & character counts  [Basic]
// Problem : Given a string, return the first character that appears only once ("swiss" → 'w'). Also show how to count each character.
// Approach: Count characters with a Dictionary in one pass, then scan the string again to find the first with count 1.
// Complexity: Time O(n), space O(k) where k = distinct characters.

public static class CharStats
{
    public static Dictionary<char, int> Counts(string s)
    {
        var counts = new Dictionary<char, int>();
        foreach (char c in s)
            counts[c] = counts.TryGetValue(c, out int n) ? n + 1 : 1;
        return counts;
    }

    public static char? FirstNonRepeating(string s)
    {
        var counts = Counts(s);
        foreach (char c in s)
            if (counts[c] == 1) return c;
        return null;
    }

    // LINQ version (interviewers like to see both)
    public static char? FirstNonRepeatingLinq(string s) =>
        s.GroupBy(c => c).Where(g => g.Count() == 1).Select(g => (char?)g.Key).FirstOrDefault();
}
