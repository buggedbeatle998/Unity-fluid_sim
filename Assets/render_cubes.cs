using System.Data.Common;
using UnityEngine;

public struct Sphere {
    public Vector2 pos;
    public float rad;
}


public class render_cubes : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    public ComputeShader compute_shader;

    Sphere[] make_spheres() {
        Sphere thing = new Sphere();
        thing.pos = new Vector2(0, 0);
        thing.rad = 10;

        Sphere[] data = {thing};
        return data;
    }

    void Start()
    {
        Sphere[] data = make_spheres();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
