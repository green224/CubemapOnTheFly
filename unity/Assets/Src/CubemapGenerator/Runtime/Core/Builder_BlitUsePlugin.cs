using System;
using UnityEngine;

using Unity.Mathematics;
using static Unity.Mathematics.math;



namespace CubemapGenerator.Core {

/**
 * キューブマップをレンダリングするコア処理部分。
 * 
 * RTからCubemapにBlitして生成するバージョン。
 * Nativeプラグインを使用して生成する。
 */
sealed class Builder_BlitUsePlugin : Builder_Base {
	// ------------------------------------- public メンバ ----------------------------------------

	/** 使用するカメラ、パラメータを指定してレンダリングを開始する準備をする */
	public Builder_BlitUsePlugin(Camera camera, int texSize, float3 pos)
		: base(camera, texSize, pos) {}


	// --------------------------------- private / protected メンバ -------------------------------

	RenderTexture[] _rt = new RenderTexture[6];

	/** 指定の方向の面をレンダリングする処理 */
	override protected void renderFace(
		UnityEngine.Rendering.ScriptableRenderContext context,
		CubemapFace faceIndex
	) {
		if (_rt[ (int)faceIndex ] != null)
			throw new InvalidProgramException();

		// レンダリング先のRTを確保
		var desc = new RenderTextureDescriptor(
			_texSize, _texSize, RenderTextureFormat.ARGB32, 16
		);
//		desc.sRGB = false;
		var rt = new RenderTexture(desc);
		_rt[ (int)faceIndex ] = rt;

		// RTにレンダリング
		renderFace(rt, context, faceIndex);
	}

	/** 各面をレンダリングした結果からキューブマップを生成する */
	override protected Texture compileCubemap(UnityEngine.Rendering.ScriptableRenderContext context) {
		var ret = new Cubemap(_texSize, TextureFormat.ARGB32, 1);

		// プラグインでキューブマップへBlitする
		Plugin.CubemapBuilderPlugin.blitTex2Cubemap(
			_rt[0].GetNativeTexturePtr(),
			_rt[1].GetNativeTexturePtr(),
			_rt[2].GetNativeTexturePtr(),
			_rt[3].GetNativeTexturePtr(),
			_rt[4].GetNativeTexturePtr(),
			_rt[5].GetNativeTexturePtr(),
			ret.GetNativeTexturePtr(),
			_texSize
		);

		return ret;
	}

	/** 破棄処理本体 */
	override protected void disposeCore() {

		if (_rt != null)
			foreach (var i in _rt) i?.Release();
		_rt = null;
	}


	// --------------------------------------------------------------------------------------------
}

}
