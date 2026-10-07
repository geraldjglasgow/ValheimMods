using System.Collections.Generic;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// The aging of one Aging Cask, on its container's object. Wakes only on world instances (the prefab copy sits under
    /// an inactive holder; a build ghost has no ZDO and does nothing). Every <see cref="Interval"/> seconds the cask's ZDO
    /// owner, and only the owner, ages its meads and wines (<see cref="CaskAging"/>), never while it is open (the opener
    /// takes ownership, so the owner's in-use flag is the truth) and never before the container loaded the ZDO's latest
    /// items. Records are written to the ZDO only when they change; raised stars are saved the game's way (the
    /// inventory's change event, which saves the container), so they replicate to everyone.
    /// </summary>
    public sealed class AgingCask : MonoBehaviour
    {
        public const float Interval = 5f;

        private Container container;
        private uint hoverRevision = uint.MaxValue;
        private float hoverAt = -1f;
        private string hoverLine = "";

        private void Awake() => InvokeRepeating(nameof(Tick), Random.Range(1f, Interval), Interval);

        private void Tick() => HookGuard.Run("aging cask", cask => cask.Age(), this);

        private void Age()
        {
            ZDO zdo = OwnedIdle();
            if (zdo == null)
                return;
            Inventory inventory = container.GetInventory();
            List<CaskRecord> old = CaskRecords.Read(zdo);
            double now = ZNet.instance.GetTimeSeconds();
            List<CaskRecord> records = CaskAging.Age(inventory, old, now, CaskAging.Period(EnvMan.instance), out bool raised);
            if (!raised && CaskRecords.Equal(old, records))
                return;
            CaskRecords.Write(zdo, records);
            if (raised)
                inventory.Changed();
            else
                container.m_lastRevision = zdo.DataRevision;
        }

        /// <summary>The cask's ZDO when this peer owns it, it is closed and its items are loaded; else null.</summary>
        private ZDO OwnedIdle()
        {
            if (ZNet.instance == null || EnvMan.instance == null || Container == null)
                return null;
            ZNetView view = container.m_nview;
            if (view == null || !view.IsValid() || !view.IsOwner() || container.IsInUse() || container.GetInventory() == null)
                return null;
            ZDO zdo = view.GetZDO();
            return zdo.DataRevision == container.m_lastRevision ? zdo : null;
        }

        /// <summary>The hover's aging line, worked out again when the ZDO changed or a second passed.</summary>
        public string HoverLine()
        {
            ZNetView view = Container != null ? container.m_nview : null;
            if (view == null || !view.IsValid() || ZNet.instance == null || EnvMan.instance == null)
                return CaskHover.Rule;
            ZDO zdo = view.GetZDO();
            if (zdo.DataRevision == hoverRevision && Time.time - hoverAt < 1f)
                return hoverLine;
            hoverRevision = zdo.DataRevision;
            hoverAt = Time.time;
            hoverLine = CaskHover.Line(CaskRecords.Read(zdo), ZNet.instance.GetTimeSeconds(), EnvMan.instance);
            return hoverLine;
        }

        private Container Container
        {
            get
            {
                if (container == null)
                    container = GetComponent<Container>();
                return container;
            }
        }
    }
}
