using System;
using UnityEngine;

using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Collections.Generic;
using Unity.Collections;


namespace CubemapOnTheFly {

/**
 * ランタイムでキューブマップを生成するためのモジュール。
 * このコンポーネントを付けたオブジェクトをシーン上に配置して使用する。
 * （レンダリングするカメラの設定を使用者側でカスタムしたいので、このようにしている）
 */
[ExecuteAlways]
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
	public IDisposable beginRender(
		int texSize, float3 pos,
		Action<Texture> onComplete,
		Action<Camera, UnityEngine.Rendering.ScriptableRenderContext> onBeginRenderPerFrame = null,
		Action<Camera, UnityEngine.Rendering.ScriptableRenderContext> onEndRenderPerFrame = null
	) {
		var plan = new Core.BuilderPlan(
			_camera, texSize, pos,
			onComplete,
			onBeginRenderPerFrame,
			onEndRenderPerFrame,
			_blitShader,
			renderingMode
		);
		_builderPlans.AddLast( plan );

		// 現在処理中のタスクがない場合は、即実行
		if (_curBldPlan == null) {
			_curBldPlan = _builderPlans.First.Value;
			_builderPlans.RemoveFirst();
		}

		// 途中キャンセル用のハンドルを返す
		return new CancelHandler(() => {
			if (_curBldPlan == plan) {
				_curBldPlan.Dispose();
				_curBldPlan = null;
			} else if (_builderPlans.Contains(plan)) {
				_builderPlans.Remove(plan);
				plan.Dispose();
			}
		});
	}

	/** 現在発行中のビルドを全キャンセルする */
	public void cancelAll() {
		// 現在実行中のレンダリングは予約されているものも含めて全てキャンセルして、
		// 完了コールバックにnullを渡してキャンセルを通知する。
		if (_curBldPlan != null) _curBldPlan.Dispose();
		_curBldPlan = null;
		foreach (var i in _builderPlans) i.Dispose();
		_builderPlans.Clear();
	}


	// --------------------------------- private / protected メンバ -------------------------------

	/** 発行したレンダリングをキャンセルするためのハンドル */
	sealed class CancelHandler : IDisposable {
		public CancelHandler(Action onDipose) { _onDipose = onDipose; }
		public void Dispose() { _onDipose?.Invoke(); _onDipose = null; }
		Action _onDipose;
	}

	/** レンダリングの予約用データ */
	LinkedList<Core.BuilderPlan> _builderPlans = new LinkedList<Core.BuilderPlan>();

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
					if ( _curBldPlan.proceed(_camera, context, ref remainRenderCnt) ) {
						if (_builderPlans.Count==0) {
							_curBldPlan = null;
						} else {
							_curBldPlan = _builderPlans.First.Value;
							_builderPlans.RemoveFirst();
						}
					}
				}

			}, null
		);
	}

	void OnDisable() {
		_renderFook.Dispose();
		_renderFook = null;
		cancelAll();
	}


	// --------------------------------------------------------------------------------------------
#if UNITY_EDITOR
	void OnValidate() {
		if (_blitShader == null)
			_blitShader = Shader.Find("Hidden/CubemapOnTheFly/CubemapOnTheFly_BlitMaterial");
	}
#endif
}

}
