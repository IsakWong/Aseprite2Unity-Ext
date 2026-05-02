using Aseprite2Unity.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Aseprite2Unity.Samples.SliceCollider
{
    /// <summary>
    /// Inspector 编辑器：在基类 Inspector 之上追加 Slice Collider 配置项。
    /// </summary>
    [CustomEditor(typeof(SliceColliderAsepriteImporter))]
    public class SliceColliderAsepriteImporterEditor : ScriptedImporterEditor
    {
        private SerializedProperty m_CreateColliders;
        private SerializedProperty m_IsTrigger;

        public override void OnEnable()
        {
            base.OnEnable();
            m_CreateColliders = serializedObject.FindProperty(nameof(SliceColliderAsepriteImporter.m_CreateColliders));
            m_IsTrigger = serializedObject.FindProperty(nameof(SliceColliderAsepriteImporter.m_IsTrigger));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Slice Collider (Sample)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Aseprite 中以 'unity:collider' 开头的 slice 将被转换为 BoxCollider2D。\n" +
                "支持多个 slice，例如 unity:collider_body, unity:collider_feet。\n" +
                "如果定义了 unity:pivot，collider 会以该 pivot 为本地原点对齐。",
                MessageType.Info);

            EditorGUILayout.PropertyField(m_CreateColliders, new GUIContent("Create Colliders"));
            using (new EditorGUI.DisabledScope(!m_CreateColliders.boolValue))
            {
                EditorGUILayout.PropertyField(m_IsTrigger, new GUIContent("Is Trigger"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Base Importer Settings", EditorStyles.boldLabel);

            // 绘制基础字段（除了脚本指针）
            DrawPropertiesExcluding(serializedObject,
                "m_Script",
                nameof(SliceColliderAsepriteImporter.m_CreateColliders),
                nameof(SliceColliderAsepriteImporter.m_IsTrigger));

            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }
    }
}
