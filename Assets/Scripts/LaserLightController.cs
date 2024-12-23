using UnityEngine;

public class LaserLightController : MonoBehaviour
{
    public Light laserLight;
    public float pulseSpeed = 2f;
    public float minIntensity = 0.6f;
    public float maxIntensity = 1f;

    void Update()
    {
        if (laserLight != null)
        {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            laserLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
        }
    }
}