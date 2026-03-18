using System;
using System.Collections.Generic;

using UnityEngine;

namespace PulseChain.Gameplay {
    public sealed class ComponentPool<T> where T : Component {
        private readonly Stack<T> _items = new Stack<T>(16);
        private readonly Func<T> _factory;

        public ComponentPool(Func<T> factory) {
            _factory = factory;
        }

        public T Get() {
            if (_items.Count > 0) {
                T pooledItem = _items.Pop();
                pooledItem.gameObject.SetActive(true);
                return pooledItem;
            }

            return _factory.Invoke();
        }

        public void Release(T item) {
            item.gameObject.SetActive(false);
            _items.Push(item);
        }
    }
}