using System;
using UnityEngine;



public class CameraController : MonoBehaviour {
	// ----------------------------- インスペクタに公開しているフィールド -----------------------------
	// ------------------------------------- public メンバ ----------------------------------------

	public float speed = 1;


	// ------------------------------- private / protected メンバ ---------------------------------

	Vector3 _lastMousePos;
	Vector3 _rot;

	void Start() {
		_lastMousePos = Input.mousePosition;
	}

	void Update() {

		// マウスの移動距離を得る
		var newMousePos = Input.mousePosition;
		var dMousePos = newMousePos - _lastMousePos;
		_lastMousePos = newMousePos;

		// 現在の姿勢を計算
		_rot += new Vector3(
			dMousePos.y,
			dMousePos.x,
			0
		) * speed;

		// 姿勢を適応
		transform.localRotation = Quaternion.Euler(_rot);
	}


	// -------------------------------------------------------------------------------------------
}
