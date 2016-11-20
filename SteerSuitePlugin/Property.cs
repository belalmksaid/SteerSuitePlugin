using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteerSuitePlugin
{
    public class Property
    {
        public string Name
        {
            get; private set;
        }
        public string Value
        {
            get; set;
        }
        public Type Type;

        public Property(string name, Type ty, string value)
        {
            Name = name;
            Type = ty;
            Value = value;
        }
    }
}
