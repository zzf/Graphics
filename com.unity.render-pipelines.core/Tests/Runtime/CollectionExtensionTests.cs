using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Tests
{
    public class CollectionExtensionTests
    {
        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(new List<int>(), 0)
                .Returns(new List<int>())
                .SetName("Empty"),
            new TestCaseData(new List<int>(), -1)
                .Returns(new List<int>())
                .SetName("EmptyNegative"),
            new TestCaseData(new List<int>() {1,2}, -1)
                .Returns(new List<int>() {1,2})
                .SetName("NonEmptyNegative"),
            new TestCaseData(new List<int>() {1,2}, 2)
                .Returns(new List<int>())
                .SetName("All"),
            new TestCaseData(new List<int>() {1,2}, 1)
                .Returns(new List<int>() {1})
                .SetName("JustLast"),
            new TestCaseData(new List<int>() {1,2}, 3)
                .Returns(new List<int>())
                .SetName("BiggerThanCollection"),
            new TestCaseData(new List<int>() {1,2,3,4,5,6,7}, 3)
                .Returns(new List<int>(){1,2,3,4})
                .SetName("SomeElements"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public List<int> TestsList(List<int> ints, int elementsToRemove)
        {
            ints.RemoveBack(elementsToRemove);
            return ints;
        }

        public class SimpleList : IList<int>
        {
            private List<int> m_List;

            public SimpleList(params int[] list)
            {
                m_List = new List<int>(list);
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

        static TestCaseData[] s_SimpleListTestsCaseDatas =
        {
            new TestCaseData(new SimpleList(), 0)
                .Returns(new SimpleList())
                .SetName("Empty"),
            new TestCaseData(new SimpleList(), -1)
                .Returns(new SimpleList())
                .SetName("EmptyNegative"),
            new TestCaseData(new SimpleList(1,2), -1)
                .Returns(new SimpleList(1,2))
                .SetName("NonEmptyNegative"),
            new TestCaseData(new SimpleList(1,2), 2)
                .Returns(new SimpleList())
                .SetName("All"),
            new TestCaseData(new SimpleList(1,2), 1)
                .Returns(new SimpleList(1))
                .SetName("JustLast"),
            new TestCaseData(new SimpleList(1,2), 3)
                .Returns(new SimpleList())
                .SetName("BiggerThanCollection"),
            new TestCaseData(new SimpleList(1,2,3,4,5,6,7), 3)
                .Returns(new SimpleList(1,2,3,4))
                .SetName("SomeElements"),
        };

        [Test, TestCaseSource(nameof(s_SimpleListTestsCaseDatas))]
        public SimpleList SimpleListTests(SimpleList ints, int elementsToRemove)
        {
            ints.RemoveBack(elementsToRemove);
            return ints;
        }
    }
}
