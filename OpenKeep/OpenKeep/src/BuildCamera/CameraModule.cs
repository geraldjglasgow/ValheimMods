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

        // The keys of the camera's words; their English is in translations/English.blueprints.yml.
        public const string NoStation = "ok_cam_nostation";
        public const string EntryNeeds = "ok_cam_needs";
        public const string PickupNeeds = "ok_cam_pickupneeds";
        public const string Resting = "ok_cam_resting";
        public const string Comfort = "ok_cam_comfort";

        public static void Initialize(SyncedConfiguration synced)
        {
            CameraSettings.Bind(synced);
            CameraPrefs.Bind(synced);
        }
    }
}
