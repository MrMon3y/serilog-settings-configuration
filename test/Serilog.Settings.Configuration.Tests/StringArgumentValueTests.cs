﻿using System;
using Serilog.Formatting;
using Serilog.Formatting.Json;

using Serilog.Settings.Configuration.Tests.Support;

using Xunit;

namespace Serilog.Settings.Configuration.Tests
{
    public class StringArgumentValueTests
    {
        [Fact]
        public void StringValuesConvertToDefaultInstancesIfTargetIsInterface()
        {
            var stringArgumentValue = new StringArgumentValue(() => "Serilog.Formatting.Json.JsonFormatter, Serilog");

            var result = stringArgumentValue.ConvertTo(typeof(ITextFormatter));

            Assert.IsType<JsonFormatter>(result);
        }

        [Fact]
        public void StringValuesConvertToDefaultInstancesIfTargetIsAbstractClass()
        {
            var stringArgumentValue = new StringArgumentValue(() => "Serilog.Settings.Configuration.Tests.Support.ConcreteClass, Serilog.Settings.Configuration.Tests");

            var result = stringArgumentValue.ConvertTo(typeof(AbstractClass));

            Assert.IsType<ConcreteClass>(result);
        }

        [Theory]
        [InlineData("My.NameSpace.Class+InnerClass::Member",
                   "My.NameSpace.Class+InnerClass", "Member")]
        [InlineData("  TrimMe.NameSpace.Class::NeedsTrimming  ",
                   "TrimMe.NameSpace.Class", "NeedsTrimming")]
        [InlineData("My.NameSpace.Class::Member",
                   "My.NameSpace.Class", "Member")]
        [InlineData("My.NameSpace.Class::Member, MyAssembly",
                   "My.NameSpace.Class, MyAssembly", "Member")]
        [InlineData("My.NameSpace.Class::Member, MyAssembly, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                   "My.NameSpace.Class, MyAssembly, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "Member")]
        [InlineData("Just a random string with :: in it",
                   null, null)]
        [InlineData("Its::a::trapWithColonsAppearingTwice",
                   null, null)]
        [InlineData("ThereIsNoMemberHere::",
                   null, null)]
        [InlineData(null,
                   null, null)]
        [InlineData(" ",
                   null, null)]
        // a full-qualified type name should not be considered a static member accessor
        [InlineData("My.NameSpace.Class, MyAssembly, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
           null, null)]
        public void TryParseStaticMemberAccessorReturnsExpectedResults(string input, string expectedAccessorType, string expectedPropertyName)
        {
            var actual = StringArgumentValue.TryParseStaticMemberAccessor(input,
                out var actualAccessorType,
                out var actualMemberName);

            if (expectedAccessorType == null)
            {
                Assert.False(actual, $"Should not parse {input}");
            }
            else
            {
                Assert.True(actual, $"should successfully parse {input}");
                Assert.Equal(expectedAccessorType, actualAccessorType);
                Assert.Equal(expectedPropertyName, actualMemberName);
            }
        }

        [Theory]
        [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::InterfaceProperty, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
        [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::AbstractProperty, Serilog.Settings.Configuration.Tests", typeof(AnAbstractClass))]
        [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::InterfaceField, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
        [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::AbstractField, Serilog.Settings.Configuration.Tests", typeof(AnAbstractClass))]
        private void StaticMembersAccessorsCanBeUsedForReferenceTypes(string input, Type targetType)
        {
            var stringArgumentValue = new StringArgumentValue(() => $"{input}");

            var actual = stringArgumentValue.ConvertTo(targetType);

            Assert.IsAssignableFrom(targetType, actual);
            Assert.Equal(ConcreteImpl.Instance, actual);
        }

        [Theory]
        // unknown type
        [InlineData("Namespace.ThisIsNotAKnownType::InterfaceProperty, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
        // good type name, but wrong namespace
        [InlineData("Random.Namespace.ClassWithStaticAccessors::InterfaceProperty, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
        // good full type name, but missing or wrong assembly
        [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::InterfaceProperty", typeof(IAmAnInterface))]
        public void StaticAccessorOnUnknownTypeThrowsTypeLoadException(string input, Type targetType)
        {
            var stringArgumentValue = new StringArgumentValue(() => $"{input}");
            Assert.Throws<TypeLoadException>(() =>
                stringArgumentValue.ConvertTo(targetType)
            );
        }

        [Theory]
        // unknown member
        [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::UnknownMember, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
        // static property exists but it's private
        [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::PrivateInterfaceProperty, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
        // static field exists but it's private
        [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::PrivateInterfaceField, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
        // public property exists but it's not static
        [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::InstanceInterfaceProperty, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
        // public field exists but it's not static
        [InlineData("Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors::InstanceInterfaceField, Serilog.Settings.Configuration.Tests", typeof(IAmAnInterface))]
        public void StaticAccessorWithInvalidMemberThrowsInvalidOperationException(string input, Type targetType)
        {
            var stringArgumentValue = new StringArgumentValue(() => $"{input}");
            var exception = Assert.Throws<InvalidOperationException>(() =>
                stringArgumentValue.ConvertTo(targetType)
            );

            Assert.Contains("Could not find a public static property or field ", exception.Message);
            Assert.Contains("on type `Serilog.Settings.Configuration.Tests.Support.ClassWithStaticAccessors, Serilog.Settings.Configuration.Tests`", exception.Message);
        }
    }
}