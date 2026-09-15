using OpenKeep.Core;

namespace OpenKeep.Shared
{
    /// <summary>The "$ok_shared_..." words of the module (SPEC 9.4 plus the few the spec left to the module).</summary>
    public static class SharedWords
    {
        public const string InUse = "$ok_shared_inuse";
        public const string Moving = "$ok_shared_moving";
        public const string NoAnswer = "$ok_shared_noanswer";
        public const string Denied = "$ok_shared_denied";
        public const string ReadOnly = "$ok_shared_readonly";
        public const string Someone = "$ok_shared_someone";
        public const string NoFit = "$ok_shared_nofit";
        public const string ChestFull = "$ok_shared_chestfull";
        public const string Unavailable = "$ok_shared_unavailable";

        public static void Register()
        {
            Language.Add("ok_shared_inuse", "in use by");
            Language.Add("ok_shared_moving", "is moving this");
            Language.Add("ok_shared_noanswer", "The chest did not answer");
            Language.Add("ok_shared_denied", "Someone else got there first");
            Language.Add("ok_shared_readonly", "Viewing only");
            Language.Add("ok_shared_someone", "another player");
            Language.Add("ok_shared_nofit", "Not enough room in your inventory");
            Language.Add("ok_shared_chestfull", "No room in the chest");
            Language.Add("ok_shared_unavailable", "The chest cannot be changed right now");
        }
    }
}
