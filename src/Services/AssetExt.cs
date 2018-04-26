using System;
using Lykke.Service.Assets.Client.Custom;

namespace Services
{
    /// <summary>
    /// Extends IAsset.
    /// IMPORTANT: necessary to perform a revision of this class ufter updating dependency
    /// Lykke.Service.Assets.Client.
    /// </summary>
    public static class AssetExt
    {
        public static double Multiplier(this IAsset asset)
        {
            return Math.Pow(10, asset.MultiplierPower * -1);
        }
    }
}
