namespace HexaEngine.UI
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Stores resources constructed by generated XAML code and resolves them through merged dictionaries.
    /// </summary>
    /// <remarks>
    /// This type performs no runtime XAML parsing, resource-file loading, type discovery, or object activation.
    /// </remarks>
    public class ResourceDictionary : IDictionary
    {
        private readonly Dictionary<object, object?> resources;
        private ResourceDictionaryCollection? mergedDictionaries;

        /// <summary>
        /// Initializes an empty resource dictionary.
        /// </summary>
        public ResourceDictionary()
        {
            resources = new(ResourceKeyComparer.Instance);
        }

        /// <summary>
        /// Initializes an empty resource dictionary with space for <paramref name="capacity"/> local resources.
        /// </summary>
        public ResourceDictionary(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);
            resources = new(capacity, ResourceKeyComparer.Instance);
        }

        /// <summary>
        /// Gets or sets the resource associated with <paramref name="key"/>.
        /// </summary>
        /// <remarks>
        /// Local resources take precedence. Merged dictionaries are searched from last to first.
        /// A missing key returns <see langword="null"/>.
        /// </remarks>
        public object? this[object key]
        {
            get
            {
                TryGetValue(key, out object? value);
                return value;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(key);
                resources[key] = value;
            }
        }

        /// <inheritdoc/>
        public bool IsFixedSize => false;

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public ICollection Keys => resources.Keys;

        /// <inheritdoc/>
        public ICollection Values => resources.Values;

        /// <inheritdoc/>
        public int Count => resources.Count;

        /// <summary>
        /// Gets the dictionaries used as fallbacks by this dictionary.
        /// </summary>
        public Collection<ResourceDictionary> MergedDictionaries => mergedDictionaries ??= new(this);

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => ((ICollection)resources).SyncRoot;

        internal bool IsEmpty => resources.Count == 0 && (mergedDictionaries == null || mergedDictionaries.Count == 0);

        /// <inheritdoc/>
        public void Add(object key, object? value)
        {
            ArgumentNullException.ThrowIfNull(key);
            resources.Add(key, value);
        }

        /// <summary>
        /// Ensures space for the requested number of local resources without resizing.
        /// </summary>
        public int EnsureCapacity(int capacity)
        {
            return resources.EnsureCapacity(capacity);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            resources.Clear();
        }

        /// <summary>
        /// Determines whether this dictionary or a merged dictionary contains <paramref name="key"/>.
        /// </summary>
        public bool Contains(object key)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (resources.ContainsKey(key))
            {
                return true;
            }

            if (mergedDictionaries != null)
            {
                for (int i = mergedDictionaries.Count - 1; i >= 0; i--)
                {
                    if (mergedDictionaries[i].Contains(key))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to retrieve a resource from this dictionary or its merged dictionaries.
        /// </summary>
        public bool TryGetValue(object key, out object? value)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (resources.TryGetValue(key, out value))
            {
                return true;
            }

            if (mergedDictionaries != null)
            {
                for (int i = mergedDictionaries.Count - 1; i >= 0; i--)
                {
                    if (mergedDictionaries[i].TryGetValue(key, out value))
                    {
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Gets an allocation-free lookup view for string keys represented by character spans.
        /// </summary>
        public AlternateLookup GetAlternateLookup()
        {
            return new(this);
        }

        /// <inheritdoc/>
        public void CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);
            ((ICollection)resources).CopyTo(array, index);
        }

        /// <summary>
        /// Enumerates local resources without allocating.
        /// </summary>
        public Enumerator GetEnumerator()
        {
            return new(resources);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        public void Remove(object key)
        {
            ArgumentNullException.ThrowIfNull(key);
            resources.Remove(key);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary)this).GetEnumerator();
        }

        internal void ValidateMergedDictionary(ResourceDictionary resourceDictionary)
        {
            if (ReferenceEquals(this, resourceDictionary) || resourceDictionary.ContainsDictionary(this))
            {
                throw new InvalidOperationException("A ResourceDictionary cannot merge itself directly or indirectly.");
            }
        }

        private bool ContainsStringKey(ReadOnlySpan<char> key)
        {
            Dictionary<object, object?>.AlternateLookup<ReadOnlySpan<char>> lookup = resources.GetAlternateLookup<ReadOnlySpan<char>>();
            if (lookup.ContainsKey(key))
            {
                return true;
            }

            if (mergedDictionaries != null)
            {
                for (int i = mergedDictionaries.Count - 1; i >= 0; i--)
                {
                    if (mergedDictionaries[i].ContainsStringKey(key))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryGetStringValue(ReadOnlySpan<char> key, out object? value)
        {
            var lookup = resources.GetAlternateLookup<ReadOnlySpan<char>>();
            if (lookup.TryGetValue(key, out value))
            {
                return true;
            }

            if (mergedDictionaries != null)
            {
                for (int i = mergedDictionaries.Count - 1; i >= 0; i--)
                {
                    if (mergedDictionaries[i].TryGetStringValue(key, out value))
                    {
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        private bool ContainsDictionary(ResourceDictionary resourceDictionary)
        {
            if (ReferenceEquals(this, resourceDictionary))
            {
                return true;
            }

            if (mergedDictionaries != null)
            {
                for (int i = 0; i < mergedDictionaries.Count; i++)
                {
                    if (mergedDictionaries[i].ContainsDictionary(resourceDictionary))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Provides allocation-free lookup for string keys represented by character spans.
        /// </summary>
        public readonly ref struct AlternateLookup
        {
            private readonly ResourceDictionary owner;

            internal AlternateLookup(ResourceDictionary owner)
            {
                this.owner = owner;
            }

            /// <summary>
            /// Gets a resource, or <see langword="null"/> when the key is absent.
            /// </summary>
            public object? this[ReadOnlySpan<char> key]
            {
                get
                {
                    TryGetValue(key, out object? value);
                    return value;
                }
            }

            /// <summary>
            /// Determines whether the dictionary or any merged dictionary contains the key.
            /// </summary>
            public bool ContainsKey(ReadOnlySpan<char> key) => owner.ContainsStringKey(key);

            /// <summary>
            /// Attempts to retrieve the key from the dictionary or its merged dictionaries.
            /// </summary>
            public bool TryGetValue(ReadOnlySpan<char> key, out object? value) => owner.TryGetStringValue(key, out value);
        }

        private sealed class ResourceKeyComparer : IEqualityComparer<object>, IAlternateEqualityComparer<ReadOnlySpan<char>, object>
        {
            public static ResourceKeyComparer Instance { get; } = new();

            private ResourceKeyComparer()
            {
            }

            public object Create(ReadOnlySpan<char> alternate)
            {
                return alternate.ToString();
            }

            public bool Equals(ReadOnlySpan<char> alternate, object other)
            {
                return other is string text && alternate.Equals(text.AsSpan(), StringComparison.Ordinal);
            }

            public int GetHashCode(ReadOnlySpan<char> alternate)
            {
                return string.GetHashCode(alternate);
            }

            public new bool Equals(object? x, object? y)
            {
                return EqualityComparer<object>.Default.Equals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return obj.GetHashCode();
            }
        }

        /// <summary>
        /// Enumerates the local resources without allocating when used directly.
        /// </summary>
        public struct Enumerator : IDictionaryEnumerator, IDisposable
        {
            private readonly Dictionary<object, object?> dictionary;
            private Dictionary<object, object?>.Enumerator enumerator;

            internal Enumerator(Dictionary<object, object?> dictionary)
            {
                this.dictionary = dictionary;
                enumerator = dictionary.GetEnumerator();
            }

            /// <inheritdoc/>
            public DictionaryEntry Entry => new(Key, Value);

            /// <inheritdoc/>
            public object Key => enumerator.Current.Key;

            /// <inheritdoc/>
            public object? Value => enumerator.Current.Value;

            /// <summary>
            /// Gets the current local resource entry.
            /// </summary>
            public DictionaryEntry Current => Entry;

            object IEnumerator.Current => Entry;

            /// <inheritdoc/>
            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            /// <inheritdoc/>
            public void Reset()
            {
                enumerator = dictionary.GetEnumerator();
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                enumerator.Dispose();
            }
        }
    }
}
