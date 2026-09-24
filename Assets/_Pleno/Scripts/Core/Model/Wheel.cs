using System;
using System.Collections.Generic;

namespace Pleno.Core.Model
{
    /// <summary>
    /// A roda: lista ORDENADA de casas. A ordem é a física, e é ela que define vizinhos e empurrão.
    /// O índice 0 fica ao lado do último (circular).
    /// </summary>
    public sealed class Wheel
    {
        readonly List<Pocket> _pockets = new List<Pocket>();

        /// <summary>Próximo Id a atribuir. Nunca reaproveita Ids, nem de casas removidas.</summary>
        public int NextId { get; private set; }

        public Wheel()
        {
        }

        public Wheel(IEnumerable<Pocket> pockets)
        {
            foreach (Pocket pocket in pockets)
                Insert(_pockets.Count, pocket);
        }

        public int Count => _pockets.Count;
        public IReadOnlyList<Pocket> Pockets => _pockets;
        public Pocket this[int index] => _pockets[index];

        public int Wrap(int index)
        {
            int n = _pockets.Count;
            int r = index % n;
            return r < 0 ? r + n : r;
        }

        /// <summary>Índice a <paramref name="offset"/> casas de distância, dando a volta na roda.</summary>
        public int Neighbor(int index, int offset) => Wrap(index + offset);

        /// <summary>Menor número de casas entre duas posições, contando pelos dois sentidos.</summary>
        public int CircularDistance(int a, int b)
        {
            int d = Math.Abs(Wrap(a) - Wrap(b));
            return Math.Min(d, _pockets.Count - d);
        }

        /// <summary>Índice da casa com esse Id, ou -1 se ela não está mais na roda.</summary>
        public int IndexOfId(int id)
        {
            for (int i = 0; i < _pockets.Count; i++)
                if (_pockets[i].Id == id)
                    return i;
            return -1;
        }

        /// <summary>Insere a casa na posição <paramref name="index"/> e lhe dá um Id novo.</summary>
        public Pocket Insert(int index, Pocket pocket)
        {
            if (pocket == null)
                throw new ArgumentNullException(nameof(pocket));
            if (index < 0 || index > _pockets.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            pocket.Id = NextId++;
            _pockets.Insert(index, pocket);
            return pocket;
        }

        /// <summary>Clona a casa e põe a cópia logo depois dela, na ordem física.</summary>
        public Pocket CloneAdjacent(int index) => Insert(Wrap(index) + 1, _pockets[Wrap(index)].CopyContents());

        public bool CanRemove(int minimumPockets) => _pockets.Count > minimumPockets;

        /// <summary>Remove a casa, respeitando o mínimo de casas da roda.</summary>
        public Pocket RemoveAt(int index, int minimumPockets)
        {
            if (!CanRemove(minimumPockets))
                throw new InvalidOperationException($"A roda não pode ter menos de {minimumPockets} casas.");
            Pocket removed = _pockets[index];
            _pockets.RemoveAt(index);
            return removed;
        }

        /// <summary>Reconstrói uma roda salva, preservando os Ids (apostas de Vizinhos dependem deles).</summary>
        public static Wheel Restore(IEnumerable<Pocket> pocketsWithIds, int nextId)
        {
            var wheel = new Wheel();
            foreach (Pocket pocket in pocketsWithIds)
            {
                if (pocket.Id >= nextId)
                    throw new ArgumentException($"Casa com Id {pocket.Id} >= NextId {nextId}.", nameof(nextId));
                wheel._pockets.Add(pocket);
            }
            wheel.NextId = nextId;
            return wheel;
        }

        /// <summary>Cópia profunda, com os mesmos Ids. Serve para simular sem mexer na roda real.</summary>
        public Wheel Clone()
        {
            var copy = new Wheel();
            foreach (Pocket pocket in _pockets)
            {
                Pocket c = pocket.CopyContents();
                c.Id = pocket.Id;
                copy._pockets.Add(c);
            }
            copy.NextId = NextId;
            return copy;
        }
    }
}
