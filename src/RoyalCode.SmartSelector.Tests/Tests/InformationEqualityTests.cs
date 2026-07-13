using Microsoft.CodeAnalysis;
using RoyalCode.Extensions.SourceGenerator.Descriptors;
using RoyalCode.Extensions.SourceGenerator.Descriptors.Snapshots;
using RoyalCode.SmartSelector.Generators.Generators;
using RoyalCode.SmartSelector.Generators.Models;

namespace RoyalCode.SmartSelector.Tests.Tests;

public class InformationEqualityTests
{
    [Fact]
    public void AutoProperties_success_results_with_the_same_content_should_be_equal()
    {
        var type = new TypeDescriptor("Details", ["Tests"], null);
        var left = new AutoPropertiesInformation(TypeSnapshot.Create(type), []);
        var right = new AutoPropertiesInformation(TypeSnapshot.Create(type), []);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void AutoSelect_diagnostic_results_with_the_same_content_should_be_equal()
    {
        var left = new AutoSelectInformation(Array.Empty<DiagnosticInfo>());
        var right = new AutoSelectInformation(Array.Empty<DiagnosticInfo>());

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void AutoDetails_success_results_with_the_same_content_should_be_equal()
    {
        var type = new TypeDescriptor("Details", ["Tests"], null);
        var properties = new AutoPropertiesInformation(TypeSnapshot.Create(type), []);
        var left = new AutoDetailsInformation("AddressDetails", properties);
        var right = new AutoDetailsInformation("AddressDetails", properties);

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }
}
