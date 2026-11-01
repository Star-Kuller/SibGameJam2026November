using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using StopMode = FMOD.Studio.STOP_MODE;

namespace _Game.Code.Features.Audio
{
    // Owns FMOD instances so pause/restart never leaves gameplay voices behind.
    public sealed class GateAudio : IDisposable
    {
        private readonly List<EventInstance> _instances = new();

        private bool _paused;

        public void SetVolume(float volume)
        {
            var master = RuntimeManager.GetBus("bus:/");

            if (master.isValid())
            {
                master.setVolume(Mathf.Clamp01(volume));
            }
        }

        public void Play(EventReference reference, Vector3 position, bool menu = false)
        {
            if (reference.IsNull)
            {
                return;
            }

            var instance = RuntimeManager.CreateInstance(reference);
            var attributes = RuntimeUtils.To3DAttributes(position);

            instance.set3DAttributes(attributes);
            instance.setPaused(_paused && !menu);
            instance.start();

            // Menu one-shots continue independently of the paused encounter.
            if (menu)
            {
                instance.release();
            }
            else
            {
                _instances.Add(instance);
            }
        }

        public void SetPaused(bool paused)
        {
            _paused = paused;

            foreach (var instance in _instances)
            {
                if (instance.isValid())
                {
                    instance.setPaused(paused);
                }
            }
        }

        public void ReleaseFinished()
        {
            for (var index = _instances.Count - 1; index >= 0; index--)
            {
                var instance = _instances[index];
                instance.getPlaybackState(out var state);

                if (state != PLAYBACK_STATE.STOPPED)
                {
                    continue;
                }

                instance.release();
                _instances.RemoveAt(index);
            }
        }

        public void Dispose()
        {
            foreach (var instance in _instances)
            {
                if (!instance.isValid())
                {
                    continue;
                }

                instance.stop(StopMode.IMMEDIATE);
                instance.release();
            }

            _instances.Clear();
            _paused = false;
        }
    }
}
