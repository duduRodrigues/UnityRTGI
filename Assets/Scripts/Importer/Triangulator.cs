using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions;


public class Triangulator
{
    private List<Vector2> m_points = new List<Vector2>();

    public Triangulator(Vector2[] points)
    {
        if (points.Length < 3)
            throw new System.Exception("Not enough points");

        m_points = new List<Vector2>(points);
    }

    public Triangulator(Vector3[] points)
    {
        if (points.Length < 3)
            throw new System.Exception("Not enough points");

        // Find 2 plane vectors
        Vector3 v1 = points[1] - points[0];
        Vector3 v2 = points[2] - points[0];

        // Find normal vector
        Vector3 normal = Vector3.Cross(v1, v2);

        // Specify new orthogonal base vectors
        Vector3 b1 = v1;
        Vector3 b2 = Vector3.Cross(v1, normal);
        Vector3 b3 = normal;
        b1.Normalize();
        b2.Normalize();
        b3.Normalize();

        // Convert points to new coordinates
        List<Vector2> transfPoints = new List<Vector2>();

        //float[,] transfMatrix = _CreatePlanarBasisMatrix(b1, b2, b3);
        Matrix4x4 matrix = new Matrix4x4();
        matrix.SetColumn(0, new Vector4(b1.x, b1.y, b1.z, points[0].x));
        matrix.SetColumn(1, new Vector4(b2.x, b2.y, b2.z, points[0].y));
        matrix.SetColumn(2, new Vector4(b3.x, b3.y, b3.z, points[0].z));
        matrix.SetColumn(3, new Vector4(0, 0, 0, 1));

        Matrix4x4 transfMatrix = matrix.inverse;

        Vector3 temp = transfMatrix.MultiplyPoint(new Vector3(points[1].x, points[1].y, points[1].z));
        Vector4 temp2 = _MatrixPointMultiplication4x4(transfMatrix, new Vector4(points[1].x, points[1].y, points[1].z, 1));
        Vector4 temp3 = _MatrixPointMultiplication4x4(transfMatrix, new Vector4(points[1].x, points[1].y, points[1].z, 0));

        foreach (Vector3 point in points)
        {
            Vector4 transfPoint = _MatrixPointMultiplication4x4(transfMatrix, new Vector4(point.x, point.y, point.z, 1));
            transfPoints.Add(new Vector2(transfPoint.x, transfPoint.y));
        }

        // Set class attribute
        m_points = transfPoints;
    }

    private Vector4 _MatrixPointMultiplication4x4(Matrix4x4 transf, Vector4 point)
    {
        float x = (point.x * transf[0, 0]) + (point.y * transf[0, 1]) + (point.z * transf[0, 2]) + (point.w * transf[0, 3]);
        float y = (point.x * transf[1, 0]) + (point.y * transf[1, 1]) + (point.z * transf[1, 2]) + (point.w * transf[1, 3]);
        float z = (point.x * transf[2, 0]) + (point.y * transf[2, 1]) + (point.z * transf[2, 2]) + (point.w * transf[2, 3]);
        float w = (point.x * transf[3, 0]) + (point.y * transf[3, 1]) + (point.z * transf[3, 2]) + (point.w * transf[3, 3]);

        return new Vector4(x, y, z, w);
    }

    private float[,] _CreatePlanarBasisMatrix(Vector3 b1, Vector3 b2, Vector3 b3)
    {
        float a = b1.x, b = b1.y, c = b1.z;
        float d = b2.x, e = b2.y, f = b2.z;
        float g = b3.x, h = b3.y, i = b3.z;

        float denominator = 1 / (a * e * i - a * f * h - b * d * i + b * f * h + c * d * h - c * e * g);

        float[,] matrix = new float[3, 3];
        matrix[0, 0] = (e * i - f * h) / denominator;
        matrix[0, 1] = (f * g - d * i) / denominator;
        matrix[0, 2] = (d * h - e * g) / denominator;
        matrix[1, 0] = (c * h - b * i) / denominator;
        matrix[1, 1] = (a * i - c * g) / denominator;
        matrix[1, 2] = (b * g - a * h) / denominator;
        matrix[2, 0] = (b * f - c * e) / denominator;
        matrix[2, 1] = (c * d - a * f) / denominator;
        matrix[2, 2] = (a * e - b * d) / denominator;

        return matrix;
    }

    public int[] Triangulate()
    {
        List<int> indices = new List<int>();

        int n = m_points.Count;
        if (n < 3)
            return indices.ToArray();

        int[] V = new int[n];
        if (Area() > 0)
        {
            for (int v = 0; v < n; v++)
                V[v] = v;
        }
        else
        {
            for (int v = 0; v < n; v++)
                V[v] = (n - 1) - v;
        }

        int nv = n;
        int count = 2 * nv;
        for (int m = 0, v = nv - 1; nv > 2;)
        {
            if ((count--) <= 0)
                return indices.ToArray();

            int u = v;
            if (nv <= u)
                u = 0;
            v = u + 1;
            if (nv <= v)
                v = 0;
            int w = v + 1;
            if (nv <= w)
                w = 0;

            if (Snip(u, v, w, nv, V))
            {
                int a, b, c, s, t;
                a = V[u];
                b = V[v];
                c = V[w];
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
                m++;
                for (s = v, t = v + 1; t < nv; s++, t++)
                    V[s] = V[t];
                nv--;
                count = 2 * nv;
            }
        }

        indices.Reverse();
        return indices.ToArray();
    }

    private float Area()
    {
        int n = m_points.Count;
        float A = 0.0f;
        for (int p = n - 1, q = 0; q < n; p = q++)
        {
            Vector2 pval = m_points[p];
            Vector2 qval = m_points[q];
            A += pval.x * qval.y - qval.x * pval.y;
        }
        return (A * 0.5f);
    }

    private bool Snip(int u, int v, int w, int n, int[] V)
    {
        int p;
        Vector2 A = m_points[V[u]];
        Vector2 B = m_points[V[v]];
        Vector2 C = m_points[V[w]];
        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
            return false;
        for (p = 0; p < n; p++)
        {
            if ((p == u) || (p == v) || (p == w))
                continue;
            Vector2 P = m_points[V[p]];
            if (InsideTriangle(A, B, C, P))
                return false;
        }
        return true;
    }

    private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
        float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
        float cCROSSap, bCROSScp, aCROSSbp;

        ax = C.x - B.x; ay = C.y - B.y;
        bx = A.x - C.x; by = A.y - C.y;
        cx = B.x - A.x; cy = B.y - A.y;
        apx = P.x - A.x; apy = P.y - A.y;
        bpx = P.x - B.x; bpy = P.y - B.y;
        cpx = P.x - C.x; cpy = P.y - C.y;

        aCROSSbp = ax * bpy - ay * bpx;
        cCROSSap = cx * apy - cy * apx;
        bCROSScp = bx * cpy - by * cpx;

        return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
    }
}