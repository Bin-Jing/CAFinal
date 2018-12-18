

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace DigitalRuby.RainMaker
{
    public class BaseRainScript : MonoBehaviour
    {
        public Camera Camera;

        public bool FollowCamera = true;

        public AudioClip RainSoundLight;

        public AudioClip RainSoundMedium;

        public AudioClip RainSoundHeavy;

        [Range(0.0f, 1.0f)]
        public float RainIntensity;

        public ParticleSystem RainFallParticleSystem;

        public ParticleSystem RainExplosionParticleSystem;

        public ParticleSystem RainMistParticleSystem;

        [Range(0.0f, 1.0f)]
        public float RainMistThreshold = 0.5f;

        public AudioClip WindSound;

        public float WindSoundVolumeModifier = 0.5f;

        public WindZone WindZone;

        public Vector3 WindSpeedRange = new Vector3(50.0f, 500.0f, 500.0f);

        public Vector2 WindChangeInterval = new Vector2(5.0f, 30.0f);

        public bool EnableWind = true;

        protected LoopingAudioSource audioSourceRainLight;
        protected LoopingAudioSource audioSourceRainMedium;
        protected LoopingAudioSource audioSourceRainHeavy;
        protected LoopingAudioSource audioSourceRainCurrent;
        protected LoopingAudioSource audioSourceWind;
        protected Material rainMaterial;
        protected Material rainExplosionMaterial;
        protected Material rainMistMaterial;

        private float lastRainIntensityValue = -1.0f;
        private float nextWindTime;

        private void UpdateWind()
        {
            if (EnableWind && WindZone != null && WindSpeedRange.y > 1.0f)
            {
                WindZone.gameObject.SetActive(true);
                if (FollowCamera)
                {
                    WindZone.transform.position = Camera.transform.position;
                }
                if (!Camera.orthographic)
                {
                    WindZone.transform.Translate(0.0f, WindZone.radius, 0.0f);
                }
                if (nextWindTime < Time.time)
                {
                    WindZone.windMain = UnityEngine.Random.Range(WindSpeedRange.x, WindSpeedRange.y);
                    WindZone.windTurbulence = UnityEngine.Random.Range(WindSpeedRange.x, WindSpeedRange.y);
                    if (Camera.orthographic)
                    {
                        int val = UnityEngine.Random.Range(0, 2);
                        WindZone.transform.rotation = Quaternion.Euler(0.0f, (val == 0 ? 90.0f : -90.0f), 0.0f);
                    }
                    else
                    {
                        WindZone.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-30.0f, 30.0f), UnityEngine.Random.Range(0.0f, 360.0f), 0.0f);
                    }
                    nextWindTime = Time.time + UnityEngine.Random.Range(WindChangeInterval.x, WindChangeInterval.y);
                    audioSourceWind.Play((WindZone.windMain / WindSpeedRange.z) * WindSoundVolumeModifier);
                }
            }
            else
            {
                if (WindZone != null)
                {
                    WindZone.gameObject.SetActive(false);
                }
                audioSourceWind.Stop();
            }

            audioSourceWind.Update();
        }

        private void CheckForRainChange()
        {
            if (lastRainIntensityValue != RainIntensity)
            {
                lastRainIntensityValue = RainIntensity;
                if (RainIntensity <= 0.01f)
                {
                    if (audioSourceRainCurrent != null)
                    {
                        audioSourceRainCurrent.Stop();
                        audioSourceRainCurrent = null;
                    }
                    if (RainFallParticleSystem != null)
                    {
                        ParticleSystem.EmissionModule e = RainFallParticleSystem.emission;
                        e.enabled = false;
                        RainFallParticleSystem.Stop();
                    }
                    if (RainMistParticleSystem != null)
                    {
                        ParticleSystem.EmissionModule e = RainMistParticleSystem.emission;
                        e.enabled = false;
                        RainMistParticleSystem.Stop();
                    }
                }
                else
                {
                    LoopingAudioSource newSource;
                    if (RainIntensity >= 0.67f)
                    {
                        newSource = audioSourceRainHeavy;
                    }
                    else if (RainIntensity >= 0.33f)
                    {
                        newSource = audioSourceRainMedium;
                    }
                    else
                    {
                        newSource = audioSourceRainLight;
                    }
                    if (audioSourceRainCurrent != newSource)
                    {
                        if (audioSourceRainCurrent != null)
                        {
                            audioSourceRainCurrent.Stop();
                        }
                        audioSourceRainCurrent = newSource;
                        audioSourceRainCurrent.Play(1.0f);
                    }
                    if (RainFallParticleSystem != null)
                    {
                        ParticleSystem.EmissionModule e = RainFallParticleSystem.emission;
                        e.enabled = RainFallParticleSystem.GetComponent<Renderer>().enabled = true;
                        if (!RainFallParticleSystem.isPlaying)
                        {
                            RainFallParticleSystem.Play();
                        }
                        ParticleSystem.MinMaxCurve rate = e.rateOverTime;
                        rate.mode = ParticleSystemCurveMode.Constant;
                        rate.constantMin = rate.constantMax = RainFallEmissionRate();
                        e.rateOverTime = rate;
                    }
                    if (RainMistParticleSystem != null)
                    {
                        ParticleSystem.EmissionModule e = RainMistParticleSystem.emission;
                        e.enabled = RainMistParticleSystem.GetComponent<Renderer>().enabled = true;
                        if (!RainMistParticleSystem.isPlaying)
                        {
                            RainMistParticleSystem.Play();
                        }
                        float emissionRate;
                        if (RainIntensity < RainMistThreshold)
                        {
                            emissionRate = 0.0f;
                        }
                        else
                        {
                            // must have RainMistThreshold or higher rain intensity to start seeing mist
                            emissionRate = MistEmissionRate();
                        }
                        ParticleSystem.MinMaxCurve rate = e.rateOverTime;
                        rate.mode = ParticleSystemCurveMode.Constant;
                        rate.constantMin = rate.constantMax = emissionRate;
                        e.rateOverTime = rate;
                    }
                }
            }
        }

        protected virtual void Start()
        {

            if (Camera == null)
            {
                Camera = Camera.main;
            }

            audioSourceRainLight = new LoopingAudioSource(this, RainSoundLight);
            audioSourceRainMedium = new LoopingAudioSource(this, RainSoundMedium);
            audioSourceRainHeavy = new LoopingAudioSource(this, RainSoundHeavy);
            audioSourceWind = new LoopingAudioSource(this, WindSound);

            if (RainFallParticleSystem != null)
            {
                ParticleSystem.EmissionModule e = RainFallParticleSystem.emission;
                e.enabled = false;
                Renderer rainRenderer = RainFallParticleSystem.GetComponent<Renderer>();
                rainRenderer.enabled = false;
                rainMaterial = new Material(rainRenderer.material);
                rainMaterial.EnableKeyword("SOFTPARTICLES_OFF");
                rainRenderer.material = rainMaterial;
            }
            if (RainExplosionParticleSystem != null)
            {
                ParticleSystem.EmissionModule e = RainExplosionParticleSystem.emission;
                e.enabled = false;
                Renderer rainRenderer = RainExplosionParticleSystem.GetComponent<Renderer>();
                rainExplosionMaterial = new Material(rainRenderer.material);
                rainExplosionMaterial.EnableKeyword("SOFTPARTICLES_OFF");
                rainRenderer.material = rainExplosionMaterial;
            }
            if (RainMistParticleSystem != null)
            {
                ParticleSystem.EmissionModule e = RainMistParticleSystem.emission;
                e.enabled = false;
                Renderer rainRenderer = RainMistParticleSystem.GetComponent<Renderer>();
                rainRenderer.enabled = false;
                rainMistMaterial = new Material(rainRenderer.material);
                if (UseRainMistSoftParticles)
                {
                    rainMistMaterial.EnableKeyword("SOFTPARTICLES_ON");
                }
                else
                {
                    rainMistMaterial.EnableKeyword("SOFTPARTICLES_OFF");
                }
                rainRenderer.material = rainMistMaterial;
            }
        }

        protected virtual void Update()
        {

#if DEBUG

            if (RainFallParticleSystem == null)
            {
                Debug.LogError("Rain fall particle system must be set to a particle system");
                return;
            }

#endif

            CheckForRainChange();
            UpdateWind();
            audioSourceRainLight.Update();
            audioSourceRainMedium.Update();
            audioSourceRainHeavy.Update();
        }

        protected virtual float RainFallEmissionRate()
        {
            return (RainFallParticleSystem.main.maxParticles / RainFallParticleSystem.main.startLifetime.constant) * RainIntensity;
        }

        protected virtual float MistEmissionRate()
        {
            return (RainMistParticleSystem.main.maxParticles / RainMistParticleSystem.main.startLifetime.constant) * RainIntensity * RainIntensity;
        }

        protected virtual bool UseRainMistSoftParticles
        {
            get
            {
                return true;
            }
        }
    }

    /// <summary>
    /// Provides an easy wrapper to looping audio sources with nice transitions for volume when starting and stopping
    /// </summary>
    public class LoopingAudioSource
    {
        public AudioSource AudioSource { get; private set; }
        public float TargetVolume { get; private set; }

        public LoopingAudioSource(MonoBehaviour script, AudioClip clip)
        {
            AudioSource = script.gameObject.AddComponent<AudioSource>();
            AudioSource.loop = true;
            AudioSource.clip = clip;
            AudioSource.playOnAwake = false;
            AudioSource.volume = 0.0f;
            AudioSource.Stop();
            TargetVolume = 1.0f;
        }

        public void Play(float targetVolume)
        {
            if (!AudioSource.isPlaying)
            {
                AudioSource.volume = 0.0f;
                AudioSource.Play();
            }
            TargetVolume = targetVolume;
        }

        public void Stop()
        {
            TargetVolume = 0.0f;
        }

        public void Update()
        {
            if (AudioSource.isPlaying && (AudioSource.volume = Mathf.Lerp(AudioSource.volume, TargetVolume, Time.deltaTime)) == 0.0f)
            {
                AudioSource.Stop();
            }
        }
    }

}