using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesPack.Headsman
{
    /// <summary>
    /// Stones for the Executioner's ground attacks, drawn on every peer and never networked: chunks thrown up where the
    /// slam lands, which fall, lie and sink; and slabs jutting up out of the floor in rows along the scrape's shockwave
    /// (<see cref="HeadsmanWave"/>: each later the further out it is, leaning outward, holding, sinking). A pool of
    /// faceted chunks wearing the game's Black Forest rock.
    /// </summary>
    public sealed class HeadsmanRocks : MonoBehaviour
    {
        private sealed class Rock
        {
            public Transform Body = null!;
            public float Start = -1f, Size, Floor;
            public bool Thrown;
            public Vector3 At, Velocity, Spin;
        }

        private const int Count = 90;
        private const float Rise = 0.09f, Hold = 0.3f, Sink = 0.35f;

        /// <summary>The stones' material: the game's forest rock, set when the prefabs are built.</summary>
        public static Material? Stone;

        private static HeadsmanRocks? instance;
        private readonly List<Rock> rocks = new List<Rock>();
        private readonly System.Random random = new System.Random(11);

        private static HeadsmanRocks? Instance => instance != null ? instance : Stone == null ? null : instance = Create();

        /// <summary>Twelve chunks thrown up where a blow lands.</summary>
        public static void Burst(Vector3 at)
        {
            HeadsmanRocks? rocks = Instance;
            for (int i = 0; i < 12 && rocks != null; i++)
            {
                rocks.Throw(at);
            }
        }

        /// <summary>A row of slabs pushing up outward from `from` along `outward`, each later by its distance.</summary>
        public static void Row(Vector3 from, Vector3 outward)
        {
            HeadsmanRocks? rocks = Instance;
            for (float d = 0.15f; d <= HeadsmanWave.Reach && rocks != null; d += 0.32f)
            {
                Vector3 side = Vector3.Cross(Vector3.up, outward) * (rocks.Jitter() * 0.25f);
                rocks.Pop(from + outward * d + side, outward, Time.time + d / HeadsmanWave.Speed, Mathf.Lerp(0.6f, 0.3f, d / HeadsmanWave.Reach));
            }
        }

        private static HeadsmanRocks Create()
        {
            var holder = new GameObject("ECP_Headsman_rocks");
            DontDestroyOnLoad(holder);
            HeadsmanRocks made = holder.AddComponent<HeadsmanRocks>();
            Mesh[] shapes = { HeadsmanChunk.Mesh(1), HeadsmanChunk.Mesh(2), HeadsmanChunk.Mesh(3), HeadsmanChunk.Mesh(4) };
            for (int i = 0; i < Count; i++)
            {
                var body = new GameObject("rock", typeof(MeshFilter), typeof(MeshRenderer));
                body.transform.SetParent(holder.transform, false);
                body.GetComponent<MeshFilter>().sharedMesh = shapes[i % shapes.Length];
                body.GetComponent<MeshRenderer>().sharedMaterial = Stone;
                body.SetActive(false);
                made.rocks.Add(new Rock { Body = body.transform });
            }
            return made;
        }

        private void Throw(Vector3 at)
        {
            Rock? rock = Free();
            if (rock == null)
            {
                return;
            }
            float angle = (float)random.NextDouble() * 360f;
            (rock.Thrown, rock.Start, rock.At, rock.Size, rock.Floor) = (true, Time.time, at + Vector3.up * 0.05f, 0.1f + 0.12f * (float)random.NextDouble(), at.y);
            rock.Velocity = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 3f + 2.5f * (float)random.NextDouble(), 1f + 2f * (float)random.NextDouble());
            rock.Spin = new Vector3(Jitter(), Jitter(), Jitter()) * 900f;
        }

        private void Pop(Vector3 at, Vector3 outward, float start, float size)
        {
            Rock? rock = Free();
            if (rock == null)
            {
                return;
            }
            (rock.Thrown, rock.Start, rock.At, rock.Size) = (false, start, at, size * (0.8f + 0.4f * (float)random.NextDouble()));
            Vector3 lean = Quaternion.AngleAxis(Jitter() * 25f, Vector3.up) * Vector3.Lerp(Vector3.up, outward, 0.3f + 0.15f * Jitter());
            rock.Body.rotation = Quaternion.LookRotation(lean, outward) * Quaternion.Euler(0f, 0f, Jitter() * 40f);
        }

        private void Update()
        {
            foreach (Rock rock in rocks)
            {
                if (rock.Start < 0f)
                {
                    continue;
                }
                if (rock.Thrown)
                {
                    Fly(rock, Time.time - rock.Start, Time.deltaTime);
                }
                else
                {
                    Stand(rock, Time.time - rock.Start);
                }
            }
        }

        /// <summary>A slab jutting up, holding with a shiver, sinking back.</summary>
        private static void Stand(Rock rock, float since)
        {
            rock.Body.gameObject.SetActive(since >= 0f);
            if (since < 0f)
            {
                return;
            }
            float up = since < Rise ? since / Rise : since < Rise + Hold ? 1f : 1f - (since - Rise - Hold) / Sink;
            float shake = since < Rise + Hold ? 0.015f * Mathf.Sin(since * 90f) : 0f;
            rock.Body.position = rock.At + Vector3.up * (rock.Size * (0.8f * Mathf.Clamp01(up) - 0.55f) + shake);
            rock.Body.localScale = Vector3.one * rock.Size;
            if (up <= 0f)
            {
                Retire(rock);
            }
        }

        /// <summary>A chunk flying and falling, lying where it lands, sinking after a while.</summary>
        private void Fly(Rock rock, float since, float dt)
        {
            rock.Body.gameObject.SetActive(true);
            float rest = rock.Floor + rock.Size * 0.3f;
            if (rock.Velocity.y > 0f || rock.At.y > rest)
            {
                rock.Velocity += Vector3.down * 9.8f * dt;
                rock.At += rock.Velocity * dt;
                rock.Body.rotation = Quaternion.Euler(rock.Spin * dt) * rock.Body.rotation;
            }
            rock.At.y = Mathf.Max(rock.At.y, rest);
            float sink = Mathf.Clamp01((since - 1.4f) / Sink);
            rock.Body.position = rock.At + Vector3.down * (sink * rock.Size);
            rock.Body.localScale = Vector3.one * rock.Size;
            if (sink >= 1f || since > 3f)
            {
                Retire(rock);
            }
        }

        private static void Retire(Rock rock)
        {
            rock.Start = -1f;
            rock.Body.gameObject.SetActive(false);
        }

        private Rock? Free() => rocks.Find(r => r.Start < 0f);

        private float Jitter() => (float)random.NextDouble() * 2f - 1f;
    }
}
