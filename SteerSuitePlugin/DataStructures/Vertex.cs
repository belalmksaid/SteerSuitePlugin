using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteerSuitePlugin.DataStructures
{
    public class Vertex<T>
    {
        private T data;
        private LinkedList<Vertex<T>> neighbors;
    }
}
