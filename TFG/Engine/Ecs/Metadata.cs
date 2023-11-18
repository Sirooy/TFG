using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Ecs
{
    internal class CmpMetadata
    {
        internal static int CurrentId = 0;
        internal static ulong CurrentFlag = 0x1;
    }

    internal class CmpMetadataGenerator<T>
    {
        public static int Id;
        public static ulong Flag;

        static CmpMetadataGenerator()
        {
            Id = CmpMetadata.CurrentId;
            Flag = CmpMetadata.CurrentFlag;

            CmpMetadata.CurrentId++;
            CmpMetadata.CurrentFlag <<= 1;
        }
    }
    
    internal class IdMetadata<TBase>
    {
        public static int CurrentId = 0;
    }
    
    internal class IdMetadataGenerator<TBase, T> where T : TBase
    {
        public static int Id;

        static IdMetadataGenerator()
        {
            Id = IdMetadata<TBase>.CurrentId++;
        }
    }
}
