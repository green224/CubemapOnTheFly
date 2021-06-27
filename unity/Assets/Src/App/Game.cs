using System;
using UnityEngine;
using UnityEngine.UI;



public class Game : MonoBehaviour {
	// ----------------------------- インスペクタに公開しているフィールド -----------------------------

	[SerializeField] CubemapOnTheFly.Debugger _cmGenDebugger = null;

	[Space]
	[SerializeField] Button _btn_reload = null;


	// ------------------------------------- public メンバ ----------------------------------------
	// ------------------------------- private / protected メンバ ---------------------------------

	void Start() {
		_btn_reload.onClick.AddListener(() => {
			_cmGenDebugger.gameObject.SetActive(false);
			_cmGenDebugger.gameObject.SetActive(true);
		});
	}


	// -------------------------------------------------------------------------------------------
}
