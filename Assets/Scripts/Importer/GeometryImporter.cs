using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;

public class GeometryImporter
{
    //read all the geometry
    List<Vector3> vertexList = new List<Vector3>();
    List<Vector3> normalList = new List<Vector3>();
    List<Vector3> uvList = new List<Vector3>();
    List<List<Vector3>> faceList = new List<List<Vector3>>();
    List<Vector3> triangulatedFaces = new List<Vector3>();

    Vector3[] vertexArray;
    Vector3[] normalArray;
    Vector2[] uvArray;
    int[] faceArray;

    float maxX = -9999;
    float maxY = -9999;
    float maxZ = -9999;

    List<GameObject> objects = new List<GameObject>();

    public void ResetParameters()
    {
        vertexList.Clear();
        normalList.Clear();
        uvList.Clear();
        faceList.Clear();
        triangulatedFaces.Clear();

        maxX = -9999;
        maxY = -9999;
        maxZ = -9999;
    }

    public GameObject ImportObj(string filepath)
    {
        //Clean all parameters
        ResetParameters();

        //Initialize the game object
        GameObject gameObject = null;

        //extract all general informations about the file and the file content

        if(filepath != null && filepath != "")
        {
            string file = GetFileContent(filepath);

            //split all lines
            string[] lines = file.Split("\n"[0]);
            
            foreach (string line in lines)
            {
                //remove any trailing white-space chararcters to ensure that there will be no empty splits
                char[] charsToTrim = { ' ', '\n', '\t', '\r' };
                string trimLine = line.TrimEnd(charsToTrim);
                //split the incoming string in words
                string[] words = trimLine.Split(" "[0]);
                //trim each word to avoid white-space chararcters
                foreach (string word in words)
                    word.Trim();
                
                if (words[0] == ("v"))
                    vertexList.Add(ReadVertex(words));
                else if (words[0] == ("vt"))
                    uvList.Add(ReadTexture(words));
                else if (words[0] == ("vn"))
                    normalList.Add(ReadNormal(words));
                else if (words[0] == ("f"))
                    faceList.Add(ReadFace(words));
            }

            //triangulate the object
            triangulatedFaces = TriangulateFaces();

            //convert all geometry from list to array, so they will be in the unity format
            PopulateArrays();

            //Normalize the object scale
            NormalizeObjectScale();

            //Get the box collider limits
            GetBoxColliderLimits();

            string filename = GetFileName(filepath);
            gameObject = CreateGameObject(filename);

            objects.Add(gameObject);
        }

        return gameObject;
    }

    public string GetFileContent(string filepath)
    {
        string file;
        if (filepath.Contains("http://") || filepath.Contains("https://"))
        {
            WWW www3d = new WWW(filepath);
            while (!www3d.isDone) ;
            file = www3d.text;
        }
        else
        {
            StreamReader sr = new StreamReader(filepath);
            file = sr.ReadToEnd();
        }

        //replace double spaces and dot-notations
        file = file.Replace("  ", " ");
        file = file.Replace("  ", " ");
        file = file.Replace(".", ",");

        return file;
    }

    public string GetFileName(string filepath)
    {
        string filename;
        if (filepath.Contains("http://") || filepath.Contains("https://"))
            filename = filepath.Substring(filepath.LastIndexOf("/") + 1, filepath.LastIndexOf('.') - filepath.LastIndexOf("/") - 1);
        else
            filename = filepath.Substring(filepath.LastIndexOf("\\") + 1, filepath.LastIndexOf('.') - filepath.LastIndexOf("\\") - 1);

        return filename;
    }

    public string GetFileAddress(string filepath)
    {
        string address;
        if (filepath.Contains("http://") || filepath.Contains("https://"))
            address = filepath.Substring(0, filepath.LastIndexOf("/") + 1);
        else
            address = filepath.Substring(0, filepath.LastIndexOf("\\") + 1);

        return address;
    }

    public Vector3 ReadVertex(string[] words)
    {
        return new Vector3(System.Convert.ToSingle(words[1]), System.Convert.ToSingle(words[2]), System.Convert.ToSingle(words[3]));
    }

    public Vector3 ReadNormal(string[] words)
    {
        return new Vector3(System.Convert.ToSingle(words[1]), System.Convert.ToSingle(words[2]), System.Convert.ToSingle(words[3]));
    }

    public Vector3 ReadTexture(string[] words)
    {
        return new Vector3(System.Convert.ToSingle(words[1]), System.Convert.ToSingle(words[2]), System.Convert.ToSingle(words[3]));
    }

    public List<Vector3> ReadFace(string[] words)
    {

        List<Vector3> temp = new List<Vector3>();
        for (int j = 1; j < words.Length; ++j)
        {
            Vector3 indexVector = new Vector3(0, 0);
            string[] indices = words[j].Split("/"[0]);
            indexVector.x = System.Convert.ToInt32(indices[0]);
            if (indices.Length > 1)
            {
                if (indices[1] != "")
                    indexVector.y = System.Convert.ToInt32(indices[1]);
                else
                    indexVector.y = -1;
            }
            if (indices.Length > 2)
            {
                if (indices[2] != "")
                    indexVector.z = System.Convert.ToInt32(indices[2]);
                else
                    indexVector.y = -1;
            }

            temp.Add(indexVector);
        }
        
        return temp;
    }

    public List<Vector3> TriangulateFaces()
    {
        List<Vector3> faces = new List<Vector3>();
        faces.Clear();

        for (int i = 0; i < faceList.Count; i++)
        {
            if(faceList[i].Count > 3)
            {
                Vector3[] facePoints = new Vector3[faceList[i].Count];

                for (int j = 0; j < faceList[i].Count; j++)
                    facePoints[j] = vertexList[((int)faceList[i][j].x) - 1];

                Triangulator tr = new Triangulator(facePoints);
                int[] faceIndex = tr.Triangulate();

                for (int j = 0; j < faceIndex.Length; j++)
                    faces.Add(faceList[i][faceIndex[j]]);
            }
            else
            {
                faces.AddRange(faceList[i]);
            }
        }

        return faces;
    }

    public void PopulateArrays()
    {
        //initialize the arrays
        vertexArray = new Vector3[triangulatedFaces.Count];
        uvArray = new Vector2[triangulatedFaces.Count];
        normalArray = new Vector3[triangulatedFaces.Count];
        faceArray = new int[triangulatedFaces.Count];

        // fill the arrays by crossreferencing the data in _facesVertNormUV and 
        // the arraylists of each type
        int i = 0;
        foreach (Vector3 item in triangulatedFaces)
        {
            vertexArray[i] = (Vector3)vertexList[(int)item.x - 1];
            if (uvList.Count > 0)
            {
                Vector3 tVec = (Vector3)uvList[(int)item.y - 1];
                uvArray[i] = new Vector2(tVec.x, tVec.y);
            }
            if (normalList.Count > 0)
            {
                normalArray[i] = (Vector3)normalList[(int)item.z - 1];
            }

            faceArray[i] = i;
            i++;
        }
    }

    public void NormalizeObjectScale()
    {
        float maxSize = -99999;

        foreach(Vector3 vertex in vertexArray)
        {
            maxSize = (vertex.x > maxSize) ? vertex.x : maxSize;
            maxSize = (vertex.y > maxSize) ? vertex.y : maxSize;
            maxSize = (vertex.z > maxSize) ? vertex.z : maxSize;
        }

        for(int i=0; i<vertexArray.Length; i++)
            vertexArray[i] /= maxSize;
    }

    public void GetBoxColliderLimits()
    {
        foreach (Vector3 vertex in vertexArray)
        {
            maxX = (Math.Abs(vertex.x) > maxX) ? Math.Abs(vertex.x) : maxX;
            maxY = (Math.Abs(vertex.y) > maxY) ? Math.Abs(vertex.y) : maxY;
            maxZ = (Math.Abs(vertex.z) > maxZ) ? Math.Abs(vertex.z) : maxZ;
        }
    }

    public GameObject CreateGameObject(string name)
    {
        GameObject go = new GameObject();
        go.name = name;

        Mesh myMesh = new Mesh();
        myMesh.vertices = vertexArray;
        myMesh.triangles = faceArray;

        if (uvList.Count > 0)
            myMesh.uv = uvArray;
        if (normalList.Count > 0)
            myMesh.normals = normalArray;
        else
            myMesh.RecalculateNormals();
        
        //calculate the bounds
        myMesh.RecalculateBounds();
        
        // check if there is allready a MeshFilter present, if not add one
        if ((MeshFilter)go.GetComponent("MeshFilter") == null)
            go.AddComponent<MeshFilter>();
        //assign the mesh to the meshfilter
        MeshFilter temp;
        temp = (MeshFilter)go.GetComponent("MeshFilter");
        temp.mesh = myMesh;
        
        // check if there is allready a MeshRenderer present, if not add one
        if ((MeshRenderer)go.GetComponent("MeshRenderer") == null)
            go.AddComponent<MeshRenderer>();

        Material material = new Material(Shader.Find("Diffuse"));
        
        // retrieve the texture
        //if (uvList.Count > 0 && _textureLink != "")
        //{
        //    WWW wwwtx = new WWW(_textureLink);
        //    while (!wwwtx.isDone) ;
        //    material.mainTexture = wwwtx.texture;
        //}

        // assign the texture to the meshrenderer
        MeshRenderer temp2;
        temp2 = (MeshRenderer)go.GetComponent("MeshRenderer");

        //if (uvList.Count > 0 && _textureLink != "")
        //{
        //    temp2.material = material;
        //    material.shader = Shader.Find("Diffuse");
        //}
        //else
        //{
            temp2.material = new Material(Shader.Find("Diffuse"));
        //}

        go.AddComponent<BoxCollider>();
        BoxCollider box_col = go.GetComponent<BoxCollider>();
        box_col.size = new Vector3(2 * maxX, 2 * maxY, 2 * maxZ);

        float maxSize = (maxX > maxY) ? maxX : maxY;
        maxSize = (maxSize > maxZ) ? maxSize : maxZ;

        go.transform.Translate(maxSize * objects.Count * 2, 0, 0);

        return go;
    }
}
