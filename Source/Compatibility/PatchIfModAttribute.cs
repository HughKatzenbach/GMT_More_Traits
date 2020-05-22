using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Garthor_More_Traits.Compatibility
{
    internal class PatchIfModAttribute : Attribute
    {
        string modPackageID;
        public PatchIfModAttribute(string modPackageID)
        {
            this.modPackageID = modPackageID;
        }

        public bool IsModLoaded()
        {
            bool b = Verse.ModLister.AllInstalledMods.Any(x => x.Active && x.SamePackageId(modPackageID));
            Log.Message($"{modPackageID} present: {b}");
            return ModLister.AllInstalledMods.Any(x => x.Active && x.SamePackageId(modPackageID));
        }
    }
}
