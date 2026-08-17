using GameDefs;

namespace Entities
{
    public class LimbInstance
    {
        public LimbDef def;
        public LimbInstance child;
        public bool attached = true;

        public LimbInstance(LimbDef def)
        {
            this.def = def;
        }
    }
}