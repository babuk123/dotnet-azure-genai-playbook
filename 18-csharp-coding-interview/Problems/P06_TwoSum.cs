namespace Interview;

// #6 Two Sum  [Basic]
// Problem : Given an int array and a target, return the indexes of two numbers that add up to the target. Example: [2, 7, 11, 15], target 9 → [0, 1].
// Approach: Brute force is O(n²). Better: one pass with a Dictionary<value, index>; for each number check if (target − number) was already seen.
// Complexity: Time O(n), space O(n).

public static class TwoSumSolver
{
    public static int[] TwoSum(int[] nums, int target)
    {
        var seen = new Dictionary<int, int>(); // value -> index
        for (int i = 0; i < nums.Length; i++)
        {
            int needed = target - nums[i];
            if (seen.TryGetValue(needed, out int j))
                return new[] { j, i };
            seen[nums[i]] = i;
        }
        return Array.Empty<int>(); // or throw if a solution must exist
    }
}
