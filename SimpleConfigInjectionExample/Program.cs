using System;
using System.Configuration;
using Autofac;

namespace SimpleConfigInjectionExample
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
             * Yeah, this is a pretty contrived example, but imagine being able to:
             *      - Abstract away the app settings
             *      - Test the provider that extracts from app settings
             *      - Insert a layer that could provide default values
             *      - Encapsulate a lot of those helper functions.
             *      - Easily trace issues with configuration.
            */
            Console.WriteLine("Hard-coded example: ");
            var example1_noInjection = new MyAwesomeClass(new EmailSettings("sean@sean.com", 3), new FluxCapacitorSettings(88, 1.21));
            example1_noInjection.DoSomething();

            Console.WriteLine("Injection example: ");

            var containerBuilder = new ContainerBuilder();
            containerBuilder.Register(c => new HardCodedFluxCapacitorSettingsRetriever().GetSettings())
                .As<IFluxCapacitorSettings>();
            containerBuilder.Register(c => new AppSettingsBasedEmailSettingsRetriever(new AppSettingsWrapper()).GetSettings())
                .As<IEmailSettings>();
            containerBuilder.RegisterType<MyAwesomeClass>();

            var container = containerBuilder.Build();

            var example2_injection = container.Resolve<MyAwesomeClass>();
            example2_injection.DoSomething();
            Console.ReadLine();
        }
    }
    public class MyAwesomeClass
    {
        private readonly IEmailSettings _emailSettings;
        private readonly IFluxCapacitorSettings _fluxCapacitorSettings;
        public MyAwesomeClass(IEmailSettings emailSettings, IFluxCapacitorSettings fluxCapacitorSettings)
        {
            _emailSettings = emailSettings;
            _fluxCapacitorSettings = fluxCapacitorSettings;
        }

        public void DoSomething()
        {
            Console.WriteLine("the default e-mail is {0}", _emailSettings.DefaultEmailAddress);
            Console.WriteLine("I can retry an e-mail {0} times", _emailSettings.NumberOfRetries);
            Console.WriteLine("the flux capacitor requires {0} gigwatts of power", _fluxCapacitorSettings.RequiredGigawatts);
        }

    }

    public interface IFluxCapacitorSettings
    {
        int RequiredSpeedInMPH { get; }
        double RequiredGigawatts { get; }
    }

    public class FluxCapacitorSettings : IFluxCapacitorSettings
    {
        public int RequiredSpeedInMPH { get; }
        public double RequiredGigawatts { get; }

        public FluxCapacitorSettings(int requiredSpeedInMph, double requiredGigawatts)
        {
            RequiredSpeedInMPH = requiredSpeedInMph;
            RequiredGigawatts = requiredGigawatts;
        }
    }


    public class HardCodedFluxCapacitorSettingsRetriever
    {
        public FluxCapacitorSettings GetSettings()
        {
            // Everyone knows these values are constant, so just hard-code them.
            return new FluxCapacitorSettings(88, 1.21);
        }
    }

    public class AppSettingsBasedEmailSettingsRetriever
    {
        private readonly IAppSettings _appSettings;
        public AppSettingsBasedEmailSettingsRetriever(IAppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public EmailSettings GetSettings()
        {
            // Lots of this handling could eventually be extracted into helper methods like ValueOrThrow("defaultEmail"), 
            // or ValueOrDefault("numberRetries", 3), etc. etc.

            var defaultEmail = _appSettings.GetValue("defaultEmail");
            if (string.IsNullOrWhiteSpace(defaultEmail)) { throw new Exception("No default e-mail provided in configuration"); }

            var strNumberOfRetries = _appSettings.GetValue("numberRetries");
            int numberOfRetries;
            if (strNumberOfRetries == null || !int.TryParse(strNumberOfRetries, out numberOfRetries) || numberOfRetries < 0)
            {
                numberOfRetries = 10;
            }

            return new EmailSettings(defaultEmail, numberOfRetries);
        }
    }

    public interface IEmailSettings
    {
        string DefaultEmailAddress { get; }
        int NumberOfRetries { get; }
    }

    public class EmailSettings : IEmailSettings
    {
        public string DefaultEmailAddress { get; }

        public int NumberOfRetries { get; }

        public EmailSettings(string defaultEmailAddress, int numberOfRetries)
        {
            DefaultEmailAddress = defaultEmailAddress;
            NumberOfRetries = numberOfRetries;
        }
    }

    public interface IAppSettings
    {
        string GetValue(string settingName);
    }

    public class AppSettingsWrapper : IAppSettings
    {
        public string GetValue(string settingName)
        {
            return ConfigurationManager.AppSettings[settingName];
        }
    }

}