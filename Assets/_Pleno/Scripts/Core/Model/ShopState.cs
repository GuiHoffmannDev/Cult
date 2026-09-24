using System.Collections.Generic;

namespace Pleno.Core.Model
{
    public sealed class ShopOffer
    {
        public string Id { get; set; }
        public int Price { get; set; }
        public bool Sold { get; set; }

        public ShopOffer()
        {
        }

        public ShopOffer(string id, int price)
        {
            Id = id;
            Price = price;
        }
    }

    /// <summary>Um pacote aberto esperando a escolha do jogador.</summary>
    public sealed class PackOpening
    {
        public string PackId { get; set; }

        /// <summary>Ids das opções (Tábuas, Cartas da Casa ou amuletos), todas diferentes.</summary>
        public List<string> Options { get; set; } = new List<string>();
    }

    public sealed class ShopState
    {
        public List<ShopOffer> Relics { get; set; } = new List<ShopOffer>();
        public List<ShopOffer> Consumables { get; set; } = new List<ShopOffer>();
        public ShopOffer Pack { get; set; }

        /// <summary>Rerolls feitos nesta loja; cada um encarece o próximo.</summary>
        public int Rerolls { get; set; }

        public PackOpening OpenPack { get; set; }
    }
}
