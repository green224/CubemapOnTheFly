using System;
using UnityEngine;

using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Collections.Generic;


namespace CubemapOnTheFly {

/**
 * ランタイムでキューブマップを生成するためのモジュール。
 * このコンポーネントを付けたオブジェクトをシーン上に配置して使用する。
 * （レンダリングするカメラの設定を使用者側でカスタムしたいので、このようにしている）
 */
[AddComponentMenu("CubemapOnTheFly/CubemapOnTheFly_Manager")]
public sealed class Manager : MonoBehaviour {
	// --------------------------- インスペクタに公開しているフィールド -----------------------------

	/** レンダリングに使用するカメラ */
	[SerializeField] Camera _camera = null;
	/** Blitに使用するシェーダ。これは自動で代入される */
	[SerializeField][HideInInspector] Shader _blitShader = null;

	[Space]

	/** 何フレームごとに処理ステップを進めるか */
	[SerializeField][Range(1,10)] int _frameCntBetweenSteps = 1;
	/** 1処理ステップあたり何枚レンダリングを行うか */
	[SerializeField][Range(1,7)] int _renderCntPerSteps = 1;


	[Space]


	// ------------------------------------- public メンバ ----------------------------------------

	/** レンダリング方法 */
	public RenderingMode renderingMode = RenderingMode.DirectRT;


	/** 指定のパラメータでキューブマップ生成を開始する */
	public void beginRender(
		int texSize, float3 pos,
		Action<Texture> onComplete,
		Action onBeginRenderPerFrame = null,
		Action onEndRenderPerFrame = null
	) {
		_builderPlans.Enqueue( new Core.BuilderPlan(
			_camera, texSize, pos,
			onComplete,
			onBeginRenderPerFrame,
			onEndRenderPerFrame,
			_blitShader,
			renderingMode
		) );

		// 現在処理中のタスクがない場合は、即実行
		if (_curBldPlan == null)
			_curBldPlan = _builderPlans.Dequeue();
	}


	// --------------------------------- private / protected メンバ -------------------------------

	/** レンダリングの予約用データ */
	Queue<Core.BuilderPlan> _builderPlans = new Queue<Core.BuilderPlan>();

	Core.BuilderPlan _curBldPlan;
	Core.RenderFook _renderFook;

	// 処理ステップを進めるためのステップカウント
	int _waitFrameCnt;

	// BeginFrameRenderingがEditor中だと１フレームに何度も呼ばれるので、
	// 二重呼び出しを回避するために、前回更新時のFrameCountを記憶しておく
	int _lastTFCnt;

	void OnEnable() {
		_camera.enabled = false;

		_renderFook = new Core.RenderFook(
			null, null,
			(context, cams) => {

				if (_lastTFCnt == Time.frameCount) return;
				_lastTFCnt = Time.frameCount;

				// 処理を行うフレーム間隔が指定されている場合は、それだけ待つ
				if (++_waitFrameCnt < _frameCntBetweenSteps) return;
				_waitFrameCnt = 0;
				
				// レンダリング可能数を全消費するまでレンダリングを進める
				var remainRenderCnt = _renderCntPerSteps;
				while ( _curBldPlan!=null && 0<remainRenderCnt ) {
					if ( _curBldPlan.proceed(context, ref remainRenderCnt) )
						_curBldPlan = _builderPlans.Count==0 ? null : _builderPlans.Dequeue();
				}

			}, null
		);
	}

	void OnDisable() {
		_renderFook.Dispose();
		_renderFook = null;

		// 現在実行中のレンダリングは予約されているものも含めて全てキャンセルして、
		// 完了コールバックにnullを渡してキャンセルを通知する。
		if (_curBldPlan != null) _curBldPlan.Dispose();
		_curBldPlan = null;
		foreach (var i in _builderPlans) i.Dispose();
		_builderPlans.Clear();
	}


	// --------------------------------------------------------------------------------------------
#if UNITY_EDITOR
	void OnValidate() {
		if (_blitShader == null)
			_blitShader = Shader.Find("CubemapOnTheFly/CubemapOnTheFly_BlitMaterial");
	}
#endif
}

}
