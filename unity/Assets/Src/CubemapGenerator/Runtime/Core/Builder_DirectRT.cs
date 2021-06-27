using System;
using UnityEngine;

using Unity.Mathematics;
using static Unity.Mathematics.math;



namespace CubemapGenerator.Core {

/**
 * キューブマップをレンダリングするコア処理部分。
 * 
 * RTをCubemapとして直接使用するバージョン。
 */
sealed class Builder_DirectRT : Builder_Base {
	// ------------------------------------- public メンバ ----------------------------------------

	/** 使用するカメラ、パラメータを指定してレンダリングを開始する準備をする */
	public Builder_DirectRT(Camera camera, int texSize, float3 pos, Shader blitShader)
		: base(camera, texSize, pos)
	{
		if (s_blitMtl == null)
			s_blitMtl = new Material(blitShader);
	}


	// --------------------------------- private / protected メンバ -------------------------------

	static Material s_blitMtl;
	RenderTexture _cubemapRT;

	/** 指定の方向の面をレンダリングする処理 */
	override protected void renderFace(
		UnityEngine.Rendering.ScriptableRenderContext context,
		CubemapFace faceIndex
	) {
		// キューブマップ本体のRTを確保
		if (_cubemapRT == null) {
			_cubemapRT = new RenderTexture(
				new RenderTextureDescriptor(
					_texSize, _texSize,
					RenderTextureFormat.ARGB32
				) {
					dimension = UnityEngine.Rendering.TextureDimension.Cube
				}
			);
		}

		// レンダリング先のRTを確保
		var desc = new RenderTextureDescriptor(
			_texSize, _texSize, RenderTextureFormat.ARGB32, 16
		);
		var rt = RenderTexture.GetTemporary(desc);

		// RTにレンダリング
		renderFace(rt, context, faceIndex);

		// キューブマップへBlit
		Graphics.SetRenderTarget( _cubemapRT, 0, faceIndex );
		Graphics.Blit( rt, s_blitMtl );
		RenderTexture.active = null;
	}

	/** 各面をレンダリングした結果からキューブマップを生成する */
	override protected Texture compileCubemap(UnityEngine.Rendering.ScriptableRenderContext context) {
		// キューブマップを直接RTとしてレンダリングしているので、ここは返すだけでいい
		var ret = _cubemapRT;
		_cubemapRT = null;
		return ret;
	}

	/** 破棄処理本体 */
	override protected void disposeCore() {

		if (_cubemapRT != null) _cubemapRT.Release();
		_cubemapRT = null;
	}


	// --------------------------------------------------------------------------------------------
}

}
