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
        static GUIContent findLockerText = new GUIContent("Show locked property");

        static string pushAncestorText = "Apply to {0}";


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
                MaterialVariant[] matVariantsWithoutDescendants = m_Variants.Where(mv => !matVariants.Any(candidate => mv.DescendsFrom(candidate))).ToArray();
                MaterialProperty[] matProperties = m_MaterialProperties;


                // These actions are only available if all variants have the same MaterialVariant parent
                GenericMenu.MenuFunction showLocker = null;
                List<KeyValuePair<string, GenericMenu.MenuFunction>> pushAncestor = null;
                var parents = m_Variants.Select(mv => mv.GetParent() as MaterialVariant).Where(p => p != null).Distinct(); // TODO This should check SGs too when we add SG locks
                if (parents.Count() == 1)
                {
                    MaterialVariant commonParent = parents.First();

                    // Menu action to ping the ancestor that is locking the property
                    if (anyVariantIsLockedByAncestors)
                    {
                        // Assertion: All variants have the same parent, so the ancestor locking the property should be the same
                        Object[] lockers = m_ObjectsLockingProperties[m_Variants[0]];
                        if (lockers != null && lockers.Length > 0 && lockers[0] is MaterialVariant ancestorLockingProperty)
                        {
                            showLocker = () => { EditorGUIUtility.PingObject(ancestorLockingProperty.material); };
                        }
                    }

                    // Menu action to push an override to an ancestor (only available if all variants are overriding to the same value)
                    if (numVariantsOverriding > 0 && matProperties.All(mp => !mp.hasMixedValue))
                    {
                        // Create a menu entry for each ancestor that is a MaterialVariant
                        pushAncestor = new List<KeyValuePair<string, GenericMenu.MenuFunction>>();

                        // Need to record the chain from the variants to each ancestor so, after pushing the value to it,
                        // the algorithm can follow the reverse path, back to the variants
                        IEnumerable<MaterialVariant> hierarchyStack = new MaterialVariant[0];
                        MaterialVariant currentAncestor = commonParent;
                        while (currentAncestor != null)
                        {
                            // Add the ancestor to the queue
                            hierarchyStack = hierarchyStack.Concat(new[] { currentAncestor });

                            // Copy the queue for this ancestor's action (in reverse order)
                            MaterialVariant[] currentStack = hierarchyStack.Reverse().ToArray();

                            // Create the action
                            pushAncestor.Add(new KeyValuePair<string, GenericMenu.MenuFunction>(currentAncestor.material.name,
                                () =>
                                {
                                    // Prepare the list of changes to push
                                    IEnumerable<MaterialPropertyModification> changes = new MaterialPropertyModification[0];
                                    foreach (var materialProperty in matProperties)
                                        changes = changes.Concat(MaterialPropertyModification.CreateMaterialPropertyModifications(materialProperty));

                                    // TODO Undo

                                    // Add the changes as overrides to the ancestor and apply them to its material
                                    MaterialVariant top = currentStack[0];
                                    top.TrimPreviousOverridesAndAdd(changes);
                                    top.ApplyOverridesToMaterial(top.material);

                                    // Follow the inheritance chain back to the variants
                                    MaterialVariant previous = top;
                                    for (int i = 1; i < currentStack.Length; i++)
                                    {                                    
                                        MaterialVariant ancestor = currentStack[i];

                                        // Reset the overrides of the current ancestor
                                        ancestor.ResetOverrides(matProperties);

                                        // Now, make the previous ancestor (this ancestor's parent) propagate its values to the current one
                                        MaterialVariant.UpdateHierarchy(previous.GUID, matProperties);

                                        previous = ancestor;
                                    }

                                    // Reset the overrides of all variants
                                    foreach (MaterialVariant variant in matVariants)
                                    {
                                        variant.ResetOverrides(matProperties);
                                    }

                                    // Make the last ancestor (the variants' common parent) propagate its values to all variants
                                    MaterialVariant.UpdateHierarchy(previous.GUID, matProperties);
                                }));

                            currentAncestor = currentAncestor.GetParent() as MaterialVariant;
                        }
                    }
                }

                DrawContextMenuAndIcons(m_Variants.Length > 1, numVariantsOverriding, anyVariantIsLockedByAncestors, anyVariantIsLocking, r,
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
                    (locked) => Array.ForEach(matVariantsWithoutDescendants, variant => variant.SetPropertiesLocked(matProperties, locked)),
                    showLocker,
                    pushAncestor);
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

        internal delegate void SetLockedFunction(bool locked);
        internal static void DrawContextMenuAndIcons(bool multiediting, int numVariantsOverriding, bool anyVariantIsLockedByAncestors, bool anyVariantIsLocking, Rect labelRect, GenericMenu.MenuFunction resetFunction, GenericMenu.MenuFunction resetAllFunction, SetLockedFunction lockFunction, GenericMenu.MenuFunction showLockerFunction, List<KeyValuePair<string, GenericMenu.MenuFunction>> pushAncestorFunctions)
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
                    if (!multiediting)
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
                    // At least one variant is locked by ancestors, show find locker option, available only if all share the same parent
                    if (showLockerFunction != null)
                    {
                        menu.AddItem(findLockerText, false, showLockerFunction);
                    }
                    else
                    {
                        menu.AddDisabledItem(findLockerText);
                    }
                }
                else if (anyVariantIsLocking)
                {
                    // At least one variant is locking, show unlock option
                    menu.AddItem(unlockText, false, () => lockFunction(false));
                }
                else
                {
                    // No variant is locking, show lock option
                    menu.AddItem(lockText, false, () => lockFunction(true));
                }

                // Push functions
                if (numVariantsOverriding > 0 && pushAncestorFunctions != null)
                {
                    menu.AddSeparator("");
                    foreach(var ancestor in pushAncestorFunctions)
                    {
                        GUIContent option = new GUIContent(string.Format(pushAncestorText, ancestor.Key));
                        menu.AddItem(option, false, ancestor.Value);
                    }
                }

                menu.ShowAsContext();
            }

            // White bar on overridden properties
            if (numVariantsOverriding > 0)
            {
                labelRect.width = 3;
                EditorGUI.DrawRect(labelRect, Color.white);
            }

            // Show locks on locked properties
            if (anyVariantIsLockedByAncestors || anyVariantIsLocking)
            {
                labelRect.xMin = 8;
                labelRect.width = 32;

                // If locked in ancestors, gray out
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
            if (m_Variants != null)
            {
                foreach (var matVariant in m_Variants)
                {
                    m_ObjectLockingProperty[matVariant] = matVariant.FindObjectLockingProperty(k_SerializedPropertyName);
                }
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
            MaterialVariant[] matVariantsWithoutDescendants = m_Variants.Where(mv => !matVariants.Any(candidate => mv.DescendsFrom(candidate))).ToArray();

            // This actions is only available if all variants have the same MaterialVariant parent
            GenericMenu.MenuFunction showLocker = null;
            List<KeyValuePair<string, GenericMenu.MenuFunction>> pushAncestor = null;
            var parents = m_Variants.Select(mv => mv.GetParent() as MaterialVariant).Where(p => p != null).Distinct(); // TODO This should check SGs too when we add SG locks
            if (parents.Count() == 1)
            {
                MaterialVariant commonParent = parents.First();

                // Menu action to ping the ancestor that is locking the property
                if (anyVariantIsLockedByAncestors)
                {
                    // Assertion: All variants have the same parent, so the ancestor locking the property should be the same
                    Object locker = m_ObjectLockingProperty[m_Variants[0]];
                    if (locker != null && locker is MaterialVariant ancestorLockingProperty)
                    {
                        showLocker = () => { EditorGUIUtility.PingObject(ancestorLockingProperty.material); };
                    }
                }
            }

            MaterialPropertyScope.DrawContextMenuAndIcons(
                m_Variants.Length > 1,
                numVariantsOverriding, anyVariantIsLockedByAncestors, anyVariantIsLocking,
                r,
                () => Array.ForEach(matVariants, variant => variant.ResetOverrideForNonMaterialProperty(k_SerializedPropertyName)),
                () => Array.ForEach(matVariants, variant => variant.ResetAllOverrides()),
                (locked) => Array.ForEach(matVariantsWithoutDescendants, variant => variant.SetPropertyLocked(k_SerializedPropertyName, locked)),
                showLocker,
                null); // Can't push this kind of property to ancestors


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
