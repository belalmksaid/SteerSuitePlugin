using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteerSuitePlugin.DataStructures
{
    public class Graph<T>
    {
        List<Vertex<T>> vertices;
        bool[,] edges = new bool[10,10];
    }
}
