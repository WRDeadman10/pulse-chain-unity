using UnityEngine;

namespace PulseChain.Gameplay {
    public sealed class AudioSystem : MonoBehaviour {
        private AudioSource _audioSource;
        private AudioClip _hitClip;
        private AudioClip _perfectClip;
        private AudioClip _missClip;
        private AudioClip _comboClip;
        private float _comboCooldown;

        public void Initialize() {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null) {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0.0f;
            _hitClip = CreateToneClip("Hit", 150.0f, 0.08f, 0.12f);
            _perfectClip = CreateToneClip("Perfect", 380.0f, 0.12f, 0.18f);
            _missClip = CreateToneClip("Miss", 90.0f, 0.18f, 0.18f);
            _comboClip = CreateToneClip("Combo", 520.0f, 0.15f, 0.14f);
        }

        public void Tick(float deltaTime, int combo, bool overloadActive) {
            if (_comboCooldown > 0.0f) {
                _comboCooldown -= deltaTime;
            }

            if (combo >= 3 && overloadActive && _comboCooldown <= 0.0f) {
                PlayClip(_comboClip, 0.16f);
                _comboCooldown = 0.55f;
            }
        }

        public void PlayHit() {
            PlayClip(_hitClip, 0.18f);
        }

        public void PlayPerfect(int combo) {
            float pitch = Mathf.Clamp(1.0f + (combo * 0.03f), 1.0f, 1.3f);
            PlayClip(_perfectClip, 0.24f, pitch);
            if (combo >= 2) {
                PlayClip(_comboClip, 0.10f, pitch + 0.15f);
            }
        }

        public void PlayMiss() {
            PlayClip(_missClip, 0.26f, 0.86f);
        }

        private void PlayClip(AudioClip clip, float volume) {
            PlayClip(clip, volume, 1.0f);
        }

        private void PlayClip(AudioClip clip, float volume, float pitch) {
            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(clip, volume);
            _audioSource.pitch = 1.0f;
        }

        private AudioClip CreateToneClip(string clipName, float frequency, float duration, float amplitude) {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++) {
                float time = (float)i / sampleRate;
                float envelope = 1.0f - (time / duration);
                samples[i] = Mathf.Sin(time * frequency * Mathf.PI * 2.0f) * amplitude * envelope;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}