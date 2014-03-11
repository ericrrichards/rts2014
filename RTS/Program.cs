namespace RTS {
    using System.Reflection;

    using log4net;
    using log4net.Config;

    class Program {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public static void Main(string[] args) {
            XmlConfigurator.Configure();
            Log.Info("foo");

            var app = new App();
            app.Init(640, 480, true);
            app.Run();

            
        }
    }

    
}
