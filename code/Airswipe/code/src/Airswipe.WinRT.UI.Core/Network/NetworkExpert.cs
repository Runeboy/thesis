using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;

namespace Airswipe.WinRT.Core.Network
{
    public class NetworkExpert
    {
        #region Methods

        public static IEnumerable<string> GetCurrentIpAddresses()
        {
            var profiles = new[] { NetworkInformation.GetInternetConnectionProfile() }.Union(NetworkInformation.GetConnectionProfiles()).ToList();
     
            IEnumerable<HostName> hostnames = NetworkInformation.GetHostNames().Where(h =>
                h.IPInformation != null &&
                h.IPInformation.NetworkAdapter != null &&
                h.Type == HostNameType.Ipv4
                ).ToList();

            return (from h in hostnames
                    from p in profiles
                    where h.IPInformation.NetworkAdapter.NetworkAdapterId ==
                          p.NetworkAdapter.NetworkAdapterId
                    select h.CanonicalName //string.Format("{0}, {1}", p.ProfileName, h.CanonicalName)
                    ).Distinct().ToList();
        }

        #endregion
    }
}
