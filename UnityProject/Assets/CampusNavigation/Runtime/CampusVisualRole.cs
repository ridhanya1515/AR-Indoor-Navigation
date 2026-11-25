using UnityEngine;
namespace CampusNavigation
{
    // Visibility changes only. Physics and navigation components stay intact.
    public sealed class CampusVisualRole : MonoBehaviour
    {
        public int floor=-1;
        public bool replaced;
        public bool cutawayExterior;
    }
}
