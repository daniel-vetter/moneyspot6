using JetBrains.Annotations;
using System.Reflection;

namespace MoneySpot6.WebApp;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddServiceFromAttributes(this IServiceCollection serviceCollection)
    {
        foreach (var type in typeof(Program).Assembly.GetTypes())
        {
            foreach (var attribute in type.GetCustomAttributes())
            {
                if (attribute is SingletonServiceAttribute singletonServiceAttribute)
                {
                    if (singletonServiceAttribute.ServiceType == null)
                        serviceCollection.AddSingleton(type);
                    else
                        serviceCollection.AddSingleton(singletonServiceAttribute.ServiceType, type);
                }

                if (attribute is ScopedServiceAttribute scopedServiceAttribute)
                {
                    if (scopedServiceAttribute.ServiceType == null)
                        serviceCollection.AddScoped(type);
                    else
                        serviceCollection.AddScoped(scopedServiceAttribute.ServiceType, type);
                }
            }
        }

        return serviceCollection;
    }
}

[MeansImplicitUse﻿]
[AttributeUsage(AttributeTargets.Class)]
public class SingletonServiceAttribute<TServiceType> : SingletonServiceAttribute
{
    public SingletonServiceAttribute() : base(typeof(TServiceType)) { }
}

[MeansImplicitUse﻿]
[AttributeUsage(AttributeTargets.Class)]
public class SingletonServiceAttribute : Attribute
{
    public Type? ServiceType { get; }

    public SingletonServiceAttribute(Type? serviceType = null)
    {
        ServiceType = serviceType;
    }
}

[MeansImplicitUse﻿]
[AttributeUsage(AttributeTargets.Class)]
public class ScopedServiceAttribute<TServiceType> : ScopedServiceAttribute
{
    public ScopedServiceAttribute() : base(typeof(TServiceType)) { }
}

[MeansImplicitUse﻿]
[AttributeUsage(AttributeTargets.Class)]
public class ScopedServiceAttribute : Attribute
{
    public Type? ServiceType { get; }

    public ScopedServiceAttribute(Type? serviceType = null)
    {
        ServiceType = serviceType;
    }
}