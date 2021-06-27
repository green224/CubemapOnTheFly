using System;
using UnityEngine;
using UnityEngine.UI;



public class Game : MonoBehaviour {
	// ----------------------------- �C���X�y�N�^�Ɍ��J���Ă���t�B�[���h -----------------------------

	[SerializeField] CubemapOnTheFly.Debugger _cmGenDebugger = null;

	[Space]
	[SerializeField] Button _btn_reload = null;


	// ------------------------------------- public �����o ----------------------------------------
	// ------------------------------- private / protected �����o ---------------------------------

	void Start() {
		_btn_reload.onClick.AddListener(() => {
			_cmGenDebugger.gameObject.SetActive(false);
			_cmGenDebugger.gameObject.SetActive(true);
		});
	}


	// -------------------------------------------------------------------------------------------
}
