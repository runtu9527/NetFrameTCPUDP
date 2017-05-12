using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetFrame.Net.TCP.Sock.IOCP
{
    /// <summary>
    /// AsyncUserToken对象池（固定缓存设计）
    /// </summary>
    public class AsyncUserTokenPool
    {
        Stack<AsyncUserToken> m_pool;

        // Initializes the object pool to the specified size
        //
        // The "capacity" parameter is the maximum number of 
        // AsyncUserToken objects the pool can hold
        public AsyncUserTokenPool(int capacity)
        {
            m_pool = new Stack<AsyncUserToken>(capacity);
        }

        // Add a SocketAsyncEventArg instance to the pool
        //
        //The "item" parameter is the AsyncUserToken instance 
        // to add to the pool
        public void Push(AsyncUserToken item)
        {
            if (item == null) { throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); }
            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }

        // Removes a AsyncUserToken instance from the pool
        // and returns the object removed from the pool
        public AsyncUserToken Pop()
        {
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }

        // The number of AsyncUserToken instances in the pool
        public int Count
        {
            get { return m_pool.Count; }
        }
    }
}
