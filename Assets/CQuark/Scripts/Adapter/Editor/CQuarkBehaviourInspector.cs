﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using CQuark;

[CustomEditor(typeof(CQuarkBehaviourText))]
public class ScriptMonoInspector : Editor {

	CQuarkBehaviourText _script;

	public override void OnInspectorGUI ()
	{
		//base.OnInspectorGUI ();
		_script = target as CQuarkBehaviourText;

		_script.m_codeType = (CQuarkBehaviourText.ECodeType)EditorGUILayout.EnumPopup ("CodeType", _script.m_codeType);

		switch (_script.m_codeType) {
		case CQuarkBehaviourText.ECodeType.FileName:
			GUILayout.Label("编译StreamingAssets下所有脚本");
			GUILayout.Space(5);
			_script.m_className = EditorGUILayout.TextField("类名", _script.m_className);
			break;

		case CQuarkBehaviourText.ECodeType.TextAsset:
			GUILayout.Label("TextAsset作为类");
			GUILayout.Space(5);
			_script.m_className = EditorGUILayout.TextField("类名", _script.m_className);
			_script.m_ta = EditorGUILayout.ObjectField(_script.m_ta, typeof(TextAsset), false) as TextAsset;
			break;

		case CQuarkBehaviourText.ECodeType.Text:
			GUILayout.Label("文本作为类");
			GUILayout.Space(5);
			_script.m_className = EditorGUILayout.TextField("类名", _script.m_className);
			_script.m_codeText = EditorGUILayout.TextArea(_script.m_codeText);
			break;
		}
	}
}
