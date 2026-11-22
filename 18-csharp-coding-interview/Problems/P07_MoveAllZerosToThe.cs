namespace Interview;

// #7 Move all zeros to the front (keep order of others)  [Basic]
// Problem : Rearrange an int array in place so all zeros come first and the non-zero numbers keep their relative order. [1,0,2,0,3] → [0,0,1,2,3].
// Approach: Walk from the end with a write pointer: copy each non-zero to the back, then fill the remaining front positions with zeros.
// Complexity: Time O(n), space O(1).

public static class ArrayOps
{
    public static void MoveZerosToFront(int[] a)
    {
        int write = a.Length - 1;
        for (int read = a.Length - 1; read >= 0; read--)
        {
            if (a[read] != 0)
                a[write--] = a[read];
        }
        while (write >= 0) a[write--] = 0;
    }

    // The mirror question: move zeros to the END
    public static void MoveZerosToEnd(int[] a)
    {
        int write = 0;
        foreach (int x in a)
            if (x != 0) a[write++] = x;
        while (write < a.Length) a[write++] = 0;
    }
}
