namespace Interview;

// #17 LRU cache  [Advanced]
// Problem : Implement an LRU (least recently used) cache with Get and Put in O(1).
// Approach: Dictionary for O(1) lookup + LinkedList to keep usage order: move to front on access; evict from the back when over capacity.
// Complexity: Get and Put O(1); space O(capacity).

public class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _map = new();
    private readonly LinkedList<(TKey Key, TValue Value)> _order = new();

    public LruCache(int capacity) => _capacity = capacity;

    public bool TryGet(TKey key, out TValue value)
    {
        if (_map.TryGetValue(key, out var node))
        {
            _order.Remove(node);
            _order.AddFirst(node);          // mark as most recently used
            value = node.Value.Value;
            return true;
        }
        value = default!;
        return false;
    }

    public void Put(TKey key, TValue value)
    {
        if (_map.TryGetValue(key, out var existing))
        {
            _order.Remove(existing);
        }
        else if (_map.Count == _capacity)
        {
            var lru = _order.Last!;          // least recently used
            _order.RemoveLast();
            _map.Remove(lru.Value.Key);
        }
        var node = new LinkedListNode<(TKey, TValue)>((key, value));
        _order.AddFirst(node);
        _map[key] = node;
    }
}
