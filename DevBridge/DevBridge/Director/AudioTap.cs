using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// The game's mixed sound as the listener hears it (OnAudioFilterRead on the AudioListener, the audio thread), kept
    /// in memory with the sample position reached at each recorded frame. Finish writes a WAV laid on the film's own
    /// time: one frame's worth of samples per frame, and where the real clock ran away from the film (a hitch) it
    /// jumps to where the frame really was with a 10 ms crossfade, so sound stays on its picture.
    /// </summary>
    internal sealed class AudioTap
    {
        private readonly ListenerTap tap;
        private readonly List<long> marks = new List<long>();
        private readonly int fps;
        private int jumps;

        internal int Rate { get; }
        internal int Jumps => jumps;
        internal float Seconds => tap.Count / (float)Math.Max(1, tap.Channels) / Rate;

        private AudioTap(ListenerTap tap, int fps)
        {
            this.tap = tap;
            this.fps = fps;
            Rate = AudioSettings.outputSampleRate;
        }

        /// <summary>Null when there is no listener to tap.</summary>
        internal static AudioTap Start(int fps)
        {
            GameCamera camera = GameCamera.instance;
            AudioListener listener = camera && camera.m_listner ? camera.m_listner : null;
            if (!listener) return null;
            ListenerTap tap = listener.gameObject.GetComponent<ListenerTap>() ?? listener.gameObject.AddComponent<ListenerTap>();
            tap.Begin();
            return new AudioTap(tap, fps);
        }

        /// <summary>Called at each recorded frame: where the sound had got to.</summary>
        internal void Mark() => marks.Add(tap.Count);

        /// <summary>Writes the WAV; when that file is held open elsewhere, under a fresh name beside it. Returns the path written.</summary>
        internal string Finish(string path)
        {
            float[] laid = Layout(tap.End(), Math.Max(1, tap.Channels));
            try
            {
                Write(path, laid);
                return path;
            }
            catch (IOException)
            {
                string other = Path.ChangeExtension(path, null) + "_" + DateTime.Now.ToString("HHmmss") + ".wav";
                Write(other, laid);
                return other;
            }
        }

        private void Write(string path, float[] samples)
        {
            int channels = Math.Max(1, tap.Channels);
            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write)) WavFile.Write(file, samples, channels, Rate);
        }

        /// <summary>The film-timed sound: per frame rate/fps samples, from where the real clock says the frame was.</summary>
        private float[] Layout(float[] source, int channels)
        {
            double perFrame = (double)Rate / fps;
            var output = new float[(long)(marks.Count * perFrame) * channels];
            long read = marks.Count > 0 ? marks[0] / channels : 0, written = 0;
            for (int frame = 0; frame < marks.Count; frame++)
            {
                long wanted = (long)((frame + 1) * perFrame) - written, actual = marks[frame] / channels, fadeFrom = -1;
                if (Math.Abs(actual - read) > Rate / 25) (fadeFrom, read, jumps) = (read, actual, jumps + 1);
                Copy(source, output, read, written, wanted, channels, fadeFrom);
                read += wanted;
                written += wanted;
            }
            return output;
        }

        private void Copy(float[] source, float[] output, long read, long at, long count, int channels, long fadeFrom)
        {
            int fade = fadeFrom < 0 ? 0 : Rate / 100;
            for (long i = 0; i < count && (at + i + 1) * channels <= output.Length; i++)
                for (int c = 0; c < channels; c++)
                {
                    float value = Sample(source, read + i, c, channels);
                    if (i < fade) value = Mathf.Lerp(Sample(source, fadeFrom + i, c, channels), value, i / (float)fade);
                    output[(at + i) * channels + c] = value;
                }
        }

        private static float Sample(float[] source, long frame, int channel, int channels)
        {
            long index = frame * channels + channel;
            return index >= 0 && index < source.Length ? source[index] : 0f;
        }
    }

    /// <summary>On the listener's object: copies every mixed buffer the audio thread passes through it.</summary>
    internal sealed class ListenerTap : MonoBehaviour
    {
        private readonly object gate = new object();
        private List<float> samples;
        private volatile int channels;

        internal int Channels => channels;
        internal long Count { get { lock (gate) return samples?.Count ?? 0; } }

        internal void Begin()
        {
            lock (gate) samples = new List<float>(48000 * 2 * 30);
        }

        internal float[] End()
        {
            lock (gate)
            {
                float[] all = samples?.ToArray() ?? new float[0];
                samples = null;
                return all;
            }
        }

        private void OnAudioFilterRead(float[] data, int count)
        {
            channels = count;
            lock (gate) samples?.AddRange(data);
        }
    }

    /// <summary>A RIFF/WAVE file of 32-bit float samples.</summary>
    internal static class WavFile
    {
        internal static void Write(Stream stream, float[] samples, int channels, int rate)
        {
            var w = new BinaryWriter(stream);
            uint data = (uint)(samples.Length * 4);
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36u + data);
            w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            w.Write(16u);
            w.Write((ushort)3);
            w.Write((ushort)channels);
            w.Write((uint)rate);
            w.Write((uint)(rate * channels * 4));
            w.Write((ushort)(channels * 4));
            w.Write((ushort)32);
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(data);
            var bytes = new byte[samples.Length * 4];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
            w.Write(bytes);
            w.Flush();
        }
    }
}
