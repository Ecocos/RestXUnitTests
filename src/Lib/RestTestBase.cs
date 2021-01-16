
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace RestXUnitTests.Lib
{

    /// <summary>
    /// (c) 2021 Erich Tinguely
    /// GNU General Public License v3
    /// 
    /// Base class for tests for common functionality
    /// </summary>
    public abstract class RestTestBase
    {
        protected static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    }
}
