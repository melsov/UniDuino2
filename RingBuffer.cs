using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RingBuffer<T>
{
    private T[] storage;
    public int size {
        get {
            return storage.Length;
        }
    }

    public int count { get; private set; }

    public int cursor { get; private set; }
    private int _first;

    public RingBuffer(int size) {
        storage = new T[size];
        _first = 1;
    }

    public void push(T t) {
        storage[cursor] = t;
        cursor = (cursor + 1) % size;
        _first = (cursor + 1) % size;
        count = Mathf.Min(count + 1, size);
    }

    public T pop() {
        T result = storage[cursor];
        if (cursor == 0) {
            cursor = size - 1;
        }
        else {
            cursor -= 1;
        }
        _first = (cursor + 1) % size;
        count = Mathf.Max(0, count - 1);
        return result;
    }

    public IEnumerable<T> getValues() {
        foreach (int i in Enumerable.Range(0, size)) {
            yield return this[i];
        }
    }

    public int internalIndex(int i) {
        return (_first + i) % size;
    }

    public T this[int i] {
        get {
            return storage[(_first + i) % size];
        }
        set {
            storage[(_first + i) % size] = value;
        }
    }

    public T first {
        get {
            return this[0];
        }
        set {
            this[0] = value;
        }
    }

    public T last {
        get {
            return this[size - 1];
        }
        set {
            this[size - 1] = value;
        }
    }

    public void Clear() {
        foreach(int i in Enumerable.Range(0, size)) {
            this[i] = default(T);
        }
        count = 0;
    }


}
