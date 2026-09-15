namespace OpenKeep.Signs
{
    /// <summary>
    /// Writes a sign's words into its ZDO the way the game's <c>Sign.SetText</c> does (the <c>text</c> key the sign
    /// reads when its data revision changes), with an empty author: the game's sign grants viewing to everybody when
    /// the author is not a platform user, so the text shows on every platform. What the mod wrote is recorded as
    /// <c>OpenKeep.autoText</c>; a sign whose text differs from that record was edited by a player and is left alone
    /// until the player clears it.
    /// </summary>
    public static class SignWriter
    {
        /// <summary>The mod may write when the sign shows what the mod last wrote, or nothing at all.</summary>
        public static bool MayWrite(ZDO sign)
        {
            string current = sign.GetString(ZDOVars.s_text, "");
            return current.Length == 0 || current == sign.GetString(SignLinks.AutoTextKey, "");
        }

        /// <summary>Writes the text unless the sign already shows it; force writes it anyway. True when written.</summary>
        public static bool Write(ZDO sign, string text, bool force)
        {
            if (!force && sign.GetString(ZDOVars.s_text, "") == text && sign.GetString(SignLinks.AutoTextKey, "") == text)
                return false;
            if (!SignLinks.Claim(sign))
            {
                Plugin.Log.LogWarning($"OpenKeep: sign {sign.m_uid} could not be claimed; its text is not updated");
                return false;
            }
            sign.Set(ZDOVars.s_text, text);
            sign.Set(ZDOVars.s_author, "");
            sign.Set(ZDOVars.s_authorDisplayName, "");
            sign.Set(SignLinks.AutoTextKey, text);
            RefreshWidget(sign);
            return true;
        }

        /// <summary>Carries one sign's words to another (a sign placed anew): text, author, display name, the mod's record.</summary>
        public static void Copy(ZDO from, ZDO to)
        {
            to.Set(ZDOVars.s_text, from.GetString(ZDOVars.s_text, ""));
            to.Set(ZDOVars.s_author, from.GetString(ZDOVars.s_author, ""));
            to.Set(ZDOVars.s_authorDisplayName, from.GetString(ZDOVars.s_authorDisplayName, ""));
            to.Set(SignLinks.AutoTextKey, from.GetString(SignLinks.AutoTextKey, ""));
            RefreshWidget(to);
        }

        /// <summary>The loaded sign shows the new text at once instead of at its next two-second check.</summary>
        private static void RefreshWidget(ZDO sign)
        {
            ZNetView view = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(sign) : null;
            Sign component = view != null ? view.GetComponent<Sign>() : null;
            if (component != null)
                component.UpdateText();
        }
    }
}
