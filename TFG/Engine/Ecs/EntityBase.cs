

namespace Engine.Ecs
{
    public class EntityBase
    {
        public const int NullId = -1;

        public int Id { get; internal set; }
        public ulong CmpFlags { get; internal set; }
        public bool IsValid { get { return Id != NullId; } }

        public EntityBase()
        {
            Id = NullId;
            CmpFlags = 0;
        }

        public bool HasCmp<TCmp>()
        {
            return (CmpFlags & CmpMetadataGenerator<TCmp>.Flag) != 0;
        }

        internal void AddCmpFlag<TCmp>()
        {
            CmpFlags |= CmpMetadataGenerator<TCmp>.Flag;
        }

        internal void RemoveCmpFlag<TCmp>()
        {
            CmpFlags &= (~CmpMetadataGenerator<TCmp>.Flag);
        }
    }
}
