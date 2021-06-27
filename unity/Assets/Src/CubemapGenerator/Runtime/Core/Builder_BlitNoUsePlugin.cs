using System;
using UnityEngine;

using Unity.Mathematics;
using static Unity.Mathematics.math;



namespace CubemapGenerator.Core {

/**
 * キューブマップをレンダリングするコア処理部分。
 * 
 * RTからCubemapにBlitして生成するバージョン。
 * Nativeプラグインを使用せずに、Unity内の機能のみで生成する。遅い。
 */
sealed class Builder_BlitNoUsePlugin : Builder_Base {
	// ------------------------------------- public メンバ ----------------------------------------

	/** 使用するカメラ、パラメータを指定してレンダリングを開始する準備をする */
	public Builder_BlitNoUsePlugin(Camera camera, int texSize, float3 pos)
		: base(camera, texSize, pos) {}


	// --------------------------------- private / protected メンバ -------------------------------

	PixelDataCache[] _pixels = new PixelDataCache[6];		//!< 各面のレンダリング結果のキャッシュ
	Texture2D _tmpTex2D;		//!< RTからピクセル情報を取得するためのテンポラリバッファ


	/** 指定の方向の面をレンダリングする処理 */
	override protected void renderFace(
		UnityEngine.Rendering.ScriptableRenderContext context,
		CubemapFace faceIndex
	) {
		// コンストラクタでメモリ確保を極力したくないので、_tmpTex2Dはここで生成する。
		if (_tmpTex2D == null) _tmpTex2D = new Texture2D(_texSize, _texSize, TextureFormat.ARGB32, false, true);

		// レンダリング先のRTを確保
		var desc = new RenderTextureDescriptor(_texSize, _texSize, RenderTextureFormat.ARGB32);
		desc.sRGB = false;
		var rt = RenderTexture.GetTemporary(desc);

		// RTにレンダリング
		renderFace(rt, context, faceIndex);

		// レンダリング結果をTexture2Dへ転写
		var lastRTTgt = RenderTexture.active;
		RenderTexture.active = rt;
		_tmpTex2D.ReadPixels(new Rect(0, 0, _texSize, _texSize), 0, 0);
		_tmpTex2D.Apply(false);
		RenderTexture.active = lastRTTgt;

		// Texture2Dからピクセル情報を吸出してキャッシュに格納
		var pd = new PixelDataCache(true);
		pd.readFromTex2D(_tmpTex2D, false, false);
		_pixels[(int)faceIndex] = pd;


		RenderTexture.ReleaseTemporary(rt);
	}

	/** 各面をレンダリングした結果からキューブマップを生成する */
	override protected Texture compileCubemap(UnityEngine.Rendering.ScriptableRenderContext context) {
		var ret = new Cubemap(_texSize, TextureFormat.ARGB32, 1);
		for (int i=0; i<6; ++i)
			_pixels[i].writeToCubemap( ret, (CubemapFace)i );
		ret.Apply(false, true);

		foreach (var i in _pixels) i.Dispose();
		UnityEngine.Object.DestroyImmediate(_tmpTex2D);

		return ret;
	}

	/** 破棄処理本体 */
	override protected void disposeCore() {
		foreach (var i in _pixels) i.Dispose();
		_pixels = null;

		if (_tmpTex2D != null) UnityEngine.Object.DestroyImmediate(_tmpTex2D);
		_tmpTex2D = null;
	}


	// --------------------------------------------------------------------------------------------
}

}
