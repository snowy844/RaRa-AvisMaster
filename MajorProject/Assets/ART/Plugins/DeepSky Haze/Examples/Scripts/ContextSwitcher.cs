using UnityEngine;
using System.Collections.Generic;

using DeepSky.Haze;

public class ContextSwitcher : MonoBehaviour {

    public List<DS_HazeContextAsset> contexts = new List<DS_HazeContextAsset>();
        	
    void Start()
    {
        _view = GetComponent<DS_HazeView>();
    }

	void Update () {

        if (contexts.Count > 0 && _view != null)
        {
            if (Input.GetKeyUp(KeyCode.C))
            {
                _contextIndex++;
                if (_contextIndex == contexts.Count) _contextIndex = 0;

                _view.ContextAsset = contexts[_contextIndex];
                _view.OverrideContextAsset = true;
            }
        }
	}

    DS_HazeView _view;
    int _contextIndex = 0;
}
