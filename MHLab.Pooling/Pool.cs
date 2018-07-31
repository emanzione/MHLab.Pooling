using System;
using System.Threading;

namespace MHLab.Pooling
{
    public class Pool<T> where T : IPoolable
    {
        public bool OverflowAllowed = false;
        public int GrowingSize = 8;

        public int Count { get { return m_count; } }

        public bool IsFull { get { return m_count >= m_buffer.Length; } }

        private T[] m_buffer;
        private int m_position;
        private int m_count;

        private readonly Func<T> m_factoryMethod;

        public Pool(int initialSize, Func<T> factoryMethod)
        {
            if(initialSize < 0) throw new ArgumentException("The initial size should be atleast equal to 0.", nameof(initialSize));

            m_buffer = new T[initialSize];
            m_position = -1;
            m_count = 0;

            m_factoryMethod = factoryMethod;
        }

        /// <summary>
        /// Fulls the pool with newly allocated objects.
        /// </summary>
        public void Populate()
        {
            Populate(m_buffer.Length - m_position + 1);
        }

        /// <summary>
        /// Adds a bunch of newly allocated objects.
        /// </summary>
        public void Populate(int amount)
        {
            if (amount <= 0) return;

            if(m_count + amount > m_buffer.Length)
                EnsureCapacity(m_count + amount);

            int i = m_position;
            while (i < amount)
            {
                i++;
                m_buffer[i] = m_factoryMethod.Invoke();
            }
            Interlocked.Exchange(ref m_position, i);
        }

        private void EnsureCapacity(int capacity)
        {
            // The size of the pool is already valid
            if (m_buffer.Length >= capacity) return;

            Array.Resize(ref m_buffer, capacity + GrowingSize);
        }

        /// <summary>
        /// Retrieves a pooled object from the pool, if available.
        /// Returns a newly allocated object if the pool is empty.
        /// </summary>
        public T Rent()
        {
            if (m_count <= 0)
            {
                // Fast end: if the buffer is empty, create
                // a new object and return it.
                return m_factoryMethod.Invoke();
            }

            // Read the current position and decrement it.
            // Should be enough to guarantee thread safety.
            // If not, let's lock 'em all! :)
            int position = Interlocked.Exchange(ref m_position, m_position - 1);

            // Retrieve the object
            var rentObject = m_buffer[position];

            // Set its reference/value in the buffer to the default value
            m_buffer[position] = default(T);

            // Decrement the objects count
            Interlocked.Decrement(ref m_count);

            return rentObject;
        }

        /// <summary>
        /// Returns the object to the pool, making it
        /// available for reusing.
        /// </summary>
        public void Recycle(T obj)
        {
            if (IsFull && !OverflowAllowed) return;

            if(IsFull)
            {
                EnsureCapacity(m_buffer.Length + 1);
            }

            int position = Interlocked.Exchange(ref m_position, m_position + 1);
            obj.Recycle();
            m_buffer[position] = obj;
            Interlocked.Increment(ref m_count);
        }

        /// <summary>
        /// Clear the content of this pool.
        /// </summary>
        public void Clear()
        {
            int position = Interlocked.Exchange(ref m_position, -1);
            Interlocked.Exchange(ref m_count, 0);

            Array.Clear(m_buffer, 0, m_buffer.Length);
        }

        /// <summary>
        /// Unloads the pool by removing spare objects, based on maxSize parameter.
        /// </summary>
        /*public void Unload(int maxSize)
        {
            if (_pool.Count <= maxSize) return;

            int difference = _pool.Count - maxSize;
            
            lock (_pool)
            {
                for (int i = 0; i < difference; i++)
                {
                    _pool.Dequeue();
                }
            }
        }*/
    }
}
