using System;

namespace Domain0.Exceptions
{
    public class NotFoundException : Exception
    {
        public string Name { get; set; }

        public object Value { get; set; }

        public NotFoundException(string name, object value = null)
        {
            Name = name;
            Value = value;
        }
    }
}
