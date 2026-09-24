namespace Pleno.Core.Model
{
    /// <summary>Recursos de uma mesa depois dos modificadores (amuletos, roda inicial, chefe).</summary>
    public struct MesaResources
    {
        public int Spins;
        public int ChipsPerSpin;
        public int Nudges;

        public MesaResources(int spins, int chipsPerSpin, int nudges)
        {
            Spins = spins;
            ChipsPerSpin = chipsPerSpin;
            Nudges = nudges;
        }
    }
}
