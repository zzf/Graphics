using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A set of extension methods for collections
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Removes a given count from the back of the given list
        /// </summary>
        /// <param name="inputList">The list to be removed</param>
        /// <param name="elementsToRemoveFromBack">The number of elements to be removed from the back</param>
        public static void RemoveBack<T>([NotNull] this IList<T> inputList, int elementsToRemoveFromBack)
        {
            // Collection is empty
            var count = inputList.Count;
            elementsToRemoveFromBack = Mathf.Clamp(elementsToRemoveFromBack, 0, count);

            var index = count - elementsToRemoveFromBack;
            if (inputList is List<T> genericList)
            {
                genericList.RemoveRange(index, elementsToRemoveFromBack);
            }
            else
            {
                for (var i = count - 1; i >= index; --i)
                    inputList.RemoveAt(i);
            }
        }
    }
}
