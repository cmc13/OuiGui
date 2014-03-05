using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuiGui.Lib
{
    public static class LinqExtensions
    {
        public static IEnumerable<T> AllButLast<T>(this IEnumerable<T> e)
        {
            bool first = true;
            T prev = default(T);
            foreach (var obj in e)
            {
                if (first)
                    first = false;
                else
                    yield return prev;

                prev = obj;
            }
        }
    }
}
