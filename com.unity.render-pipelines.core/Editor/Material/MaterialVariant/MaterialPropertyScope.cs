using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.MaterialVariants
{
    public struct MaterialPropertyScope : IDisposable
    {
        static GUIContent revertSingleText = new GUIContent("Revert");
        static string revertMultiText = "Revert on {0} Material(s)";
        static GUIContent revertAllText = new GUIContent("Revert all Overrides");

        static GUIContent lockCurrentIcon = new GUIContent(EditorGUIUtility.IconContent("AssemblyLock").image, "Property is locked in children");
        static GUIContent lockAncestorIcon = new GUIContent(EditorGUIUtility.IconContent("AssemblyLock").image, "Property is locked by an ancestor");
        static GUIContent lockText = new GUIContent("Lock in Children");
        static GUIContent unlockText = new GUIContent("Unlock");
        static GUIContent findLockerText = new GUIContent("See Locked Property");


        static bool insidePropertyScope = false;


        MaterialProperty[] m_MaterialProperties;

        MaterialVariant[] m_Variants;
        Dictionary<MaterialVariant, Object[]> m_ObjectsLockingProperties;

        Object[] m_Materials;

        bool m_Force;
        DelayedOverrideRegisterer m_Registerer;
        float m_StartY;

        /// <summary>
        /// MaterialPropertyScope are used to handle MaterialPropertyModification in material instances.
        /// This will do the registration of any new override but also this will do the UI (contextual menu and left bar displayed when there is an override).
        /// </summary>
        /// <param name="materialProperty">The materialProperty that we need to register</param>
        /// <param name="variants">The list of MaterialVariant should have the same size than elements in selection.</param>
        /// <param name="force">
        /// The force registration is for MaterialProperty that are changed at inspector frame without change from the user.
        /// In this case, we skip the UI part (contextual menu and left bar displayed when there is an override).
        /// </param>
        public MaterialPropertyScope(MaterialProperty[] materialProperties, MaterialVariant[] variants, bool force = false)
        {
            if (insidePropertyScope == true)
            {
                insidePropertyScope = false;
                throw new InvalidOperationException("Nested MaterialPropertyScopes are not allowed");
            }

            insidePropertyScope = true;

            m_MaterialProperties = materialProperties.Where(p => p != null).ToArray();

            m_Variants = variants;
            m_ObjectsLockingProperties = new Dictionary<MaterialVariant, Object[]>();
            foreach (var matVariant in m_Variants)
            {
                m_ObjectsLockingProperties[matVariant] = m_MaterialProperties.Select(mp => matVariant.FindObjectLockingProperty(mp.name)).Where(o => o != null).Distinct().ToArray();
            }


            m_Materials = null;

            m_Force = force;
            m_Registerer = null;
            // Get the current Y coordinate before drawing the property
            // We define a new empty rect in order to grab the current height even if there was nothing drawn in the block (GetLastRect cause issue if it was first element of block)
            m_StartY = GUILayoutUtility.GetRect(0, 0).yMax;

            if (m_Force)
                return;

            bool anyVariantIsLockedByAncestors = m_ObjectsLockingProperties.Any(kv => kv.Value.Any(o => o != kv.Key));
            if (anyVariantIsLockedByAncestors)
                EditorGUI.BeginDisabledGroup(true);
            else
                EditorGUI.BeginChangeCheck();
        }

        public MaterialPropertyScope(MaterialProperty[] materialProperties, Object[] materials)
        {
            m_MaterialProperties = materialProperties;

            m_Variants = null;
            m_ObjectsLockingProperties = null;

            m_Materials = materials;

            m_Force = false;
            m_Registerer = null;
            m_StartY = 0;

            EditorGUI.BeginChangeCheck();
        }

        void IDisposable.Dispose()
        {
            insidePropertyScope = false;

            // Exit path for the alternative material-based constructor
            if (m_Variants == null)
            {
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var material in m_Materials)
                    {
                        var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material));
                        MaterialVariant.RecordObjectsUndo(guid, m_MaterialProperties);
                        MaterialVariant.UpdateHierarchy(guid, m_MaterialProperties);
                    }
                }
                return;
            }

            bool anyVariantIsLockedByAncestors = m_ObjectsLockingProperties.Any(kv => kv.Value.Any(o => o != kv.Key));

            // force registration is for MaterialProperty that are changed at inspector frame without change from the user
            if (!m_Force)
            {
                if (anyVariantIsLockedByAncestors)
                {
                    EditorGUI.EndDisabledGroup();
                }

                int numVariantsOverriding = 0;
                bool anyVariantIsLocking = false;
                if (!anyVariantIsLockedByAncestors)
                {
                    var materialProperties = m_MaterialProperties;
                    numVariantsOverriding = m_Variants.Count(mv => materialProperties.Any(mp => mv.IsOverriddenProperty(mp)));
                    anyVariantIsLocking = m_Variants.Any(mv => materialProperties.Any(mp => mv.FindObjectLockingProperty(mp.name) == mv));
                }

                Rect r = GUILayoutUtility.GetLastRect();
                float endY = r.yMax;
                r.xMin = 1;
                r.yMin = m_StartY + 2;
                r.yMax = endY - 2;
                r.width = EditorGUIUtility.labelWidth;

                MaterialVariant[] matVariants = m_Variants;
                MaterialProperty[] matProperties = m_MaterialProperties;
                DrawContextMenuAndIcons(m_Variants.Length, numVariantsOverriding, anyVariantIsLockedByAncestors, anyVariantIsLocking, r,
                    () =>
                    {
                        MaterialVariant.RecordObjectsUndo(matVariants, matProperties);
                        foreach (var variant in matVariants)
                        {
                            variant.ResetOverrides(matProperties);
                            MaterialVariant.UpdateHierarchy(variant.GUID, matProperties);
                        }
                    },
                    () => Array.ForEach(matVariants, variant => variant.ResetAllOverrides()),
                    () => Array.ForEach(matVariants, variant => variant.TogglePropertiesBlocked(matProperties)));
            }

            bool hasChanged = m_Force;
            if (!hasChanged && !anyVariantIsLockedByAncestors)
                hasChanged = EditorGUI.EndChangeCheck(); //Stop registering change

            if (hasChanged)
            {
                if (m_Registerer != null)
                    m_Registerer.SetAllowRegister(true);
                else
                    ApplyChangesToMaterialVariants();
            }
        }

        void ApplyChangesToMaterialVariants()
        {
            if (m_Variants == null)
                return;

            IEnumerable<MaterialPropertyModification> changes = new MaterialPropertyModification[0];
            foreach (var materialProperty in m_MaterialProperties)
                changes = changes.Concat(MaterialPropertyModification.CreateMaterialPropertyModifications(materialProperty));

            MaterialVariant.RecordObjectsUndo(m_Variants, m_MaterialProperties);
            foreach (var variant in m_Variants)
            {
                variant?.TrimPreviousOverridesAndAdd(changes);
                MaterialVariant.UpdateHierarchy(variant.GUID, m_MaterialProperties);
            }
        }

        public DelayedOverrideRegisterer ProduceDelayedRegisterer()
        {
            if (m_Registerer != null)
                throw new Exception($"A delayed registerer already exists for this MaterialPropertyScope for {m_MaterialProperties[0].displayName}. You should only use one at the end of all operations on this property.");

            m_Registerer = new DelayedOverrideRegisterer(ApplyChangesToMaterialVariants);
            return m_Registerer;
        }

        public class DelayedOverrideRegisterer
        {
            internal delegate void RegisterFunction();

            RegisterFunction m_RegisterFunction;
            bool m_AlreadyRegistered;
            bool m_AllowRegister;

            internal DelayedOverrideRegisterer(RegisterFunction registerFunction)
            {
                m_RegisterFunction = registerFunction;
                m_AlreadyRegistered = false;
                m_AllowRegister = false;
            }

            internal void SetAllowRegister(bool allow)
            {
                m_AllowRegister = allow;
            }

            public void RegisterNow()
            {
                if (!m_AlreadyRegistered && m_AllowRegister)
                {
                    m_RegisterFunction();
                    m_AlreadyRegistered = true;
                }
            }
        }

        internal static void DrawContextMenuAndIcons(int numVariants, int numVariantsOverriding, bool anyVariantIsLockedByAncestors, bool anyVariantIsLocking, Rect labelRect, GenericMenu.MenuFunction resetFunction, GenericMenu.MenuFunction resetAllFunction, GenericMenu.MenuFunction blockFunction)
        {
            // Assertion: If anyVariantIsLockedByAncestors is set to true, both anyVariantIsOverriding and anyVariantIsLocking must be false

            // Contextual menu
            if (Event.current.rawType == EventType.ContextClick && labelRect.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();

                // Revert and Revert All options
                if (numVariantsOverriding > 0)
                {
                    // Single material
                    if (numVariants == 1)
                    {
                        menu.AddItem(revertSingleText, false, resetFunction);
                        menu.AddItem(revertAllText, false, resetAllFunction);
                    }
                    // Multiediting
                    else
                    {
                        menu.AddItem(new GUIContent(String.Format(revertMultiText, numVariantsOverriding)), false, resetFunction);
                        menu.AddDisabledItem(revertAllText);
                    }
                }

                // Lock options
                if (anyVariantIsLockedByAncestors)
                {
                    menu.AddDisabledItem(findLockerText);
                }
                else
                {
                    menu.AddItem(!anyVariantIsLocking ? lockText : unlockText, false, blockFunction);
                }

                menu.ShowAsContext();
            }

            // White bar on overriden properties
            if (numVariantsOverriding > 0)
            {
                labelRect.width = 3;
                EditorGUI.DrawRect(labelRect, Color.white);
            }

            // Locks on locked properties
            if (anyVariantIsLockedByAncestors || anyVariantIsLocking)
            {
                labelRect.xMin = 8;
                labelRect.width = 32;
                EditorGUI.BeginDisabledGroup(anyVariantIsLockedByAncestors);
                GUI.Label(labelRect, anyVariantIsLockedByAncestors ? lockAncestorIcon : lockCurrentIcon);
                EditorGUI.EndDisabledGroup();
            }
        }
    }

    public struct MaterialRenderQueueScope : IDisposable
    {
        MaterialVariant[] m_Variants;
        Dictionary<MaterialVariant, Object> m_ObjectLockingProperty;

        Func<int> m_ValueGetter;

        bool m_HaveDelayedRegisterer;
        float m_StartY;

        const string k_SerializedPropertyName = "_RenderQueueType";

        /// <summary>
        /// MaterialRenderQueueScope is used to handle MaterialPropertyModification in material instances around renderqueue.
        /// This will do the registration of any new override but also this will do the UI (contextual menu and left bar displayed when there is an override).
        /// </summary>
        /// <param name="variants">The list of MaterialVariant should have the same size than elements in selection.</param>
        public MaterialRenderQueueScope(MaterialVariant[] variants, Func<int> valueGetter)
        {
            m_Variants = variants;
            m_ObjectLockingProperty = new Dictionary<MaterialVariant, Object>();
            foreach (var matVariant in m_Variants)
            {
                m_ObjectLockingProperty[matVariant] = matVariant.FindObjectLockingProperty(k_SerializedPropertyName);
            }
            m_ValueGetter = valueGetter;

            m_HaveDelayedRegisterer = false;

            // Get the current Y coordinate before drawing the property
            // We define a new empty rect in order to grab the current height even if there was nothing drawn in the block (GetLastRect cause issue if it was first element of block)
            m_StartY = GUILayoutUtility.GetRect(0, 0).yMax;

            //Starting registering change
            if (m_Variants == null)
                return;

            bool anyVariantIsLockedByAncestors = m_ObjectLockingProperty.Any(kv => kv.Value  != null && kv.Value != kv.Key);
            if (anyVariantIsLockedByAncestors)
                EditorGUI.BeginDisabledGroup(true);
            else
                EditorGUI.BeginChangeCheck();
        }

        void IDisposable.Dispose()
        {
            if (m_Variants == null)
                return;

            bool anyVariantIsLockedByAncestors = m_ObjectLockingProperty.Any(kv => kv.Value != null && kv.Value != kv.Key);
            if (anyVariantIsLockedByAncestors)
                EditorGUI.EndDisabledGroup();


            int numVariantsOverriding = anyVariantIsLockedByAncestors ? 0 : m_Variants.Count(mv => mv.IsOverriddenPropertyForNonMaterialProperty(k_SerializedPropertyName));
            bool anyVariantIsLocking = !anyVariantIsLockedByAncestors && m_Variants.Any(o => o.FindObjectLockingProperty(k_SerializedPropertyName) == o);

            Rect r = GUILayoutUtility.GetLastRect();
            float endY = r.yMax;
            r.xMin = 1;
            r.yMin = m_StartY + 2;
            r.yMax = endY - 2;
            r.width = EditorGUIUtility.labelWidth;

            MaterialVariant[] matVariants = m_Variants;
            MaterialPropertyScope.DrawContextMenuAndIcons(m_Variants.Length, numVariantsOverriding, anyVariantIsLockedByAncestors, anyVariantIsLocking, r,
                () => Array.ForEach(matVariants, variant => variant.ResetOverrideForNonMaterialProperty(k_SerializedPropertyName)),
                () => Array.ForEach(matVariants, variant => variant.ResetAllOverrides()),
                () => Array.ForEach(matVariants, variant => variant.TogglePropertyBlocked(k_SerializedPropertyName)));


            bool hasChanged = !anyVariantIsLockedByAncestors && EditorGUI.EndChangeCheck();
            if (hasChanged && !m_HaveDelayedRegisterer)
            {
                ApplyChangesToMaterialVariants();
            }
        }

        void ApplyChangesToMaterialVariants()
        {
            System.Collections.Generic.IEnumerable<MaterialPropertyModification> changes = MaterialPropertyModification.CreateMaterialPropertyModificationsForNonMaterialProperty(k_SerializedPropertyName, m_ValueGetter());
            foreach (var variant in m_Variants)
                variant?.TrimPreviousOverridesAndAdd(changes);
        }

        public DelayedOverrideRegisterer ProduceDelayedRegisterer()
        {
            if (m_HaveDelayedRegisterer)
                throw new Exception($"A delayed registerer already exists for this MaterialPropertyScope for {k_SerializedPropertyName}. You should only use one at the end of all operations on this property.");

            m_HaveDelayedRegisterer = true;

            return new DelayedOverrideRegisterer(ApplyChangesToMaterialVariants);
        }

        public struct DelayedOverrideRegisterer
        {
            internal delegate void RegisterFunction();

            RegisterFunction m_RegisterFunction;
            bool m_AlreadyRegistered;

            internal DelayedOverrideRegisterer(RegisterFunction registerFunction)
            {
                m_RegisterFunction = registerFunction;
                m_AlreadyRegistered = false;
            }

            public void RegisterNow()
            {
                if (!m_AlreadyRegistered)
                {
                    m_RegisterFunction();
                    m_AlreadyRegistered = true;
                }
            }
        }
    }

    public struct MaterialNonDrawnPropertyScope<T> : IDisposable
        where T : struct
    {
        MaterialVariant[] m_Variants;
        string m_PropertyName;
        T m_Value;

        /// <summary>
        /// MaterialPropertyScope are used to handle MaterialPropertyModification in material instances.
        /// This will do the registration of any new override but also this will do the UI (contextual menu and left bar displayed when there is an override).
        /// </summary>
        /// <param name="propertyName">The key to register</param>
        /// <param name="value">value to register</param>
        /// <param name="variants">The list of MaterialVariant should have the same size than elements in selection.</param>
        public MaterialNonDrawnPropertyScope(string propertyName, T value, MaterialVariant[] variants)
        {
            m_Variants = variants;
            m_PropertyName = propertyName;
            m_Value = value;
        }

        void IDisposable.Dispose()
        {
            if (m_Variants == null)
                return;

            IEnumerable<MaterialPropertyModification> changes = MaterialPropertyModification.CreateMaterialPropertyModificationsForNonMaterialProperty(m_PropertyName, m_Value);
            foreach (var variant in m_Variants)
                variant?.TrimPreviousOverridesAndAdd(changes);
        }
    }
}
