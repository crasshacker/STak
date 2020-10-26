using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace STak.TakEngine
{
    public class ConcurrentSet<T> : IEnumerable<T>, ISet<T>, ICollection<T>
    {
        private readonly ConcurrentDictionary<T, byte> m_dictionary = new ConcurrentDictionary<T, byte>();

        public ICollection<T> Values     => m_dictionary.Keys;
        public int            Count      => m_dictionary.Count;
        public bool           IsEmpty    => m_dictionary.IsEmpty;
        public bool           IsReadOnly => false;


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        public IEnumerator<T> GetEnumerator()
        {
            return m_dictionary.Keys.GetEnumerator();
        }


        public bool Remove(T item)
        {
            return TryRemove(item);
        }


        void ICollection<T>.Add(T item)
        {
            if (! Add(item))
            {
                throw new ArgumentException("Item already exists in the set.");
            }
        }


        public void UnionWith(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                TryAdd(item);
            }
        }


        public void IntersectWith(IEnumerable<T> other)
        {
            var enumerable = other as IList<T> ?? other.ToArray();
            foreach (var item in this)
            {
                if (! enumerable.Contains(item))
                    TryRemove(item);
            }
        }


        public void ExceptWith(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                TryRemove(item);
            }
        }


        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }


        public bool IsSubsetOf(IEnumerable<T> other)
        {
            var enumerable = other as IList<T> ?? other.ToArray();
            return this.AsParallel().All(enumerable.Contains);
        }


        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return other.AsParallel().All(Contains);
        }


        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            var enumerable = other as IList<T> ?? other.ToArray();
            return this.Count != enumerable.Count && IsSupersetOf(enumerable);
        }


        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            var enumerable = other as IList<T> ?? other.ToArray();
            return Count != enumerable.Count && IsSubsetOf(enumerable);
        }


        public bool Overlaps(IEnumerable<T> other)
        {
            return other.AsParallel().Any(Contains);
        }


        public bool SetEquals(IEnumerable<T> other)
        {
            var enumerable = other as IList<T> ?? other.ToArray();
            return Count == enumerable.Count && enumerable.AsParallel().All(Contains);
        }


        public bool Add(T item)
        {
            return TryAdd(item);
        }


        public void Clear()
        {
            m_dictionary.Clear();
        }


        public bool Contains(T item)
        {
            return m_dictionary.ContainsKey(item);
        }


        public void CopyTo(T[] array, int arrayIndex)
        {
            Values.CopyTo(array, arrayIndex);
        }


        public T[] ToArray()
        {
            return m_dictionary.Keys.ToArray();
        }


        public bool TryAdd(T item)
        {
            return m_dictionary.TryAdd(item, default);
        }


        public bool TryRemove(T item)
        {
            return m_dictionary.TryRemove(item, out _);
        }
    }
}
