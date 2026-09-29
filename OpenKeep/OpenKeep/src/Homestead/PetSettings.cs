using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>The pet feeding keys of section "8. Homestead": they move items out of containers, so they are synced and lockable.</summary>
    public static class PetSettings
    {
        public const string Section = HomesteadModule.Section;

        public static ConfigEntry<bool> PetsEatFromChests { get; private set; }
        public static ConfigEntry<float> PetChestRange { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            PetsEatFromChests = synced.Bind(Section, "Pets Eat From Chests", true,
                "Hungry tamed animals (wolves, boars, lox, chickens, asksvin and any other tamed creature) walk to a container within Pet Chest Range that holds food they eat and eat one item from it, as they would from the ground. "
                + "Food lying on the ground near them still comes first. Animals still being tamed do not eat from containers. "
                + "The containers: rules of OpenKeep.Reach.yml apply, as do the switches of section 0 and the wards and chest access of the player whose game runs the animal.");
            PetChestRange = synced.Bind(Section, "Pet Chest Range", 10f,
                "Metres from a hungry tamed animal to the middle of a container it eats from.",
                acceptableValues: new AcceptableValueRange<float>(1f, 30f));
        }
    }
}
