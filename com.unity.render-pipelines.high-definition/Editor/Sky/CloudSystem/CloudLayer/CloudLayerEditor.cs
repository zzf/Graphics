using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CloudLayer))]
    class CloudLayerEditor : VolumeComponentEditor
    {
        readonly GUIContent scrollOrientationLabel = new GUIContent("Orientation", "Controls the orientation of the distortion relative to the X world vector (in degrees).\nThis value can be relative to the Global Wind Orientation defined in the Visual Environment.");
        readonly GUIContent scrollSpeedLabel = new GUIContent("Speed", "Sets the cloud scrolling speed in kilometers per hour.\nThis value can be relative to the Global Wind Speed defined in the Visual Environment.");

        struct CloudMapParameter
        {
            public SerializedDataParameter cloudMap;
            public SerializedDataParameter[] opacities;

            public SerializedDataParameter rotation;
            public SerializedDataParameter tint;

            public SerializedDataParameter distortion;
            public SerializedDataParameter scrollOrientation;
            public SerializedDataParameter scrollSpeed;
            public SerializedDataParameter flowmap;

            public SerializedDataParameter raymarching;
            public SerializedDataParameter steps;
            public SerializedDataParameter altitude;
            public SerializedDataParameter thickness;
            public SerializedDataParameter density;
            public SerializedDataParameter multiScattering;

            public SerializedDataParameter castShadows;
        }

        CloudMapParameter UnpackCloudMap(SerializedProperty serializedProperty)
        {
            var p = new RelativePropertyFetcher<CloudLayer.CloudMap>(serializedProperty);

            return new CloudMapParameter
            {
                cloudMap = Unpack(p.Find(x => x.cloudMap)),
                opacities = new SerializedDataParameter[]
                {
                    Unpack(p.Find(x => x.opacityR)),
                    Unpack(p.Find(x => x.opacityG)),
                    Unpack(p.Find(x => x.opacityB)),
                    Unpack(p.Find(x => x.opacityA))
                },

                rotation = Unpack(p.Find(x => x.rotation)),
                tint = Unpack(p.Find(x => x.tint)),
                distortion = Unpack(p.Find(x => x.distortionMode)),
                scrollOrientation = Unpack(p.Find(x => x.scrollOrientation)),
                scrollSpeed = Unpack(p.Find(x => x.scrollSpeed)),
                flowmap = Unpack(p.Find(x => x.flowmap)),

                raymarching = Unpack(p.Find(x => x.raymarching)),
                steps = Unpack(p.Find(x => x.steps)),
                altitude = Unpack(p.Find(x => x.altitude)),
                thickness = Unpack(p.Find(x => x.thickness)),
                density = Unpack(p.Find(x => x.density)),
                multiScattering = Unpack(p.Find(x => x.multiScattering)),
                castShadows = Unpack(p.Find(x => x.castShadows)),
            };
        }

        SerializedDataParameter m_Coverage, m_UpperHemisphereOnly, m_LayerCount;
        SerializedDataParameter m_Resolution, m_ShadowResolution;
        SerializedDataParameter m_ShadowMultiplier, m_ShadowTint, m_ShadowSize;
        CloudMapParameter[] m_Layers;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<CloudLayer>(serializedObject);

            m_Coverage = Unpack(o.Find(x => x.coverage));
            m_UpperHemisphereOnly = Unpack(o.Find(x => x.upperHemisphereOnly));
            m_LayerCount = Unpack(o.Find(x => x.layers));
            m_Resolution = Unpack(o.Find(x => x.resolution));

            m_ShadowMultiplier = Unpack(o.Find(x => x.shadowMultiplier));
            m_ShadowTint = Unpack(o.Find(x => x.shadowTint));
            m_ShadowResolution = Unpack(o.Find(x => x.shadowResolution));
            m_ShadowSize = Unpack(o.Find(x => x.shadowSize));

            m_Layers = new CloudMapParameter[]
            {
                UnpackCloudMap(o.Find(x => x.layerA)),
                UnpackCloudMap(o.Find(x => x.layerB))
            };
        }

        void PropertyField(CloudMapParameter map, string label)
        {
            DrawHeader(label);

            PropertyField(map.cloudMap);
            using (new IndentLevelScope())
            {
                for (int i = 0; i < 4; i++)
                    PropertyField(map.opacities[i]);
            }

            PropertyField(map.rotation);
            PropertyField(map.tint);

            PropertyField(map.distortion);
            if (map.distortion.value.intValue != (int)CloudDistortionMode.None)
            {
                EditorGUI.indentLevel++;
                PropertyField(map.scrollOrientation, scrollOrientationLabel);
                PropertyField(map.scrollSpeed, scrollSpeedLabel);
                if (map.distortion.value.intValue == (int)CloudDistortionMode.Flowmap)
                {
                    PropertyField(map.flowmap);
                }
                EditorGUI.indentLevel--;
            }

            PropertyField(map.raymarching);
            using (new IndentLevelScope())
            {
                PropertyField(map.steps);
                PropertyField(map.altitude);
                PropertyField(map.thickness);
                PropertyField(map.density);
                PropertyField(map.multiScattering);
            }
            PropertyField(map.castShadows);
        }

        bool CastShadows => m_Layers[0].castShadows.value.boolValue || (m_LayerCount.value.intValue == (int)CloudMapMode.Double && m_Layers[1].castShadows.value.boolValue);

        public override void OnInspectorGUI()
        {
            PropertyField(m_Coverage);
            if (showAdditionalProperties)
                PropertyField(m_UpperHemisphereOnly);
            PropertyField(m_LayerCount);
            if (showAdditionalProperties)
                PropertyField(m_Resolution);

            PropertyField(m_Layers[0], "Layer A");
            if (m_LayerCount.value.intValue == (int)CloudMapMode.Double)
                PropertyField(m_Layers[1], "Layer B");

            PropertyField(m_ShadowMultiplier);
            PropertyField(m_ShadowTint);
            if (showAdditionalProperties)
                PropertyField(m_ShadowResolution);

            PropertyField(m_ShadowSize);
        }
    }
}
