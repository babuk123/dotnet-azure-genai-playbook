namespace Interview;

// #5 LINQ: filter 5-letter words starting with "s" and sort them  [Basic]
// Problem : Using LINQ, filter an array of strings to those with exactly 5 characters that start with "s", and sort them.
// Approach: Where + OrderBy; mention case-insensitivity and both method and query syntax.
// Complexity: Time O(n log n) for the sort.

public static class WordFilter
{
    public static List<string> FiveLetterSWords(IEnumerable<string> words) =>
        words.Where(w => w.Length == 5 && w.StartsWith("s", StringComparison.OrdinalIgnoreCase))
             .OrderBy(w => w, StringComparer.OrdinalIgnoreCase)
             .ToList();

    // Same thing in query syntax
    public static List<string> FiveLetterSWordsQuery(IEnumerable<string> words) =>
        (from w in words
         where w.Length == 5 && w.StartsWith("s", StringComparison.OrdinalIgnoreCase)
         orderby w
         select w).ToList();
}
