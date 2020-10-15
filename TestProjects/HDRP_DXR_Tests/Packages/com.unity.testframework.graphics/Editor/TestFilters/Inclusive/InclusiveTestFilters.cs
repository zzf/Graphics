#if UNITY_EDITOR
using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using UnityEngine;

[System.Serializable]

[CreateAssetMenu(fileName = "InclusiveTestCaseFilters", menuName = "Testing/Inclusive Test Filter ScriptableObject", order = 101)]
public class InclusiveTestFilters : ScriptableObject
{
    public TestFilterConfig filter;
}
#endif
