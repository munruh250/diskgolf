using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Shared one-shot audio playback for gameplay events.</summary>
    public static class GameplayAudio
    {
        static AudioSource _oneShot;

        public static void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
                return;

            EnsureOneShotSource().PlayOneShot(clip, volume);
        }

        static AudioSource EnsureOneShotSource()
        {
            if (_oneShot != null)
                return _oneShot;

            var go = new GameObject("GameplayAudio");
            Object.DontDestroyOnLoad(go);
            _oneShot = go.AddComponent<AudioSource>();
            _oneShot.playOnAwake = false;
            return _oneShot;
        }
    }
}
