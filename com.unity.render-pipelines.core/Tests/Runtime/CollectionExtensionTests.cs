using System;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering.Tests
{
    public class CollectionExtensionTests
    {
        static TestCaseData[] s_ListTestsCaseDatas =
        {
            new TestCaseData(new List<int> {1,2,3,4,5,6}, 1, 2).SetName("Remove middle"),
            new TestCaseData(new List<int> {1,2,3,4,5,6}, 0, 2).SetName("Remove front"),
            new TestCaseData(new List<int> {1,2,3,4,5,6}, 0, 6).SetName("Remove all"),
            new TestCaseData(new List<int> {1,2,3,4,5,6}, 5, 1).SetName("Remove back"),
            new TestCaseData(new List<int> {1,2,3,4,5,6}, 5, -1).SetName("Count negative"),
            new TestCaseData(new List<int> {1,2,3,4,5,6}, -1, 2).SetName("Index negative"),
            new TestCaseData(new List<int> {1,2,3,4,5,6}, 5, 0).SetName("Count 0"),
            new TestCaseData(new List<int> {1,2,3,4,5,6}, 0, 0).SetName("Index 0"),
            new TestCaseData(new List<int> {1,2,3,4,5,6}, 5, 5).SetName("Count exceeds list size"),
        };

        [Test, TestCaseSource(nameof(s_ListTestsCaseDatas))]
        public void CheckRemoveRange(List<int> list, int startIndex, int count)
        {
            using (ListPool<int>.Get(out var copy))
            using (GenericPool<SimpleList>.Get(out var simpleList))
            {
                copy.AddRange(list);
                simpleList.AddRange(list);

                Type exceptionType = null;

                try
                {
                    (list as IList<int>).RemoveRange(startIndex, count);
                }
                catch (Exception e)
                {
                    exceptionType = e.GetType();
                }

                try
                {
                    simpleList.RemoveRange(startIndex, count);
                }
                catch (Exception e)
                {
                    Assert.AreEqual(e.GetType(), exceptionType);
                }

                try
                {
                    // Use the List<T> standard implementation to check the correct values
                    copy.RemoveRange(startIndex, count);
                }
                catch (Exception e)
                {
                    Assert.AreEqual(e.GetType(), exceptionType);
                }

                Assert.AreEqual(copy, list);
                Assert.AreEqual(copy, simpleList);
            }
        }

        class SimpleList : IList<int>
        {
            private List<int> m_List = new List<int>();

            public void AddRange(List<int> list)
            {
                m_List.Clear();
                m_List.AddRange(list);
            }

            public IEnumerator<int> GetEnumerator()
            {
                return m_List.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(int item)
            {
                m_List.Add(item);
            }

            public void Clear()
            {
                m_List.Clear();
            }

            public bool Contains(int item)
            {
                return m_List.Contains(item);
            }

            public void CopyTo(int[] array, int arrayIndex)
            {
                m_List.CopyTo(array, arrayIndex);
            }

            public bool Remove(int item)
            {
                return m_List.Remove(item);
            }

            public int Count => m_List.Count;
            public bool IsReadOnly => false;
            public int IndexOf(int item)
            {
                return m_List.IndexOf(item);
            }

            public void Insert(int index, int item)
            {
                m_List.Insert(index, item);
            }

            public void RemoveAt(int index)
            {
                m_List.RemoveAt(index);
            }

            public int this[int index]
            {
                get => m_List[index];
                set => m_List[index] = value;
            }
        }

    }
}
