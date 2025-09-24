using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SerializedDictionaryBase { }

[Serializable]
public class SerializedDictionary<TKey, TValue> : SerializedDictionaryBase,
    ISerializationCallbackReceiver, IEnumerable<KeyValuePair<TKey, TValue>>
{
    [SerializeField] private List<TKey> keys = new();
    [SerializeField] private List<TValue> values = new();

    private Dictionary<TKey, TValue> dict = new();
    private bool built; 


    public int Count { get { Initailize(); return dict.Count; } }
    public bool ContainsKey(TKey key) { Initailize(); return dict.ContainsKey(key); }
    public bool TryGetValue(TKey key, out TValue value) { Initailize(); return dict.TryGetValue(key, out value); }
    public TValue this[TKey key] { get { Initailize(); return dict[key]; } set { Initailize(); dict[key] = value; } }
    public Dictionary<TKey, TValue>.Enumerator GetEnumerator() { Initailize(); return dict.GetEnumerator(); }
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Initailize()
    {
        if (built) return;
        RebuildFromSerialized();
    }

    public void RebuildFromSerialized()
    {
        dict.Clear();
        int count = Mathf.Min(keys.Count, values.Count);
        for (int i = 0; i < count; i++)
        {
            dict[keys[i]] = values[i];
        }
        built = true;
    }

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
        built = false;
        RebuildFromSerialized();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var seen = new HashSet<TKey>();
        for (int i = 0; i < keys.Count; i++)
        {
            if (!seen.Add(keys[i]))
                Debug.LogWarning($"[SerializedDictionary] Duplicate key: {keys[i]}", this as UnityEngine.Object);
        }
        built = false; 
    }
#endif
}
