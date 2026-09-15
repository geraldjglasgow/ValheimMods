namespace OpenKeep.Signs
{
    /// <summary>
    /// Removes a sign: through the scene when it is loaded (<c>ZNetScene.Destroy</c>), through the ZDO manager when it
    /// is not, after claiming its ZDO. Nothing drops; only the game's own destruction returns the sign's wood.
    /// </summary>
    public static class SignRemover
    {
        /// <summary>Removes the container's sign, if any, and clears the link. The container's owner calls this.</summary>
        public static bool RemoveFrom(ZDO container)
        {
            ZDO sign = SignLinks.LinkedSign(container);
            bool removed = sign != null && Destroy(sign);
            if (container.IsOwner())
                SignLinks.Unlink(container);
            return removed;
        }

        public static bool Destroy(ZDO sign)
        {
            if (sign == null || !sign.IsValid() || ZNetScene.instance == null || ZDOMan.instance == null)
                return false;
            if (!SignLinks.Claim(sign))
            {
                Plugin.Log.LogWarning($"OpenKeep: sign {sign.m_uid} could not be claimed; it stays");
                return false;
            }
            ZNetView view = ZNetScene.instance.FindInstance(sign);
            if (view != null)
                ZNetScene.instance.Destroy(view.gameObject);
            else
                ZDOMan.instance.DestroyZDO(sign);
            return true;
        }
    }
}
