using System;
using UnityEngine;
using System.Runtime.InteropServices;


namespace CubemapGenerator.Plugin {

/**
 * NativePlugin部分のラッパー
 */
static class CubemapBuilderPlugin {
	// ------------------------------------- public メンバ ----------------------------------------

	/** キューブマップへ各面のテクスチャをBlitする */
	public static void blitTex2Cubemap(
		IntPtr srcTex0,
		IntPtr srcTex1,
		IntPtr srcTex2,
		IntPtr srcTex3,
		IntPtr srcTex4,
		IntPtr srcTex5,
		IntPtr cubemapTex,
		int texWidth
	) {
		checkInitialized();

		BlitCubemap(
			srcTex0,
			srcTex1,
			srcTex2,
			srcTex3,
			srcTex4,
			srcTex5,
			cubemapTex,
			texWidth
		);
	}


	// --------------------------------- private / protected メンバ -------------------------------

	// プラグインの生関数定義
#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
	[DllImport("__Internal")]
#else
	[DllImport("CubemapBuilderPlugin")]
#endif
	static extern void BlitCubemap(
		IntPtr srcTex0,
		IntPtr srcTex1,
		IntPtr srcTex2,
		IntPtr srcTex3,
		IntPtr srcTex4,
		IntPtr srcTex5,
		IntPtr cubemapTex,
		int texWidth
	);


	// 初期化チェック。WebGLの場合は初期化が必要なので、これを呼ぶ必要がある
#if UNITY_WEBGL && !UNITY_EDITOR
	static bool s_isInitialized = false;
	static void checkInitialized() {
		if (s_isInitialized) return;
		s_isInitialized = true;

		RegisterPlugin();
	}
	[DllImport("__Internal")] static extern void RegisterPlugin();
#else
	[System.Diagnostics.Conditional( "___DUMMY___" )]
	static void checkInitialized() {}
#endif


	// --------------------------------------------------------------------------------------------
}

}
