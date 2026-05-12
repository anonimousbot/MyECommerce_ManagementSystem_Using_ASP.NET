namespace EMS.Models.Security
{
    public enum PostLoginDestinationKind
    {
        Login,
        LocalReturnUrl,
        AdminHome,
        CustomerHome
    }

    public sealed record PostLoginDestination(PostLoginDestinationKind Kind, string? ReturnUrl = null);

    public static class PostLoginNavigation
    {
        public static HashSet<string> BuildRoleSet(IEnumerable<string?> roles)
        {
            var normalizedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var roleName in roles)
            {
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    continue;
                }

                normalizedRoles.Add(CanonicalizeRole(roleName));
            }

            return normalizedRoles;
        }

        public static PostLoginDestination Decide(
            string? returnUrl,
            IEnumerable<string?> roles,
            Func<string, bool> isLocalUrl)
        {
            var roleNames = BuildRoleSet(roles);
            return Decide(returnUrl, roleNames, isLocalUrl);
        }

        public static PostLoginDestination Decide(
            string? returnUrl,
            HashSet<string> roleNames,
            Func<string, bool> isLocalUrl)
        {
            if (roleNames.Contains("Admin"))
            {
                return new PostLoginDestination(PostLoginDestinationKind.AdminHome);
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && CanAccessReturnUrl(returnUrl, roleNames, isLocalUrl))
            {
                return new PostLoginDestination(PostLoginDestinationKind.LocalReturnUrl, returnUrl);
            }

            if (roleNames.Contains("Customer"))
            {
                return new PostLoginDestination(PostLoginDestinationKind.CustomerHome);
            }

            return new PostLoginDestination(PostLoginDestinationKind.Login);
        }

        public static bool CanAccessReturnUrl(
            string returnUrl,
            HashSet<string> roleNames,
            Func<string, bool> isLocalUrl)
        {
            if (!isLocalUrl(returnUrl))
            {
                return false;
            }

            if (returnUrl.Equals("/Customer", StringComparison.OrdinalIgnoreCase))
            {
                return roleNames.Contains("Customer");
            }

            var customerAllowedPrefixes = new[]
            {
                "/Customer/Index",
                "/Customer/CustomerProfile",
                "/Customer/Update",
                "/Customer/Delete",
                "/Cart",
                "/Payment",
                "/Order/CreateOrder",
                "/Order/GetOrdersByCustomerId"
            };

            if (PathMatchesAnyPrefix(returnUrl, customerAllowedPrefixes))
            {
                return roleNames.Contains("Customer");
            }

            var adminAllowedPrefixes = new[]
            {
                "/Admin",
                "/Item",
                "/Order",
                "/Customer/GetAllCustomer",
                "/Customer/GetCustomerById"
            };

            if (PathMatchesAnyPrefix(returnUrl, adminAllowedPrefixes))
            {
                return roleNames.Contains("Admin");
            }

            return true;
        }

        private static bool PathMatchesAnyPrefix(string path, IEnumerable<string> prefixes)
        {
            return prefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static string CanonicalizeRole(string roleName)
        {
            if (roleName.Equals("customer", StringComparison.OrdinalIgnoreCase))
            {
                return "Customer";
            }

            if (roleName.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return "Admin";
            }

            return roleName.Trim();
        }
    }
}
