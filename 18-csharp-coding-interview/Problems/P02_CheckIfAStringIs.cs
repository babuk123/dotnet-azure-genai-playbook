namespace Interview;

// #2 Check if a string is a palindrome  [Basic]
// Problem : Return true if the text reads the same forwards and backwards, ignoring case, spaces and punctuation ("A man, a plan, a canal: Panama" → true).
// Approach: Two pointers; skip characters that are not letters/digits; compare lower-case characters.
// Complexity: Time O(n), space O(1).

public static class Palindrome
{
    public static bool IsPalindrome(string s)
    {
        int i = 0, j = s.Length - 1;
        while (i < j)
        {
            if (!char.IsLetterOrDigit(s[i])) { i++; continue; }
            if (!char.IsLetterOrDigit(s[j])) { j--; continue; }
            if (char.ToLowerInvariant(s[i]) != char.ToLowerInvariant(s[j])) return false;
            i++; j--;
        }
        return true;
    }
}
