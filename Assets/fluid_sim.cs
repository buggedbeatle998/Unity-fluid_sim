using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public struct Particles {
    public Vector3[] positions;
    public Vector3[] velocities;
    public float[] masses;
}

public struct Hashes {
    public uint[] part_map;
    public uint[] keys;
    public uint[] start_i;
}

public class fluid_sim : MonoBehaviour
{
    public ComputeShader precalc_compute;
    public ComputeShader bitonic_compute;
    public ComputeShader physics_compute;
    public ComputeShader renderer_compute;
    public RenderTexture render_texture;
    uint xsize;
    uint ysize;
    uint physics_xsize;
    uint bitonic_xsize;
    ComputeBuffer position_buffer;
    ComputeBuffer velocity_buffer;
    ComputeBuffer mass_buffer;
    Particles data;
    bool reset = true;
    ComputeBuffer density_buffer;
    ComputeBuffer predpos_buffer;
    ComputeBuffer map_buffer;
    ComputeBuffer key_buffer;
    ComputeBuffer start_buffer;

    Vector3 bounds = new Vector3(15.0f, 15.0f, 100.0f);
    uint num_particles = 5000;
    bool playing = false;
    bool step = false;
    float press_mult = 50.0f;
    float smoothing_rad = 5.0f;
    float gravity = 50.0f;
    float smooth_scale = 2.0f;//4.7f;
    float look_ahead = 1.0f/14.0f;
    float visc_mult = 1.0f;
    float dist = 100.0f;
    float radius = 1.0f;
    int pos_kernel;
    int bitonic_kernel;
    int reorder_kernel;
    int dens_kernel;
    int step_kernel;
    int ray_kernel;
    int[] prev1;
    int[] prev2;
    int[] prev3;


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

        Event e = Event.current;
        
        if (e.isKey) {
            if (e.type == EventType.KeyDown) {
                // if (e.keyCode == KeyCode.W) {
                //     selected.y++;
                // } else if (e.keyCode == KeyCode.S) {
                //     selected.y--;
                // } else if (e.keyCode == KeyCode.D) {
                //     selected.x++;
                // } else if (e.keyCode == KeyCode.A) {
                //     selected.x--;
                if (e.keyCode == KeyCode.V) {
                    Debug.Log("now");
                    int[] thing1 = new int[num_particles];
                    int[] thing2 = new int[num_particles];
                    int[] thing3 = new int[num_particles];
                    map_buffer.GetData(thing1);
                    key_buffer.GetData(thing2);
                    start_buffer.GetData(thing3);
                    // string printer = "|";
                    // for (int i = 0; i < num_particles; i++) {
                    //     printer += thing1[i].ToString() + " ";
                    //     printer += thing2[i].ToString() + " ";
                    //     printer += thing3[i].ToString() + "|";
                    // }
                    // Debug.Log(printer);
                    if (prev1 != null && prev2 != null && prev3 != null) {
                        for (int i = 0; i < num_particles; i++) {
                            if (prev1[i] != thing1[i]) {
                                Debug.Log("1 " + prev1[i].ToString() + " " + thing1[i].ToString());
                            }
                            if (prev2[i] != thing2[i]) {
                                Debug.Log("2 " + prev2[i].ToString() + " " + thing2[i].ToString());
                            }
                            if (prev3[i] != thing3[i]) {
                                Debug.Log("3 " + prev3[i].ToString() + " " + thing3[i].ToString());
                            }
                        }
                    }
                    prev1 = thing1;
                    prev2 = thing2;
                    prev3 = thing3;
                }
            }
        }

        if (GUI.Button(new Rect(25, 25, 200, 100), "Reset", myButtonStyle)) {
            reset = true;
            playing = false;
        }
        
        if (GUI.Button(new Rect(25, 150, 200, 100), playing ? "Pause" : "Play", myButtonStyle)) {
            playing = !playing;
        }

        if (!playing && GUI.Button(new Rect(25, 275, 200, 100), "Step", myButtonStyle)) {
            step = true;
        }

        GUI.Box(new Rect(25, 400, 300, 100), "Scale: " + Math.Round(smooth_scale, 2), myBoxStyle);
        smooth_scale = GUI.HorizontalSlider(new Rect(25, 450, 300, 50), smooth_scale, 0.0f, 20.0f);

        GUI.Box(new Rect(25, 525, 300, 100), "Mult: " + Math.Round(press_mult, 2), myBoxStyle);
        press_mult = GUI.HorizontalSlider(new Rect(25, 575, 300, 50), press_mult, 0.0f, 500.0f);

        GUI.Box(new Rect(25, 650, 300, 100), "Rad: " + Math.Round(smoothing_rad, 2), myBoxStyle);
        smoothing_rad = GUI.HorizontalSlider(new Rect(25, 700, 300, 50), smoothing_rad, 0.1f, 100.0f);

        GUI.Box(new Rect(25, 775, 300, 100), "Grav: " + Math.Round(gravity, 2), myBoxStyle);
        gravity = GUI.HorizontalSlider(new Rect(25, 825, 300, 50), gravity, 0.0f, 500.0f);
        
        GUI.Box(new Rect(25, 900, 300, 100), "Look: " + Math.Round(look_ahead, 2), myBoxStyle);
        look_ahead = GUI.HorizontalSlider(new Rect(25, 950, 300, 50), look_ahead, 0.0f, 0.1f);

        GUI.Box(new Rect(350, 25, 300, 100), "Visc: " + Math.Round(visc_mult, 2), myBoxStyle);
        visc_mult = GUI.HorizontalSlider(new Rect(350, 75, 300, 50), visc_mult, 0.0f, 5.0f);

        GUI.Box(new Rect(350, 150, 300, 100), "Dist: " + Math.Round(dist, 2), myBoxStyle);
        dist = GUI.HorizontalSlider(new Rect(350, 200, 300, 50), dist, 0.0f, 180.0f);

        GUI.Box(new Rect(350, 275, 300, 100), "Rad: " + Math.Round(radius, 2), myBoxStyle);
        radius = GUI.HorizontalSlider(new Rect(350, 325, 300, 50), radius, 0.0f, 5.0f);
    }


    void setup() {
        data = new Particles();
        data.positions = new Vector3[num_particles];
        data.velocities = new Vector3[num_particles];
        data.masses = new float[num_particles];
        float spacing = 0.70f;
        uint edge = 10;//(uint)Math.Sqrt(num_particles);
        Vector3 bottomleft = new Vector3((bounds.x - (edge * spacing)) / 2, (bounds.y - (edge * spacing)) / 2, (bounds.z - (edge * spacing)) / 2);
        for (uint i = 0; i < num_particles; i++) {
            data.positions[i] = bottomleft + new Vector3((i % edge) * spacing, (uint)i / (edge * edge) * spacing, (((uint)i / edge) % edge) * spacing);
            //data[i].pos = new Vector3(UnityEngine.Random.Range(0, bounds.x), UnityEngine.Random.Range(0, bounds.y), UnityEngine.Random.Range(0, bounds.z));
            data.velocities[i] = new Vector3(0.0f, 0.0f, 0.0f);
            data.masses[i] = 1.0f;
        }

        position_buffer.SetData(data.positions);
        velocity_buffer.SetData(data.velocities);
        mass_buffer.SetData(data.masses);

        pos_kernel = precalc_compute.FindKernel("calc_pos");
        bitonic_kernel = bitonic_compute.FindKernel("swapper");
        reorder_kernel = bitonic_compute.FindKernel("reorderer");
        dens_kernel = precalc_compute.FindKernel("calc_dens");
        step_kernel = physics_compute.FindKernel("sim_step");
        ray_kernel = renderer_compute.FindKernel("ray_trace");

        physics_compute.GetKernelThreadGroupSizes(step_kernel, out bitonic_xsize, out _, out _);
        physics_compute.GetKernelThreadGroupSizes(step_kernel, out physics_xsize, out _, out _);
        renderer_compute.GetKernelThreadGroupSizes(ray_kernel, out xsize, out ysize, out _);

        // Array.Sort(data, delegate(Sphere s1, Sphere s2) {
        //     return (s1.pos.x * s1.pos.x + s1.pos.y * s1.pos.y + s1.pos.z * s1.pos.z).CompareTo(s2.pos.x * s2.pos.x + s2.pos.y * s2.pos.y + s2.pos.z * s2.pos.z);
        // });
    }

    readonly uint[] tab32 = {
        0,  9,  1, 10, 13, 21,  2, 29,
        11, 14, 16, 18, 22, 25,  3, 30,
        8, 12, 20, 28, 15, 17, 24,  7,
        19, 27, 23,  6, 26,  5,  4, 31};
    uint log2_32(uint num) {
        num |= num >> 1;
        num |= num >> 2;
        num |= num >> 4;
        num |= num >> 8;
        num |= num >> 16;
        return tab32[(num * 0x07C4ACDD) >> 27];
    }


    void bitonic_sort() {
        uint passes = log2_32(num_particles - 1) + 1;
        uint ceil_particles = (uint)(1 << (int)passes);

        for (uint i = 1; i < passes + 1; i++) {
            bitonic_compute.SetInt("main", (int)i);
            for (uint j = i; j > 0; j--) {
                bitonic_compute.SetInt("swap_num", (int)j);
                bitonic_compute.Dispatch(bitonic_kernel, (int)Math.Ceiling((float)ceil_particles / bitonic_xsize), 1, 1);
            }
        }

        bitonic_compute.Dispatch(reorder_kernel, 1, 1, 1);

        // Debug.Log("now");
        // int[] thing = new int[num_particles];
        // map_buffer.GetData(thing);
        // string printer = "";
        // foreach (int item in thing) {
        //     printer += item.ToString() + " ";
        // }
        // Debug.Log(printer);

        // key_buffer.GetData(thing);
        // printer = "";
        // foreach (int item in thing) {
        //     printer += item.ToString() + " ";
        // }
        // Debug.Log(printer);
        
        // start_buffer.GetData(thing);
        // printer = "";
        // foreach (int item in thing) {
        //     printer += item.ToString() + " ";
        // }
        // Debug.Log(printer);
    }


    void submit_particle_buffers(ComputeShader shader, int kernel) {
        shader.SetBuffer(kernel, "positions", position_buffer);
        shader.SetBuffer(kernel, "velocities", velocity_buffer);
        shader.SetBuffer(kernel, "masses", mass_buffer);
    }


    void submit_search_buffers(ComputeShader shader, int kernel) {
        shader.SetBuffer(kernel, "part_map", map_buffer);
        shader.SetBuffer(kernel, "keys", key_buffer);
        shader.SetBuffer(kernel, "start_i", start_buffer);
    }


    private void OnRenderImage(RenderTexture src, RenderTexture dest) {
        if (reset) {
            setup();

            precalc_compute.SetBuffer(dens_kernel, "densities", density_buffer);
            physics_compute.SetBuffer(step_kernel, "densities", density_buffer);
            renderer_compute.SetBuffer(ray_kernel, "densities", density_buffer);

            submit_search_buffers(precalc_compute, pos_kernel);
            submit_search_buffers(bitonic_compute, bitonic_kernel);
            submit_search_buffers(bitonic_compute, reorder_kernel);
            submit_search_buffers(precalc_compute, dens_kernel);
            submit_search_buffers(physics_compute, step_kernel);
            submit_search_buffers(renderer_compute, ray_kernel);
            
            precalc_compute.SetBuffer(pos_kernel, "pred_pos", predpos_buffer);
            precalc_compute.SetBuffer(dens_kernel, "pred_pos", predpos_buffer);
            physics_compute.SetBuffer(step_kernel, "pred_pos", predpos_buffer);

            submit_particle_buffers(precalc_compute, pos_kernel);
            submit_particle_buffers(precalc_compute, dens_kernel);
            submit_particle_buffers(physics_compute, step_kernel);
            submit_particle_buffers(renderer_compute, ray_kernel);

            renderer_compute.SetTexture(ray_kernel, "Result", render_texture);

            bitonic_compute.SetInt("num_particles", (int)num_particles);
            uint passes = log2_32(num_particles - 1) + 1;
            bitonic_compute.SetInt("ceil_particles", (int)(1 << (int)passes));
            bitonic_compute.SetInt("log_parts", (int)passes);

            reset = false;
        }
            precalc_compute.SetBool("orig", true);
            precalc_compute.SetInt("num_particles", (int)num_particles);
            precalc_compute.SetFloat("smoothing_rad", smoothing_rad);
            precalc_compute.SetFloat("smooth_scale", smooth_scale);
            precalc_compute.SetFloat("delta_time", Time.deltaTime);
            precalc_compute.SetFloat("look_ahead", look_ahead);
            precalc_compute.SetVector("bounds", bounds);
            //precalc_compute.Dispatch(0, (int)Math.Ceiling((float)num_particles / physics_xsize), 1, 1);
            
            physics_compute.SetVector("bounds", bounds);
            physics_compute.SetInt("num_particles", (int)num_particles);
            physics_compute.SetFloat("delta_time", Time.deltaTime);
            physics_compute.SetFloat("press_mult", press_mult);
            physics_compute.SetFloat("smoothing_rad", smoothing_rad);
            physics_compute.SetFloat("smooth_scale", smooth_scale);
            physics_compute.SetVector("gravity", new Vector3(0, -gravity, 0));
            physics_compute.SetFloat("look_ahead", look_ahead);
            physics_compute.SetVector("bounds", bounds);
            physics_compute.SetFloat("visc_mult", visc_mult);
            
            renderer_compute.SetVector("res", new Vector3(render_texture.width, render_texture.height, 1920.0f));
            renderer_compute.SetFloat("fov", 60.0f);
            renderer_compute.SetVector("origin", new Vector3(bounds.x / 2, bounds.y / 2, -dist));
            renderer_compute.SetInt("num_particles", (int)num_particles);
            renderer_compute.SetFloat("smoothing_rad", smoothing_rad);
            renderer_compute.SetFloat("delta_time", Time.deltaTime);
            Vector3 sun = new Vector3(1.0f, 1.0f, -1.0f);
            sun.Normalize();
            renderer_compute.SetVector("sun", sun);
            renderer_compute.SetFloat("rad", radius);
            physics_compute.SetVector("bounds", bounds);
            precalc_compute.Dispatch(pos_kernel, (int)Math.Ceiling((float)num_particles / physics_xsize), 1, 1);

        bitonic_sort();

        if (playing || step) {
            precalc_compute.Dispatch(dens_kernel, (int)Math.Ceiling((float)num_particles / physics_xsize), 1, 1);

            physics_compute.SetFloat("delta_time", Time.deltaTime);
            physics_compute.Dispatch(step_kernel, (int)Math.Ceiling((float)num_particles / physics_xsize), 1, 1);
            step = false;
        }

        renderer_compute.Dispatch(ray_kernel, (int)Math.Ceiling((float)render_texture.width / xsize), render_texture.height / (int)ysize, 1);

        Graphics.Blit(render_texture, dest);
    }

    void OnEnable()
    {
        predpos_buffer = new ComputeBuffer((int)num_particles, sizeof(float) * 3);

        map_buffer = new ComputeBuffer((int)num_particles, sizeof(uint));
        key_buffer = new ComputeBuffer((int)num_particles, sizeof(uint));
        start_buffer = new ComputeBuffer((int)num_particles, sizeof(uint));

        density_buffer = new ComputeBuffer((int)num_particles, sizeof(float));

        position_buffer = new ComputeBuffer((int)num_particles, sizeof(float) * 3);
        velocity_buffer = new ComputeBuffer((int)num_particles, sizeof(float) * 3);
        mass_buffer = new ComputeBuffer((int)num_particles, sizeof(float));
        
        render_texture = new RenderTexture(1920, 1080, 24);
        render_texture.enableRandomWrite = true;
        render_texture.Create();
    }

    private void OnDisable() {
        predpos_buffer.Release();

        map_buffer.Release();
        key_buffer.Release();
        start_buffer.Release();

        density_buffer.Release();

        position_buffer.Release();
        velocity_buffer.Release();
        mass_buffer.Release();

        render_texture.Release();
    }
}
