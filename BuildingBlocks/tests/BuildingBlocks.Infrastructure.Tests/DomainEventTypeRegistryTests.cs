using System.Reflection.Emit;
using System.Reflection;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Messaging.DomainEvents;
using BuildingBlocks.Infrastructure.Messaging;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class DomainEventTypeRegistryTests
{
    [Fact]
    public void ARegisteredEvent_ResolvesInBothDirections()
    {
        var registry = new DomainEventTypeRegistry([typeof(DomainEventTypeRegistryTests).Assembly]);

        Assert.Equal("flush-probe-started-v1", registry.NameOf(typeof(FlushProbeStarted)));
        Assert.Equal(typeof(FlushProbeStarted), registry.Resolve("flush-probe-started-v1"));
    }

    [Fact]
    public void ADomainEventWithoutAnEventName_FailsAtRegistrationNotAtTheFirstCommit()
    {
        var assembly = AssemblyWithDomainEvents(("UnnamedEvent", null));

        var exception = Assert.Throws<InvalidOperationException>(() => new DomainEventTypeRegistry([assembly]));

        Assert.Contains("EventName", exception.Message, StringComparison.Ordinal);
        Assert.Contains("UnnamedEvent", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TwoEventsSharingOneName_FailAtRegistration()
    {
        var assembly = AssemblyWithDomainEvents(("First", "shared-v1"), ("Second", "shared-v1"));

        var exception = Assert.Throws<InvalidOperationException>(() => new DomainEventTypeRegistry([assembly]));

        Assert.Contains("shared-v1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ScanningTheSameAssemblyTwice_IsIdempotent()
    {
        var assembly = typeof(DomainEventTypeRegistryTests).Assembly;

        var registry = new DomainEventTypeRegistry([assembly, assembly]);

        Assert.Equal(typeof(FlushProbeStarted), registry.Resolve("flush-probe-started-v1"));
    }

    [Fact]
    public void AnUnregisteredType_NamesTheRegistrationCallInsteadOfFailingObscurely()
    {
        var registry = new DomainEventTypeRegistry([]);

        var exception = Assert.Throws<InvalidOperationException>(() => registry.NameOf(typeof(FlushProbeStarted)));

        Assert.Contains("AddDomainEventsFrom", exception.Message, StringComparison.Ordinal);
    }

    private static Assembly AssemblyWithDomainEvents(params (string TypeName, string? EventName)[] events)
    {
        var assemblyName = new AssemblyName("GeneratedDomainEvents" + Guid.NewGuid().ToString("N")[..8]);
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule(assemblyName.Name!);
        var attributeConstructor = typeof(EventNameAttribute).GetConstructor([typeof(string)])!;

        foreach (var (typeName, eventName) in events)
        {
            var type = module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
            type.AddInterfaceImplementation(typeof(IDomainEvent));

            if (eventName is not null)
            {
                type.SetCustomAttribute(new CustomAttributeBuilder(attributeConstructor, [eventName]));
            }

            type.CreateType();
        }

        return assembly;
    }
}
