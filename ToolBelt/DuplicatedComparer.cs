using System;
using System.Collections.Generic;

namespace ToolBelt
{
    public class DuplicateKeyComparer<TKey>
                :
             IComparer<TKey> where TKey : IComparable
    {
        #region IComparer<TKey> Members

        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);

            if (result == 0)
                return -1;
            else
                return result;
        }

        #endregion
    }
}
