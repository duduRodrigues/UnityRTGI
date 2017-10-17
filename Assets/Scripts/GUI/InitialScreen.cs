using UnityEngine;
using System.Collections;

public class InitialScreen : MonoBehaviour {

    private string _textFieldString;
    private string _textureLink;
    private bool isOnModelView;

    private GeometryImporter _importer;

    // Use this for initialization
    void Start () {
        _textFieldString = "";
        _textureLink = "";
        isOnModelView = false;
        _importer = new GeometryImporter();

    }

    // Update is called once per frame
    void Update () {
	    if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isOnModelView)
                Application.Quit();
            else
                isOnModelView = false;
        }
	}

    public void OnGUI()
    {
        if (!isOnModelView)
        {
            int width = 300;
            int height = 20;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.black;

            GUI.Label(new Rect(21, 10, width, height), "Mesh Address", style);
            _textFieldString = GUI.TextField(new Rect(108, 10, width, height), _textFieldString);

            GUI.Label(new Rect(10, 40, width, height), "Texture Address", style);
            _textureLink = GUI.TextField(new Rect(108, 40, width, height), _textureLink);

            if (GUI.Button(new Rect(10, 70, 100, 20), "Download"))
            {
                GameObject gameObject = _importer.ImportObj(_textFieldString);
                isOnModelView = true;
            }
        }
    }
}
