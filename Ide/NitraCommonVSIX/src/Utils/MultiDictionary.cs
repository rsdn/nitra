using System.Collections;
using System.Collections.Generic;

namespace Nitra.VisualStudio.Utils
{
  public class MultiDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, List<TValue>>>
  {
    readonly Dictionary<TKey, List<TValue>> _dic = new Dictionary<TKey, List<TValue>>();

    public int Count => _dic.Count;

    public List<TValue> this[TKey key]
    {
      get
      {
        if (!_dic.TryGetValue(key, out var values))
          _dic[key] = values = new List<TValue>();
        return values;
      }
    }

    public void Add(TKey key, TValue value)
    {
      if (!_dic.TryGetValue(key, out var values))
        _dic.Add(key, values = new List<TValue>());

      values.Add(value);
    }

    public void AddRange(TKey key, IEnumerable<TValue> values)
    {
      if (!_dic.TryGetValue(key, out var oldValues))
        _dic.Add(key, oldValues = new List<TValue>());

      oldValues.AddRange(values);
    }

    public void Clear() => _dic.Clear();

    public bool ContainsKey(TKey key) => _dic.ContainsKey(key);

    public bool RemoveValue(TKey key, TValue value) => _dic[key].Remove(value);

    public bool Remove(TKey key) => _dic.Remove(key);

    public bool TryGetValue(TKey key, out List<TValue> value) => _dic.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<TKey, List<TValue>>> GetEnumerator() => _dic.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _dic.GetEnumerator();
  }
}
