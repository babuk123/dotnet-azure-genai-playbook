namespace Interview;

// #4 Are two strings anagrams?  [Basic]
// Problem : "listen" and "silent" are anagrams. Write IsAnagram(a, b).
// Approach: Use one int[26] (or Dictionary for Unicode): add counts for a, subtract for b, all must end at zero.
// Complexity: Time O(n), space O(k).

public static class Anagram
{
    public static bool IsAnagram(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var counts = new Dictionary<char, int>();
        foreach (char c in a.ToLowerInvariant()) counts[c] = counts.GetValueOrDefault(c) + 1;
        foreach (char c in b.ToLowerInvariant())
        {
            if (!counts.TryGetValue(c, out int n) || n == 0) return false;
            counts[c] = n - 1;
        }
        return true;
    }
}
