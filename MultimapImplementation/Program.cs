using System.Collections;
public class MultiMap<Tkey, Tvalue>: IEnumerable<KeyValuePair<Tkey, Tvalue>> where Tkey: notnull
{
    private readonly Dictionary<Tkey, List<Tvalue>> _storage;
    private readonly ReaderWriterLockSlim _lock;
    private int _keyvaluePairCount;

    public MultiMap()
    {
        _storage = new Dictionary<Tkey, List<Tvalue>>();
        _lock = new ReaderWriterLockSlim();
    }

    //public MultiMap(IEqualityComparer<Tkey>? comparer)
    //{
    //    _storage = new Dictionary<Tkey, List<Tvalue>>(comparer);
    //}

    public int KeyCount
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _storage.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }            
        }
    }

    // Total number of key-value pairs
    public int KeyValuePairCount 
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _keyvaluePairCount;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public IEnumerable<Tkey> Keys
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _storage.Keys;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public void Add(Tkey key, Tvalue value)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_storage.TryGetValue(key, out var list))
            {
                list = new List<Tvalue>();
                _storage[key] = list;
            }

            list.Add(value);
            _keyvaluePairCount++;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        
    }

    public bool RemoveKey(Tkey key)
    {
        if (!_storage.TryGetValue(key,out var list))
        {
            return false;
        }

        _storage.Remove(key);
        _keyvaluePairCount -= list.Count;
        return true;
    }

    public bool Remove(Tkey key, Tvalue value)
    {
        if (!_storage.TryGetValue(key,out var list))
        {
            return false;
        }
        _keyvaluePairCount--;
        _storage[key].Remove(value);
        if (_storage[key].Count == 0)
        {
            _storage.Remove(key);
        }
        return true;
    }

    public void Clear()
    {
        _storage.Clear();
        _keyvaluePairCount = 0;
    }

    public bool Contains(Tkey key, Tvalue value)
    {
        if (!_storage.TryGetValue(key, out var list))
        {
            return false;
        }
        if (list.Contains(value))
        {
            return true;
        }
        return false;
    }

    public bool ContainsKey(Tkey key)
    {
        return _storage.ContainsKey(key);
    }

    public IReadOnlyList<Tvalue> GetValues(Tkey key)
    {
        if (_storage.TryGetValue(key, out var list))
        {
            return list;
        }
        return new List<Tvalue>();
    }

    public void AddRange(Tkey key, IEnumerable<Tvalue> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }
        foreach(var value in values)
        {
            Add(key, value);
        }
    }

    public IEnumerator<KeyValuePair<Tkey, Tvalue>> GetEnumerator()
    {
        var snapshout = CreateSnapshot();

        foreach(var pair in snapshout)
        {
            yield return pair;
        }

        //foreach (KeyValuePair<Tkey, List<Tvalue>> entry in _storage)
        //{
        //    foreach(Tvalue value in entry.Value)
        //    {
        //        yield return new KeyValuePair<Tkey, Tvalue>(entry.Key, value);
        //    }
        //}
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override string ToString()
    {
        return string.Join(Environment.NewLine, _storage.Select(x => $"{x.Key} = {string.Join(", ", x.Value) }"));
    }

    private KeyValuePair<Tkey, Tvalue>[] CreateSnapshot()
    {
        _lock.EnterReadLock();
        try
        {
            KeyValuePair<Tkey, Tvalue>[] snapShot = new KeyValuePair<Tkey, Tvalue>[_keyvaluePairCount];
            int i = 0;
            foreach(var key in _storage)
            {
                foreach(var value in key.Value)
                {
                    snapShot[i++] = new KeyValuePair<Tkey, Tvalue>(key.Key, value);
                }
            }
            return snapShot;
        }
        finally { _lock.ExitReadLock(); }
    }
}

public static class MultimapExtension
{
    public static IEnumerable<KeyValuePair <TKey, TValue>> Flatten<TKey, TValue>(
        this MultiMap<TKey, TValue> source) where TKey : notnull
    {
        foreach (var outer in source.Keys)
        {
            foreach (var value in source.GetValues(outer))
            {
                yield return new KeyValuePair<TKey, TValue>(outer, value);
            }
        }
    }

    public static MultiMap<TKey, TInner> Flatten<TKey, TInner>(
       this MultiMap<TKey, IEnumerable<TInner>> source)
       where TKey : notnull
    {
        MultiMap<TKey, TInner> result = new MultiMap<TKey, TInner>();
        foreach(TKey key in source.Keys)
        {
            var inner = source.GetValues(key);
            foreach(IEnumerable<TInner> innerInner in inner)
            {
                if (innerInner== null)
                    continue;

                foreach (TInner item in innerInner)
                {
                    result.Add(key, item);
                }
            }
        }
        return result;
    }

    
    public static void UnionWith<TKey, TValue>(this MultiMap<TKey, TValue> first,
        MultiMap<TKey, TValue> second)
        where TKey : notnull
    {
        if (first == null)
        {
            throw new ArgumentNullException(nameof(first));
        }

        if (second == null)
        {
            throw new ArgumentNullException(nameof(second));
        }

        //valueComparer ??= EqualityComparer<TValue>.Default;
        List<TKey> secondKeys = new List<TKey>(second.Keys);
        foreach (TKey key in secondKeys)
        {
            HashSet<TValue> valuesInFirstForKey = new HashSet<TValue>(first.GetValues(key));
            IReadOnlyList<TValue> x = second.GetValues(key);

            foreach (TValue value in x)
            {
                if (valuesInFirstForKey.Add(value))
                {
                    first.Add(key, value);
                }
            }
        }
    }

    public static MultiMap<TKey, TValue> UnionWith1<TKey, TValue>(this MultiMap<TKey, TValue> first,
        MultiMap<TKey, TValue> second)
        where TKey : notnull
    {
        if (first == null)
        {
            throw new ArgumentNullException(nameof(first));
        }

        if (second == null)
        {
            throw new ArgumentNullException(nameof(second));
        }

        MultiMap<TKey, TValue> toReturn = new MultiMap<TKey, TValue>();

        foreach(var key in  first.Keys)
        {
            HashSet<TValue> secondValues = new HashSet<TValue>(second.GetValues(key));
            HashSet<TValue> noDuplicate = new HashSet<TValue>();

            foreach (TValue value in first.GetValues(key))
            {
                if (noDuplicate.Add(value))
                {
                    toReturn.Add(key, value);
                }
            }

            foreach(var value in secondValues)
            {
                if (noDuplicate.Add(value))
                    toReturn.Add(key, value);
            }
        }

        foreach(var key in second.Keys)
        {
            if (toReturn.ContainsKey(key))
                continue;
            HashSet<TValue> noDuplicate = new HashSet<TValue>();
            foreach (var value in second.GetValues(key))
            {
                if (noDuplicate.Add(value))
                    toReturn.Add(key, value);
            }
        }
        //first.Clear();
        //foreach(var kvp in toReturn)
        //{
        //    first.Add(kvp.Key, kvp.Value);
        //}
        return toReturn;
    }

    public static void IntersectWith<TKey, TValue>(this MultiMap<TKey, TValue> first,
       MultiMap<TKey, TValue> second)
       where TKey : notnull
    {
        if (first == null)
        {
            throw new ArgumentNullException(nameof(first));
        }

        if (second == null)
        {
            throw new ArgumentNullException(nameof(second));
        }
        var firstKeys = new List<TKey>(first.Keys);

        foreach (TKey key in firstKeys)
        {
            HashSet<TValue> secondValues = new HashSet<TValue>(second.GetValues(key));
            HashSet<TValue> alreadyKept = new HashSet<TValue>();
            List<TValue> keptValues = new List<TValue>();
            
            foreach (TValue value in first.GetValues(key))
            {
                if (secondValues.Contains(value) && alreadyKept.Add(value))
                {
                    keptValues.Add(value);
                }
            }

            first.RemoveKey(key);

            if (keptValues.Count > 0)
            {
                first.AddRange(key, keptValues);
            }
        }
    }
    public static MultiMap<TKey, TValue> IntersectWith1<TKey, TValue>(this MultiMap<TKey, TValue> first,
       MultiMap<TKey, TValue> second) where TKey : notnull
    {
        if (first == null)
        {
            throw new ArgumentNullException(nameof(first));
        }

        if (second == null)
        {
            throw new ArgumentNullException(nameof(second));
        }

        MultiMap<TKey, TValue> newMap = new MultiMap<TKey, TValue>();

        foreach(var key in first.Keys)
        {
            HashSet<TValue> secondValueSet = new HashSet<TValue>(second.GetValues(key));
            HashSet<TValue> noDuplicate = new HashSet<TValue>();
            foreach(var value in first.GetValues(key))
            {
                if (secondValueSet.Contains(value) && noDuplicate.Add(value))
                {
                    newMap.Add(key, value);
                }
            }
        }
        return newMap;
    }

    public static void ExceptWith<TKey, TValue>(this MultiMap<TKey, TValue> first, MultiMap<TKey, TValue> second)
       where TKey : notnull
    {
        if (first == null)
        {
            throw new ArgumentNullException(nameof(first));
        }

        if (second == null)
        {
            throw new ArgumentNullException(nameof(second));
        }
        var firstKeys = new List<TKey>(first.Keys);

        foreach (TKey key in firstKeys)
        {
            HashSet<TValue> secondValues = new HashSet<TValue>(second.GetValues(key));
            HashSet<TValue> alreadyKept = new HashSet<TValue>();
            List<TValue> keptValues = new List<TValue>();

            foreach (TValue value in first.GetValues(key))
            {
                if (!secondValues.Contains(value) && alreadyKept.Add(value))
                {
                    keptValues.Add(value);
                }
            }

            first.RemoveKey(key);

            if (keptValues.Count > 0)
            {
                first.AddRange(key, keptValues);
            }
        }
    }

    public static MultiMap<TKey, TValue> ExceptWith1<TKey, TValue>(this MultiMap<TKey, TValue> first,
       MultiMap<TKey, TValue> second) where TKey : notnull
    {
        if (first == null)
        {
            throw new ArgumentNullException(nameof(first));
        }

        if (second == null)
        {
            throw new ArgumentNullException(nameof(second));
        }

        MultiMap<TKey, TValue> newMap = new MultiMap<TKey, TValue>();
        foreach (var key in first.Keys)
        {
            HashSet<TValue> noDuplicate = new HashSet<TValue>();
            HashSet<TValue> secondValues = new HashSet<TValue>(second.GetValues(key));
            
            foreach (var value in first.GetValues(key))
            {
                if (!secondValues.Contains(value) && noDuplicate.Add(value))
                {
                    newMap.Add(key, value); ;
                }
            }
        }
        return newMap;
    }

    public static void SymmetricExceptWith<TKey, TValue>(this MultiMap<TKey, TValue> first,MultiMap<TKey, TValue> second)
        where TKey : notnull
    {
        if (first == null)
        {
            throw new ArgumentNullException(nameof(first));
        }

        if (second == null)
        {
            throw new ArgumentNullException(nameof(second));
        }

        Dictionary<TKey, HashSet<TValue>> firstSnapshot = new Dictionary<TKey, HashSet<TValue>>();
        Dictionary<TKey, HashSet<TValue>> secondSnapshot = new Dictionary<TKey, HashSet<TValue>>();

        foreach (TKey key in first.Keys)
        {
            firstSnapshot[key] = new HashSet<TValue>(first.GetValues(key));
        }

        foreach (TKey key in second.Keys)
        {
            secondSnapshot[key] = new HashSet<TValue>(second.GetValues(key));
        }

        HashSet<TKey> allKeys = new HashSet<TKey>(firstSnapshot.Keys);
        allKeys.UnionWith(secondSnapshot.Keys);

        first.Clear();

        foreach (TKey key in allKeys)
        {
            firstSnapshot.TryGetValue(key, out HashSet<TValue>? firstValuesForKey);
            secondSnapshot.TryGetValue(key, out HashSet<TValue>? secondValuesForKey);

            firstValuesForKey ??= new HashSet<TValue>();
            secondValuesForKey ??= new HashSet<TValue>();

            foreach (TValue value in firstValuesForKey)
            {
                if (!secondValuesForKey.Contains(value))
                {
                    first.Add(key, value);
                }
            }

            foreach (TValue value in secondValuesForKey)
            {
                if (!firstValuesForKey.Contains(value))
                {
                    first.Add(key, value);
                }
            }
        }
    }

    public static MultiMap<TKey, TValue> SymmetricExceptWith1<TKey, TValue>(this MultiMap<TKey, TValue> first,
       MultiMap<TKey, TValue> second) where TKey : notnull
    {
        if (first == null)
        {
            throw new ArgumentNullException(nameof(first));
        }

        if (second == null)
        {
            throw new ArgumentNullException(nameof(second));
        }

        MultiMap<TKey, TValue> newMap = new MultiMap<TKey, TValue>();

        foreach(var key in first.Keys)
        {
            HashSet<TValue> secondValues = new HashSet<TValue>(second.GetValues(key));
            HashSet <TValue> noDuplicate = new HashSet<TValue>();
            
            foreach(var value in first.GetValues(key))
            {
                if (!secondValues.Contains(value) && noDuplicate.Add(value))
                {
                    newMap.Add(key, value);
                }
            }
        }

        foreach (var key in second.Keys)
        {
            HashSet<TValue> firstValues = new HashSet<TValue>(first.GetValues(key));
            HashSet<TValue> noDuplicate = new HashSet<TValue>();

            foreach (var value in second.GetValues(key))
            {
                if (!firstValues.Contains(value) && noDuplicate.Add(value))
                {
                    newMap.Add(key, value);
                }
            }
        }

        return newMap;
    }

}

//public class Program
//{
//    public static void Main()
//    {
//        //MultiMap<string, string> multiMap = new MultiMap<string, string>();
//        //multiMap.Add("fruit", "apple");
//        //multiMap.Add("fruit", "banana");
//        //multiMap.Add("fruit", "orange");

//        //multiMap.Add("color", "red");
//        //multiMap.Add("color", "blue");

//        //Console.WriteLine("Values for 'fruit':");
//        //foreach (string value in multiMap.GetValues("fruit"))
//        //{
//        //    Console.WriteLine(value);
//        //}

//        //Console.WriteLine();
//        //Console.WriteLine("All key-value pairs:");
//        //foreach (KeyValuePair<string, string> pair in multiMap)
//        //{
//        //    Console.WriteLine($"{pair.Key} -> {pair.Value}");
//        //}

//        //Console.WriteLine();
//        //Console.WriteLine($"Contains fruit -> banana: {multiMap.Contains("fruit", "banana")}");
//        //Console.WriteLine($"Distinct keys: {multiMap.KeyCount}");
//        //Console.WriteLine($"Total pairs: {multiMap.KeyValuePairCount}");

//        //MultiMap<string, string> map = new MultiMap<string, string>();

//        //map.Add("fruit", "apple");
//        //map.Add("fruit", "banana");
//        //map.Add("color", "red");

//        //var flattened = map.Flatten();

//        //foreach (var pair in flattened)
//        //{
//        //    Console.WriteLine($"{pair.Key} -> {pair.Value}");
//        //}

//        MultiMap<string, int> first = new MultiMap<string, int>();
//        first.Add("fruit", 1);
//        first.Add("fruit", 2);
//        first.Add("color", 10);

//        MultiMap<string, int> second = new MultiMap<string, int>();
//        second.Add("fruit", 2);
//        second.Add("fruit", 3);
//        second.Add("shape", 10);
//        var x = first.SymmetricExceptWith1(second);
//        foreach (KeyValuePair<string, int> pair in x)
//        {
//            Console.WriteLine($"{pair.Key} -> {pair.Value}");
//        }
//    }
//}