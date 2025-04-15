using System;
using UnityEngine;

public struct Particle {
    public Vector3 pos;
    public Vector3 velocity;
    public float mass;
}

public class fluid_sim : MonoBehaviour
{
    public ComputeShader precalc_compute;
    public ComputeShader physics_compute;
    public ComputeShader renderer_compute;
    public RenderTexture render_texture;
    uint xsize;
    uint ysize;
    uint physics_xsize;
    ComputeBuffer particle_buffer;
    Particle[] data;
    ComputeBuffer density_buffer;
    Vector3 origin = new Vector3(24.0f, 13.5f, -60.0f);
    uint num_particles = 256;
    bool playing = false;
    bool step = false;
    float targ_dens;
    float press_mult;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
    GUIStyle myButtonStyle;
    GUIStyle myBoxStyle;
    void OnGUI () {
        if (myButtonStyle == null) {
            myButtonStyle = new GUIStyle(GUI.skin.button);
            myButtonStyle.fontSize = 50;
        }
        if (myBoxStyle == null) {
            myBoxStyle = new GUIStyle(GUI.skin.box);
            myBoxStyle.fontSize = 50;
        }

        if (GUI.Button(new Rect(25, 25, 200, 100), "Reset", myButtonStyle)) {
            particle_buffer.Release();
            data = null;
            playing = false;
        }
        
        if (GUI.Button(new Rect(25, 150, 200, 100), playing ? "Pause" : "Play", myButtonStyle)) {
            playing = !playing;
        }

        if (!playing && GUI.Button(new Rect(25, 275, 200, 100), "Step", myButtonStyle)) {
            step = true;
        }

        GUI.Box(new Rect(25, 400, 300, 100), "Target: " + Math.Round(targ_dens, 2), myBoxStyle);
        targ_dens = GUI.HorizontalSlider(new Rect(25, 450, 300, 50), targ_dens, 0.0f, 10.0f);
        GUI.Box(new Rect(25, 525, 300, 100), "Mult: " + Math.Round(press_mult, 2), myBoxStyle);
        press_mult = GUI.HorizontalSlider(new Rect(25, 575, 300, 50), press_mult, 0.0f, 500.0f);
    }

    void setup() {
        data = new Particle[num_particles];
        float spacing = 0.70f;
        uint edge = (uint)Math.Sqrt(num_particles);
        Vector3 bottomleft = new Vector3(24.0f - (edge >> 1) * spacing, 13.5f - (edge >> 1) * spacing, 0.0f);
        for (uint i = 0; i < num_particles; i++) {
            data[i] = new Particle();
            data[i].pos = bottomleft + new Vector3((i % edge) * spacing, (uint)i / edge * spacing, 0.0f);
            data[i].velocity = new Vector3(0.0f, 0.0f, 0.0f);
            data[i].mass = 1.0f;
        }

        renderer_compute.GetKernelThreadGroupSizes(renderer_compute.FindKernel("CSMain"), out xsize, out ysize, out _);
        renderer_compute.GetKernelThreadGroupSizes(renderer_compute.FindKernel("CSMain"), out physics_xsize, out _, out _);

        density_buffer = new ComputeBuffer((int)num_particles, sizeof(float));
        particle_buffer = new ComputeBuffer((int)num_particles, sizeof(float) * 7);
        // Array.Sort(data, delegate(Sphere s1, Sphere s2) {
        //     return (s1.pos.x * s1.pos.x + s1.pos.y * s1.pos.y + s1.pos.z * s1.pos.z).CompareTo(s2.pos.x * s2.pos.x + s2.pos.y * s2.pos.y + s2.pos.z * s2.pos.z);
        // });

        render_texture = new RenderTexture(1920, 1080, 24);
        render_texture.enableRandomWrite = true;
        render_texture.Create();
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        if (data == null) {
            setup();

            precalc_compute.SetBuffer(0, "densities", density_buffer);
            physics_compute.SetBuffer(0, "densities", density_buffer);

            particle_buffer.SetData(data);
            precalc_compute.SetBuffer(0, "particles", particle_buffer);
            physics_compute.SetBuffer(0, "particles", particle_buffer);
            renderer_compute.SetBuffer(0, "particles", particle_buffer);
        }

        if (playing || step) {
            precalc_compute.SetInt("num_particles", (int)num_particles);
            precalc_compute.Dispatch(0, (int)Math.Ceiling((float)num_particles / physics_xsize), 1, 1);

            physics_compute.SetVector("bounds", new Vector2(48.0f, 27.0f));
            physics_compute.SetInt("num_particles", (int)num_particles);
            physics_compute.SetFloat("delta_time", Time.deltaTime);
            physics_compute.SetInt("targ_dens", (int)num_particles);
            physics_compute.SetFloat("press_mult", Time.deltaTime);
            physics_compute.Dispatch(0, (int)Math.Ceiling((float)num_particles / physics_xsize), 1, 1);
            step = false;
        }

        renderer_compute.SetTexture(0, "Result", render_texture);
        renderer_compute.SetVector("res", new Vector2(render_texture.width, render_texture.height));
        renderer_compute.SetFloat("fov", 60.0f);
        renderer_compute.SetVector("origin", origin);
        renderer_compute.SetInt("num_particles", (int)num_particles);
        renderer_compute.Dispatch(0, render_texture.width / (int)xsize, render_texture.height / (int)ysize, 1);

        Graphics.Blit(render_texture, dest);
    }

    private void OnDisable() {
        density_buffer.Release();
        particle_buffer.Release();
        render_texture.Release();
    }
}
