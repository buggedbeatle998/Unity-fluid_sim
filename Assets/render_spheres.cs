using System;
using System.Collections;
using System.Data.Common;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public struct Sphere {
    public Vector3 pos;
    public float rad;
}


public class render_spheres : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public ComputeShader compute_shader;
    uint xsize;
    uint ysize;
    ComputeBuffer spheres_buffer;
    Sphere[] data;
    Vector3 origin = new Vector3(0.0f, 2.0f, 0.0f);
    uint velocity = 0;
    float speed = 10;


    public RenderTexture render_texture;

    Sphere[] make_spheres() {
        Sphere[] data = new Sphere[10];
        for (uint i = 0; i < 10; i++) {
            Sphere thing = new Sphere();
            thing.pos = new Vector3(UnityEngine.Random.Range(-20, 20), UnityEngine.Random.Range(-20, 20), UnityEngine.Random.Range(30, 60));
            thing.rad = 2;
            data[i] = thing;
        }

        return data;
    }

    void Update() {
        origin += Time.deltaTime * speed * new Vector3((float)((velocity & (1 << 2)) >> 2) - (float)((velocity & (1 << 3)) >> 3),
        (float)((velocity & (1 << 4)) >> 4) - (float)((velocity & (1 << 5)) >> 5),
        (float)((velocity & (1 << 0)) >> 0) - (float)((velocity & (1 << 1)) >> 1));
    }

    GUIStyle myButtonStyle;
    void OnGUI ()
    {
        Event e = Event.current;
        
        if (e.isKey) {
            if (e.type == EventType.KeyDown) {
                if (e.keyCode == KeyCode.W) {
                    velocity |= (uint)1 << 0;
                } else if (e.keyCode == KeyCode.S) {
                    velocity |= (uint)1 << 1;
                } else if (e.keyCode == KeyCode.D) {
                    velocity |= (uint)1 << 2;
                } else if (e.keyCode == KeyCode.A) {
                    velocity |= (uint)1 << 3;
                } else if (e.keyCode == KeyCode.Space) {
                    velocity |= (uint)1 << 4;
                } else if (e.keyCode == KeyCode.LeftShift) {
                    velocity |= (uint)1 << 5;
                }
            } else if (e.type == EventType.KeyUp) {
                if (e.keyCode == KeyCode.W) {
                    velocity &= ~(uint)(1 << 0);
                } else if (e.keyCode == KeyCode.S) {
                    velocity &= ~(uint)(1 << 1);
                } else if (e.keyCode == KeyCode.D) {
                    velocity &= ~(uint)(1 << 2);
                } else if (e.keyCode == KeyCode.A) {
                    velocity &= ~(uint)(1 << 3);
                } else if (e.keyCode == KeyCode.Space) {
                    velocity &= ~(uint)(1 << 4);
                } else if (e.keyCode == KeyCode.LeftShift) {
                    velocity &= ~(uint)(1 << 5);
                }
            }
        }
        // data[0].pos.x = GUI.HorizontalSlider(new Rect(25, 25, 100, 30), data[0].pos.x, -20.0f, 20.0f);
        // data[0].pos.y = GUI.HorizontalSlider(new Rect(25, 50, 100, 30), data[0].pos.y, -20.0f, 20.0f);
        if (GUI.Button(new Rect(25, 25, 100, 100), "Randomise")) {
            data = make_spheres();
            Array.Sort(data, delegate(Sphere s1, Sphere s2) {
                return (s1.pos.x * s1.pos.x + s1.pos.y * s1.pos.y + s1.pos.z * s1.pos.z).CompareTo(s2.pos.x * s2.pos.x + s2.pos.y * s2.pos.y + s2.pos.z * s2.pos.z);
            });
        }
        
        GUI.Box(new Rect(10,200,600,90), "x: " + origin.x + " y: " + origin.y + " z: " + origin.z, myButtonStyle);
    }


    void setup() {
        compute_shader.GetKernelThreadGroupSizes(compute_shader.FindKernel("CSMain"), out xsize, out ysize, out _);
        spheres_buffer = new ComputeBuffer(10, sizeof(uint) * 3 + sizeof(float));
        data = make_spheres();
        Array.Sort(data, delegate(Sphere s1, Sphere s2) {
            return (s1.pos.x * s1.pos.x + s1.pos.y * s1.pos.y + s1.pos.z * s1.pos.z).CompareTo(s2.pos.x * s2.pos.x + s2.pos.y * s2.pos.y + s2.pos.z * s2.pos.z);
        });

        render_texture = new RenderTexture(1920, 1080, 24);
        render_texture.enableRandomWrite = true;
        render_texture.Create();
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (data == null) {
            setup();
        }
        spheres_buffer.SetData(data);
        compute_shader.SetBuffer(0, "spheres", spheres_buffer);
        compute_shader.SetTexture(0, "Result", render_texture);
        compute_shader.SetVector("res", new Vector2(render_texture.width, render_texture.height));
        compute_shader.SetFloat("fov", 60);
        compute_shader.SetVector("origin", origin);
        compute_shader.SetInt("num_spheres", 10);
        compute_shader.SetVector("light_source", new Vector3(1.0f, 1.0f, -1.0f));
        compute_shader.Dispatch(0, render_texture.width / (int)xsize, render_texture.height / (int)ysize, 1);

        Graphics.Blit(render_texture, destination);
    }

    private void OnDisable() {
        spheres_buffer.Release();
        render_texture.Release();
    }
}
