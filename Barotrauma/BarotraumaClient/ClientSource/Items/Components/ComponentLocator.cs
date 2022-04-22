using Barotrauma.Items.Components;
using System.Linq;

namespace Barotrauma.ClientSource.Items.Components
{
    internal static class ComponentLocator
    {
        /// <summary>
        /// Retrieves the turret component that is connected to the passed periscope. If no periscope
        /// is passed, null is returned.
        /// </summary>
        /// <returns></returns>
        internal static Turret GetTurret(Item periscope)
        {
            Connection outgoingConnection = periscope?.Connections?.FirstOrDefault(c => c.Name == "position_out");

            if(outgoingConnection == null)
            {
                return null;
            }

            Item turretItem = outgoingConnection.Recipients.Select(r => r.Item).FirstOrDefault();

            Turret turret = turretItem?.Components.OfType<Turret>().FirstOrDefault();

            return turret;
        }
    }
}
