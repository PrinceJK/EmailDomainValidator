using Microsoft.Extensions.DependencyInjection;

namespace EmailDomainValidator
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IEmailDomainValidator"/> as a singleton using default options.
        /// </summary>
        public static IServiceCollection AddEmailDomainValidator(this IServiceCollection services)
            => services.AddEmailDomainValidator(new EmailValidatorOptions());

        /// <summary>
        /// Registers <see cref="IEmailDomainValidator"/> as a singleton with the supplied options.
        /// </summary>
        public static IServiceCollection AddEmailDomainValidator(
            this IServiceCollection services,
            EmailValidatorOptions options)
        {
            services.AddMemoryCache();
            services.AddHttpClient();
            services.AddSingleton(options);
            services.AddSingleton<IEmailDomainValidator>(sp =>
                new EmailDomainValidatorService(
                    options: sp.GetRequiredService<EmailValidatorOptions>(),
                    cache: sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                    httpClient: sp.GetRequiredService<IHttpClientFactory>().CreateClient()));
            return services;
        }

        /// <summary>
        /// Registers <see cref="IEmailDomainValidator"/> as a singleton, configuring options via a delegate.
        /// </summary>
        public static IServiceCollection AddEmailDomainValidator(
            this IServiceCollection services,
            Action<EmailValidatorOptions> configure)
        {
            var options = new EmailValidatorOptions();
            configure(options);
            return services.AddEmailDomainValidator(options);
        }
    }
}
