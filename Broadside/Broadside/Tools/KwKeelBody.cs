using System;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A keel's thickness: half its width and its depth under the keel line. Its posts scale with it: a third wider than
    /// the keel (at least 4 cm each side, so the planking's laps stay inside a slim one) and six sevenths of its depth
    /// along it, which for the standard keel (0.3 by 0.35 m) is a post 0.4 by 0.3 m.
    /// </summary>
    public readonly struct KwKeelBody
    {
        public const float MinWidth = 0.1f, MaxWidth = 0.6f, MinDepth = 0.1f, MaxDepth = 0.8f;

        public KwKeelBody(float half, float depth)
        {
            Half = half;
            Depth = depth;
        }

        public float Half { get; }
        public float Depth { get; }

        /// <summary>Half a post's width across the ship: a third wider than the keel, and at least 4 cm each side of it.</summary>
        public float PostHalf => Mathf.Max(Half * 4f / 3f, Half + 0.04f);

        /// <summary>A post's depth along the keel.</summary>
        public float PostDepth => Depth * 6f / 7f;
    }

    /// <summary>A keel's line and body as its mesh and snap points are built: length, the keel line's height along it, the post's place and height.</summary>
    public sealed class KwKeelLine
    {
        public KwKeelLine(float length, Func<float, float> rise, float? post, float postAbove, KwKeelBody body)
        {
            Length = length;
            Rise = rise;
            Post = post;
            PostAbove = postAbove;
            Body = body;
        }

        public float Length { get; }
        public Func<float, float> Rise { get; }
        public float? Post { get; }
        public float PostAbove { get; }
        public KwKeelBody Body { get; }

        /// <summary>The keel line's height at x, held to the keel's own length.</summary>
        public float RiseAt(float x) => Rise(Mathf.Clamp(x, 0f, Length));
    }
}
