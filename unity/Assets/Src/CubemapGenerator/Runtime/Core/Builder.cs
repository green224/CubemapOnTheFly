using System;
using UnityEngine;

using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections;



namespace CubemapGenerator.Core {

/**
 * キューブマップをレンダリングするコア処理部分。
 * 
 * これ自体も使用後はDisposeする必要があるが、
 * 生成したCubemapも、扱う側で不要になった際にDisposeする必要がある。
 */
sealed class Builder : IDisposable {
	// ------------------------------------- public メンバ ----------------------------------------

	/** 作成が完了したか否か */
	public bool IsComplete {get; private set;}

	/** 作成結果のキューブマップ。これは不要になったら使用する側で開放すること */
	public Cubemap Result {get; private set;}


	/** 使用するカメラ、パラメータを指定してレンダリングを開始する準備をする */
	public Builder(Camera camera, int texSize, float3 pos) {
		_camera = camera;
		_texSize = texSize;
		_pos = pos;

		_camera.enabled = false;
		_camera.fieldOfView = 90;
	}

	/**
	 * キューブマップをビルドする処理を進める。
	 * コルーチンでやりたいところだが、レンダリング時にコンテキスト情報が必要なため、RenderPipeline内で行う必要がある。
	 * したがってこのような関数呼び出しにしている。
	 */
	public void proceed(UnityEngine.Rendering.ScriptableRenderContext context) {
		if (_isDisposed) throw new InvalidOperationException();

		// コンストラクタでメモリ確保を極力したくないので、_tmpTex2Dはここで生成する。
		if (_tmpTex2D == null) _tmpTex2D = new Texture2D(_texSize, _texSize, TextureFormat.ARGB32, false, true);


		switch (_proccessIdx++) {

		// 0~5までは各面のレンダリングを行う
		case 0: renderFace(context, CubemapFace.PositiveX); break;
		case 1: renderFace(context, CubemapFace.NegativeX); break;
		case 2: renderFace(context, CubemapFace.PositiveY); break;
		case 3: renderFace(context, CubemapFace.NegativeY); break;
		case 4: renderFace(context, CubemapFace.PositiveZ); break;
		case 5: renderFace(context, CubemapFace.NegativeZ); break;

		// 各面のレンダリングが終了したら、キューブマップに格納する。
		// このキューブマップはDisposeされても解放されないので、使用する側で開放する必要があることに注意。
		case 6:
			Result = new Cubemap(_texSize, TextureFormat.ARGB32, 1);
			for (int i=0; i<6; ++i)
				_pixels[i].writeToCubemap( Result, (CubemapFace)i );
			Result.Apply(false, true);

			foreach (var i in _pixels) i.Dispose();
			UnityEngine.Object.DestroyImmediate(_tmpTex2D);

			IsComplete = true;
			break;

		default:
			break;
		}
	}

	public void Dispose() {
		if (_isDisposed) return;

		foreach (var i in _pixels) i.Dispose();
		_pixels = null;

		if (_tmpTex2D!=null) UnityEngine.Object.DestroyImmediate(_tmpTex2D);
		_tmpTex2D = null;
		_isDisposed = true;
	}


	// --------------------------------- private / protected メンバ -------------------------------

	bool _isDisposed = false;
	int _proccessIdx = 0;		//!< 処理の進行カウント

	Camera _camera;
	int _texSize;
	float3 _pos;

	PixelDataCache[] _pixels = new PixelDataCache[6];		//!< 各面のレンダリング結果のキャッシュ
	Texture2D _tmpTex2D;		//!< RTからピクセル情報を取得するためのテンポラリバッファ


	/** 指定の方向の面をレンダリングする処理 */
	void renderFace(
		UnityEngine.Rendering.ScriptableRenderContext context,
		CubemapFace faceIndex
	) {

		// カメラの姿勢を決定
		Quaternion rot;
		switch (faceIndex) {
		case CubemapFace.PositiveX : rot = Quaternion.Euler(   0,  90, 0 ); break;
		case CubemapFace.NegativeX : rot = Quaternion.Euler(   0, -90, 0 ); break;
		case CubemapFace.PositiveY : rot = Quaternion.Euler( -90,   0, 0 ); break;
		case CubemapFace.NegativeY : rot = Quaternion.Euler(  90,   0, 0 ); break;
		case CubemapFace.PositiveZ : rot = Quaternion.Euler(   0,   0, 0 ); break;
		case CubemapFace.NegativeZ : rot = Quaternion.Euler(   0, 180, 0 ); break;
		default: throw new ArgumentException("faceIndex:" + faceIndex);
		}
		var camTrans = _camera.transform;
		camTrans.rotation = rot;
		camTrans.position = _pos;

		// レンダリング先のRTを確保
		var desc = new RenderTextureDescriptor(_texSize, _texSize, RenderTextureFormat.ARGB32);
		desc.sRGB = false;
		var rt = RenderTexture.GetTemporary(desc);

		// RTにレンダリング
		_camera.targetTexture = rt;
		UnityEngine.Rendering.Universal.UniversalRenderPipeline.RenderSingleCamera(context, _camera);
		_camera.targetTexture = null;

		// レンダリング結果をTexture2Dへ転写
		var lastRTTgt = RenderTexture.active;
		RenderTexture.active = rt;
		_tmpTex2D.ReadPixels(new Rect(0, 0, _texSize, _texSize), 0, 0);
		_tmpTex2D.Apply(false);
		RenderTexture.active = lastRTTgt;

		// Texture2Dからピクセル情報を吸出してキャッシュに格納
		var pd = new PixelDataCache(true);
		pd.readFromTex2D(_tmpTex2D, false, true);
		_pixels[(int)faceIndex] = pd;


		RenderTexture.ReleaseTemporary(rt);
	}

	~Builder() {
		if (!_isDisposed) throw new InvalidProgramException();
	}



	// --------------------------------------------------------------------------------------------
}

}
