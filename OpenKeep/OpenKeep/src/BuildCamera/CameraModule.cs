using OpenKeep.Core;
using SyncedConfig;

namespace OpenKeep.BuildCamera
{
    /// <summary>
    /// Entry point of section 11, the build camera. With a build tool in hand (hammer, hoe, cultivator: anything that
    /// puts the player in the game's place mode) inside a crafting station's camera area, Toggle Key detaches the
    /// camera from the player (<see cref="CameraToggle"/>); it flies inside the stations' areas (<see cref="CameraArea"/>,
    /// <see cref="CameraMotion"/>), never through the ground (<see cref="CameraCollision"/>), and the game's own build,
    /// remove and repair work from it (<see cref="CameraReach"/>). Items lying by the camera can come into the
    /// inventory (<see cref="CameraPickup"/>), a light worn on the head can shine from it (<see cref="CircletLight"/>).
    /// Everything runs on the player's own client; pieces are placed and removed through the game's own paths.
    /// </summary>
    public static class CameraModule
    {
        public const string Section = "11. Build Camera";

        public static string NoStation { get; private set; }
        public static string EntryNeeds { get; private set; }
        public static string PickupNeeds { get; private set; }
        public static string Resting { get; private set; }
        public static string Comfort { get; private set; }

        public static void Initialize(SyncedConfiguration synced)
        {
            CameraSettings.Bind(synced);
            CameraPrefs.Bind(synced);
            NoStation = Language.Add("ok_cam_nostation", "The build camera works only near a crafting station");
            EntryNeeds = Language.Add("ok_cam_needs", "The build camera needs");
            PickupNeeds = Language.Add("ok_cam_pickupneeds", "Camera pickup needs");
            Resting = Language.Add("ok_cam_resting", "Resting");
            Comfort = Language.Add("ok_cam_comfort", "comfort {0} (you have {1})");
        }
    }
}
