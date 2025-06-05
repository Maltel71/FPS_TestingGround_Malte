using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Used for client-side authoritative transforms
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        /// <summary>
        /// Used to determine who can write to this transform
        /// Server authoritative: false
        /// Client authoritative: true
        /// </summary>
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}