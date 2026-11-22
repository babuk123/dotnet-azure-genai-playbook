namespace Interview;

// #1 Reverse a string without using Reverse()  [Basic]
// Problem : Write a method that reverses a string. Do not use LINQ Reverse() or Array.Reverse().
// Approach: Copy to a char array, swap characters from both ends moving toward the middle.
// Complexity: Time O(n), space O(n) for the new string (strings are immutable).

public static class Strings
{
    public static string Reverse(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        char[] chars = input.ToCharArray();
        int left = 0, right = chars.Length - 1;
        while (left < right)
        {
            (chars[left], chars[right]) = (chars[right], chars[left]); // tuple swap
            left++;
            right--;
        }
        return new string(chars);
    }
}
