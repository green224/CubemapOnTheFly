using System;
using UnityEngine;


namespace CubemapOnTheFly {

/**
 * デバッグ用コンポーネント。
 * これを使用して挙動を確認することができる。
 */
[AddComponentMenu("CubemapOnTheFly/CubemapOnTheFly_Debugger")]
[ExecuteAlways]
public sealed class Debugger : MonoBehaviour {
	// --------------------------- インスペクタに公開しているフィールド -----------------------------

	[SerializeField] Manager _manager = null;
	[SerializeField] MeshRenderer _meshRenderer4check = null;

	[Space]
	[SerializeField][Range(8,1024)] int _texSize = 512;


	// ------------------------------------- public メンバ ----------------------------------------

	// --------------------------------- private / protected メンバ -------------------------------

	void OnEnable() {
		// 現在設定されているテクスチャを開放
		var lastTex = _meshRenderer4check.material.mainTexture;
		if (lastTex != null) DestroyImmediate(lastTex);
		_meshRenderer4check.material.mainTexture = null;

		// Cubemapテクスチャを再生成
		_manager.beginRender(
			_texSize, transform.position,
			cubemap => {
				if (cubemap != null)
					_meshRenderer4check.material.mainTexture = cubemap;
			}
		);
	}


	// --------------------------------------------------------------------------------------------
}

}
