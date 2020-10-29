using System.Collections.Generic;

namespace Rias.Implementation
{
#nullable disable
    public struct SingleOrList<T>
    {
        private readonly T _value;
        private IList<T> _list;
        
        public SingleOrList(T item)
        {
            _value = item;
            _list = null;
        }
        
        public SingleOrList(IEnumerable<T> enumerable)
        {
            _value = default;
            _list = new List<T>(enumerable);
        }

        public T Value => List is null ? _value : default;
        public IList<T> List => _list;

        public SingleOrList<T> Add(T item)
        {
            _list ??= new List<T> { _value };
            _list.Add(item);
            return this;
        }
    }
#nullable enable
}