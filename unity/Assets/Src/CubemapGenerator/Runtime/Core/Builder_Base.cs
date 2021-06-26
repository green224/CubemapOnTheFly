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
abstract class Builder_Base : IDisposable {
	// ------------------------------------- public メンバ ----------------------------------------

	/** 作成が完了したか否か */
	public bool IsComplete {get; private set;}

	/** 作成結果のキューブマップ。これは不要になったら使用する側で開放すること */
	public Cubemap Result {get; private set;}


	/** 使用するカメラ、パラメータを指定してレンダリングを開始する準備をする */
	public Builder_Base(Camera camera, int texSize, float3 pos) {
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
			Result = compileCubemap(context);
			IsComplete = true;
			break;

		default:
			break;
		}
	}

	public void Dispose() {
		if (_isDisposed) return;

		disposeCore();

		_isDisposed = true;
	}


	// --------------------------------- private / protected メンバ -------------------------------

	bool _isDisposed = false;
	int _proccessIdx = 0;		//!< 処理の進行カウント

	Camera _camera;
	protected int _texSize;
	float3 _pos;


	/** 指定の方向の面をレンダリングする処理 */
	abstract protected void renderFace(
		UnityEngine.Rendering.ScriptableRenderContext context,
		CubemapFace faceIndex
	);

	/** 指定の方向の面をレンダリングする処理のコア部分 */
	protected void renderFace(
		RenderTexture rt,
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

		// RTにレンダリング
		_camera.targetTexture = rt;
		UnityEngine.Rendering.Universal.UniversalRenderPipeline.RenderSingleCamera(context, _camera);
		_camera.targetTexture = null;
	}

	/** 各面をレンダリングした結果からキューブマップを生成する */
	abstract protected Cubemap compileCubemap(UnityEngine.Rendering.ScriptableRenderContext context);

	/** 破棄処理本体。これを派生先から実装する */
	abstract protected void disposeCore();

	~Builder_Base() {
		if (!_isDisposed) throw new InvalidProgramException();
	}


	// --------------------------------------------------------------------------------------------
}

}
