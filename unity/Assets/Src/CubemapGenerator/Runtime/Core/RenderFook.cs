using System;
using UnityEngine;

using Unity.Mathematics;
using static Unity.Mathematics.math;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace CubemapGenerator.Core {

/**
 * レンダリング時の処理フック
 */
sealed class RenderFook : IDisposable {
	// ------------------------------------- public メンバ ----------------------------------------

	readonly public Camera tgtCamera;

	public RenderFook(
		Camera camera,
		Action< ScriptableRenderContext > beginCameraRendering,
		Action< ScriptableRenderContext > endCameraRendering,
		Action< ScriptableRenderContext > beginFrameRendering,
		Action< ScriptableRenderContext > endFrameRendering
	) : this (
		beginCameraRendering == null
			? (Action< ScriptableRenderContext, Camera >) null
			: (context,cam) => { if (cam==camera) beginCameraRendering(context); },
		endCameraRendering == null
			? (Action< ScriptableRenderContext, Camera >) null
			: (context,cam) => { if (cam==camera) endCameraRendering(context); },
		beginFrameRendering == null
			? (Action< ScriptableRenderContext, Camera[] >) null
			: (context,cams) => beginFrameRendering(context),
		endFrameRendering == null
			? (Action< ScriptableRenderContext, Camera[] >) null
			: (context,cams) => endFrameRendering(context)
	) {
		tgtCamera = camera;
	}

	public RenderFook(
		Action< ScriptableRenderContext, Camera > beginCameraRendering,
		Action< ScriptableRenderContext, Camera > endCameraRendering,
		Action< ScriptableRenderContext, Camera[] > beginFrameRendering,
		Action< ScriptableRenderContext, Camera[] > endFrameRendering
	) {
		_bgnCamRdr = beginCameraRendering;
		_endCamRdr = endCameraRendering;
		_bgnFrmRdr = beginFrameRendering;
		_endFrmRdr = endFrameRendering;

		if (_bgnCamRdr != null) {
			RenderPipelineManager.beginCameraRendering -= _bgnCamRdr;
			RenderPipelineManager.beginCameraRendering += _bgnCamRdr;
		}
		if (_endCamRdr != null) {
			RenderPipelineManager.endCameraRendering -= _endCamRdr;
			RenderPipelineManager.endCameraRendering += _endCamRdr;
		}
		if (_bgnFrmRdr != null) {
			RenderPipelineManager.beginFrameRendering -= _bgnFrmRdr;
			RenderPipelineManager.beginFrameRendering += _bgnFrmRdr;
		}
		if (_endFrmRdr != null) {
			RenderPipelineManager.endFrameRendering -= _endFrmRdr;
			RenderPipelineManager.endFrameRendering += _endFrmRdr;
		}
	}

	public void Dispose() {
		if (_bgnCamRdr != null)
			RenderPipelineManager.beginCameraRendering -= _bgnCamRdr;
		if (_endCamRdr != null)
			RenderPipelineManager.endCameraRendering -= _endCamRdr;
		if (_bgnFrmRdr != null)
			RenderPipelineManager.beginFrameRendering -= _bgnFrmRdr;
		if (_endFrmRdr != null)
			RenderPipelineManager.endFrameRendering -= _endFrmRdr;

		_bgnCamRdr = null;
		_endCamRdr = null;
		_bgnFrmRdr = null;
		_endFrmRdr = null;
	}


	// --------------------------------- private / protected メンバ -------------------------------

	Action< ScriptableRenderContext, Camera > _bgnCamRdr;
	Action< ScriptableRenderContext, Camera > _endCamRdr;
	Action< ScriptableRenderContext, Camera[] > _bgnFrmRdr;
	Action< ScriptableRenderContext, Camera[] > _endFrmRdr;

	~RenderFook() {
		if (
			_bgnCamRdr != null ||
			_endCamRdr != null ||
			_bgnFrmRdr != null ||
			_endFrmRdr != null
		) {
			Debug.LogError("render fook is not disposed");
			Dispose();
		}
	}


	// --------------------------------------------------------------------------------------------
}

}
