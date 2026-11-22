namespace Interview;

// #12 Implement a simple generic List<T> (dynamic array)  [Intermediate]
// Problem : Implement MyList<T> with Add, indexer, Count, RemoveAt, and support foreach.
// Approach: Back it with an array; double the capacity when full; shift items left on RemoveAt; implement IEnumerable<T> with yield return.
// Complexity: Add amortized O(1); indexer O(1); RemoveAt O(n).

public class MyList<T> : IEnumerable<T>
{
    private T[] _items = new T[4];
    public int Count { get; private set; }

    public void Add(T item)
    {
        if (Count == _items.Length)
            Array.Resize(ref _items, _items.Length * 2);  // amortized O(1)
        _items[Count++] = item;
    }

    public T this[int index]
    {
        get { CheckIndex(index); return _items[index]; }
        set { CheckIndex(index); _items[index] = value; }
    }

    public void RemoveAt(int index)
    {
        CheckIndex(index);
        Array.Copy(_items, index + 1, _items, index, Count - index - 1);
        _items[--Count] = default!;   // release reference for GC
    }

    private void CheckIndex(int index)
    {
        if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < Count; i++) yield return _items[i];
    }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
