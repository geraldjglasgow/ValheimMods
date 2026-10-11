using BundlePrefabs;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Humans;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// A component a shell is given (taming, breeding) takes its values from one of the game's creatures that has it, so
    /// it behaves as the game's own do: the teacher's component is read from a private copy of the teacher, made for the
    /// purpose and destroyed after, so no list or effect is shared with the game's prefab. Only the serialized fields the
    /// component's own type declares are copied (<see cref="HumanFieldCopy"/>), the ones every creature made from the
    /// prefab carries.
    /// </summary>
    internal static class Lessons
    {
        /// <summary>Copies the values of <paramref name="teacher"/>'s component of the pupil's type; false when the game has
        /// no such creature or it has no such component (the pupil keeps the game's defaults).</summary>
        public static bool Learn(CreatureBuild build, Component pupil, string teacher)
        {
            GameObject? prefab = build.Find.Prefab(teacher);
            if (prefab == null || prefab.GetComponent(pupil.GetType()) == null)
            {
                return false;
            }
            GameObject lesson = PrefabBench.Copy(prefab, teacher + "_ecp_lesson");
            try
            {
                HumanFieldCopy.Copy(pupil.GetType(), lesson.GetComponent(pupil.GetType()), pupil);
                return true;
            }
            finally
            {
                Object.DestroyImmediate(lesson);
            }
        }
    }
}
