namespace Interview;

// #10 Merge two sorted arrays (last step of merge sort)  [Intermediate]
// Problem : Merge two sorted int arrays into one sorted array. Then explain merge sort.
// Approach: Two pointers: always take the smaller current element; copy the leftovers.
// Complexity: Merge: O(n + m). Merge sort: O(n log n) time, O(n) extra space, stable.

public static class Sorting
{
    public static int[] Merge(int[] a, int[] b)
    {
        var result = new int[a.Length + b.Length];
        int i = 0, j = 0, k = 0;
        while (i < a.Length && j < b.Length)
            result[k++] = a[i] <= b[j] ? a[i++] : b[j++];
        while (i < a.Length) result[k++] = a[i++];
        while (j < b.Length) result[k++] = b[j++];
        return result;
    }

    public static int[] MergeSort(int[] arr)
    {
        if (arr.Length <= 1) return arr;
        int mid = arr.Length / 2;
        return Merge(MergeSort(arr[..mid]), MergeSort(arr[mid..]));
    }
}
