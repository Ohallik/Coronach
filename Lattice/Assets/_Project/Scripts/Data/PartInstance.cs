using System;
using System.Collections.Generic;
namespace Lattice.Data
{
    [Serializable] public sealed class PartInstance
    {
        public string instanceId=Guid.NewGuid().ToString("N"),definitionId;
        public int upgrade;
        public List<string> affixes=new();
    }
}
