using Autofac;
using Grand.Core.Configuration;
using Grand.Core.Infrastructure;
using Grand.Core.Infrastructure.DependencyManagement;
using NopBrasil.Plugin.Payments.PagSeguro.Controllers;
using NopBrasil.Plugin.Payments.PagSeguro.Services;
using NopBrasil.Plugin.Payments.PagSeguro.Task;

namespace NopBrasil.Plugin.Payments.PagSeguro.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, GrandConfig GrandConfig)
        {
            builder.RegisterType<PaymentPagSeguroController>().AsSelf();
            builder.RegisterType<PagSeguroService>().As<IPagSeguroService>().InstancePerDependency();
            builder.RegisterType<CheckPaymentTask>().AsSelf().InstancePerDependency();
        }

        public int Order => 2;
    }
}
