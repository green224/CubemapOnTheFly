using System;
using UnityEngine;

using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections;


namespace CubemapGenerator.Core {

/**
 * レンダリングの予約用データ。
 *
 * ManagerからBuilderを少し呼びやすくラップしたもの。
 */
sealed class BuilderPlan : IDisposable {
	// ------------------------------------- public メンバ ----------------------------------------

	public BuilderPlan(
		Camera camera, int texSize, float3 pos,
		Action<Cubemap> onComplete,
		Action onBeginRender, Action onEndRender,
		bool usePluginVer
	) {
		if (usePluginVer)
			_renderer = new Builder_UsePlugin(camera, texSize, pos);
		else
			_renderer = new Builder_NoUsePlugin(camera, texSize, pos);
		_onComplete = onComplete;
		_onBeginRender = onBeginRender;
		_onEndRender = onEndRender;
	}

	/**
	 * 処理を進める。
	 * 呼び出し時に残りレンダリング可能数を指定して、使用結果の値で更新する。
	 * 処理が全完了した場合はtrueを返す。
	 */
	public bool proceed(
		UnityEngine.Rendering.ScriptableRenderContext context,
		ref int remainRenderableCnt
	) {
		_onBeginRender?.Invoke();

		for (; 0<remainRenderableCnt; --remainRenderableCnt) {
			_renderer.proceed(context);
			if (_renderer.IsComplete) break;
		}

		_onEndRender?.Invoke();

		// レンダリング処理全完了チェック
		if (_renderer.IsComplete) {
			_onComplete?.Invoke(_renderer.Result);
			_onComplete = null;

			_renderer.Dispose();
			_renderer = null;

			return true;
		}

		return false;
	}

	public void Dispose() {
		_onComplete?.Invoke(null); _onComplete = null;
		_renderer?.Dispose(); _renderer = null;
	}


	// --------------------------------- private / protected メンバ -------------------------------

	Builder_Base _renderer;
	readonly Action _onBeginRender, _onEndRender;
	Action<Cubemap> _onComplete;


	~BuilderPlan() {
		if (_renderer != null) throw new InvalidProgramException();
	}


	// --------------------------------------------------------------------------------------------
}

}
