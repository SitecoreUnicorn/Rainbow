using System;

namespace Rainbow.SourceControl
{
    public class SourceControlManager : ISourceControlManager
    {
        public IDisposable GetSourceControlManager(string filename)
        {
            //return Activator.CreateInstance(typeof(DefaultManager), filename) as IDisposable;
            return Activator.CreateInstance(typeof(TfsManager), filename) as IDisposable;
        }
    }
}