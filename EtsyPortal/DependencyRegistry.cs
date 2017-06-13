using EtsyServices;
using StructureMap;

namespace EtsyPortal
{
    public class DependencyRegistry : Registry
    {
        public DependencyRegistry()
        {
            For<IEtsyService>().Use<EtsyService>();
        }

    }
}