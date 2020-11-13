using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ToyParty.Collections
{
    // 외부에서 가져가는 데이터를 계속 추적하고, 그 상태를 바탕으로 해당 데이터가 사용 중인가 아닌가를 추적할 수 있는 데이터 형
    [Serializable]
    public class Pool<T>
    {
        [ShowInInspector]
        private Stack<T> readyStack = null;

        [ShowInInspector]
        private HashSet<T> usingCollection = new HashSet<T>();

        public int Count { get => readyStack.Count + usingCollection.Count; }

        private Func<T> poolingFunc = null;

        private int poolIncreaseMultiplier = 2;

        public Pool(IEnumerable<T> collection)
        {
            readyStack = new Stack<T>(collection);
        }

        public Pool(Func<T> poolingFunc)
        {
            SetPoolingFunc(poolingFunc);
        }

        public void SetPoolingFunc(Func<T> poolingFunc)
        {
            this.poolingFunc = poolingFunc;
        }

        public Pool()
        {
            readyStack = new Stack<T>();
        }

        public void Clear()
        {
            readyStack.Clear();
            usingCollection.Clear();
        }

        public T Fetch()
        {
            if (readyStack.Count == 0)
            {
                int baseCount = usingCollection.Count;
                int newCount = Math.Max(1, baseCount) * Math.Max(0, poolIncreaseMultiplier - 1);

                if (poolingFunc == null)
                {
                    throw new Exception("Pooling Container Didn't have Pooling Function");
                }

                for (int i = 0; i < newCount; i++)
                {
                    readyStack.Push(poolingFunc.Invoke());
                }
            }

            var item = readyStack.Pop();

            if (usingCollection.Contains(item))
            {
                throw new ArgumentException($"{item} is already Used");
            }

            usingCollection.Add(item);
            return item;
        }

        public void PushNewItem(T newItem)
        {
            if (usingCollection.Contains(newItem))
            {
                throw new ArgumentException($"{newItem} is already Pooled");
            }

            usingCollection.Add(newItem);
        }

        public void Store(T item)
        {
            if (usingCollection.Contains(item) == false)
            {
                throw new ArgumentException($"{item} is not pooling Item");
            }

            usingCollection.Remove(item);
            readyStack.Push(item);
        }

    }
}
