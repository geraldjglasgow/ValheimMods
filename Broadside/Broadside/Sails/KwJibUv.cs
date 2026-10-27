using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a jib's cloth lies in its cloth's texture (<see cref="KwJibDress"/>): the jib's own triangle cut out of the
    /// texture at one scale both ways, as its frame places it (<see cref="KwSailFrame.Corners"/>: its foot along the
    /// texture's foot, its panels square to the foot, as much cloth a metre as its foremast's lowest sail, or as large as
    /// fits when that would not), so a jib shows the cloth as the sails beside it do, nothing stretched or skewed. The
    /// cloth's mesh is a triangle sail's
    /// (<see cref="KwSailCanvas"/>), square-cornered and set into the jib's shape by a shear (<see cref="KwJibShear"/>);
    /// its places (shares along its foot and up its luff) go to the same shares between the jib's corners in the texture,
    /// which undoes the shear. Raised part way, the standing cloth shows the texture as it stands there (the foot's hem
    /// on the club, its panels kept their size) and the flakes on the club the cloth's lower rows, as a triangle sail's do.
    /// </summary>
    public readonly struct KwJibUv
    {
        public KwJibUv(Vector2 tack, Vector2 clew, Vector2 head)
        {
            (Tack, Clew, Head) = (tack, clew, head);
        }

        /// <summary>The jib's corners in the texture.</summary>
        public Vector2 Tack { get; }
        public Vector2 Clew { get; }
        public Vector2 Head { get; }

        /// <summary>A jib's place in the texture from its frame (<see cref="KwJibDress.Frame"/>).</summary>
        public static KwJibUv Of(KwSailFrame frame)
        {
            Vector2[] corners = frame.Corners;
            return new KwJibUv(corners[0], corners[1], corners[2]);
        }

        /// <summary>The texture place of a cloth place: a share of the way from the tack to the clew, and of the way to the head.</summary>
        public Vector2 At(float along, float up) => Tack + along * (Clew - Tack) + up * (Head - Tack);

        /// <summary>The standing cloth at hoist f: u along its foot, v up from the flakes to its head (the cloth's own place there).</summary>
        public Vector2 Standing(float u, float v, float f) => At(u * (1f - v), v * f);

        /// <summary>The flaked cloth: u along the club, y the row of the cloth from its foot.</summary>
        public Vector2 Flaked(float u, float y) => At(u * (1f - y), y);
    }
}
