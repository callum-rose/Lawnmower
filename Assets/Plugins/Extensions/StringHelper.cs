using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BalsamicBits.Extensions
{
    public static class StringHelper
    {
        public static string Stringify<T>(this IEnumerable<T> list)
        {
            if (list == null)
            {
                throw new NullReferenceException();
            }

            if (list.Count() == 0)
            {
                return "Empty";
            }

            StringBuilder sb = new StringBuilder();

            IEnumerator<T> enumerator = list.GetEnumerator();
            enumerator.MoveNext();

            sb.Append(enumerator.Current.ToString());

            while (enumerator.MoveNext())
            {
                sb.Append(",");
                sb.Append(enumerator.Current.ToString());
            }

            return sb.ToString();
        }
    }
}
