using System;

namespace Utils
{
    public sealed class WriteOnce<T>
    {
        public bool HasValue { get; private set; }

        private T value;

        public WriteOnce()
        {
            HasValue = false;
            value = default;
        }

        public WriteOnce(T value)
        {
            HasValue = true;
            this.value = value;
        }

        public T Value
        {
            get
            {
                if (!HasValue)
                {
                    throw new InvalidOperationException("Value not set");
                }

                return value;
            }
            set
            {
                if (HasValue)
                {
                    throw new InvalidOperationException("Value already set");
                }

                this.value = value;
                HasValue = true;
            }
        }

        public T ValueOrDefault => value;

        public bool Set(T value)
        {
            if (HasValue)
            {
                return false;
            }

            Value = value;

            return true;
        }

        public void Reset()
        {
            value = default;
            HasValue = false;
        }

        public static implicit operator T(WriteOnce<T> value)
        {
            return value.Value;
        }

        public override string ToString()
        {
            return HasValue ? Convert.ToString(value) : "";
        }
    }
}
