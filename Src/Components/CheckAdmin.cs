using System;
using System.Diagnostics;
using System.Security.Principal;

namespace PicoGAUpdate.Components
{
    internal class CheckAdmin
    {
        private static readonly WindowsIdentity Identity = WindowsIdentity.GetCurrent();
        private static readonly WindowsPrincipal Principal = new WindowsPrincipal(Identity ?? throw new InvalidOperationException());

        public static bool IsElevated
        {
            get
            {
                Debug.Assert(Principal != null, nameof(Principal) + " != null");
                return Principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}