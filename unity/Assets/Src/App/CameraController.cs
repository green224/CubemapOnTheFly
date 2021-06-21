using System;
using UnityEngine;



public class CameraController : MonoBehaviour {
	// ----------------------------- �C���X�y�N�^�Ɍ��J���Ă���t�B�[���h -----------------------------
	// ------------------------------------- public �����o ----------------------------------------

	public float speed = 1;


	// ------------------------------- private / protected �����o ---------------------------------

	Vector3 _lastMousePos;
	Vector3 _rot;

	void Start() {
		_lastMousePos = Input.mousePosition;
	}

	void Update() {

		// �}�E�X�̈ړ������𓾂�
		var newMousePos = Input.mousePosition;
		var dMousePos = newMousePos - _lastMousePos;
		_lastMousePos = newMousePos;

		// ���݂̎p�����v�Z
		_rot += new Vector3(
			dMousePos.y,
			dMousePos.x,
			0
		) * speed;

		// �p����K��
		transform.localRotation = Quaternion.Euler(_rot);
	}


	// -------------------------------------------------------------------------------------------
}
