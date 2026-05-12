using EMS.Models.DTOs;
using EMS.Models.Security;

var failures = new List<string>();

AssertEqual(
    expected: (PaginationHelper.DefaultPageNumber, PaginationHelper.MaxPageSize),
    actual: PaginationHelper.Normalize(0, 500),
    testName: "PaginationHelper caps oversized page requests",
    failures: failures);

AssertEqual(
    expected: (PaginationHelper.DefaultPageNumber, PaginationHelper.DefaultPageSize),
    actual: PaginationHelper.Normalize(-3, 0),
    testName: "PaginationHelper normalizes negative values",
    failures: failures);

var adminDestination = PostLoginNavigation.Decide(
    returnUrl: "/Customer/Index",
    roles: new[] { "Customer", "Admin" },
    isLocalUrl: static path => path.StartsWith('/'));

AssertEqual(
    expected: PostLoginDestinationKind.AdminHome,
    actual: adminDestination.Kind,
    testName: "Admin login takes precedence over customer return URLs",
    failures: failures);

var customerReturnDestination = PostLoginNavigation.Decide(
    returnUrl: "/Cart",
    roles: new[] { "Customer" },
    isLocalUrl: static path => path.StartsWith('/'));

AssertEqual(
    expected: PostLoginDestinationKind.LocalReturnUrl,
    actual: customerReturnDestination.Kind,
    testName: "Customer can return to customer-safe local routes",
    failures: failures);

var customerBlockedFromAdminRoute = PostLoginNavigation.Decide(
    returnUrl: "/Item/Index",
    roles: new[] { "Customer" },
    isLocalUrl: static path => path.StartsWith('/'));

AssertEqual(
    expected: PostLoginDestinationKind.CustomerHome,
    actual: customerBlockedFromAdminRoute.Kind,
    testName: "Customer cannot be redirected into admin routes",
    failures: failures);

if (failures.Count > 0)
{
    Console.Error.WriteLine("Test failures:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    return 1;
}

Console.WriteLine("EMS.Tests passed.");
return 0;

static void AssertEqual<T>(T expected, T actual, string testName, List<string> failures)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        failures.Add($"{testName}: expected '{expected}' but got '{actual}'.");
    }
}
