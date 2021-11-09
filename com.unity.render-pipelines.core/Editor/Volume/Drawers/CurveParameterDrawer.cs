using UnityEngine;
using UnityEngine.Rendering;
using UnityEditorInternal;

namespace UnityEditor.Rendering
{
    [VolumeParameterDrawer(typeof(TextureCurveParameter))]
    sealed class TextureCurveParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Generic)
                return false;

            EditorGUILayout.PropertyField(value.FindPropertyRelative("m_Curve"), title);
            var o = parameter.GetObjectRef<TextureCurveParameter>();
            return true;
        }
    }
}
