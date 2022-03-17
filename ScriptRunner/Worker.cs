using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace ScriptRunning
{
    public class Worker<OUT, IN>
    {
        private System.Collections.Concurrent.ConcurrentQueue<IN> InputQueue { get; set; }

        public Worker(IN[] inputList)
        {
            this.InputQueue = new ConcurrentQueue<IN>(inputList);
        }
    }

    public class SafeValue<T>
    {
        private object LOCK;

        private T lockedValue;
        public T Value { get { return this.Get(); } set { this.Set(value); } }

        public event EventHandler ValueChanged;

        private void OnValueChanged()
        {
            if(this.ValueChanged != null)
            {
                this.ValueChanged(this, new EventArgs());
            }
        }

        public SafeValue()
        {
            this.LOCK = new object();
            this.lockedValue = default(T);
        }

        public SafeValue(object lockValue)
        {
            this.LOCK = lockValue;
            this.lockedValue = default(T);
        }

        public SafeValue(object lockValue, T value)
        {
            this.LOCK = lockValue;
            this.lockedValue = value;
        }

        public T Get()
        {
            lock (this.LOCK)
            {
                return this.lockedValue;
            }
        }

        public void Set(T value)
        {
            this.Set((v) => value);
        }

        public void Set(Func<T, T> lockedAction)
        {
            lock (this.LOCK)
            {
                T oldValue = this.lockedValue;
                T newValue = lockedAction(this.lockedValue);

                if (oldValue != null && newValue == null)
                {
                    this.lockedValue = newValue;
                    this.OnValueChanged();
                }
                else if (oldValue == null && newValue != null)
                {
                    this.lockedValue = newValue;
                    this.OnValueChanged();
                }
                else if (oldValue == null && newValue == null)
                {
                    // não faz nada
                }
                else if (!object.ReferenceEquals(oldValue, newValue) && !oldValue.Equals(newValue))
                {
                    this.lockedValue = newValue;
                    this.OnValueChanged();
                }
            }
        }

        // ********************************
        // *** Type casting overloading ***
        // ********************************
        public static implicit operator T(SafeValue<T> safeValue)
        {
            return safeValue.Value;
        }
    }
}